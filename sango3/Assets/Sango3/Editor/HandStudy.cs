using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Sango3.Editor
{
    public static class HandStudy
    {
        public static void RawGuan()=>RawGuanSource(false);
        public static void RawHighGuan()=>RawGuanSource(true);
        static void RawGuanSource(bool high){
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var source=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(high?"Assets/Sango3/Characters/Studies/guanyu-high-hands.obj":"Assets/Sango3/Characters/Recovered/guanyu.obj"));var renderer=source.GetComponentInChildren<Renderer>();var bounds=renderer.bounds;if(!high){float scale=4.4f/bounds.size.y;source.transform.localScale*=scale;source.transform.position=new Vector3(-bounds.center.x*scale,-bounds.min.y*scale,-bounds.center.z*scale);}
            renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Sango3/Characters/Recovered/Processed/guanyu.mat");var light=new GameObject("key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(30,-25,0);RenderSettings.ambientLight=Color.gray;
            if(high){renderer.sharedMaterial=new Material(renderer.sharedMaterial);renderer.sharedMaterial.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sango3/Characters/Studies/guanyu-high-albedo.jpg");}
            var target=new Vector3(-1.32f,2.35f,.15f);for(int side=0;side<2;side++){
                var cam=new GameObject("Raw hand").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=.55f;cam.transform.position=target+new Vector3(0,0,side==0?3:-3);cam.transform.LookAt(target);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.05f,.07f,.09f);
                var rt=new RenderTexture(768,768,24);cam.targetTexture=rt;cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(768,768,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,768,768),0,0);tex.Apply();File.WriteAllBytes("QA/guanyu-"+(high?"high":"raw")+"-hand-"+side+".png",tex.EncodeToPNG());RenderTexture.active=previous;cam.targetTexture=null;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);Object.DestroyImmediate(cam.gameObject);
            }Object.DestroyImmediate(source);Object.DestroyImmediate(light.gameObject);
        }
        public static void Review(){Run();RecoveredHeroAcceptance.Run();}
        public static void Run(){
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            HeroLighting.Apply();
            foreach(string id in new[]{"liubei","guanyu","zhugeliang"}){
                var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sango3/Resources/Heroes/Final/"+id+".prefab"));
                var skin=root.GetComponent<SkinnedMeshRenderer>();
                foreach(string state in new[]{"rest","idle","cast"}){bool posed=state!="rest";
                    if(posed){var animator=root.GetComponent<Animator>();animator.Rebind();animator.Play(state=="cast"?"Cast":"Idle",0,0);animator.Update(.25f);var grip=root.GetComponent<RecoveredPolearmGrip>();if(grip)grip.Solve();var drape=root.GetComponent<RecoveredSleeveDrape>();if(drape)drape.Solve();}
                    var mesh=new Mesh();skin.BakeMesh(mesh);var visible=new GameObject("Baked",typeof(MeshFilter),typeof(MeshRenderer));visible.GetComponent<MeshFilter>().sharedMesh=mesh;visible.GetComponent<Renderer>().sharedMaterial=skin.sharedMaterial;skin.enabled=false;
                    var wrist=skin.bones[7].position;var light=new GameObject("key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(30,-25,0);RenderSettings.ambientLight=Color.gray;
                    for(int side=0;side<3;side++){
                        var cam=new GameObject("Study").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=.48f;cam.nearClipPlane=.01f;cam.backgroundColor=new Color(.05f,.07f,.09f);cam.clearFlags=CameraClearFlags.SolidColor;
                        var target=wrist+Vector3.down*.12f;cam.transform.position=target+(side==0?new Vector3(0,.1f,3):side==1?new Vector3(-3,.2f,1):new Vector3(0,.2f,-3));cam.transform.LookAt(target);
                        var rt=new RenderTexture(768,768,24);cam.targetTexture=rt;cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(768,768,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,768,768),0,0);tex.Apply();File.WriteAllBytes("QA/hand-"+id+"-"+state+"-"+side+".png",tex.EncodeToPNG());RenderTexture.active=previous;cam.targetTexture=null;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);Object.DestroyImmediate(cam.gameObject);
                    }
                    Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(visible);Object.DestroyImmediate(mesh);skin.enabled=true;
                }Object.DestroyImmediate(root);
            }Debug.Log("HAND_STUDY_READY");
        }
    }
}
