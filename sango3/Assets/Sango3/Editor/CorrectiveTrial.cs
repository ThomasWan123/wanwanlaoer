using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace Sango3.Editor
{
    // Hand-made corrective shapes, trial stage (Docs/External-Art-Scope): Guan Yu's ultimate raise and Liu Bei's sweep.
    //  ExportTrials: for each trial pose writes, into QA/corrective-trial/<trial>/,
    //    rest.obj      the bind-pose mesh,
    //    posed.obj     the mesh at the pose with linear skinning only (what the artist sculpts),
    //    generated.obj the same pose with the current generated corrective (reference only),
    //    README.txt    pose, units, axes, vertex counts, what to return.
    //    Meshes are welded by position (seam duplicates share one OBJ vertex, UVs kept via vt indices), so sculpting
    //    cannot open seams; the OBJ vertex order is the contract and must not change.
    //  ImportTrials: reads <trial>/returned.obj (the sculpted posed mesh), checks vertex count and that untouched areas
    //    still match, converts the change into bind-space blend shapes split by arm side, and puts them in place of
    //    the generated shape at that pose node (the generated one is removed, so the two never add up).
    //  SelfTest: round trip with an unchanged file (must give no change) and with a known displacement (must be
    //    reproduced by the skinned mesh at that pose).
    // Nothing here sends anything anywhere.
    public static partial class GuanyuSample
    {
        sealed class Trial{public string name,hero,clip,node;public float time;public int[] sides;}
        static readonly Trial[] Trials={
            new Trial{name="guanyu-raise",hero="guanyu",clip="Ultimate",time=.50f,node="Raise",sides=new[]{0,1}},
            new Trial{name="liubei-sweep",hero="liubei",clip="Attack2",time=.31f,node="Sweep",sides=new[]{0}}};
        const string TrialRoot="QA/corrective-trial/";

        public static void ExportTrials(){foreach(var t in Trials)ExportTrial(t,TrialRoot+t.name+"/");heroId="guanyu";Debug.Log("SANGO3_TRIAL_EXPORTED");}
        public static void ImportTrials(){var log=new StringBuilder();foreach(var t in Trials){var path=TrialRoot+t.name+"/returned.obj";if(!File.Exists(path)){log.AppendLine(t.name+": no returned.obj");continue;}log.AppendLine(ImportTrial(t,path));}heroId="guanyu";AssetDatabase.SaveAssets();File.WriteAllText(TrialRoot+"import-log.txt",log.ToString());Debug.Log("SANGO3_TRIAL_IMPORT\n"+log);}

        // Welded view of the skin mesh: unique positions in first-appearance order, and the map from mesh vertex to it.
        static (int[] map,int count) Weld(Vector3[] v){var groups=new Dictionary<Vector3Int,int>();var map=new int[v.Length];for(int i=0;i<v.Length;i++){var k=Vector3Int.RoundToInt(v[i]*10000);if(!groups.TryGetValue(k,out int g)){g=groups.Count;groups.Add(k,g);}map[i]=g;}return (map,groups.Count);}
        static void Pose(Trial t,bool corrective){var c=root.GetComponent<ShoulderCorrective>();c.active=corrective;AssetDatabase.LoadAssetAtPath<AnimationClip>(HeroFolder+t.clip+".anim").SampleAnimation(root,t.time);if(grip)grip.Solve();SolveDrape();}
        // Skinned positions in hero-root space, computed here rather than with BakeMesh (whose output space follows the
        // renderer's root bone, which made the round trip come back rotated): active blend shapes first, then linear
        // skinning with all four weights, as the GPU does.
        static Vector3[] Baked()
        {
            var skin=root.GetComponent<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;var rest=mesh.vertices;var bones=skin.bones;var bind=mesh.bindposes;var weights=mesh.boneWeights;
            var shaped=(Vector3[])rest.Clone();var d=new Vector3[rest.Length];
            for(int s=0;s<mesh.blendShapeCount;s++){float w=skin.GetBlendShapeWeight(s)/100f;if(w<=1e-4f)continue;mesh.GetBlendShapeFrameVertices(s,0,d,null,null);for(int i=0;i<rest.Length;i++)shaped[i]+=d[i]*w;}
            var S=new Matrix4x4[bones.Length];for(int b=0;b<bones.Length;b++)S[b]=root.transform.worldToLocalMatrix*bones[b].localToWorldMatrix*bind[b];
            var outp=new Vector3[rest.Length];
            for(int i=0;i<rest.Length;i++){var w=weights[i];var p=shaped[i];outp[i]=S[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+S[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+S[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+S[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;}
            return outp;
        }

        static void ExportTrial(Trial t,string folder)
        {
            heroId=t.hero;Directory.CreateDirectory(folder);Load();
            try{
                var mesh=root.GetComponent<SkinnedMeshRenderer>().sharedMesh;var rest=mesh.vertices;var (map,count)=Weld(rest);
                WriteObj(folder+"rest.obj",rest,mesh,map,count);
                Pose(t,false);WriteObj(folder+"posed.obj",Baked(),mesh,map,count);
                Pose(t,true);WriteObj(folder+"generated.obj",Baked(),mesh,map,count);
                var readme=new StringBuilder();
                readme.AppendLine("Trial "+t.name+": hero "+t.hero+", clip "+t.clip+" at "+t.time.ToString("0.00",CultureInfo.InvariantCulture)+" s, arm(s) to correct: "+string.Join(" and ",t.sides.Select(s=>s==0?"right":"left")));
                readme.AppendLine("Files: rest.obj (bind pose), posed.obj (this pose, linear skinning only: sculpt THIS), generated.obj (current automatic correction, reference only).");
                readme.AppendLine("Units: 1 unit = 1/4.4 of the hero's height (hero stands 4.4 units tall). Axes: OBJ right-handed, +Y up, hero faces +Z; x is mirrored relative to Unity (Unity is left-handed).");
                readme.AppendLine("Vertices: "+count+" (welded; UV seams are kept through vt indices). Faces: "+mesh.triangles.Length/3+".");
                readme.AppendLine("Return: returned.obj = posed.obj with the shoulder, armpit and sleeve reshaped. Keep the vertex count and order. Do not move the hands, head, legs or weapon.");
                File.WriteAllText(folder+"README.txt",readme.ToString());
            }finally{Object.DestroyImmediate(root);}
        }
        static void WriteObj(string path,Vector3[] positions,Mesh mesh,int[] map,int count)
        {
            var welded=new Vector3[count];for(int i=0;i<positions.Length;i++)welded[map[i]]=positions[i];var uv=mesh.uv;var tris=mesh.triangles;
            var sb=new StringBuilder();var ci=CultureInfo.InvariantCulture;sb.AppendLine("# sango3 corrective trial mesh; vertex order is fixed");
            foreach(var p in welded)sb.Append("v ").Append((-p.x).ToString("0.000000",ci)).Append(' ').Append(p.y.ToString("0.000000",ci)).Append(' ').Append(p.z.ToString("0.000000",ci)).Append('\n');
            foreach(var u in uv)sb.Append("vt ").Append(u.x.ToString("0.000000",ci)).Append(' ').Append(u.y.ToString("0.000000",ci)).Append('\n');
            // x is mirrored, so the winding is reversed to keep faces pointing outward.
            for(int i=0;i<tris.Length;i+=3){int a=tris[i],b=tris[i+1],c=tris[i+2];sb.Append("f ").Append(map[a]+1).Append('/').Append(a+1).Append(' ').Append(map[c]+1).Append('/').Append(c+1).Append(' ').Append(map[b]+1).Append('/').Append(b+1).Append('\n');}
            File.WriteAllText(path,sb.ToString());
        }
        static Vector3[] ReadObjPositions(string path)
        {
            var list=new List<Vector3>();var ci=CultureInfo.InvariantCulture;
            foreach(var line in File.ReadLines(path)){if(!line.StartsWith("v "))continue;var p=line.Split(new[]{' '},System.StringSplitOptions.RemoveEmptyEntries);list.Add(new Vector3(-float.Parse(p[1],ci),float.Parse(p[2],ci),float.Parse(p[3],ci)));}
            return list.ToArray();
        }

        static string ImportTrial(Trial t,string path)
        {
            heroId=t.hero;Load();var log=new StringBuilder(t.name+":");
            try{
                var skin=root.GetComponent<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;var rest=mesh.vertices;var (map,count)=Weld(rest);var bones=skin.bones;var bind=mesh.bindposes;var weights=mesh.boneWeights;
                shrink.Clear();var target=ReadObjPositions(path);if(target.Length!=count)throw new System.Exception(t.name+": returned.obj has "+target.Length+" vertices, expected "+count+" (vertex order/count changed)");
                Pose(t,false);var linear=Baked();
                // Order check: the hands, head and feet must be where posed.obj put them (they are not to be touched).
                int far=0;for(int i=0;i<rest.Length;i++){var w=weights[i];float pinned=Share(w,new[]{4,7,10,13,16});if(pinned>.9f&&Vector3.Distance(target[map[i]],linear[i])>.01f)far++;}
                if(far>rest.Length/200)throw new System.Exception(t.name+": "+far+" hand/head/foot vertices moved; the vertex order probably changed");
                var corrective=root.GetComponent<ShoulderCorrective>();var chest=corrective.chest;
                var S=new Matrix4x4[bones.Length];for(int b=0;b<bones.Length;b++)S[b]=root.transform.worldToLocalMatrix*bones[b].localToWorldMatrix*bind[b];
                var shoulder=new[]{(Vector3)bind[5].inverse.GetColumn(3),(Vector3)bind[8].inverse.GetColumn(3)};
                var shapes=corrective.shapes.ToList();
                foreach(int side in t.sides){
                    var delta=new Vector3[rest.Length];float largest=0;int moved=0,clamped=0,degenerate=0;
                    for(int i=0;i<rest.Length;i++){var change=target[map[i]]-linear[i];if(change.sqrMagnitude<1e-10f)continue;
                        // Split by side: this arm's half of the body, blended across the middle of the chest.
                        float x=rest[i].x*(side==0?-1:1);float half=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.12f,.12f,x));if(half<=0)continue;
                        var w=weights[i];var M=new Matrix4x4();AccW(ref M,S[w.boneIndex0],w.weight0);AccW(ref M,S[w.boneIndex1],w.weight1);AccW(ref M,S[w.boneIndex2],w.weight2);AccW(ref M,S[w.boneIndex3],w.weight3);
                        float det=M.m00*(M.m11*M.m22-M.m12*M.m21)-M.m01*(M.m10*M.m22-M.m12*M.m20)+M.m02*(M.m10*M.m21-M.m11*M.m20);
                        // The bind-space change is M^-1 * change, exactly, with no size cap. Only a numerically singular M (smallest
                        // singular value below 0.001) falls back to the closest reachable change (damped least squares). The smallest
                        // singular value of each vertex is kept, so the self-test reports errors by how much skinning shrinks the vertex.
                        float sMin=SingularValues(M)[2];shrink[i]=sMin;
                        Vector3 d;if(sMin>=.001f)d=M.inverse.MultiplyVector(change);else{degenerate++;var Mt=M.transpose;var A=Mt*M;A.m00+=1e-6f;A.m11+=1e-6f;A.m22+=1e-6f;A.m33=1;A.m03=A.m13=A.m23=A.m30=A.m31=A.m32=0;d=A.inverse.MultiplyVector(Mt.MultiplyVector(change));}d*=half;delta[i]=d;moved++;largest=Mathf.Max(largest,d.magnitude);}
                    int up=side==0?5:8,lo=side==0?6:9;var dir=chest.InverseTransformDirection((bones[lo].position-bones[up].position).normalized);
                    string name="Hand"+(side==0?"R":"L")+t.node;
                    // Replace whatever shape sits at this pose node (generated, or an earlier hand shape) so corrections never add up.
                    int node=shapes.FindIndex(s=>s.side==side&&Vector3.Angle(s.direction,dir)<12);
                    var keep=new List<(string n,Vector3[] d,Vector3[] nn,Vector3[] tt,float w)>();var drop=node>=0&&shapes[node].index>=0?mesh.GetBlendShapeName(shapes[node].index):null;
                    for(int s=0;s<mesh.blendShapeCount;s++){var nm=mesh.GetBlendShapeName(s);if(nm==drop||nm==name)continue;var dd=new Vector3[mesh.vertexCount];var nn=new Vector3[mesh.vertexCount];var tt=new Vector3[mesh.vertexCount];mesh.GetBlendShapeFrameVertices(s,0,dd,nn,tt);keep.Add((nm,dd,nn,tt,mesh.GetBlendShapeFrameWeight(s,0)));}
                    var names=shapes.Select(s=>s.index>=0?mesh.GetBlendShapeName(s.index):null).ToList();
                    mesh.ClearBlendShapes();foreach(var k in keep)mesh.AddBlendShapeFrame(k.n,k.w,k.d,k.nn,k.tt);mesh.AddBlendShapeFrame(name,100,delta,null,null);
                    if(node>=0){shapes[node]=new ShoulderCorrective.Shape{index=mesh.GetBlendShapeIndex(name),side=side,direction=dir};names[node]=name;}else{shapes.Add(new ShoulderCorrective.Shape{index=mesh.GetBlendShapeIndex(name),side=side,direction=dir});names.Add(name);}
                    for(int s=0;s<shapes.Count;s++)if(names[s]!=null){var sh=shapes[s];sh.index=mesh.GetBlendShapeIndex(names[s]);shapes[s]=sh;}
                    log.Append(" "+name+" "+moved+"v max "+largest.ToString("0.000")+" numerically singular "+degenerate+(drop!=null?" (replaces "+drop+")":" (new node)"));}
                EditorUtility.SetDirty(mesh);
                var contents=PrefabUtility.LoadPrefabContents(HeroPrefab);try{contents.GetComponent<ShoulderCorrective>().shapes=shapes.ToArray();PrefabUtility.SaveAsPrefabAsset(contents,HeroPrefab);}finally{PrefabUtility.UnloadPrefabContents(contents);}
            }finally{Object.DestroyImmediate(root);}
            return log.ToString();
        }
        // Smallest singular value of each vertex's skin matrix at the pose of the last import (how much skinning shrinks it).
        static readonly Dictionary<int,float> shrink=new Dictionary<int,float>();
        static void AccW(ref Matrix4x4 m,Matrix4x4 s,float w){if(w<=0)return;for(int e=0;e<16;e++)m[e]+=s[e]*w;}
        static float Share(BoneWeight w,int[] set){float p=0;if(set.Contains(w.boneIndex0))p+=w.weight0;if(set.Contains(w.boneIndex1))p+=w.weight1;if(set.Contains(w.boneIndex2))p+=w.weight2;if(set.Contains(w.boneIndex3))p+=w.weight3;return p;}

        // Round trip on copies of the assets: (1) unchanged posed.obj must give a zero shape; (2) posed.obj with the shoulder
        // region pushed out by 0.02 must come back, at that pose with the corrective on, within 0.002 of the edited mesh.
        public static void TrialSelfTest()
        {
            var report=new StringBuilder();var failures=new List<string>();
            foreach(var t in Trials){
                var folder=TrialRoot+t.name+"/";ExportTrial(t,folder);heroId=t.hero;
                // Work on temporary copies of the hero prefab and skin mesh; the real assets are never touched.
                const string Temp="Assets/Sango3/Editor/_CorrectiveTrialTemp/";AssetDatabase.DeleteAsset(Temp.TrimEnd('/'));AssetDatabase.CreateFolder("Assets/Sango3/Editor","_CorrectiveTrialTemp");
                var realPrefab=HeroPrefab;var realMesh=AssetDatabase.LoadAssetAtPath<GameObject>(realPrefab).GetComponent<SkinnedMeshRenderer>().sharedMesh;
                var meshCopy=Object.Instantiate(realMesh);AssetDatabase.CreateAsset(meshCopy,Temp+"mesh.asset");AssetDatabase.CopyAsset(realPrefab,Temp+"hero.prefab");
                {var c=PrefabUtility.LoadPrefabContents(Temp+"hero.prefab");try{c.GetComponent<SkinnedMeshRenderer>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Temp+"mesh.asset");PrefabUtility.SaveAsPrefabAsset(c,Temp+"hero.prefab");}finally{PrefabUtility.UnloadPrefabContents(c);}}
                trialPrefab=Temp+"hero.prefab";var meshPath=Temp+"mesh.asset";
                try{
                    // (1) unchanged
                    File.Copy(folder+"posed.obj",folder+"selftest.obj",true);var line=ImportTrial(t,folder+"selftest.obj");report.AppendLine("unchanged: "+line);
                    // (2) known displacement on the corrected arm(s)
                    var posed=ReadObjPositions(folder+"posed.obj");var restW=ReadObjPositions(folder+"rest.obj");var edited=(Vector3[])posed.Clone();
                    var bind=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath).bindposes;
                    foreach(int side in t.sides){var sh=(Vector3)bind[side==0?5:8].inverse.GetColumn(3);for(int g=0;g<restW.Length;g++){float r=Vector3.Distance(restW[g],sh);if(r<.45f&&restW[g].x*(side==0?-1:1)>.15f)edited[g]+=Vector3.up*.02f*(1-r/.45f);}}
                    WriteObjFromPositions(folder+"selftest.obj",folder+"posed.obj",edited);report.AppendLine("displaced: "+ImportTrial(t,folder+"selftest.obj"));
                    // Real check: must pass.
                    var real=EvaluateTrial(t,posed,edited,report,"check");
                    if(real>=.002f)failures.Add(t.name+": worst error "+real.ToString("0.0000"));
                    // Negative case: halve the imported shapes on the temporary copy; the same check must now fail.
                    {var m=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);var keep=new List<(string n,Vector3[] d,float w)>();
                        for(int b=0;b<m.blendShapeCount;b++){var nm=m.GetBlendShapeName(b);var dd=new Vector3[m.vertexCount];m.GetBlendShapeFrameVertices(b,0,dd,null,null);if(nm.StartsWith("Hand"))for(int k=0;k<dd.Length;k++)dd[k]*=.5f;keep.Add((nm,dd,m.GetBlendShapeFrameWeight(b,0)));}
                        m.ClearBlendShapes();foreach(var k in keep)m.AddBlendShapeFrame(k.n,k.w,k.d,null,null);}
                    var negative=EvaluateTrial(t,posed,edited,report,"negative case (imported shapes halved on purpose; must fail)");
                    if(negative<.002f)failures.Add(t.name+": the negative case did not fail (worst "+negative.ToString("0.0000")+"), so the check cannot detect errors");
                }finally{
                    trialPrefab=null;AssetDatabase.DeleteAsset(Temp.TrimEnd('/'));
                    File.Delete(folder+"selftest.obj");
                }
            }
            heroId="guanyu";report.AppendLine(failures.Count==0?"RESULT: PASS":"RESULT: FAIL - "+string.Join("; ",failures));
            File.WriteAllText(TrialRoot+"selftest.txt",report.ToString());Debug.Log("SANGO3_TRIAL_SELFTEST "+report);
            if(failures.Count>0)throw new System.Exception("Corrective trial self-test failed: "+string.Join("; ",failures));
        }
        // Poses the hero (corrective on) and compares every edited vertex with the target, by shrink band. Vertices without
        // shrink data from the import go to an "unknown" band; any there count as a failure. Returns the worst error.
        static float EvaluateTrial(Trial t,Vector3[] posed,Vector3[] edited,StringBuilder report,string label)
        {
            heroId=t.hero;Load();Pose(t,true);var got=Baked();var mesh=root.GetComponent<SkinnedMeshRenderer>().sharedMesh;var (map,_)=Weld(mesh.vertices);float worst=0;
            var bands=new[]{(lo:0f,hi:.125f,name:"s<0.125"),(lo:.125f,hi:.25f,name:"0.125-0.25"),(lo:.25f,hi:.5f,name:"0.25-0.5"),(lo:.5f,hi:float.PositiveInfinity,name:"s>=0.5"),(lo:float.NaN,hi:float.NaN,name:"unknown (no shrink data)")};
            var stats=new (int n,int off,float worst)[bands.Length];
            for(int i=0;i<got.Length;i++){if((edited[map[i]]-posed[map[i]]).sqrMagnitude<=1e-8f)continue;float e=Vector3.Distance(got[i],edited[map[i]]);
                int b=shrink.TryGetValue(i,out float sv)?System.Array.FindIndex(bands,q=>sv>=q.lo&&sv<q.hi):bands.Length-1;if(b<0)b=bands.Length-1;
                stats[b].n++;if(e>.002f)stats[b].off++;stats[b].worst=Mathf.Max(stats[b].worst,e);worst=Mathf.Max(worst,e);}
            if(stats[bands.Length-1].n>0)worst=Mathf.Max(worst,.002f+stats[bands.Length-1].worst);
            report.AppendLine("  "+label+":");for(int b=0;b<bands.Length;b++)report.AppendLine("    "+bands[b].name+": "+stats[b].n+" edited vertices, "+stats[b].off+" off by more than 0.002, worst error "+stats[b].worst.ToString("0.0000"));
            report.AppendLine("    verdict: "+(worst<.002f?"PASS (every edited vertex within 0.002)":"FAIL (worst error "+worst.ToString("0.0000")+")"));
            Object.DestroyImmediate(root);return worst;
        }

        // Acceptance for a trial (before and after hand shapes): the whole clip that contains the trial pose, sampled at
        // 60 fps (so 0.5x-2x playback speeds are covered by the same frames), close-up front / side / back, corrective
        // off (left) and on (right); plus a popping check (the largest vertex step between neighbouring frames, with the
        // corrective, against without it) and an idle check (idle must be uncorrected: zero node).
        public static void TrialReels()
        {
            var report=new StringBuilder();
            foreach(var t in Trials){heroId=t.hero;Load();HeroLighting.Apply();var folder=TrialRoot+t.name+"/reel/";if(Directory.Exists(folder))Directory.Delete(folder,true);Directory.CreateDirectory(folder);
                var light=new GameObject("key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(40,-35,0);RenderSettings.ambientLight=new Color(.55f,.56f,.6f);
                var camera=new GameObject("camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.10f,.12f,.15f);camera.orthographic=true;camera.orthographicSize=1.45f;camera.aspect=1;
                const int cell=320;var frame=new Texture2D(cell*3,cell*2,TextureFormat.RGB24,false);var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(HeroFolder+t.clip+".anim");
                int frames=Mathf.CeilToInt(clip.length*60);var trace=new StringBuilder();Vector3[] prevOff=null,prevOn=null,prev2Off=null,prev2On=null;float worstAccel=0;int worstAccelAt=0;float worstRatio=0,worstStepOn=0,worstStepOff=0;int worstAt=0;
                for(int f=0;f<=frames;f++){float time=f/60f;
                    Pose(new Trial{hero=t.hero,clip=t.clip,time=time},false);var off=Baked();var chest=bones[Chest].position+Vector3.up*.05f;
                    for(int on=0;on<2;on++){Pose(new Trial{hero=t.hero,clip=t.clip,time=time},on==1);var shown=Bake();
                        var views=new[]{new Vector3(2.5f,.5f,7.6f),new Vector3(8f,.5f,.3f),new Vector3(-2.5f,.5f,-7.6f)};
                        for(int v=0;v<3;v++){camera.transform.position=chest+views[v];camera.transform.LookAt(chest);Shot(camera,frame,v*cell,(1-on)*cell,cell);}Clear(shown);}
                    var onPos=Baked();
                    if(f>=24&&f<=36){var sk=root.GetComponent<SkinnedMeshRenderer>();var tr=new StringBuilder("   f"+f+":");for(int b=0;b<sk.sharedMesh.blendShapeCount;b++){float w=sk.GetBlendShapeWeight(b);if(w>1)tr.Append(" "+sk.sharedMesh.GetBlendShapeName(b)+"="+w.ToString("0"));}trace.AppendLine(tr.ToString());}
                    if(prevOff!=null){
                        // Extra jump the corrective adds at any vertex between neighbouring frames: |step with| - |step without|.
                        for(int i=0;i<off.Length;i++){float extra=(onPos[i]-prevOn[i]).magnitude-(off[i]-prevOff[i]).magnitude;if(extra>worstRatio){worstRatio=extra;worstAt=f;worstStepOn=(onPos[i]-prevOn[i]).magnitude;worstStepOff=(off[i]-prevOff[i]).magnitude;}}}
                    // Pop check: extra acceleration (second difference between neighbouring frames) the corrective adds at any vertex.
                    if(prev2Off!=null)for(int i=0;i<off.Length;i++){float extra=(onPos[i]-2*prevOn[i]+prev2On[i]).magnitude-(off[i]-2*prevOff[i]+prev2Off[i]).magnitude;if(extra>worstAccel){worstAccel=extra;worstAccelAt=f;}}
                    prev2Off=prevOff;prev2On=prevOn;prevOff=off;prevOn=onPos;File.WriteAllBytes(folder+f.ToString("0000")+".jpg",frame.EncodeToJPG(90));}
                AssetDatabase.LoadAssetAtPath<AnimationClip>(HeroFolder+"Idle.anim").SampleAnimation(root,0);if(grip)grip.Solve();var cr=root.GetComponent<ShoulderCorrective>();cr.active=false;SolveDrape();var idleOff=Baked();cr.active=true;SolveDrape();var idleOn=Baked();
                float idleDiff=0;for(int i=0;i<idleOff.Length;i++)idleDiff=Mathf.Max(idleDiff,(idleOff[i]-idleOn[i]).magnitude);
                report.AppendLine(t.name+": "+(frames+1)+" frames at 60 fps (top row off, bottom row on; front, side, back). Largest extra jump the corrective adds at a vertex between neighbouring frames: "+worstRatio.ToString("0.0000")+" at frame "+worstAt+" (that vertex steps "+worstStepOn.ToString("0.000")+" with, "+worstStepOff.ToString("0.000")+" without). Largest extra acceleration (pop check): "+worstAccel.ToString("0.0000")+" at frame "+worstAccelAt+". Idle difference on vs off: "+idleDiff.ToString("0.0000"));report.Append(trace);
                Object.DestroyImmediate(frame);Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(root);}
            heroId="guanyu";File.WriteAllText(TrialRoot+"reels.txt",report.ToString());Debug.Log("SANGO3_TRIAL_REELS "+report);
        }
        // Same-camera scene comparison for stage 16: every capture view with the 0.22 ground variation and distant ridges
        // switched off, then on (files *-off.png / *-on.png in QA/sample-130/compare/).
        public static void CaptureStageCompare()
        {
            Directory.CreateDirectory(Evidence+"compare/");
            foreach(bool on in new[]{false,true}){ScenarioScenery.SkipGroundVariation=!on;ScenarioScenery.SkipRidges=!on;CaptureStage();
                foreach(var f in Directory.GetFiles(Evidence,"stage16*.png"))File.Copy(f,Evidence+"compare/"+Path.GetFileNameWithoutExtension(f)+(on?"-on":"-off")+".png",true);}
            ScenarioScenery.SkipGroundVariation=ScenarioScenery.SkipRidges=false;Debug.Log("SANGO3_STAGE_COMPARE");
        }
        // Removes blend shapes named Hand* (hand-made trial shapes) from the two sample heroes. Used once in 0.23 to clear
        // shapes a self-test leaked into the real assets; afterwards BuildPoseCorrectives restores the generated shapes.
        public static void RemoveHandShapes()
        {
            foreach(var id in new[]{"guanyu","liubei"}){var mesh=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sango3/Resources/Heroes/Final/"+id+".prefab").GetComponent<SkinnedMeshRenderer>().sharedMesh;
                var keep=new List<(string n,Vector3[] d,Vector3[] nn,Vector3[] tt,float w)>();int removed=0;
                for(int s=0;s<mesh.blendShapeCount;s++){var nm=mesh.GetBlendShapeName(s);if(nm.StartsWith("Hand")){removed++;continue;}var dd=new Vector3[mesh.vertexCount];var nn=new Vector3[mesh.vertexCount];var tt=new Vector3[mesh.vertexCount];mesh.GetBlendShapeFrameVertices(s,0,dd,nn,tt);keep.Add((nm,dd,nn,tt,mesh.GetBlendShapeFrameWeight(s,0)));}
                mesh.ClearBlendShapes();foreach(var k in keep)mesh.AddBlendShapeFrame(k.n,k.w,k.d,k.nn,k.tt);EditorUtility.SetDirty(mesh);Debug.Log("SANGO3_REMOVED_HAND "+id+" "+removed);}
            AssetDatabase.SaveAssets();
        }
        static void WriteObjFromPositions(string path,string template,Vector3[] positions)
        {
            var ci=CultureInfo.InvariantCulture;var sb=new StringBuilder();int k=0;
            foreach(var line in File.ReadLines(template)){if(line.StartsWith("v ")){var p=positions[k++];sb.Append("v ").Append((-p.x).ToString("0.000000",ci)).Append(' ').Append(p.y.ToString("0.000000",ci)).Append(' ').Append(p.z.ToString("0.000000",ci)).Append('\n');}else sb.Append(line).Append('\n');}
            File.WriteAllText(path,sb.ToString());
        }
    }
}
