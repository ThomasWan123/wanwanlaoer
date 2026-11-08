using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Sango3.Editor {
 public static class RawCompare101 {
  public static void Run(){
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);HeroLighting.Apply();
   var light=new GameObject("Key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(35,-30,0);RenderSettings.ambientLight=Color.gray;
   var cam=new GameObject("Raw comparison").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=.75f;cam.transform.position=new Vector3(0,3.7f,5);cam.transform.LookAt(new Vector3(0,3.7f,0));cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.08f,.10f,.14f);
   foreach(string variant in new[]{"packed","rebuilt","high"}){bool high=variant!="packed";
    var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(variant=="rebuilt"?"Assets/Sango3/Characters/Studies/guanyu-rebuilt.obj":high?"Assets/Sango3/Characters/Studies/guanyu-high-upper.obj":"Assets/Sango3/Characters/Recovered/guanyu.obj"));var renderer=root.GetComponentInChildren<Renderer>();
    if(!high){var bounds=renderer.bounds;float scale=4.4f/bounds.size.y;root.transform.localScale*=scale;root.transform.position=new Vector3(-bounds.center.x*scale,-bounds.min.y*scale,-bounds.center.z*scale);}
    var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sango3/Characters/Recovered/guanyu-albedo.jpg");material.SetFloat("_Smoothness",.25f);material.SetFloat("_Cull",0);renderer.sharedMaterial=material;
    var rt=new RenderTexture(768,768,24);cam.targetTexture=rt;cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(768,768,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,768,768),0,0);tex.Apply();File.WriteAllBytes("QA/raw-101-"+variant+".png",tex.EncodeToPNG());RenderTexture.active=previous;cam.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(root);Object.DestroyImmediate(material);
   }Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(light.gameObject);Debug.Log("RAW_COMPARISON_READY");
  }
 }
}
