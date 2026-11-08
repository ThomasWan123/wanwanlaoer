using Unity.Profiling;
using UnityEngine;
namespace Sango3
{
    // Development builds only; measures delivered Update cadence, not GPU timing.
    public class FrameDiagnostics : MonoBehaviour
    {
        float previousRealTime=-1;float elapsed,maxDelta;int frames;string phase;BattleController battle;
        float backgroundSince=-1;
        // Hitch attribution: on a long frame, log which engine and script markers took the time (development builds
        // record profiler markers). Values are for the previous frame, the one that produced the long delivered delta.
        static readonly string[] Markers={"Main Thread","PlayerLoop","Update.ScriptRunBehaviourUpdate","PreLateUpdate.ScriptRunBehaviourLateUpdate","Update.ScriptRunDelayedDynamicFrameRate","Gfx.WaitForPresentOnGfxThread","Gfx.WaitForGfxCommandsFromMainThread","Shader.CreateGPUProgram","Shader.Parse","GC.Collect","Loading.ReadObject","Loading.AwakeFromLoad","Camera.Render","RenderPipelineManager.DoRenderLoop_Internal()","Canvas.SendWillRenderCanvases","Canvas.BuildBatch","UGUI.Rendering.UpdateBatches","Mesh.UploadMeshData","Texture.AwakeFromLoad","Graphics.PresentAndSync","Application.Integrate Assets in Background","WaitForTargetFPS"};
        ProfilerRecorder[] recorders;bool previousStartup=true;
        static readonly ProfilerCategory[] Categories={ProfilerCategory.Internal,ProfilerCategory.Render,ProfilerCategory.Scripts,ProfilerCategory.Loading,ProfilerCategory.Gui,ProfilerCategory.Memory,ProfilerCategory.Animation};
        void OnEnable(){recorders=new ProfilerRecorder[Markers.Length];for(int i=0;i<Markers.Length;i++)foreach(var c in Categories){var r=ProfilerRecorder.StartNew(c,Markers[i],1);if(r.Valid){recorders[i]=r;break;}r.Dispose();}}
        void OnDisable(){if(recorders!=null)foreach(var r in recorders)r.Dispose();}
        string Attribution(){var sb=new System.Text.StringBuilder();for(int i=0;i<recorders.Length;i++){if(!recorders[i].Valid)continue;double ms=recorders[i].LastValue/1e6;if(ms>=2)sb.Append(' ').Append(Markers[i].Replace(' ','_')).Append('=').Append(ms.ToString("F1"));}return sb.ToString();}
        // Time spent suspended is not a rendered frame: log it as a background interval and restart the cadence window.
        void OnApplicationPause(bool suspended){if(suspended)Suspend();else Resume();}
        void OnApplicationFocus(bool focused){if(!focused)Suspend();else Resume();}
        void Suspend(){if(backgroundSince<0)backgroundSince=Time.realtimeSinceStartup;}
        void Resume(){if(backgroundSince<0)return;Debug.Log($"SANGO3_BACKGROUND phase={phase} seconds={Time.realtimeSinceStartup-backgroundSince:F2}");backgroundSince=-1;if(frames>0)Debug.Log($"SANGO3_FRAME_SAMPLE phase={phase} fps={frames/Mathf.Max(elapsed,.001f):F1} maxFrameMs={maxDelta*1000:F1} size={Screen.width}x{Screen.height} closedBy=background");elapsed=0;frames=0;maxDelta=0;previousRealTime=Time.realtimeSinceStartup;skipNext=true;}
        bool skipNext;
        void Update()
        {
            if(skipNext){skipNext=false;previousRealTime=Time.realtimeSinceStartup;return;}
            float now=Time.realtimeSinceStartup;if(previousRealTime<0){Debug.Log($"SANGO3_FIRST_UPDATE frame={Time.frameCount} engineDeltaMs={Time.unscaledDeltaTime*1000:F1} realtime={now:F2}");previousRealTime=now;return;}float deliveredDelta=now-previousRealTime;previousRealTime=now;
            if(!battle)battle=GetComponent<BattleController>();string current=!battle||!battle.Ready?"loading":battle.Finished?"finished":battle.Paused?"paused":battle.Started?"combat":"deployment";if(current!=phase){if(frames>0)Debug.Log($"SANGO3_PHASE phase={phase} frames={frames} seconds={elapsed:F2} maxFrameMs={maxDelta*1000:F1}");phase=current;elapsed=0;frames=0;maxDelta=0;}
            // Frame 2 is the app's first presented frame after the scene loads; it is long even when frame 1 runs no game
            // code at all (0.16.0 experiment), so it is reported separately from the loading statistics.
            if(Time.frameCount<=2&&previousStartup){previousStartup=false;Debug.Log($"SANGO3_STARTUP firstPresentMs={deliveredDelta*1000:F1} |{Attribution()}");return;}
            if(deliveredDelta>.040f)Debug.Log($"SANGO3_HITCH phase={phase} frame={Time.frameCount} ms={deliveredDelta*1000:F1} wave={(battle?battle.Wave:0)} |{Attribution()}");
            elapsed+=deliveredDelta;frames++;maxDelta=Mathf.Max(maxDelta,deliveredDelta);
            if(elapsed>=10){Debug.Log($"SANGO3_FRAME_SAMPLE phase={phase} fps={frames/elapsed:F1} maxFrameMs={maxDelta*1000:F1} size={Screen.width}x{Screen.height}");elapsed=0;frames=0;maxDelta=0;}
        }
    }
}
