using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Sango3.Editor
{
    [InitializeOnLoad]
    public static class CampaignCheck
    {
        static BattleController battle;static int index,waitFrame;static bool prepared;static double deadline;static float nextAction;
        static readonly System.Text.StringBuilder results=new System.Text.StringBuilder();
        static CampaignCheck(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){CharacterBuilder.Build();RunReview();}
        // Review the shipped assets without regenerating characters before the test.
        public static void RunReview(){EditorSceneManager.OpenScene("Assets/Sango3/Generated/Yingchuan.unity");SessionState.SetBool("CampaignCheck",true);EditorApplication.isPlaying=true;}
        static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("CampaignCheck",false))return;
            Campaign.BeginTest("sango3.qa.campaign."+Guid.NewGuid().ToString("N")+".");
            index=0;prepared=false;deadline=EditorApplication.timeSinceStartup+1200;results.Clear();EditorApplication.update+=Tick;
        }
        static void Tick()
        {
            try{
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Campaign timed out on "+index);
                if(Time.frameCount<waitFrame)return;
                if(!battle)battle=UnityEngine.Object.FindObjectOfType<BattleController>();if(!battle||!battle.Ready||!UnityEngine.Object.FindObjectOfType<BattleHud>())return;
                if(!prepared){
                    var stage=Campaign.Current;if(stage.Index!=index||stage.Route.Length<2||stage.Slots.Length!=8)throw new Exception("Invalid stage definition");
                    if(battle.Gold!=stage.StartingGold)throw new Exception("Stage economy not initialized");
                    for(int p=0;p<stage.Slots.Length;p++)for(int q=1;q<stage.Route.Length;q++){
                        var a=stage.Route[q-1];var d=stage.Route[q]-a;if(d.sqrMagnitude<.1f)throw new Exception("Zero-length path");var closest=a+d*Mathf.Clamp01(Vector3.Dot(stage.Slots[p]-a,d)/d.sqrMagnitude);if(Vector3.Distance(closest,stage.Slots[p])<1.2f)throw new Exception("Deployment on path stage="+index+" slot="+p);
                    }
                    if(index==0){if(Campaign.Unlocked!=1||Campaign.Select(1))throw new Exception("Locked stage selectable");}
                    // A documented deterministic strategy, using only normal player actions/economy.
                    var initial=new[]{0,1,3};var heroes=new[]{0,4,1};for(int i=0;i<3;i++){battle.SelectHero(heroes[i]);battle.Deploy(initial[i]);}
                    battle.UseTactic();if(battle.TacticsUsed!=0)throw new Exception("Pre-start tactic consumed");battle.StartBattle();battle.Pause();battle.UseTactic();if(battle.TacticsUsed!=0)throw new Exception("Paused tactic consumed");battle.Pause();Time.timeScale=20;prepared=true;nextAction=0;
                }
                if(Time.time>=nextAction&&!battle.Finished){
                    nextAction=Time.time+.35f;
                    for(int slot=0;slot<8;slot++){
                        battle.SelectHero(slot==4||slot==6?1:slot%3==0?0:4);battle.Deploy(slot);
                        if(battle.SelectedLevel>0){while(battle.SkillPoints>0&&battle.AttackRank<4)battle.TrainSkill(false);while(battle.SkillPoints>0&&battle.SkillRank<3)battle.TrainSkill(true);if(battle.SelectedEquipment<2&&battle.Gold>=120)battle.Upgrade();}
                    }
                    battle.Deploy(0);if(battle.Morale==100)battle.Skill();if(battle.TacticReady){battle.UseTactic();int used=battle.TacticsUsed;battle.UseTactic();if(battle.TacticsUsed!=used)throw new Exception("Tactic bypassed cooldown");}
                }
                if(!battle.Finished)return;
                if(!battle.Won||!battle.ObjectiveSatisfied||battle.Life<=0)throw new Exception("Strategy lost stage "+(index+1)+" wave "+battle.Wave);
                if(battle.Wave!=battle.MaxWaves||Campaign.Stars(index)==0||Campaign.Unlocked!=Mathf.Min(30,index+2))throw new Exception("Completion/unlock invalid");
                if(index==3||index==13||index==17||index==22||index==24||index==27||index==29)ScenarioValidation.Capture("QA/stage-"+(index+1).ToString("00")+"-110.png");
                if(index==24)ValidateBurningFleet();int best=Campaign.Stars(index);Campaign.Complete(index,1);if(Campaign.Stars(index)!=best)throw new Exception("Replay reduced best stars");
                results.AppendLine("PASS "+(index+1).ToString("00")+" "+battle.Stage.Name+" waves="+battle.Wave+" life="+battle.Life+" kills="+battle.Kills+" objective="+battle.ObjectiveStatus+" tactics="+battle.TacticsUsed+" stars="+best);File.WriteAllText("QA/campaign-110.txt",results.ToString());
                index++;if(index==Campaign.Count){if(Campaign.Select(30)||Campaign.Unlocked!=30)throw new Exception("Final stage bounds");Debug.Log("SANGO3_CAMPAIGN_30_PASS");Finish(0);return;}
                if(!Campaign.Select(index))throw new Exception("Next stage locked");battle.Restart();battle=null;prepared=false;waitFrame=Time.frameCount+5;
            }catch(Exception ex){results.AppendLine("FAIL "+ex);File.WriteAllText("QA/campaign-110.txt",results.ToString());Debug.LogException(ex);Finish(1);}
        }
        static void ValidateBurningFleet(){
            var fires=UnityEngine.Object.FindObjectsOfType<BurningShip>();
            if(fires.Length!=Campaign.Current.Scenario.Target)throw new Exception("Naval victory without burning target ships");
            foreach(var fire in fires){
                var update=typeof(BurningShip).GetMethod("Update",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);update.Invoke(fire,null);
                var ps=fire.GetComponentInChildren<ParticleSystem>();
                if(!ps||ps.main.simulationSpeed!=0||!ps.GetComponent<Renderer>().sharedMaterial)throw new Exception("Finished naval fire did not stop or lacks material");
            }
            results.AppendLine("PASS Red Cliffs: four burning ship targets, valid fire materials, stopped at terminal state");
        }
        static void Finish(int code){Time.timeScale=1;Campaign.EndTest();SessionState.SetBool("CampaignCheck",false);EditorApplication.update-=Tick;EditorApplication.Exit(code);}
    }
}
