using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace Sango3.Editor
{
    // Liu Bei's sweep (0.24): measures how badly the wide right sleeve bunches during Attack2, for several local fixes,
    // without changing assets: sleeve-drape follow amount, and smoothed cuff weights (forearm / upper arm / sleeve bone).
    // Bunching = sleeve triangles crushed below 40% of their rest area or turned inside out, averaged per frame, with
    // the cloth follow-through stepped at 1/60 s as in the game. ApplyLiubeiSleeve writes the chosen weights.
    public static partial class GuanyuSample
    {
        public static void RebakeLiubei(){heroId="liubei";try{MatchHands();}catch(System.Exception ex){Debug.LogWarning(ex.Message);}Build();heroId="guanyu";Debug.Log("SANGO3_LIUBEI_REBAKED");}
        // Chosen in 0.24 from LiubeiSleeveSweep: sleeve follow 0.8 and 6 passes of cuff smoothing (with the new sweep path).
        public static void ApplyLiubeiSleeve()
        {
            heroId="liubei";var contents=PrefabUtility.LoadPrefabContents(HeroPrefab);
            try{var skin=contents.GetComponent<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;mesh.boneWeights=SmoothCuff(mesh,mesh.boneWeights,6);EditorUtility.SetDirty(mesh);
                contents.GetComponent<RecoveredSleeveDrape>().follow=.8f;PrefabUtility.SaveAsPrefabAsset(contents,HeroPrefab);}
            finally{PrefabUtility.UnloadPrefabContents(contents);heroId="guanyu";}
            AssetDatabase.SaveAssets();Debug.Log("SANGO3_LIUBEI_SLEEVE_APPLIED");
        }
        // Bunching over every move with the prefab's own settings (0.25: all moves, not only the sweep).
        public static void LiubeiSleeveAllMoves()
        {
            ClothFollow.NoCollision=System.Environment.GetEnvironmentVariable("SANGO3_NOCOLLIDE")=="1";heroId="liubei";Load();var report=new StringBuilder((ClothFollow.NoCollision?"[torso collision off] ":"")+"Liu Bei sleeve bunching per move (prefab settings; crushed = area < 40% of rest, flipped = facing reversed): average / worst frame\n");
            try{var mesh=root.GetComponent<SkinnedMeshRenderer>().sharedMesh;float follow=root.GetComponent<RecoveredSleeveDrape>().follow;
                foreach(var clip in new[]{"Idle","Attack1","Attack2","Attack3","Ultimate"}){var r=SleeveBunching(mesh.boneWeights,follow,clip);report.AppendLine(" "+clip.PadRight(8)+" crushed "+r.crushed.ToString("0.0")+" / "+r.worstCrushed+", flipped "+r.flipped.ToString("0.0")+" / "+r.worstFlipped+" (of "+r.total+")");}}
            finally{Object.DestroyImmediate(root);heroId="guanyu";}
            File.WriteAllText(TrialRoot+"liubei-sleeve-all.txt",report.ToString());Debug.Log("SANGO3_LIUBEI_ALL "+report);
        }
        // 0.25: a second bone half way down each sleeve (child of the sleeve drape bone). The lower part of the sleeve moves
        // to it with a gradient, so the sleeve bends and trails instead of hanging as one rigid slab. Idempotent.
        public static void AddLiubeiSleeveTips()
        {
            heroId="liubei";var contents=PrefabUtility.LoadPrefabContents(HeroPrefab);
            try{var skin=contents.GetComponent<SkinnedMeshRenderer>();var bones=skin.bones.ToList();if(bones.Any(b=>b.name.EndsWith("SleeveTip")))return;
                var mesh=skin.sharedMesh;var bind=mesh.bindposes.ToList();var rest=mesh.vertices;var weights=mesh.boneWeights;var log=new StringBuilder();
                for(int s=0;s<2;s++){int drape=bones.FindIndex(b=>b.name==(s==0?"Right":"Left")+"SleeveDrape");var drapeBind=bind[drape].inverse;var top=(Vector3)drapeBind.GetColumn(3);
                    float minY=top.y;for(int v=0;v<rest.Length;v++)if(W(weights[v],drape)>.3f)minY=Mathf.Min(minY,rest[v].y);
                    var tipPos=top+Vector3.down*(top.y-minY)*.5f;var tip=new GameObject((s==0?"Right":"Left")+"SleeveTip").transform;tip.SetParent(bones[drape],false);
                    tip.position=contents.transform.TransformPoint(tipPos);tip.rotation=contents.transform.rotation*drapeBind.rotation;
                    bones.Add(tip);int ti=bones.Count-1;bind.Add(Matrix4x4.TRS(tipPos,drapeBind.rotation,Vector3.one).inverse);
                    int moved=0;for(int v=0;v<rest.Length;v++){float wd=W(weights[v],drape);if(wd<=0)continue;float t=Mathf.SmoothStep(0,1,Mathf.InverseLerp(top.y-(top.y-minY)*.25f,tipPos.y-(top.y-minY)*.15f,rest[v].y));if(t<=0)continue;
                        var d=new Dictionary<int,float>();var w=weights[v];void A(int i,float x){if(x<=0)return;d.TryGetValue(i,out var o);d[i]=o+x;}A(w.boneIndex0,w.weight0);A(w.boneIndex1,w.weight1);A(w.boneIndex2,w.weight2);A(w.boneIndex3,w.weight3);
                        d[drape]=wd*(1-t);A(ti,wd*t);var topW=d.Where(k=>k.Value>0).OrderByDescending(k=>k.Value).Take(4).ToArray();float sum=topW.Sum(k=>k.Value);var nw=new BoneWeight();
                        for(int k=0;k<topW.Length;k++){float x=topW[k].Value/sum;if(k==0){nw.boneIndex0=topW[k].Key;nw.weight0=x;}if(k==1){nw.boneIndex1=topW[k].Key;nw.weight1=x;}if(k==2){nw.boneIndex2=topW[k].Key;nw.weight2=x;}if(k==3){nw.boneIndex3=topW[k].Key;nw.weight3=x;}}weights[v]=nw;moved++;}
                    log.Append((s==0?"right":"left")+" tip at y "+tipPos.y.ToString("0.00")+" (sleeve from "+top.y.ToString("0.00")+" to "+minY.ToString("0.00")+"), "+moved+" vertices; ");}
                mesh.bindposes=bind.ToArray();mesh.boneWeights=weights;EditorUtility.SetDirty(mesh);skin.bones=bones.ToArray();PrefabUtility.SaveAsPrefabAsset(contents,HeroPrefab);Debug.Log("SANGO3_SLEEVE_TIPS "+log);}
            finally{PrefabUtility.UnloadPrefabContents(contents);heroId="guanyu";}
            AssetDatabase.SaveAssets();
        }
        static float W(BoneWeight w,int b)=>(w.boneIndex0==b?w.weight0:0)+(w.boneIndex1==b?w.weight1:0)+(w.boneIndex2==b?w.weight2:0)+(w.boneIndex3==b?w.weight3:0);
        // Sleeve-follow scan over every move (0.25).
        public static void LiubeiFollowScan()
        {
            heroId="liubei";Load();var report=new StringBuilder("Liu Bei sleeve bunching by sleeve follow, per move: crushed avg/worst, flipped avg/worst ");
            try{var mesh=root.GetComponent<SkinnedMeshRenderer>().sharedMesh;
                foreach(var (follow,ff,fromV) in new[]{(.8f,.3f,50f),(.8f,.3f,70f),(.8f,.3f,90f),(.8f,.3f,110f)}){var dr=root.GetComponent<RecoveredSleeveDrape>();dr.followForward=ff;RecoveredSleeveDrape.turnFrom=fromV;report.Append(" follow "+follow.ToString("0.00")+" turned "+ff.ToString("0.00")+" from "+fromV.ToString("0")+" deg:");float tc=0,tf=0;
                    foreach(var clip in new[]{"Idle","Attack1","Attack2","Attack3","Ultimate"}){var r=SleeveBunching(mesh.boneWeights,follow,clip);tc+=r.crushed;tf+=r.flipped;report.Append(" "+clip+" "+r.crushed.ToString("0")+"/"+r.worstCrushed+" "+r.flipped.ToString("0")+"/"+r.worstFlipped+";");}
                    report.AppendLine(" | sum of averages crushed "+tc.ToString("0")+", flipped "+tf.ToString("0"));}}
            finally{Object.DestroyImmediate(root);heroId="guanyu";}
            File.WriteAllText(TrialRoot+"liubei-follow-scan.txt",report.ToString());Debug.Log("SANGO3_FOLLOW_SCAN "+report);
        }
        // 0.26 experiment: narrower, slightly shorter sleeves in the rest mesh. Each sleeve vertex moves toward the vertical
        // line through its sleeve drape bone (horizontal factor "width") and its lower part is pulled up ("length"),
        // in proportion to its sleeve weight. A change of Liu Bei's look, not of his rig: for review before adoption.
        public static void NarrowLiubeiSleeves()
        {
            float width=.75f,length=.85f;heroId="liubei";var contents=PrefabUtility.LoadPrefabContents(HeroPrefab);
            try{var skin=contents.GetComponent<SkinnedMeshRenderer>();var bones=skin.bones;var mesh=skin.sharedMesh;var bind=mesh.bindposes;var v=mesh.vertices;var w=mesh.boneWeights;int moved=0;
                for(int s=0;s<2;s++){int d=System.Array.FindIndex(bones,b=>b.name==(s==0?"Right":"Left")+"SleeveDrape");var top=(Vector3)bind[d].inverse.GetColumn(3);
                    for(int i=0;i<v.Length;i++){float wd=W(w[i],d);if(wd<=.05f)continue;var p=v[i];var off=new Vector3(p.x-top.x,0,p.z-top.z);float h=top.y-p.y;
                        float k=Mathf.Lerp(1,width,wd);float l=Mathf.Lerp(1,length,wd*Mathf.Clamp01(h/1.2f));v[i]=new Vector3(top.x+off.x*k,top.y-h*l,top.z+off.z*k);moved++;}}
                mesh.vertices=v;mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);PrefabUtility.SaveAsPrefabAsset(contents,HeroPrefab);Debug.Log("SANGO3_NARROW "+moved);}
            finally{PrefabUtility.UnloadPrefabContents(contents);heroId="guanyu";}
            AssetDatabase.SaveAssets();
        }
        public static void LiubeiSleeveSweep()
        {
            heroId="liubei";Load();var report=new StringBuilder("Liu Bei sleeve bunching over Attack2 (crushed = area < 40% of rest; flipped = facing reversed), per frame average / worst frame\n");
            try{
                var skin=root.GetComponent<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;var baseWeights=mesh.boneWeights;
                foreach(int passes in new[]{0,6})foreach(float follow in new[]{.55f,.8f,1f}){
                    var w=passes==0?baseWeights:SmoothCuff(mesh,baseWeights,passes);var r=SleeveBunching(w,follow);
                    report.AppendLine(" cuff smoothing "+passes.ToString().PadLeft(2)+" passes, sleeve follow "+follow.ToString("0.00")+": crushed "+r.crushed.ToString("0.0")+" / "+r.worstCrushed+", flipped "+r.flipped.ToString("0.0")+" / "+r.worstFlipped+" (of "+r.total+" sleeve triangles)");}
            }finally{Object.DestroyImmediate(root);heroId="guanyu";}
            File.WriteAllText(TrialRoot+"liubei-sleeve.txt",report.ToString());Debug.Log("SANGO3_LIUBEI_SLEEVE\n"+report);
        }
        // Sleeve region in bind space (right arm: x<0): outside the shoulder, between hip and shoulder height.
        static bool SleeveTri(Vector3 c,int side){float x=c.x*(side==0?-1:1);return x>.55f&&c.y>1.3f&&c.y<3.4f;}
        static (float crushed,int worstCrushed,float flipped,int worstFlipped,int total) SleeveBunching(BoneWeight[] weights,float follow,string clipName="Attack2")
        {
            var skin=root.GetComponent<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;var rest=mesh.vertices;var tris=mesh.triangles;var bones=skin.bones;var bind=mesh.bindposes;
            var drape=root.GetComponent<RecoveredSleeveDrape>();float keep=drape.follow;drape.follow=follow;
            ClothFollow.Attach(root);var cloth=root.GetComponent<ClothFollow>();
            var list=new List<int>();for(int t=0;t<tris.Length;t+=3){var c=(rest[tris[t]]+rest[tris[t+1]]+rest[tris[t+2]])/3;if(SleeveTri(c,0))list.Add(t);}
            var restArea=list.Select(t=>Vector3.Cross(rest[tris[t+1]]-rest[tris[t]],rest[tris[t+2]]-rest[tris[t]])).ToArray();
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(HeroFolder+clipName+".anim");int frames=Mathf.CeilToInt(clip.length*60);float sumC=0,sumF=0;int worstC=0,worstF=0;
            var corrective=root.GetComponent<ShoulderCorrective>();
            for(int f=0;f<=frames;f++){clip.SampleAnimation(root,f/60f);if(grip)grip.Solve();SolveDrape();if(cloth)cloth.Step(1/60f);
                var S=new Matrix4x4[bones.Length];for(int b=0;b<bones.Length;b++)S[b]=root.transform.worldToLocalMatrix*bones[b].localToWorldMatrix*bind[b];
                var shaped=(Vector3[])rest.Clone();var d=new Vector3[rest.Length];for(int s=0;s<mesh.blendShapeCount;s++){float bw=skin.GetBlendShapeWeight(s)/100f;if(bw<=1e-4f)continue;mesh.GetBlendShapeFrameVertices(s,0,d,null,null);for(int i=0;i<rest.Length;i++)shaped[i]+=d[i]*bw;}
                var p=new Vector3[rest.Length];for(int i=0;i<rest.Length;i++){var w=weights[i];var q=shaped[i];p[i]=S[w.boneIndex0].MultiplyPoint3x4(q)*w.weight0+S[w.boneIndex1].MultiplyPoint3x4(q)*w.weight1+S[w.boneIndex2].MultiplyPoint3x4(q)*w.weight2+S[w.boneIndex3].MultiplyPoint3x4(q)*w.weight3;}
                int crushed=0,flipped=0;for(int k=0;k<list.Count;k++){int t=list[k];var n=Vector3.Cross(p[tris[t+1]]-p[tris[t]],p[tris[t+2]]-p[tris[t]]);
                    // The rest normal is carried by the bones only roughly; use the area ratio for crushing and the sign against the rest normal rotated by the dominant bone for flipping.
                    var w=weights[tris[t]];var rotN=S[w.boneIndex0].MultiplyVector(restArea[k]);if(n.magnitude<.4f*restArea[k].magnitude)crushed++;if(Vector3.Dot(n,rotN)<0)flipped++;}
                sumC+=crushed;sumF+=flipped;worstC=Mathf.Max(worstC,crushed);worstF=Mathf.Max(worstF,flipped);}
            drape.follow=keep;Object.DestroyImmediate(cloth);
            return (sumC/(frames+1),worstC,sumF/(frames+1),worstF,list.Count);
        }
        // Smooths the weights among forearm, upper arm and sleeve bone of each arm within the sleeve, so the cuff does not
        // switch from forearm to sleeve bone within one or two rows of vertices.
        static BoneWeight[] SmoothCuff(Mesh mesh,BoneWeight[] weights,int passes)
        {
            var rest=mesh.vertices;var (map,count)=Weld(rest);var tris=mesh.triangles;var adj=Enumerable.Range(0,count).Select(_=>new HashSet<int>()).ToArray();
            for(int i=0;i<tris.Length;i+=3)for(int e=0;e<3;e++){int a=map[tris[i+e]],b=map[tris[i+(e+1)%3]];if(a!=b){adj[a].Add(b);adj[b].Add(a);}}
            int nb=mesh.bindposes.Length;var data=new float[count,nb];var pos=new Vector3[count];
            for(int i=0;i<rest.Length;i++){int g=map[i];var w=weights[i];data[g,w.boneIndex0]=w.weight0;data[g,w.boneIndex1]=w.weight1;data[g,w.boneIndex2]=w.weight2;data[g,w.boneIndex3]=w.weight3;pos[g]=rest[i];}
            var sets=new[]{new[]{5,6,18},new[]{8,9,19}};
            for(int pass=0;pass<passes;pass++){var next=(float[,])data.Clone();
                for(int g=0;g<count;g++){int side=pos[g].x<0?0:1;if(!SleeveTri(pos[g],side))continue;var set=sets[side];float total=set.Sum(b=>data[g,b]);if(total<.5f)continue;
                    foreach(int b in set){float sum=0;int n=0;foreach(int k in adj[g]){sum+=data[k,b];n++;}if(n>0)next[g,b]=data[g,b]*.5f+sum/n*.5f;}
                    float after=set.Sum(b=>next[g,b]);if(after>0)foreach(int b in set)next[g,b]*=total/after;}
                data=next;}
            var outW=new BoneWeight[rest.Length];
            for(int i=0;i<rest.Length;i++){int g=map[i];var top=Enumerable.Range(0,nb).Where(b=>data[g,b]>0).OrderByDescending(b=>data[g,b]).Take(4).ToArray();float sum=top.Sum(b=>data[g,b]);var nw=new BoneWeight();
                for(int k=0;k<top.Length;k++){float x=data[g,top[k]]/sum;if(k==0){nw.boneIndex0=top[k];nw.weight0=x;}if(k==1){nw.boneIndex1=top[k];nw.weight1=x;}if(k==2){nw.boneIndex2=top[k];nw.weight2=x;}if(k==3){nw.boneIndex3=top[k];nw.weight3=x;}}outW[i]=nw;}
            return outW;
        }
    }
}
