using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Sango3.Editor
{
    public static class Validation080
    {
        public static void CaptureEnvironments(){
            Campaign.BeginTest("sango3.qa.visual080.");
            try{foreach(int index in new[]{3,13,24,27}){
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Campaign.SelectTest(index);WorldArt.ResetMaterialCache();HeroArt.ResetMaterialCache();
                var sun=new GameObject("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.shadows=LightShadows.Soft;
                var camera=new GameObject("Camera").AddComponent<Camera>();camera.tag="MainCamera";ScenicArt.ConfigureCamera(camera);WorldArt.Landscape(new GameObject("Battlefield").transform);
                ScenarioValidation.Capture("QA/environment-"+(index+1).ToString("00")+"-080.png");
            }}finally{Campaign.EndTest();}
        }
        public static void Run(){
            ScenarioMechanicsCheck.Run();
            foreach(var path in Directory.GetFiles("Assets/Sango3/Resources/Heroes/Final","*.prefab")){
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach(var r in prefab.GetComponentsInChildren<MeshRenderer>())if(r.name.Contains("blade")||r.name.Contains("point")||r.name.Contains("jian")||r.name=="Feather vane"){
                    r.sharedMaterial.SetFloat("_Metallic",r.name=="Feather vane"?0:.35f);r.sharedMaterial.SetFloat("_Smoothness",r.name=="Feather vane"?.18f:.58f);EditorUtility.SetDirty(r.sharedMaterial);
                }
            }
            AssetDatabase.SaveAssets();RecoveredHeroBuilder.RenderAccepted();
            CaptureEnvironments();
            PlayCheck.Run();
        }
    }
}
