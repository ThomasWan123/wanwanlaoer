using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Sango3.Editor
{
    public static class Review101
    {
        public static void Run(){Capture();RecoveredHeroAcceptance.Run();File.Copy("QA/roster-acceptance-100.txt","QA/roster-101.txt",true);AndroidBuilder.Build();}
        static void Capture(){
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);HeroLighting.Apply();
            var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sango3/Resources/Heroes/Final/guanyu.prefab"));
            var skin=root.GetComponent<SkinnedMeshRenderer>();var current=skin.sharedMesh;var baseline=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Sango3/Characters/Studies/ShoulderBaseline.asset");
            var light=new GameObject("Review key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(35,-30,0);RenderSettings.ambientLight=Color.gray;
            var cam=new GameObject("Review camera").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=.85f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.08f,.10f,.14f);
            foreach(var state in new[]{"Idle","Cast"}){
                var animator=root.GetComponent<Animator>();animator.Rebind();animator.Play(state,0,0);animator.Update(.25f);root.GetComponent<RecoveredPolearmGrip>().Solve();
                var target=skin.bones[5].position;cam.transform.position=target+new Vector3(-1,.4f,3);cam.transform.LookAt(target);
                foreach(bool before in new[]{true,false}){
                    skin.sharedMesh=before?baseline:current;var mesh=new Mesh();skin.BakeMesh(mesh);skin.enabled=false;
                    var view=new GameObject("Evaluated shoulder",typeof(MeshFilter),typeof(MeshRenderer));view.GetComponent<MeshFilter>().sharedMesh=mesh;view.GetComponent<Renderer>().sharedMaterial=skin.sharedMaterial;
                    var rt=new RenderTexture(768,768,24);cam.targetTexture=rt;cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(768,768,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,768,768),0,0);tex.Apply();File.WriteAllBytes("QA/shoulder-101-"+state+(before?"-before":"-after")+".png",tex.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(view);Object.DestroyImmediate(mesh);skin.enabled=true;
                }
            }
            Object.DestroyImmediate(root);Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(cam.gameObject);
        }
    }
}
