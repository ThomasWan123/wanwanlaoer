using UnityEngine;
namespace Sango3
{
    // Development builds only: a file review-stage.txt holding a stage number in the app's files folder
    // (adb push it to /sdcard/Android/data/com.wanwan.sango3.prototype/files/) opens that stage once for review, using a separate test save so the player's own progress is untouched.
    public static class ReviewLaunch
    {
        static bool done;
#if DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Open()
        {
            if(done||!Debug.isDebugBuild||Application.isEditor)return;done=true;
            var path=System.IO.Path.Combine(Application.persistentDataPath,"review-stage.txt");if(!System.IO.File.Exists(path))return;
            string value=System.IO.File.ReadAllText(path).Trim();System.IO.File.Delete(path);
            // "16 profile": also record a binary profiler log of the first 290 frames (startup-profile.raw in the same folder),
            // analysed in the editor by Editor/StartupProfile.cs.
            var parts=value.Split(' ');value=parts[0];if(System.Array.IndexOf(parts,"profile")>0)StartupProfile.Begin();
            // A/B switches for performance checks: "noground", "noridges", "nocorrective".
            ScenarioScenery.SkipGroundVariation=System.Array.IndexOf(parts,"noground")>0;ScenarioScenery.SkipRidges=System.Array.IndexOf(parts,"noridges")>0;ShoulderCorrective.Disabled=System.Array.IndexOf(parts,"nocorrective")>0;
            Debug.Log("SANGO3_REVIEW_OPTIONS "+string.Join(",",parts));
            if(!int.TryParse(value,out int stage)||stage<1||stage>Campaign.Count)return;
            Campaign.BeginTest("sango3.review.");Campaign.SelectTest(stage-1);Debug.Log("SANGO3_REVIEW_LAUNCH stage="+stage);
        }
#endif
    }
    public sealed class StartupProfile:MonoBehaviour
    {
        public static void Begin()
        {
            UnityEngine.Profiling.Profiler.logFile=System.IO.Path.Combine(Application.persistentDataPath,"startup-profile");UnityEngine.Profiling.Profiler.maxUsedMemory=256*1024*1024;
            UnityEngine.Profiling.Profiler.enableBinaryLog=true;UnityEngine.Profiling.Profiler.enabled=true;
            var go=new GameObject("Startup profile");DontDestroyOnLoad(go);go.AddComponent<StartupProfile>();Debug.Log("SANGO3_PROFILE_START");
        }
        void Update(){if(Time.frameCount<290)return; // the editor keeps 300 frames of a loaded capture
           UnityEngine.Profiling.Profiler.enabled=false;UnityEngine.Profiling.Profiler.enableBinaryLog=false;UnityEngine.Profiling.Profiler.logFile="";Debug.Log("SANGO3_PROFILE_DONE frames="+Time.frameCount);Destroy(gameObject);}
    }
}
