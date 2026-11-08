using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Sango3.Editor
{
    public static class RecoveredHeroAcceptance
    {
        public static void Run()
        {
            var report=new StringBuilder();int checkedCount=0;int failures=0;
            foreach(var path in Directory.GetFiles("Assets/Sango3/Resources/Heroes/Final","*.prefab")){
                GameObject instance=null;
                try{
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);instance=Object.Instantiate(prefab);
                    var animator=instance.GetComponent<Animator>();var skin=instance.GetComponent<SkinnedMeshRenderer>();var grip=instance.GetComponent<RecoveredPolearmGrip>();
                    if(!animator||!animator.runtimeAnimatorController||!skin||!skin.sharedMesh||skin.sharedMesh.vertexCount<100||skin.bones.Any(b=>!b))throw new Exception("Missing rig references");
                    foreach(var r in instance.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)if(!m||!m.shader||!m.shader.isSupported)throw new Exception("Missing or unsupported material");
                    foreach(var w in skin.sharedMesh.boneWeights)if(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.001f)throw new Exception("Unnormalized skin weights");
                    var hands=instance.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r!=skin).ToArray();
                    foreach(var h in hands){if(!h.sharedMesh||h.sharedMesh.vertexCount<100||h.bones.Length<16||h.bones.Any(b=>!b)||h.sharedMesh.bindposes.Length!=h.bones.Length)throw new Exception("Incomplete articulated hand");foreach(var w in h.sharedMesh.boneWeights)if(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.001f||new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3}.Any(i=>i<0||i>=h.bones.Length))throw new Exception("Invalid articulated hand weights");}
                    var rest=skin.sharedMesh.vertices;var tris=skin.sharedMesh.triangles;float largestEdge=0,largestGripError=0,largestChange=0;int badEdges=0;
                    // A hero with its own move set (Guan Yu's sample) is checked on the clips it actually plays; its support
                    // hand deliberately lets go when the shaft is out of reach, so grip contact is measured while it holds.
                    bool moveSet=animator.HasState(0,Animator.StringToHash("Attack1")); /* every hero with its own move set, grip or not */string castState=moveSet?"Ultimate":"Cast";int released=0,sampled=0;
                    foreach(string state in moveSet?new[]{"Idle","Walk","Attack1","Attack2","Attack3","Ultimate"}:new[]{"Idle","Walk","Attack","Cast"}){
                        if(grip)grip.keepRightHand=moveSet&&state!="Idle"&&state!="Walk";RecoveredPolearmGrip.StepSeconds=.05f;if(grip)grip.ResetContinuity();animator.Rebind();animator.speed=1;animator.Play(state,0,0);animator.Update(0);var start=skin.bones.Select(b=>b.localRotation).ToArray();
                        for(int frame=0;frame<(moveSet?32:24);frame++){
                            animator.Update(.05f);if(grip)grip.Solve();if(instance.GetComponent<RecoveredSleeveDrape>())instance.GetComponent<RecoveredSleeveDrape>().Solve();var baked=new Mesh();skin.BakeMesh(baked);var vertices=baked.vertices;
                            if(vertices.Any(v=>!Finite(v.x)||!Finite(v.y)||!Finite(v.z)))throw new Exception("Invalid skin coordinates");
                            if(baked.bounds.size.magnitude>15)throw new Exception("Exploding mesh bounds");
                            foreach(var h in hands){var handMesh=new Mesh();h.BakeMesh(handMesh);bool invalid=handMesh.vertices.Any(v=>!Finite(v.x)||!Finite(v.y)||!Finite(v.z))||handMesh.bounds.size.magnitude>1.6f;Object.DestroyImmediate(handMesh);if(invalid)throw new Exception("Invalid articulated hand deformation");}
                            for(int i=0;i<tris.Length;i+=3)for(int e=0;e<3;e++){int a=tris[i+e],b=tris[i+(e+1)%3];float d=Vector3.Distance(vertices[a],vertices[b]);if(Vector3.Distance(rest[a],rest[b])<.12f){largestEdge=Mathf.Max(largestEdge,d);if(d>.8f)badEdges++;}}
                            if(grip){sampled++;if(moveSet&&(grip.SupportGap>.02f||grip.Release>.01f))released++;else largestGripError=Mathf.Max(largestGripError,Vector3.Distance(grip.hand.TransformPoint(grip.palmOffset),grip.PalmTarget));}
                            if(state==castState)for(int i=0;i<skin.bones.Length;i++)largestChange=Mathf.Max(largestChange,Quaternion.Angle(start[i],skin.bones[i].localRotation));
                            Object.DestroyImmediate(baked);
                        }
                    }
                    if(badEdges>0)throw new Exception("Excessive mesh stretching: "+badEdges);if(largestGripError>.06f)throw new Exception("Polearm grip lost contact: "+largestGripError);
                    if(largestChange<5)throw new Exception("Cast does not animate bones");
                    animator.speed=0;var paused=skin.bones.Select(b=>b.localRotation).ToArray();animator.Update(.25f);if(grip)grip.Solve();if(instance.GetComponent<RecoveredSleeveDrape>())instance.GetComponent<RecoveredSleeveDrape>().Solve();var stable=skin.bones.Select(b=>b.localRotation).ToArray();animator.Update(.25f);if(grip)grip.Solve();if(instance.GetComponent<RecoveredSleeveDrape>())instance.GetComponent<RecoveredSleeveDrape>().Solve();for(int i=0;i<stable.Length;i++)if(Quaternion.Angle(stable[i],skin.bones[i].localRotation)>.05f)throw new Exception("Paused rig drifts");
                    report.AppendLine("PASS "+Path.GetFileNameWithoutExtension(path)+" bones="+skin.bones.Length+" articulatedHands="+hands.Length+" castDegrees="+largestChange.ToString("F1")+" maxShortEdge="+largestEdge.ToString("F3")+" stretchedSamples="+badEdges+" gripAnchorError="+(grip?largestGripError.ToString("F3"):"N/A")+" visualHandAcceptance=NOT_TESTED"+(moveSet?" supportHandReleased="+released+"/"+sampled:""));checkedCount++;
                }catch(Exception ex){report.AppendLine("FAIL "+path+" "+ex.Message);failures++;}finally{if(instance)Object.DestroyImmediate(instance);}
            }
            File.WriteAllText("QA/roster-acceptance-100.txt",report.ToString());// The 12 heroes plus the stage 16 spearman, by name: a missing rig fails the check instead of passing on a count.
            var expected=new[]{"caocao","guanyu","liubei","lvbu","simayi","sunquan","xiahoudun","yuansoldier","zhangfei","zhangliao","zhaoyun","zhouyu","zhugeliang"};
            var seen=Directory.GetFiles("Assets/Sango3/Resources/Heroes/Final","*.prefab").Select(Path.GetFileNameWithoutExtension).ToArray();var missing=expected.Except(seen).ToArray();
            if(failures>0||missing.Length>0||checkedCount!=expected.Length)throw new Exception("Roster acceptance failed: "+failures+"; passed="+checkedCount+"; missing="+string.Join(",",missing));
            Debug.Log("ROSTER_ANIMATION_ACCEPTANCE_PASS "+checkedCount);
        }
        static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
    }
}
