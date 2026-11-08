using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace Sango3.Editor
{
    // Per-pose corrective shapes for the two sample heroes (Guan Yu, Liu Bei), replacing their generic shoulder shapes.
    // Each pose is a real frame of one of their moves, chosen for the three pose classes in the 0.21 review (arm raised,
    // swept across the chest, recovering) plus idle. At that frame the shoulder, armpit and sleeve region is corrected
    // with delta mush: the linear-skinned surface is relaxed (which removes the hard creases and caved-in blocks) and the
    // rest-pose surface detail is put back on top in local frames, so plates, seams and embroidery keep their shape.
    // Region and strength are set per pose in the table below, chosen by looking at the shoulder reels (RenderReels).
    // The difference to the linear result is stored as a blend shape in bind space; ShoulderCorrective blends the
    // shapes at runtime by each upper arm's direction, exactly as for the generic shapes.
    public static partial class GuanyuSample
    {
        sealed class PoseSpec{public string clip;public float time,strength=1,reach=.8f;public string label;public bool zero;}
        static readonly Dictionary<string,PoseSpec[]> PoseTable=new Dictionary<string,PoseSpec[]>{
            ["guanyu"]=new[]{
                // Raise and sweep only: on Guan Yu's dense, ornamented mesh the idle and recovery shapes added creases in the
                // fold measurement (0.22 trials), while the raise shapes close the caved-in shoulder seam.
                // 0.25: raise strength 0.68 (0.85 x 0.8) keeps the frame-to-frame jump within the 0.23 baseline (0.0715 vs 0.073)
                // after the helper bones; at the raise frame the picture is the same as at 0.85.
                new PoseSpec{label="Raise",clip="Ultimate",time=.50f,strength=.68f},new PoseSpec{label="RaiseHigh",clip="Ultimate",time=.64f,strength=.68f},
                new PoseSpec{label="Sweep",clip="Attack2",time=.36f,strength=.7f},
                // Zero nodes: these arm directions keep the plain skinning, so the shapes above stay local to their poses.
                new PoseSpec{label="Idle",clip="Idle",time=0,zero=true},new PoseSpec{label="Recover",clip="Attack1",time=.55f,zero=true},
                new PoseSpec{label="RecoverLow",clip="Attack3",time=.80f,zero=true},new PoseSpec{label="Windup",clip="Attack3",time=.24f,zero=true}},
            ["liubei"]=new[]{
                new PoseSpec{label="Idle",clip="Idle",time=0,strength=.8f,reach=1.05f},
                new PoseSpec{label="Raise",clip="Ultimate",time=.60f,reach=1.05f},
                new PoseSpec{label="Sweep",clip="Attack2",time=.31f,reach=1.05f},new PoseSpec{label="Cut",clip="Attack1",time=.28f,reach=1.05f},
                new PoseSpec{label="Recover",clip="Attack1",time=.45f,reach=1.05f},new PoseSpec{label="Thrust",clip="Attack3",time=.40f,reach=1.05f}}};

        public static void BuildPoseCorrectives()
        {
            var report=new StringBuilder();
            foreach(var id in new[]{"guanyu","liubei"}){heroId=id;report.AppendLine(BuildPoseCorrectivesFor(id));}
            heroId="guanyu";AssetDatabase.SaveAssets();File.WriteAllText(Evidence+"pose-correctives.txt",report.ToString());Debug.Log("SANGO3_POSE_CORRECTIVES\n"+report);
        }
        static string BuildPoseCorrectivesFor(string id)
        {
            // Generic shapes first (fresh bind data and component), then replace them.
            ShoulderCorrectives.Build(id);
            Load();var log=new StringBuilder(id+":");
            try{
                var skin=root.GetComponent<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;var bind=mesh.bindposes;var corrective=root.GetComponent<ShoulderCorrective>();corrective.active=false;
                var rest=mesh.vertices;var weights=mesh.boneWeights;var tris=mesh.triangles;
                // Welded surface: groups, neighbours, a fixed reference neighbour per group for the tangent frame.
                var weld=new Dictionary<Vector3Int,int>();var map=new int[rest.Length];for(int i=0;i<rest.Length;i++){var key=Vector3Int.RoundToInt(rest[i]*10000);if(!weld.TryGetValue(key,out int g)){g=weld.Count;weld.Add(key,g);}map[i]=g;}
                int count=weld.Count;var adjacency=Enumerable.Range(0,count).Select(_=>new List<int>()).ToArray();
                for(int i=0;i<tris.Length;i+=3)for(int e=0;e<3;e++){int a=map[tris[i+e]],b=map[tris[i+(e+1)%3]];if(a==b)continue;if(!adjacency[a].Contains(b))adjacency[a].Add(b);if(!adjacency[b].Contains(a))adjacency[b].Add(a);}
                // Connected pieces of the surface. Small separate pieces (pauldron ornaments, studs, trims) are moved rigidly by
                // their average correction instead of being relaxed, which would crumple them.
                var component=new int[count];for(int g=0;g<count;g++)component[g]=-1;var componentSize=new List<int>();
                for(int g=0;g<count;g++){if(component[g]>=0)continue;int id2=componentSize.Count;int size=0;var queue=new Queue<int>();queue.Enqueue(g);component[g]=id2;
                    while(queue.Count>0){int q=queue.Dequeue();size++;foreach(int n in adjacency[q])if(component[n]<0){component[n]=id2;queue.Enqueue(n);}}componentSize.Add(size);}
                int smallPiece=Mathf.Max(60,count/150);
                var groupRest=new Vector3[count];for(int i=0;i<rest.Length;i++)groupRest[map[i]]=rest[i];
                double edgeSum=0;int edges=0;for(int g=0;g<count;g++)foreach(int n in adjacency[g]){edgeSum+=Vector3.Distance(groupRest[g],groupRest[n]);edges++;}
                float edge=(float)(edgeSum/edges);int passes=Mathf.Clamp(Mathf.RoundToInt(Mathf.Pow(.14f/edge,2)),8,200);
                var shoulderBind=new[]{(Vector3)bind[5].inverse.GetColumn(3),(Vector3)bind[8].inverse.GetColumn(3)};
                // Rest offsets in the relaxed rest surface's local frames.
                var mask=new bool[count];for(int g=0;g<count;g++)mask[g]=Mathf.Min(Vector3.Distance(groupRest[g],shoulderBind[0]),Vector3.Distance(groupRest[g],shoulderBind[1]))<1.3f;
                var relaxedRest=Relax(groupRest,adjacency,mask,passes);var restFrames=Frames(relaxedRest,tris,map,adjacency,count);
                var offsets=new Vector3[count];for(int g=0;g<count;g++)if(mask[g])offsets[g]=Quaternion.Inverse(restFrames[g])*(groupRest[g]-relaxedRest[g]);
                var shapes=corrective.shapes.ToList();var removeNames=new HashSet<string>();foreach(var s in shapes)removeNames.Add(mesh.GetBlendShapeName(s.index));
                var newShapes=new List<(string name,Vector3[] delta,int side,Vector3 dir)>();var bones=skin.bones;
                float raiseScale=float.TryParse(System.Environment.GetEnvironmentVariable("SANGO3_RAISE_SCALE"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var rsc)?rsc:1f; // experiment switch
                foreach(var pose0 in PoseTable[id]){var pose=pose0;if(id=="guanyu"&&pose.label.StartsWith("Raise")&&raiseScale!=1f)pose=new PoseSpec{label=pose.label,clip=pose.clip,time=pose.time,strength=pose.strength*raiseScale,reach=pose.reach,zero=pose.zero};
                    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(HeroFolder+pose.clip+".anim");clip.SampleAnimation(root,pose.time);if(grip)grip.Solve();SolveDrape();
                    var S=new Matrix4x4[bones.Length];for(int b=0;b<bones.Length;b++)S[b]=root.transform.worldToLocalMatrix*bones[b].localToWorldMatrix*bind[b];
                    var linear=new Vector3[rest.Length];var blended=new Matrix4x4[rest.Length];
                    for(int v=0;v<rest.Length;v++){var w=weights[v];var M=new Matrix4x4();Acc(ref M,S[w.boneIndex0],w.weight0);Acc(ref M,S[w.boneIndex1],w.weight1);Acc(ref M,S[w.boneIndex2],w.weight2);Acc(ref M,S[w.boneIndex3],w.weight3);blended[v]=M;linear[v]=M.MultiplyPoint3x4(rest[v]);}
                    var groupPosed=new Vector3[count];for(int v=0;v<rest.Length;v++)groupPosed[map[v]]=linear[v];
                    var relaxed=Relax(groupPosed,adjacency,mask,passes);var frames=Frames(relaxed,tris,map,adjacency,count);
                    for(int side=0;side<2;side++){
                        var shoulderNow=root.transform.InverseTransformPoint(bones[side==0?5:8].position);
                        var dir=corrective.chest.InverseTransformDirection((bones[side==0?6:9].position-bones[side==0?5:8].position).normalized);
                        if(newShapes.Any(s=>s.side==side&&Vector3.Angle(s.dir,dir)<12)){log.Append(" "+pose.label+(side==0?"R":"L")+"=dup");continue;}
                        if(pose.zero){newShapes.Add(("",null,side,dir));log.Append(" "+pose.label+(side==0?"R":"L")+"=zero");continue;}
                        var delta=new Vector3[rest.Length];int moved=0;float largest=0;
                        for(int v=0;v<rest.Length;v++){int g=map[v];if(!mask[g])continue;
                            float reach=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(pose.reach*.55f,pose.reach,Vector3.Distance(rest[v],shoulderBind[side])));if(reach<=0)continue;
                            // Only where the arm side's bones take part (not the other arm, not a hand that is entirely one bone).
                            var w=weights[v];float arm=Part(w,side==0?new[]{5,6,18}:new[]{8,9,19});if(arm<=.01f)continue;
                            var target=relaxed[g]+frames[g]*offsets[g];
                            // Fine relief (ornaments, scales: far from the relaxed surface) is left as the skinning has it, where the frame rotation would ruffle it.
                            float detail=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.018f,.05f,offsets[g].magnitude));
                            var change=(target-linear[v])*reach*pose.strength*Mathf.Clamp01(arm*4)*detail;
                            float size=change.magnitude;if(size>1e-6f)change*=.30f*(1-Mathf.Exp(-size/.30f))/size; // soft limit
                            // Where the blended skin matrix is nearly singular (opposed bone rotations cancel out), its inverse explodes:
                            // use the world-space change unrotated there instead.
                            var m3=blended[v];float det=m3.m00*(m3.m11*m3.m22-m3.m12*m3.m21)-m3.m01*(m3.m10*m3.m22-m3.m12*m3.m20)+m3.m02*(m3.m10*m3.m21-m3.m11*m3.m20);
                            var d=Mathf.Abs(det)>.35f?blended[v].inverse.MultiplyVector(change):change;if(d.magnitude>.45f)d=d.normalized*.45f;if(float.IsNaN(d.x+d.y+d.z))continue;delta[v]=d;if(d.sqrMagnitude>1e-8f)moved++;largest=Mathf.Max(largest,d.magnitude);}
                        {var sum=new Dictionary<int,Vector3>();var n=new Dictionary<int,int>();
                            for(int v=0;v<rest.Length;v++){int c=component[map[v]];if(componentSize[c]>=smallPiece)continue;sum.TryGetValue(c,out var a);sum[c]=a+delta[v];n.TryGetValue(c,out int k);n[c]=k+1;}
                            for(int v=0;v<rest.Length;v++){int c=component[map[v]];if(componentSize[c]>=smallPiece)continue;delta[v]=sum[c]/n[c];}}
                        newShapes.Add(("Pose"+(side==0?"R":"L")+pose.label,delta,side,dir));log.Append(" "+pose.label+(side==0?"R":"L")+" "+moved+"v max "+largest.ToString("0.000"));}
                }
                // Rebuild the blend shapes: keep the non-corrective ones, drop the generic shoulder shapes, add the posed ones.
                var keep=new List<(string name,Vector3[] d,Vector3[] n,Vector3[] t,float w)>();
                for(int s=0;s<mesh.blendShapeCount;s++){var name=mesh.GetBlendShapeName(s);if(removeNames.Contains(name)||name.StartsWith("Pose"))continue;for(int f=0;f<mesh.GetBlendShapeFrameCount(s);f++){var d=new Vector3[mesh.vertexCount];var n=new Vector3[mesh.vertexCount];var t=new Vector3[mesh.vertexCount];mesh.GetBlendShapeFrameVertices(s,f,d,n,t);keep.Add((name,d,n,t,mesh.GetBlendShapeFrameWeight(s,f)));}}
                mesh.ClearBlendShapes();foreach(var k in keep)mesh.AddBlendShapeFrame(k.name,k.w,k.d,k.n,k.t);
                var list=new List<ShoulderCorrective.Shape>();foreach(var s in newShapes){if(s.delta==null){list.Add(new ShoulderCorrective.Shape{index=-1,side=s.side,direction=s.dir});continue;}mesh.AddBlendShapeFrame(s.name,100,s.delta,null,null);list.Add(new ShoulderCorrective.Shape{index=mesh.GetBlendShapeIndex(s.name),side=s.side,direction=s.dir});}
                EditorUtility.SetDirty(mesh);
                var contents=PrefabUtility.LoadPrefabContents(HeroPrefab);try{contents.GetComponent<ShoulderCorrective>().shapes=list.ToArray();PrefabUtility.SaveAsPrefabAsset(contents,HeroPrefab);}finally{PrefabUtility.UnloadPrefabContents(contents);}
                log.Append(" (relax passes "+passes+", edge "+edge.ToString("0.000")+")");
            }finally{Object.DestroyImmediate(root);}
            return log.ToString();
        }
        static void Acc(ref Matrix4x4 m,Matrix4x4 s,float w){if(w<=0)return;for(int e=0;e<16;e++)m[e]+=s[e]*w;}
        static float Part(BoneWeight w,int[] set){float p=0;if(set.Contains(w.boneIndex0))p+=w.weight0;if(set.Contains(w.boneIndex1))p+=w.weight1;if(set.Contains(w.boneIndex2))p+=w.weight2;if(set.Contains(w.boneIndex3))p+=w.weight3;return p;}
        // Laplacian relaxation of the masked groups; unmasked groups stay where they are.
        static Vector3[] Relax(Vector3[] p,List<int>[] adjacency,bool[] mask,int passes)
        {
            var a=(Vector3[])p.Clone();var b=(Vector3[])p.Clone();
            for(int pass=0;pass<passes;pass++){for(int g=0;g<a.Length;g++){if(!mask[g]||adjacency[g].Count==0){b[g]=a[g];continue;}var sum=Vector3.zero;foreach(int n in adjacency[g])sum+=a[n];b[g]=a[g]*.5f+sum/adjacency[g].Count*.5f;}var t=a;a=b;b=t;}
            return a;
        }
        // Local frame per group: normal from the surrounding triangles, tangent toward the group's first neighbour.
        static Quaternion[] Frames(Vector3[] p,int[] tris,int[] map,List<int>[] adjacency,int count)
        {
            var normals=new Vector3[count];for(int i=0;i<tris.Length;i+=3){int a=map[tris[i]],b=map[tris[i+1]],c=map[tris[i+2]];var n=Vector3.Cross(p[b]-p[a],p[c]-p[a]);normals[a]+=n;normals[b]+=n;normals[c]+=n;}
            var frames=new Quaternion[count];
            for(int g=0;g<count;g++){var n=normals[g];if(n.sqrMagnitude<1e-14f||adjacency[g].Count==0){frames[g]=Quaternion.identity;continue;}n.Normalize();var t=Vector3.zero;var first=Vector3.ProjectOnPlane(p[adjacency[g][0]]-p[g],n).normalized;foreach(int k in adjacency[g]){var d=Vector3.ProjectOnPlane(p[k]-p[g],n).normalized;t+=Vector3.Dot(d,first)>=0?d:-d;}if(t.sqrMagnitude<1e-10f)t=first;if(t.sqrMagnitude<1e-14f){frames[g]=Quaternion.identity;continue;}frames[g]=Quaternion.LookRotation(n,t);}
            return frames;
        }
    }
}
