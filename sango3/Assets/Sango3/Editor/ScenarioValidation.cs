using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace Sango3.Editor
{
    public static class ScenarioValidation
    {
        public static void BuildAndVerify(){Run();RecoveredHeroBuilder.BuildAll();RecoveredHeroAcceptance.Run();RecoveredHeroBuilder.RenderAccepted();}
        public static void Capture(string path){var camera=Camera.main;var previous=camera.targetTexture;var active=RenderTexture.active;var target=new RenderTexture(1280,720,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());camera.targetTexture=previous;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(target);}
        public static void Run()
        {
            var paths=new HashSet<string>();var report=new StringBuilder();var catalog=new StringBuilder("# 30 关战役配置\n\n历史与演义题材的章节改编，路线与目标是游戏设计，不是古战场复原。\n\n|关卡|主题|目标|军令|变化|\n|---|---|---|---|---|\n");
            for(int i=0;i<Campaign.Count;i++){
                var stage=Campaign.Get(i);var s=stage.Scenario;
                if(!paths.Add(string.Join(";",stage.Route.Select(v=>v.ToString("F2")))))throw new Exception("Duplicate route "+i);
                foreach(var route in s.Flank==null?new[]{s.Route}:new[]{s.Route,s.Flank}){
                    if(route.Length<2||route[route.Length-1]!=s.Route[s.Route.Length-1])throw new Exception("Invalid endpoint");
                    for(int n=1;n<route.Length;n++)if(Vector3.Distance(route[n-1],route[n])<.1f)throw new Exception("Zero-length segment");
                    foreach(var p in stage.Slots)if(HistoricalScenarios.Distance(p,route)<1.2f)throw new Exception("Pad on route "+i);
                }
                for(int p=0;p<stage.Slots.Length;p++)for(int q=p+1;q<stage.Slots.Length;q++)if(Vector3.Distance(stage.Slots[p],stage.Slots[q])<2.5f)throw new Exception("Overlapping pads "+i);
                if(s.Objective!=BattleObjective.Defend&&(s.Target<=0||s.Target>stage.Waves))throw new Exception("Unreachable objective "+i);
                report.AppendLine("PASS "+(i+1)+" "+stage.Name+" "+s.Objective+" unique route; clear pads; valid target");
                catalog.AppendLine("|"+(i+1)+" "+stage.Name+"|"+s.Title+"|"+s.ObjectiveText+"|"+s.TacticName+"|"+s.Rule+"|");
            }
            File.WriteAllText("QA/scenario-definitions-080.txt",report.ToString());File.WriteAllText("Docs/Historical-Campaign-0.8.0.md",catalog.ToString());Debug.Log("SCENARIO_30_DEFINITIONS_PASS");
        }
    }
}
