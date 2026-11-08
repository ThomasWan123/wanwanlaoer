using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Sango3.Editor
{
    public static class HighHandRecovery
    {
        struct Vertex {public Vector3 p,n;public Vector2 uv;public static Vertex Lerp(Vertex a,Vertex b,float t)=>new Vertex{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t)};}
        public static void Merge(Mesh body,Vector3 wrist,Vector3 direction){
            const string path="Assets/Sango3/Characters/Studies/guanyu-high-hands.obj";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);if(!importer)throw new Exception("Missing preserved high resolution hand source");if(!importer.isReadable){importer.isReadable=true;importer.SaveAndReimport();}
            var source=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            try{
                var filter=source.GetComponentInChildren<MeshFilter>();var high=filter.sharedMesh;
                var vertices=new List<Vector3>();var normals=new List<Vector3>();var uvs=new List<Vector2>();var output=new List<int>();
                var index=new Dictionary<(Vector3Int,Vector3Int,Vector2Int),int>();
                int Add(Vertex v){var key=(Vector3Int.RoundToInt(v.p*100000),Vector3Int.RoundToInt(v.n*10000),Vector2Int.RoundToInt(v.uv*1000000));if(index.TryGetValue(key,out int id))return id;id=vertices.Count;index.Add(key,id);vertices.Add(v.p);normals.Add(v.n);uvs.Add(v.uv);return id;}
                void Append(Mesh mesh,bool hand){var positions=mesh.vertices;var ns=mesh.normals;var uv=mesh.uv;var tri=mesh.triangles;
                    for(int i=0;i<tri.Length;i+=3){var poly=new List<Vertex>();for(int k=0;k<3;k++){int n=tri[i+k];poly.Add(new Vertex{p=hand?filter.transform.TransformPoint(positions[n]):positions[n],n=hand?filter.transform.TransformDirection(ns[n]).normalized:ns[n],uv=uv[n]});}
                        var center=(poly[0].p+poly[1].p+poly[2].p)/3;float side=center.x<0?-1:1;var joint=new Vector3(side*wrist.x,wrist.y,wrist.z);var axis=new Vector3(side*direction.x,direction.y,direction.z);var q=center-joint;var radial=q-axis*Vector3.Dot(q,axis);
                        bool region=Mathf.Abs(center.x)>.90f&&center.y>1.70f&&center.y<2.85f&&radial.magnitude<.36f;
                        if(hand&&!region)continue;
                        if(region){
                            // The high source extends slightly under the existing cuff; no visible hard seam.
                            float cut=hand?-.12f:-.10f;float Distance(Vertex v)=>(Vector3.Dot(v.p-joint,axis)-cut)*(hand?1:-1);
                            var clipped=new List<Vertex>();for(int k=0;k<poly.Count;k++){var a=poly[k];var b=poly[(k+1)%poly.Count];float da=Distance(a),db=Distance(b);if(da>=0)clipped.Add(a);if((da>=0)!=(db>=0))clipped.Add(Vertex.Lerp(a,b,da/(da-db)));}poly=clipped;
                        }
                        for(int k=1;k+1<poly.Count;k++){output.Add(Add(poly[0]));output.Add(Add(poly[k]));output.Add(Add(poly[k+1]));}
                    }
                }
                Append(body,false);Append(high,true);if(vertices.Count<1000||output.Count<3000)throw new Exception("Hand merge produced invalid mesh");body.Clear();body.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;body.SetVertices(vertices);body.SetNormals(normals);body.SetUVs(0,uvs);body.SetTriangles(output,0);body.RecalculateBounds();body.RecalculateTangents();
            }finally{UnityEngine.Object.DestroyImmediate(source);}
        }
    }
}
