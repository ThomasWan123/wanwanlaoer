using System.Collections.Generic;
using UnityEngine;
namespace Sango3.Editor
{
    // Conforming edge splits preserve UVs and source normals before gripping deformation.
    public static class HandTopology
    {
        public static void Refine(Mesh mesh,Vector3 wrist,Vector3 direction){
            for(int pass=0;pass<4;pass++){
                var vertices=new List<Vector3>(mesh.vertices);var normals=new List<Vector3>(mesh.normals);var uv=new List<Vector2>(mesh.uv);var triangles=mesh.triangles;
                var middle=new Dictionary<ulong,int>();
                ulong Key(int a,int b)=>(ulong)(uint)Mathf.Min(a,b)<<32|(uint)Mathf.Max(a,b);
                void Mark(int a,int b){var p=(vertices[a]+vertices[b])*.5f;var q=new Vector3(Mathf.Abs(p.x),p.y,p.z)-wrist;float along=Vector3.Dot(q,direction);
                    if(along<-.03f||along>.62f||(q-direction*along).magnitude>.28f||(vertices[a]-vertices[b]).sqrMagnitude<.035f*.035f)return;
                    var key=Key(a,b);if(middle.ContainsKey(key))return;middle[key]=vertices.Count;vertices.Add(p);normals.Add((normals[a]+normals[b]).normalized);uv.Add((uv[a]+uv[b])*.5f);
                }
                for(int i=0;i<triangles.Length;i+=3){Mark(triangles[i],triangles[i+1]);Mark(triangles[i+1],triangles[i+2]);Mark(triangles[i+2],triangles[i]);}
                if(middle.Count==0)break;var output=new List<int>();void Tri(int a,int b,int c){output.Add(a);output.Add(b);output.Add(c);}
                for(int i=0;i<triangles.Length;i+=3){int a=triangles[i],b=triangles[i+1],c=triangles[i+2];bool ab=middle.TryGetValue(Key(a,b),out int d),bc=middle.TryGetValue(Key(b,c),out int e),ca=middle.TryGetValue(Key(c,a),out int f);
                    int mask=(ab?1:0)|(bc?2:0)|(ca?4:0);
                    switch(mask){
                        case 0:Tri(a,b,c);break;
                        case 1:Tri(a,d,c);Tri(d,b,c);break;
                        case 2:Tri(a,b,e);Tri(a,e,c);break;
                        case 4:Tri(a,b,f);Tri(b,c,f);break;
                        case 3:Tri(d,b,e);Tri(a,d,c);Tri(d,e,c);break;
                        case 5:Tri(a,d,f);Tri(d,b,c);Tri(d,c,f);break;
                        case 6:Tri(f,e,c);Tri(a,b,f);Tri(b,e,f);break;
                        case 7:Tri(a,d,f);Tri(d,b,e);Tri(f,e,c);Tri(d,e,f);break;
                    }
                }
                mesh.Clear();mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(output,0);
            }
            mesh.RecalculateBounds();mesh.RecalculateTangents();
        }
    }
}
