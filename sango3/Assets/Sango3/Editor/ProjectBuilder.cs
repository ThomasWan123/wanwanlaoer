using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Sango3.Editor
{
    public static class ProjectBuilder
    {
        [MenuItem("Sango3/Play Yingchuan")]
        public static void Open()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene("Assets/Sango3/Generated/Yingchuan.unity");
            EditorApplication.isPlaying=true;
        }
        public static void Create()
        {
            Directory.CreateDirectory("Assets/Sango3/Generated");
            var renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,"Assets/Sango3/Generated/ForwardRenderer.asset");
            var pipeline=ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var so=new SerializedObject(pipeline);var list=so.FindProperty("m_RendererDataList");list.arraySize=1;list.GetArrayElementAtIndex(0).objectReferenceValue=renderer;so.ApplyModifiedPropertiesWithoutUndo();
            pipeline.supportsHDR=false;pipeline.msaaSampleCount=2;pipeline.shadowDistance=65;
            AssetDatabase.CreateAsset(pipeline,"Assets/Sango3/Generated/Sango3Pipeline.asset");GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener),typeof(UniversalAdditionalCameraData));camera.tag="MainCamera";
            camera.transform.position=new Vector3(0,30,-25);camera.transform.LookAt(new Vector3(0,0,2));var cam=camera.GetComponent<Camera>();cam.orthographic=true;cam.orthographicSize=16;cam.backgroundColor=new Color(.44f,.56f,.57f);cam.clearFlags=CameraClearFlags.SolidColor;cam.farClipPlane=180;
            var light=new GameObject("Sun",typeof(Light));light.transform.rotation=Quaternion.Euler(48,-32,0);light.GetComponent<Light>().type=LightType.Directional;light.GetComponent<Light>().intensity=1.15f;light.GetComponent<Light>().shadows=LightShadows.Soft;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.62f,.65f,.63f);RenderSettings.fog=true;RenderSettings.fogColor=cam.backgroundColor;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=50;RenderSettings.fogEndDistance=110;
            new GameObject("Battle",typeof(BattleController));
            PlayerSettings.companyName="Wanwan Studio";PlayerSettings.productName="三国守城 · Sango3";PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"com.wanwan.sango3.prototype");PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.runInBackground=true;
            EditorSceneManager.SaveScene(scene,"Assets/Sango3/Generated/Yingchuan.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Sango3/Generated/Yingchuan.unity",true)};AssetDatabase.SaveAssets();
            if(BattleRules.Names.Length!=6||BattleRules.Slots.Length!=8)throw new Exception("Battle data invalid");
            Debug.Log("SANGO3_READY: 3D pipeline, Yingchuan scene, six heroes, eight deployment slots.");
        }
    }
}
