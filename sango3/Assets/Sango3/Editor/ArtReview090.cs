using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Sango3.Editor
{
    public static class ArtReview090
    {
        public static void Run(){HistoricalArchitectureBuilder.Build();RecoveredHeroBuilder.RebuildLiu();RecoveredHeroBuilder.RefineRestPoses();RecoveredHeroAcceptance.Run();Capture();}
        public static void Capture(){
            Campaign.BeginTest("sango3.qa.art090.");
            try{foreach(int index in new[]{0,3,13,17,24,27,29}){
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Campaign.SelectTest(index);WorldArt.ResetMaterialCache();HeroArt.ResetMaterialCache();
                var sun=new GameObject("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.shadows=LightShadows.Soft;
                var camera=new GameObject("Camera").AddComponent<Camera>();camera.tag="MainCamera";ScenicArt.ConfigureCamera(camera);WorldArt.Landscape(new GameObject("Battlefield").transform);
                ScenarioValidation.Capture("QA/environment-"+(index+1).ToString("00")+"-090.png");
                foreach(var r in UnityEngine.Object.FindObjectsOfType<Renderer>())foreach(var m in r.sharedMaterials)if(!m||!m.shader||!m.shader.isSupported)throw new Exception("Invalid environment material "+r.name);
                var vertices=0;var seen=new System.Collections.Generic.HashSet<Mesh>();foreach(var f in UnityEngine.Object.FindObjectsOfType<MeshFilter>())if(seen.Add(f.sharedMesh))vertices+=f.sharedMesh.vertexCount;File.AppendAllText("QA/architecture-090.txt","PASS stage="+(index+1)+" uniqueMeshVertices="+vertices+" materials valid\n");
            }}finally{Campaign.EndTest();}Debug.Log("ART_090_RENDER_REVIEW_READY");
        }
    }
}
