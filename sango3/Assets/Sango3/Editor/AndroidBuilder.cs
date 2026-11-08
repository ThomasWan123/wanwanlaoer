using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace Sango3.Editor
{
    public static class AndroidBuilder
    {
        public static void BuildReview(){RecoveredHeroBuilder.RenderAccepted();Build();}
        public static void Build()
        {
            CharacterBuilder.Build();NatureBuilder.Prepare();
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel35;
            PlayerSettings.bundleVersion="0.28.0";PlayerSettings.Android.bundleVersionCode=30;
            EditorUserBuildSettings.buildAppBundle=false;
            // Graphics API: SANGO3_GFX=vulkan|auto for comparison builds (0.19.0 experiment).
            var gfx=Environment.GetEnvironmentVariable("SANGO3_GFX");
            if(gfx=="vulkan"){PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.Vulkan});}
            else if(gfx=="auto")PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,true);
            // Default: OpenGL ES 3 only. On the SM-F9660 its first presented frame was 56-67 ms (7 launches) against
            // 72-92 ms with the automatic choice (Vulkan); see Review-0.19.0.
            else{PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});}
            Directory.CreateDirectory("Builds");Directory.CreateDirectory("Assets/Sango3/Resources");
            if(!AssetDatabase.LoadAssetAtPath<Material>("Assets/Sango3/Resources/BattleLit.mat"))AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Lit")),"Assets/Sango3/Resources/BattleLit.mat");
            var pipeline=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Sango3/Generated/Sango3Pipeline.asset");
            var pipelineSettings=new SerializedObject(pipeline);pipelineSettings.FindProperty("m_SoftShadowsSupported").boolValue=true;pipelineSettings.ApplyModifiedPropertiesWithoutUndo();pipeline.shadowDistance=65;pipeline.shadowCascadeCount=2;EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Sango3/Generated/Yingchuan.unity"},locationPathName=Environment.GetEnvironmentVariable("SANGO3_APK")??"Builds/sango3-0.28.0.apk",target=BuildTarget.Android,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Android build failed: "+report.summary.result);
            Debug.Log("SANGO3_ANDROID_READY "+report.summary.totalSize);
        }
    }
}
