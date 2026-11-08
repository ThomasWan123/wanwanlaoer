using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Sango3.Editor
{
    [InitializeOnLoad]
    public static class RosterBattleCheck
    {
        static BattleController battle;static int hero,waitFrame;static bool prepared;static double deadline;static readonly StringBuilder report=new StringBuilder();
        static RosterBattleCheck(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){EditorSceneManager.OpenScene("Assets/Sango3/Generated/Yingchuan.unity");SessionState.SetBool("RosterBattleCheck",true);EditorApplication.isPlaying=true;}
        static void Changed(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("RosterBattleCheck",false))return;Campaign.BeginTest("sango3.qa.roster."+Guid.NewGuid().ToString("N")+".");hero=0;prepared=false;report.Clear();deadline=EditorApplication.timeSinceStartup+300;EditorApplication.update+=Tick;}
        static void Tick(){try{
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timed out hero="+hero);if(Time.frameCount<waitFrame)return;
            if(!battle)battle=UnityEngine.Object.FindObjectOfType<BattleController>();if(!battle||!battle.Ready)return;
            if(!prepared){battle.SelectHero(hero);if(!battle.Deploy(0))throw new Exception("Hero deployment failed");battle.SelectHero(0);battle.Deploy(1);battle.SelectHero(4);battle.Deploy(3);battle.Deploy(0);battle.StartBattle();Time.timeScale=10;prepared=true;}
            if(battle.Morale==100){battle.Deploy(0);battle.Skill();}
            if(battle.CastsResolved>0){report.AppendLine("PASS "+BattleRules.Names[hero]+" finalPrefab="+(Resources.Load<GameObject>("Heroes/Final/"+BattleRules.Portraits[hero])!=null)+" casts="+battle.CastsResolved+" life="+battle.Life);File.WriteAllText("QA/roster-battle-080.txt",report.ToString());hero++;if(hero==6){Debug.Log("ROSTER_SIX_SKILLS_PASS");Finish(0);return;}battle.Restart();battle=null;prepared=false;waitFrame=Time.frameCount+5;}
            else if(battle.Finished)throw new Exception("Skill never resolved for "+hero);
        }catch(Exception ex){report.AppendLine("FAIL "+ex);File.WriteAllText("QA/roster-battle-080.txt",report.ToString());Debug.LogException(ex);Finish(1);}}
        static void Finish(int code){Time.timeScale=1;Campaign.EndTest();SessionState.SetBool("RosterBattleCheck",false);EditorApplication.update-=Tick;EditorApplication.Exit(code);}
    }
}
