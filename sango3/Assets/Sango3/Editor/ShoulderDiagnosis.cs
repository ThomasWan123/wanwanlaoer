using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace Sango3.Editor
{
    // Diagnosis of the limited shoulder region at Guan Yu's raise (0.24, after the 0.23 review): for each vertex, the
    // singular values of its blended linear-skinning matrix (how much the skinning shrinks it in its worst and best
    // directions), the two bones it blends most and the rotation angle between them, and where it lies. A blend shape
    // acts through that matrix, so a vertex whose smallest singular value is s can be moved by d in that direction only
    // with a shape of size d/s. This is reported by bands of s, by bone pair, and by location; no clamp is involved.
    public static partial class GuanyuSample
    {
        static readonly (string clip,float time,string label)[] DiagnosisPoses={("Ultimate",.50f,"raise 0.50"),("Ultimate",.64f,"raise 0.64"),("Attack2",.36f,"sweep 0.36"),("Idle",0f,"idle")};
        public static void DiagnoseShoulders()
        {
            var report=new StringBuilder();foreach(var id in new[]{"guanyu","liubei"}){heroId=id;report.Append(DiagnoseFor(id));}
            heroId="guanyu";Directory.CreateDirectory(TrialRoot);File.WriteAllText(TrialRoot+"diagnosis.txt",report.ToString());Debug.Log("SANGO3_DIAGNOSIS\n"+report);
        }
        public static string DiagnoseFor(string id)
        {
            heroId=id;Load();var sb=new StringBuilder("== "+id+"\n");
            try{
                var skin=root.GetComponent<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;var rest=mesh.vertices;var weights=mesh.boneWeights;var bind=mesh.bindposes;var bs=skin.bones;
                var shoulder=new[]{(Vector3)bind[5].inverse.GetColumn(3),(Vector3)bind[8].inverse.GetColumn(3)};
                foreach(var (clip,time,label) in DiagnosisPoses){
                    var c=root.GetComponent<ShoulderCorrective>();if(c)c.active=false;AssetDatabase.LoadAssetAtPath<AnimationClip>(HeroFolder+clip+".anim").SampleAnimation(root,time);if(grip)grip.Solve();SolveDrape();
                    var S=new Matrix4x4[bs.Length];for(int b=0;b<bs.Length;b++)S[b]=root.transform.worldToLocalMatrix*bs[b].localToWorldMatrix*bind[b];
                    int[] band=new int[5];var pairs=new Dictionary<string,(int n,float minS,float angle)>();var lowPoints=new List<Vector3>();float minAll=9;
                    for(int v=0;v<rest.Length;v++){if(Mathf.Min(Vector3.Distance(rest[v],shoulder[0]),Vector3.Distance(rest[v],shoulder[1]))>.8f)continue;
                        var w=weights[v];var M=new Matrix4x4();Acc(ref M,S[w.boneIndex0],w.weight0);Acc(ref M,S[w.boneIndex1],w.weight1);Acc(ref M,S[w.boneIndex2],w.weight2);Acc(ref M,S[w.boneIndex3],w.weight3);
                        var sv=SingularValues(M);float s=sv[2];minAll=Mathf.Min(minAll,s);
                        band[s<.125f?0:s<.25f?1:s<.5f?2:s<.8f?3:4]++;
                        if(s<.5f){lowPoints.Add(rest[v]);
                            var top=new[]{(w.boneIndex0,w.weight0),(w.boneIndex1,w.weight1),(w.boneIndex2,w.weight2),(w.boneIndex3,w.weight3)}.OrderByDescending(q=>q.Item2).Take(2).ToArray();
                            string key=bs[top[0].Item1].name+"+"+bs[top[1].Item1].name;float ang=Quaternion.Angle(S[top[0].Item1].rotation,S[top[1].Item1].rotation);
                            pairs.TryGetValue(key,out var p);pairs[key]=(p.n+1,p.n==0?s:Mathf.Min(p.minS,s),Mathf.Max(p.angle,ang));}}
                    sb.AppendLine(" "+label+": vertices within 0.8 of a shoulder by smallest singular value s: s<0.125 "+band[0]+", 0.125-0.25 "+band[1]+", 0.25-0.5 "+band[2]+", 0.5-0.8 "+band[3]+", >=0.8 "+band[4]+"; minimum s "+minAll.ToString("0.000"));
                    foreach(var kv in pairs.OrderByDescending(k=>k.Value.n).Take(6))sb.AppendLine("    s<0.5 between "+kv.Key+": "+kv.Value.n+" vertices, min s "+kv.Value.minS.ToString("0.000")+", rotation apart up to "+kv.Value.angle.ToString("0")+" deg");
                    if(lowPoints.Count>0){var min=lowPoints.Aggregate(Vector3.Min);var max=lowPoints.Aggregate(Vector3.Max);sb.AppendLine("    s<0.5 region (bind space): x "+min.x.ToString("0.00")+".."+max.x.ToString("0.00")+", y "+min.y.ToString("0.00")+".."+max.y.ToString("0.00")+", z "+min.z.ToString("0.00")+".."+max.z.ToString("0.00"));}
                }
            }finally{Object.DestroyImmediate(root);}
            return sb.ToString();
        }
        // Adds the shoulder helper bones to a clavicled hero (Guan Yu): a helper per side at the shoulder joint, child of
        // the clavicle, bound like the upper arm; every vertex that blends clavicle (c) and upper arm (u) moves the
        // overlap min(c,u) from both to the helper (2*min), so a 50/50 vertex follows the helper's half rotation.
        // Idempotent. The prefab gets a ShoulderHelper component; the corrective shapes must be re-baked afterwards.
        public static void ApplyShoulderHelpers(){ApplyShoulderHelpersFor("guanyu");AssetDatabase.SaveAssets();}
        public static string ApplyShoulderHelpersFor(string id)
        {
            heroId=id;var contents=PrefabUtility.LoadPrefabContents(HeroPrefab);var log="";
            try{
                var skin=contents.GetComponent<SkinnedMeshRenderer>();var bones=skin.bones.ToList();var mesh=skin.sharedMesh;
                if(bones.Any(b=>b.name.EndsWith("ShoulderHelper")))return id+": helpers already present";
                if(!(bones.Count>19&&bones[18].name.Contains("Clavicle")))throw new System.Exception(id+" has no clavicles");
                var bind=mesh.bindposes.ToList();var comp=contents.GetComponent<ShoulderHelper>()??contents.AddComponent<ShoulderHelper>();
                for(int s=0;s<2;s++){int up=s==0?5:8,cl=s==0?18:19;var upBind=bind[up].inverse;var clBind=bind[cl].inverse;
                    var h=new GameObject((s==0?"Right":"Left")+"ShoulderHelper").transform;h.SetParent(bones[cl],false);
                    // Place it where the upper arm is in the bind pose (bind matrices are relative to the hero root).
                    h.position=contents.transform.TransformPoint(upBind.GetColumn(3));h.rotation=contents.transform.rotation*upBind.rotation;
                    bones.Add(h);bind.Add(bind[up]);
                    comp.helper[s]=h;comp.clavicle[s]=bones[cl];comp.upper[s]=bones[up];comp.fromClavicle[s]=Quaternion.Inverse(clBind.rotation)*upBind.rotation;comp.fromUpper[s]=Quaternion.identity;}
                int hr=bones.Count-2,hl=bones.Count-1;var weights=mesh.boneWeights;int changed=0;
                for(int v=0;v<weights.Length;v++){var w=weights[v];var d=new Dictionary<int,float>();void A(int i,float x){if(x<=0)return;d.TryGetValue(i,out var o);d[i]=o+x;}
                    A(w.boneIndex0,w.weight0);A(w.boneIndex1,w.weight1);A(w.boneIndex2,w.weight2);A(w.boneIndex3,w.weight3);bool any=false;
                    foreach(var (cl,up,h) in new[]{(18,5,hr),(19,8,hl)}){d.TryGetValue(cl,out var c);d.TryGetValue(up,out var u);float m=Mathf.Min(c,u);if(m<=0)continue;d[cl]=c-m;d[up]=u-m;A(h,2*m);any=true;}
                    if(!any)continue;changed++;var top=d.Where(k=>k.Value>0).OrderByDescending(k=>k.Value).Take(4).ToArray();float sum=top.Sum(k=>k.Value);
                    var nw=new BoneWeight();for(int k=0;k<top.Length;k++){float x=top[k].Value/sum;if(k==0){nw.boneIndex0=top[k].Key;nw.weight0=x;}if(k==1){nw.boneIndex1=top[k].Key;nw.weight1=x;}if(k==2){nw.boneIndex2=top[k].Key;nw.weight2=x;}if(k==3){nw.boneIndex3=top[k].Key;nw.weight3=x;}}weights[v]=nw;}
                mesh.bindposes=bind.ToArray();mesh.boneWeights=weights;EditorUtility.SetDirty(mesh);skin.bones=bones.ToArray();
                PrefabUtility.SaveAsPrefabAsset(contents,HeroPrefab);log=id+": helpers added, "+changed+" vertices reweighted";
            }finally{PrefabUtility.UnloadPrefabContents(contents);}
            Debug.Log("SANGO3_HELPERS "+log);return log;
        }
        // Singular values of the 3x3 part, largest first (eigenvalues of M^T M by Jacobi rotations).
        static float[] SingularValues(Matrix4x4 m)
        {
            var a=new double[3,3];for(int i=0;i<3;i++)for(int j=0;j<3;j++){double s=0;for(int k=0;k<3;k++)s+=m[k,i]*m[k,j];a[i,j]=s;}
            for(int sweep=0;sweep<20;sweep++)for(int p=0;p<2;p++)for(int q=p+1;q<3;q++){if(System.Math.Abs(a[p,q])<1e-12)continue;double th=.5*System.Math.Atan2(2*a[p,q],a[q,q]-a[p,p]);double c=System.Math.Cos(th),s=System.Math.Sin(th);
                for(int k=0;k<3;k++){double akp=a[k,p],akq=a[k,q];a[k,p]=c*akp-s*akq;a[k,q]=s*akp+c*akq;}for(int k=0;k<3;k++){double apk=a[p,k],aqk=a[q,k];a[p,k]=c*apk-s*aqk;a[q,k]=s*apk+c*aqk;}}
            return new[]{(float)System.Math.Sqrt(System.Math.Max(0,a[0,0])),(float)System.Math.Sqrt(System.Math.Max(0,a[1,1])),(float)System.Math.Sqrt(System.Math.Max(0,a[2,2]))}.OrderByDescending(x=>x).ToArray();
        }
    }
}
