using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Sango3.Editor
{
    public static class VisualReview110
    {
        public static void Run()
        {
            var report = new StringBuilder();
            Campaign.BeginTest("sango3.qa.visual110.");
            try
            {
                for (int index = 0; index < Campaign.Count; index++)
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    Campaign.SelectTest(index);
                    WorldArt.ResetMaterialCache(); HeroArt.ResetMaterialCache();
                    var sun = new GameObject("Sun").AddComponent<Light>();
                    sun.type = LightType.Directional; sun.shadows = LightShadows.Soft;
                    var camera = new GameObject("Camera").AddComponent<Camera>();
                    camera.tag = "MainCamera"; ScenicArt.ConfigureCamera(camera);
                    WorldArt.Landscape(new GameObject("Battlefield").transform);
                    foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>())
                        foreach (var m in r.sharedMaterials)
                            if (!m || !m.shader || !m.shader.isSupported)
                                throw new Exception("Invalid material stage=" + (index + 1) + " renderer=" + r.name);
                    ScenarioValidation.Capture("QA/review-110/environment-" + (index + 1).ToString("00") + ".png");
                    report.AppendLine("CAPTURE " + (index + 1).ToString("00") + " " + Campaign.Current.Name + " material references valid; visual acceptance requires inspection");
                }
            }
            finally { Campaign.EndTest(); File.WriteAllText("QA/review-110/environments.txt", report.ToString()); }
            Debug.Log("REVIEW_30_ENVIRONMENTS_CAPTURED");
        }
    }
}
