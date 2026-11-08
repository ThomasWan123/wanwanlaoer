using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Profiling;
using UnityEngine;
namespace Sango3.Editor
{
    // Reads a binary profiler log recorded on the phone (StartupProfile, "16 profile" in review-stage.txt) and lists,
    // for every frame longer than 40 ms, where the time went: the deepest-useful samples on the main thread and the
    // render thread, by inclusive time. Input: QA/review-220/startup-profile.raw (path in SANGO3_PROFILE to override).
    public static class StartupProfileReport
    {
        public static void Run()
        {
            string path=System.Environment.GetEnvironmentVariable("SANGO3_PROFILE")??"QA/review-220/startup-profile.raw";
            if(!ProfilerDriver.LoadProfile(Path.GetFullPath(path),false))throw new System.Exception("Could not load "+path);
            var report=new StringBuilder("Profile "+path+": frames "+ProfilerDriver.firstFrameIndex+".."+ProfilerDriver.lastFrameIndex+"\n");
            for(int frame=ProfilerDriver.firstFrameIndex;frame<=ProfilerDriver.lastFrameIndex;frame++){
                using(var main=ProfilerDriver.GetRawFrameDataView(frame,0)){if(main==null||!main.valid)continue;
                    if(main.frameTimeMs<(System.Environment.GetEnvironmentVariable("SANGO3_PROFILE_MS")==null?40:float.Parse(System.Environment.GetEnvironmentVariable("SANGO3_PROFILE_MS"))))continue;
                    report.AppendLine("== frame "+frame+" ("+main.frameTimeMs.ToString("0.0")+" ms, main thread)");Top(main,report,14);
                    for(int thread=1;thread<64;thread++){using(var other=ProfilerDriver.GetRawFrameDataView(frame,thread)){if(other==null||!other.valid)break;
                        if(other.threadName=="Render Thread"||other.threadName.StartsWith("Loading")||other.threadName.Contains("Gfx")){report.AppendLine("  -- "+other.threadGroupName+"/"+other.threadName);Top(other,report,8);}}}
                }
            }
            File.WriteAllText(Path.ChangeExtension(path,".txt"),report.ToString());Debug.Log("SANGO3_PROFILE_REPORT\n"+report);
        }
        // Inclusive time per sample name, at depth 1..6 (below the frame root), largest first; nested time is shown under its parents.
        static void Top(RawFrameDataView view,StringBuilder report,int count)
        {
            var totals=new Dictionary<string,(double ms,int depth,int calls)>();var stack=new Stack<int>();var remaining=new Stack<int>();
            for(int i=0;i<view.sampleCount;i++){
                while(remaining.Count>0&&remaining.Peek()==0){remaining.Pop();stack.Pop();}
                int depth=stack.Count;string name=view.GetSampleName(i);double ms=view.GetSampleTimeMs(i);
                if(depth>=1&&depth<=7){string key=new string(' ',depth*2)+name;totals.TryGetValue(key,out var t);totals[key]=(t.ms+ms,depth,t.calls+1);}
                if(remaining.Count>0){var r=remaining.Pop();remaining.Push(r-1);}
                int children=view.GetSampleChildrenCount(i);stack.Push(i);remaining.Push(children);
            }
            foreach(var kv in totals.Where(k=>k.Value.ms>=2).OrderByDescending(k=>k.Value.ms).Take(count))report.AppendLine("   "+kv.Value.ms.ToString("0.0").PadLeft(7)+" ms  x"+kv.Value.calls+"  d"+kv.Value.depth+" "+kv.Key.Trim());
        }
    }
}
