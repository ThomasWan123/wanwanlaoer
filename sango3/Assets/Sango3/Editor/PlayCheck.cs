using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Sango3.Editor
{
    [InitializeOnLoad]
    public static class PlayCheck
    {
        static double deadline;static bool initialized,trained;static BattleController battle;static int stage,reloadFrame;
        static PlayCheck(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Sango3/Generated/Yingchuan.unity");
            SessionState.SetBool("Sango3Smoke",true);EditorApplication.isPlaying=true;
        }
        static void Changed(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("Sango3Smoke",false))
            {Campaign.BeginTest("sango3.qa.smoke.");deadline=EditorApplication.timeSinceStartup+180;initialized=false;trained=false;stage=0;EditorApplication.update+=Tick;}
        }
        static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timed out");
                if(stage==2){
                    if(Time.frameCount<reloadFrame+5)return;
                    battle=UnityEngine.Object.FindObjectOfType<BattleController>();if(!battle||!battle.Ready||!UnityEngine.Object.FindObjectOfType<BattleHud>())return;
                    if(battle.Gold!=300||battle.Life!=12||battle.Wave!=0)throw new Exception("Restart did not reset state");
                    foreach(var renderer in UnityEngine.Object.FindObjectsOfType<Renderer>())foreach(var material in renderer.sharedMaterials)if(!material||!material.shader||!material.shader.isSupported)throw new Exception("Restart lost material on "+renderer.name);
                    File.AppendAllText("playcheck.txt","; PASS: restart resources and material references after deferred destruction.");
                    Debug.Log("SANGO3_PLAYCHECK_PASS combat, pause, terminal state and restart materials");Finish(0);return;
                }
                if(!initialized)
                {
                    battle=UnityEngine.Object.FindObjectOfType<BattleController>();
                    if(!battle||!battle.Ready||!UnityEngine.Object.FindObjectOfType<BattleHud>())return;
                    for(int hero=0;hero<6;hero++){
                        var final=Resources.Load<GameObject>("Heroes/Final/"+BattleRules.Portraits[hero]);var prefab=final?final:Resources.Load<GameObject>("Heroes/"+BattleRules.Portraits[hero]);if(!prefab)throw new Exception("Missing rigged hero "+hero);
                        var animator=prefab.GetComponentInChildren<Animator>();if(!animator||!animator.runtimeAnimatorController||(!final&&(!animator.avatar||!animator.avatar.isHuman)))throw new Exception("Missing animation rig "+hero);
                        foreach(var skin in prefab.GetComponentsInChildren<SkinnedMeshRenderer>()){if(!skin.sharedMesh||skin.bones.Length==0)throw new Exception("Missing skin "+skin.name);foreach(var bone in skin.bones)if(!bone)throw new Exception("Missing bone "+skin.name);}
                        for(int phase=0;phase<2;phase++){
                            var target=new Vector3(7,0,5);var effect=SkillVfx.Create(hero,target,4.5f,battle,phase==1);
                            if(Vector3.Distance(effect.transform.position,target+Vector3.up*.2f)>.001f)throw new Exception("Skill origin does not match target");
                            foreach(var line in effect.GetComponentsInChildren<LineRenderer>())if(line.useWorldSpace||!line.sharedMaterial||!line.sharedMaterial.shader.isSupported)throw new Exception("Invalid skill coordinate space or material");
                            var ribbons=effect.GetComponentsInChildren<MeshFilter>();if(phase==1&&(hero==0||hero==4)&&ribbons.Length!=2)throw new Exception("Missing blade ribbons");
                            foreach(var ribbon in ribbons){var mesh=ribbon.sharedMesh;var renderer=ribbon.GetComponent<Renderer>();if(!mesh||mesh.vertexCount!=98||mesh.triangles.Length!=288||mesh.colors.Length!=98||!renderer.sharedMaterial||!renderer.sharedMaterial.shader.isSupported)throw new Exception("Invalid blade ribbon geometry or material");foreach(var p in mesh.vertices)if(float.IsNaN(p.x)||float.IsInfinity(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.z))throw new Exception("Nonfinite blade ribbon");}
                            UnityEngine.Object.Destroy(effect.gameObject);
                        }
                    }
                    battle.SelectHero(0);
                    if(!battle.Deploy(0)||battle.Gold!=220)throw new Exception("Deployment economy failed");
                    if(battle.Deploy(0)||battle.Gold!=220)throw new Exception("Occupied slot charged twice");
                    battle.Upgrade();if(battle.Gold!=160)throw new Exception("Upgrade economy failed");
                    battle.Pause();if(!battle.Paused)throw new Exception("Pause failed");battle.Upgrade();battle.StartBattle();if(battle.Deploy(1)||battle.Gold!=160||battle.SelectedEquipment!=1||battle.Started)throw new Exception("Paused actions changed gameplay");battle.Pause();
                    battle.SelectHero(1);battle.Deploy(3);battle.SelectHero(0);
                    battle.StartBattle();Time.timeScale=25;initialized=true;
                }
                if(battle.Gold>=80)for(int i=1;i<8;i++){if(battle.Deploy(i))break;}
                if(!trained){battle.Deploy(0);if(battle.SkillPoints>0){int points=battle.SkillPoints,rank=battle.SkillRank,gold=battle.Gold;battle.Pause();battle.TrainSkill(true);if(battle.SkillPoints!=points)throw new Exception("Paused skill upgrade consumed points");battle.Pause();battle.TrainSkill(true);if(battle.SkillPoints!=points-1||battle.SkillRank!=rank+1||battle.Gold!=gold)throw new Exception("Skill point upgrade failed");trained=true;}}
                if(battle.Morale==100){battle.Deploy(0);battle.Skill();}
                if(battle.Wave>0)stage=1;
                if(battle.Finished)
                {
                    if(!trained||battle.CastsResolved==0)throw new Exception("Growth or staged skill never exercised");
                    if(battle.Wave!=6||battle.Life<=0)throw new Exception("Expected six-wave victory");if(stage==0)throw new Exception("No wave ran");var terminalMessage=battle.Message;var terminalGold=battle.Gold;battle.SelectHero(5);battle.Upgrade();battle.Skill();battle.Deploy(7);battle.StartBattle();if(battle.Message!=terminalMessage||battle.Gold!=terminalGold)throw new Exception("Terminal state changed after input");
                    File.WriteAllText("playcheck.txt","PASS: six rigged prefabs, twelve skill effect variants and target coordinates, experience skill points, staged ultimate, deploy cost, occupied slot, upgrade cost, pause/resume and paused action guards, waves, combat and terminal state. Wave="+battle.Wave+" Life="+battle.Life+" Message="+battle.Message);
                    stage=2;reloadFrame=Time.frameCount;Time.timeScale=1;battle.Restart();
                }
            }
            catch(Exception ex){File.WriteAllText("playcheck.txt","FAIL: "+ex);Debug.LogException(ex);Finish(1);}
        }
        static void Finish(int code){Time.timeScale=1;Campaign.EndTest();SessionState.SetBool("Sango3Smoke",false);EditorApplication.update-=Tick;EditorApplication.Exit(code);}
    }
}
