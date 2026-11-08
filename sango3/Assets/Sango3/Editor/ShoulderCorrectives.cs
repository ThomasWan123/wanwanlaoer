using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace Sango3.Editor
{
    // Bakes the shoulder corrective blend shapes used by ShoulderCorrective (see there). For each playable hero and each
    // arm, the arm is posed at six directions from the bind pose (down, forward-down, forward, overhead, across the body,
    // back), with the clavicle sharing the lift the way the move baker does. For every vertex in the shoulder region the
    // dual-quaternion skinned position is computed and the difference to the linear-skinned position is taken back
    // through the vertex's blended skin matrix into bind space, so the blend shape, applied before skinning, brings
    // the linear result to the dual-quaternion one at that pose.
    public static class ShoulderCorrectives
    {
        const int Chest=2,RUpper=5,RFore=6,LUpper=8,LFore=9,RClavicle=18,LClavicle=19;
        static readonly (string name,Vector3 right)[] Directions={
            ("Down",new Vector3(-.12f,-1,.05f)),("ForwardDown",new Vector3(-.15f,-.65f,.75f)),("Forward",new Vector3(-.18f,.05f,1)),
            ("Up",new Vector3(-.15f,1,.15f)),("Across",new Vector3(.65f,-.25f,.72f)),("Back",new Vector3(-.35f,-.55f,-.75f))};
        public static void BuildAll()
        {
            var report=new StringBuilder();
            foreach(var id in BattleRules.Portraits)report.AppendLine(Build(id));
            AssetDatabase.SaveAssets();File.WriteAllText("QA/sample-130/shoulder-correctives.txt",report.ToString());Debug.Log("SANGO3_CORRECTIVES\n"+report);
        }
        public static string Build(string id)
        {
            string prefabPath="Assets/Sango3/Resources/Heroes/Final/"+id+".prefab";
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);root.transform.localScale=Vector3.one;
            try{
                var skin=root.GetComponent<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;var bones=skin.bones;var bind=mesh.bindposes;
                bool clavicles=bones.Length>LClavicle&&bones[RClavicle].name.Contains("Clavicle");var drape=root.GetComponent<RecoveredSleeveDrape>();
                // Keep any blend shapes that are not ours.
                var keep=new List<(string name,Vector3[] d,Vector3[] n,Vector3[] t,float w)>();
                for(int s=0;s<mesh.blendShapeCount;s++){var name=mesh.GetBlendShapeName(s);if(name.StartsWith("Shoulder"))continue;for(int f=0;f<mesh.GetBlendShapeFrameCount(s);f++){var d=new Vector3[mesh.vertexCount];var n=new Vector3[mesh.vertexCount];var t=new Vector3[mesh.vertexCount];mesh.GetBlendShapeFrameVertices(s,f,d,n,t);keep.Add((name,d,n,t,mesh.GetBlendShapeFrameWeight(s,f)));}}
                mesh.ClearBlendShapes();foreach(var k in keep)mesh.AddBlendShapeFrame(k.name,k.w,k.d,k.n,k.t);
                var vertices=mesh.vertices;var weights=mesh.boneWeights;
                // Welded vertex neighbours, for smoothing each corrective across the surface.
                var weld=new Dictionary<Vector3Int,int>();var map=new int[vertices.Length];for(int i=0;i<vertices.Length;i++){var key=Vector3Int.RoundToInt(vertices[i]*10000);if(!weld.TryGetValue(key,out int g)){g=weld.Count;weld.Add(key,g);}map[i]=g;}
                var adjacency=Enumerable.Range(0,weld.Count).Select(_=>new HashSet<int>()).ToArray();var tris=mesh.triangles;for(int i=0;i<tris.Length;i+=3)for(int e=0;e<3;e++){int a=map[tris[i+e]],b=map[tris[i+(e+1)%3]];if(a!=b){adjacency[a].Add(b);adjacency[b].Add(a);}}var order=Enumerable.Range(0,bones.Length).OrderBy(i=>Depth(bones[i])).ToArray();
                void Reset(){foreach(int i in order){var m=bind[i].inverse;bones[i].SetPositionAndRotation(m.GetColumn(3),m.rotation);}}
                Reset();var chest=bones[Chest];var rest=new Vector3[2];var shoulder=new Vector3[2];
                for(int side=0;side<2;side++){int up=side==0?RUpper:LUpper,lo=side==0?RFore:LFore;rest[side]=chest.InverseTransformDirection((bones[lo].position-bones[up].position).normalized);shoulder[side]=bones[up].position;}
                var shapes=new List<ShoulderCorrective.Shape>();var log=new StringBuilder(id+":");
                for(int side=0;side<2;side++)foreach(var (name,right) in Directions){
                    var d=right.normalized;if(side==1)d.x=-d.x;Reset();int up=side==0?RUpper:LUpper,lo=side==0?RFore:LFore;
                    if(clavicles){float lift=Mathf.Max(0,Vector3.Angle(Vector3.down,d)-90);var c=bones[side==0?RClavicle:LClavicle];c.localRotation=c.localRotation*Quaternion.Euler(0,0,(side==0?-1:1)*Mathf.Min(lift*.4f,26));}
                    var cur=(bones[lo].position-bones[up].position).normalized;bones[up].rotation=Quaternion.FromToRotation(cur,d)*bones[up].rotation;if(drape)drape.Solve();var helperRig=root.GetComponent<ShoulderHelper>();if(helperRig)helperRig.Solve();
                    var S=new Matrix4x4[bones.Length];var R=new Quaternion[bones.Length];var D=new Quaternion[bones.Length];
                    for(int b=0;b<bones.Length;b++){S[b]=bones[b].localToWorldMatrix*bind[b];R[b]=S[b].rotation;var t=(Vector3)S[b].GetColumn(3);D[b]=Scale(new Quaternion(t.x,t.y,t.z,0)*R[b],.5f);}
                    var delta=new Vector3[vertices.Length];int moved=0;float largest=0;
                    for(int v=0;v<vertices.Length;v++){
                        float reach=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.40f,.75f,Vector3.Distance(vertices[v],shoulder[side])));if(reach<=0)continue;
                        var w=weights[v];var idx=new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};var wt=new[]{w.weight0,w.weight1,w.weight2,w.weight3};
                        if(wt.Count(x=>x>.001f)<2)continue;
                        var M=new Matrix4x4();Vector3 linear=Vector3.zero;Quaternion rs=new Quaternion(0,0,0,0),ds=new Quaternion(0,0,0,0);var first=R[idx[0]];
                        for(int k=0;k<4;k++){if(wt[k]<=0)continue;var m=S[idx[k]];for(int e=0;e<16;e++)M[e]+=m[e]*wt[k];linear+=m.MultiplyPoint3x4(vertices[v])*wt[k];
                            float sign=Quaternion.Dot(first,R[idx[k]])<0?-1:1;rs=Add(rs,Scale(R[idx[k]],sign*wt[k]));ds=Add(ds,Scale(D[idx[k]],sign*wt[k]));}
                        float norm=Mathf.Sqrt(rs.x*rs.x+rs.y*rs.y+rs.z*rs.z+rs.w*rs.w);if(norm<1e-6f)continue;var rq=Scale(rs,1/norm);var dq=Scale(ds,1/norm);
                        var tq=dq*new Quaternion(-rq.x,-rq.y,-rq.z,rq.w);var dual=rq*vertices[v]+new Vector3(tq.x,tq.y,tq.z)*2;
                        var change=M.inverse.MultiplyVector(dual-linear)*reach;float size=change.magnitude;if(size>1e-6f)change*=.35f*(1-Mathf.Exp(-size/.35f))/size; // soft limit, no hard cap
                        if(float.IsNaN(change.x+change.y+change.z))continue;delta[v]=change;if(change.sqrMagnitude>1e-8f)moved++;largest=Mathf.Max(largest,change.magnitude);}
                    SmoothDelta(delta,adjacency,map,8);largest=delta.Max(x=>x.magnitude);
                    string shapeName="Shoulder"+(side==0?"R":"L")+name;mesh.AddBlendShapeFrame(shapeName,100,delta,null,null);
                    shapes.Add(new ShoulderCorrective.Shape{index=mesh.GetBlendShapeIndex(shapeName),side=side,direction=chest.InverseTransformDirection(d)});
                    log.Append(" "+shapeName+" "+moved+"v max "+largest.ToString("0.000"));}
                Reset();EditorUtility.SetDirty(mesh);
                var contents=PrefabUtility.LoadPrefabContents(prefabPath);
                try{var skinned=contents.GetComponent<SkinnedMeshRenderer>();var bs=skinned.bones;var corrective=contents.GetComponent<ShoulderCorrective>()??contents.AddComponent<ShoulderCorrective>();
                    corrective.chest=bs[Chest];corrective.upper=new[]{bs[RUpper],bs[LUpper]};corrective.lower=new[]{bs[RFore],bs[LFore]};corrective.rest=rest;corrective.shapes=shapes.ToArray();
                    PrefabUtility.SaveAsPrefabAsset(contents,prefabPath);}finally{PrefabUtility.UnloadPrefabContents(contents);}
                return log.ToString();
            }finally{Object.DestroyImmediate(root);}
        }
        // Laplacian smoothing of a corrective over the welded surface, so it adds no creases of its own.
        static void SmoothDelta(Vector3[] delta,HashSet<int>[] adjacency,int[] map,int passes)
        {
            int count=adjacency.Length;var g=new Vector3[count];var n=new int[count];for(int v=0;v<delta.Length;v++){g[map[v]]+=delta[v];n[map[v]]++;}for(int i=0;i<count;i++)if(n[i]>0)g[i]/=n[i];
            for(int pass=0;pass<passes;pass++){var next=new Vector3[count];for(int i=0;i<count;i++){if(adjacency[i].Count==0){next[i]=g[i];continue;}var sum=Vector3.zero;foreach(int j in adjacency[i])sum+=g[j];next[i]=g[i]*.5f+sum/adjacency[i].Count*.5f;}g=next;}
            for(int v=0;v<delta.Length;v++)delta[v]=g[map[v]];
        }
        static int Depth(Transform t){int d=0;while(t.parent){d++;t=t.parent;}return d;}
        static Quaternion Scale(Quaternion q,float s)=>new Quaternion(q.x*s,q.y*s,q.z*s,q.w*s);
        static Quaternion Add(Quaternion a,Quaternion b)=>new Quaternion(a.x+b.x,a.y+b.y,a.z+b.z,a.w+b.w);
    }
}
