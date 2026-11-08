using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Sango3.Editor
{
    public static class ArticulatedHandStudy
    {
        const string Source="Assets/Sango3/Characters/Source/Superhero_Male_FullBody.fbx";
        public static void Run(){
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var importer=(ModelImporter)AssetImporter.GetAtPath(Source);if(!importer.isReadable){importer.isReadable=true;importer.SaveAndReimport();}
            var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source));var animator=root.GetComponent<Animator>();if(!animator||!animator.avatar||!animator.avatar.isHuman)throw new Exception("Source requires a valid human avatar");
            var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);var middle=animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);var index=animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);var little=animator.GetBoneTransform(HumanBodyBones.RightLittleProximal);if(!hand||!middle||!index||!little)throw new Exception("Missing articulated fingers");
            var renderer=root.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name=="SuperHero_Male");foreach(var r in root.GetComponentsInChildren<Renderer>())r.enabled=r==renderer;
            var mesh=UnityEngine.Object.Instantiate(renderer.sharedMesh);var boneIds=new HashSet<int>();for(int i=0;i<renderer.bones.Length;i++)if(renderer.bones[i]==hand||renderer.bones[i].IsChildOf(hand))boneIds.Add(i);
            var weights=mesh.boneWeights;bool[] keep=weights.Select(w=>(boneIds.Contains(w.boneIndex0)?w.weight0:0)+(boneIds.Contains(w.boneIndex1)?w.weight1:0)+(boneIds.Contains(w.boneIndex2)?w.weight2:0)+(boneIds.Contains(w.boneIndex3)?w.weight3:0)>.65f).ToArray();
            for(int sub=0;sub<mesh.subMeshCount;sub++){var source=mesh.GetTriangles(sub);var filtered=new List<int>();for(int i=0;i<source.Length;i+=3)if(keep[source[i]]&&keep[source[i+1]]&&keep[source[i+2]]){filtered.Add(source[i]);filtered.Add(source[i+1]);filtered.Add(source[i+2]);}mesh.SetTriangles(filtered,sub);}
            renderer.sharedMesh=mesh;var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sango3/Characters/Source/BaseTextures/T_Superhero_Male_Dark.png");material.SetFloat("_Smoothness",.30f);renderer.sharedMaterials=Enumerable.Repeat(material,mesh.subMeshCount).ToArray();
            HeroLighting.Apply();var light=new GameObject("Hand key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(30,-30,0);RenderSettings.ambientLight=new Color(.5f,.5f,.5f);
            var direction=(middle.position-hand.position).normalized;var side=(index.position-little.position).normalized;var normal=Vector3.Cross(direction,side).normalized;var handler=new HumanPoseHandler(animator.avatar,root.transform);var pose=new HumanPose();handler.GetHumanPose(ref pose);var original=(float[])pose.muscles.Clone();
            var names=HumanTrait.MuscleName;File.WriteAllLines("QA/finger-muscles-100.txt",names.Where(n=>n.Contains("Right")&&(n.Contains("Thumb")||n.Contains("Index")||n.Contains("Middle")||n.Contains("Ring")||n.Contains("Little"))));
            for(int state=0;state<3;state++){
                pose.muscles=(float[])original.Clone();for(int i=0;i<names.Length;i++)if(names[i].Contains("Right")&&(names[i].Contains("Thumb")||names[i].Contains("Index")||names[i].Contains("Middle")||names[i].Contains("Ring")||names[i].Contains("Little")))pose.muscles[i]=names[i].Contains("Spread")?0:state==0?.6f:state==1?-.5f:-1f;
                handler.SetHumanPose(ref pose);var baked=new Mesh();renderer.BakeMesh(baked);var display=new GameObject("Baked hand",typeof(MeshFilter),typeof(MeshRenderer));display.transform.SetPositionAndRotation(renderer.transform.position,renderer.transform.rotation);display.transform.localScale=renderer.transform.lossyScale;display.GetComponent<MeshFilter>().sharedMesh=baked;display.GetComponent<Renderer>().sharedMaterials=renderer.sharedMaterials;renderer.enabled=false;
                for(int view=0;view<2;view++){var target=hand.position+direction*.075f;var cam=new GameObject("Hand camera").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=.16f;cam.nearClipPlane=.01f;cam.transform.position=target+normal*(view==0?.8f:-.8f)+side*.22f;cam.transform.LookAt(target,direction);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.05f,.07f,.09f);
                    var rt=new RenderTexture(512,512,24);cam.targetTexture=rt;cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(512,512,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,512,512),0,0);tex.Apply();File.WriteAllBytes("QA/articulated-hand-"+state+"-"+view+".png",tex.EncodeToPNG());RenderTexture.active=previous;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cam.gameObject);
                }UnityEngine.Object.DestroyImmediate(display);UnityEngine.Object.DestroyImmediate(baked);renderer.enabled=true;
            }
            handler.Dispose();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(light.gameObject);Debug.Log("ARTICULATED_HAND_STUDY_READY");
        }
    }
}
