using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Sango3.Editor
{
    // Play-mode checks for the stage 16 sample, at a fixed 30 fps step:
    // 1) a swing queued at 1x and switched to 2x before contact still sounds 0.12 battle s before the hit;
    // 2) pausing during the ultimate freezes the camera push, which resumes afterwards; 3) the close-up camera frames the hero.
    [InitializeOnLoad]
    public static class SampleCheck
    {
        const string Report="QA/sample-130/sample-check.txt";
        static double deadline;static int phase,wait,startFrame,slot;static BattleController battle;static Camera cam;static Vector3 frozen;static GameObject moved;static float movedHp,topMost;static readonly StringBuilder log=new StringBuilder();
        static SampleCheck(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){EditorSceneManager.OpenScene("Assets/Sango3/Generated/Yingchuan.unity");SessionState.SetBool("Sango3SampleCheck",true);EditorApplication.isPlaying=true;}
        static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("Sango3SampleCheck",false))return;
            log.Clear();Campaign.BeginTest("sango3.qa.check.");Campaign.SelectTest(BattleController.BaimaStage);deadline=EditorApplication.timeSinceStartup+240;phase=0;
            UnityEngine.Object.FindObjectOfType<BattleController>().Restart();startFrame=Time.frameCount;EditorApplication.update+=Tick;
        }
        static int lastFrame;
        static void Tick()
        {
            if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
            try{
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timed out in phase "+phase);
                if(Time.frameCount<startFrame+5)return;
                var hero=UnityEngine.Object.FindObjectsOfType<Animator>().FirstOrDefault(a=>a.name=="关羽"&&a.gameObject.activeInHierarchy&&a.GetComponent<CombatPresentation>());
                switch(phase){
                case 0:
                    battle=UnityEngine.Object.FindObjectOfType<BattleController>();if(!battle||!battle.Ready||battle.Stage.Index!=BattleController.BaimaStage)return;cam=Camera.main;
                    var route=battle.Stage.Route;slot=Enumerable.Range(0,BattleRules.Slots.Length).OrderBy(i=>Vector3.Distance(BattleRules.Slots[i],route[1])).First();
                    battle.SelectHero(0);if(!battle.Deploy(slot))throw new Exception("Deploy failed");Time.captureFramerate=30;battle.StartBattle();SangoAudio.Fired.Clear();phase=1;return;
                case 1:
                    // Switch to 2x two frames after the first swing begins, while its sound is still queued.
                    if(hero&&hero.GetCurrentAnimatorStateInfo(0).IsName("Attack1")){log.AppendLine("attack1 seen at battle "+battle.BattleTime.ToString("F3"));wait=2;phase=2;}return;
                case 2:
                    if(--wait>0)return;battle.Faster();if(battle.Speed!=2)throw new Exception("Speed switch failed");phase=3;wait=60;return;
                case 3:{
                    var swing=SangoAudio.Fired.FirstOrDefault(f=>f.name.StartsWith("swing"));var hit=SangoAudio.Fired.FirstOrDefault(f=>f.name.StartsWith("hit"));
                    if(swing.name==null||hit.name==null){if(--wait<=0)throw new Exception("Swing or hit sound never fired");return;}
                    log.AppendLine("fired: "+string.Join(", ",SangoAudio.Fired.Select(f=>f.name+"@"+f.time.ToString("F3"))));
                    float lead=hit.time-swing.time;float tolerance=2f/30;
                    log.AppendLine("audio: swing at battle "+swing.time.ToString("F3")+", hit at "+hit.time.ToString("F3")+", lead "+lead.ToString("F3")+" s (expected 0.120 ± "+tolerance.ToString("F3")+") with speed switched 1x→2x while queued");
                    if(Mathf.Abs(lead-.12f)>tolerance)throw new Exception("Swing sound out of sync: lead "+lead);
                    battle.Faster();phase=20;wait=600;return;}
                case 20:
                    // Sweep: once Attack2 is queued, carry its locked target out of reach; it must not be hit at contact.
                    if(--wait<=0)throw new Exception("No sweep observed");
                    if(hero&&hero.GetCurrentAnimatorStateInfo(0).IsName("Attack2")){moved=battle.DebugDisplaceTarget(1);if(moved){movedHp=battle.DebugHealth(moved);wait=30;phase=21;}}return;
                case 21:
                    if(--wait>0)return;{float hp=battle.DebugHealth(moved);
                    log.AppendLine("sweep: locked target carried out of reach before contact, health "+movedHp.ToString("F0")+" -> "+hp.ToString("F0"));
                    if(hp>=0&&hp<movedHp)throw new Exception("Sweep hit a target out of reach");}phase=4;return;
                case 4:
                    SangoAudio.Fired.Clear();battle.FillMorale();battle.Deploy(slot);battle.Skill();
                    // The ultimate's charge sound must fire on the cast (it was once swallowed by a trailing comment).
                    if(!SangoAudio.Fired.Any(f=>f.name=="ult_charge"))throw new Exception("Guan Yu ultimate charge sound did not fire");log.AppendLine("ultimate: charge sound fired on cast");
                    wait=8;phase=5;return;
                case 5:
                    // Mid-push: pause and hold for 60 frames; the camera must not move.
                    if(--wait>0)return;battle.Pause();frozen=cam.transform.position;wait=60;phase=6;return;
                case 6:
                    if(Vector3.Distance(cam.transform.position,frozen)>.001f)throw new Exception("Camera moved while paused: "+Vector3.Distance(cam.transform.position,frozen));
                    if(--wait>0)return;var rig=cam.GetComponent<BattleCamera>();if(!rig.Pushing)throw new Exception("Push ended while paused");
                    log.AppendLine("pause: camera held still for 60 frames during the ultimate push; push still active");battle.Pause();wait=6;phase=7;return;
                case 7:
                    if(--wait>0)return;if(!cam.GetComponent<BattleCamera>().Pushing)throw new Exception("Push did not resume after pause");
                    log.AppendLine("resume: push continued after unpause");wait=90;phase=8;return;
                case 8:
                    if(--wait>0)return;BattleCamera.Toggle();wait=60;phase=9;return;
                case 9:{
                    if(--wait>0)return;var focus=battle.CameraFocus;if(!focus)throw new Exception("No close-up focus");float distance=Vector3.Distance(cam.transform.position,focus.position);
                    log.AppendLine("close-up: camera "+distance.ToString("F1")+" from the hero (overview is about 37)");if(distance>19)throw new Exception("Close-up not framing hero");
                    // Ultimate cast while in close-up: the whole body must stay in frame through the leap.
                    battle.FillMorale();battle.Deploy(slot);battle.DebugClearCooldown();battle.Skill();wait=45;topMost=0;phase=10;return;}
                case 10:{
                    var skin=hero.GetComponent<SkinnedMeshRenderer>();var top=cam.WorldToViewportPoint(new Vector3(skin.bounds.center.x,skin.bounds.max.y,skin.bounds.center.z));topMost=Mathf.Max(topMost,top.y);
                    if(--wait>0)return;log.AppendLine("close-up ultimate: highest point of Guan Yu reached viewport y="+topMost.ToString("F2")+" (1.00 = top edge)");
                    if(topMost>.97f)throw new Exception("Close-up crops Guan Yu during the leap");
                    // Original repro: cast in the overview, then switch to close-up during the leap.
                    BattleCamera.Toggle();wait=120;phase=11;return;}
                case 11:
                    if(--wait>0)return;battle.FillMorale();battle.Deploy(slot);battle.DebugClearCooldown();battle.Skill();wait=20;topMost=0;phase=12;return;
                case 12:{
                    if(wait==12)BattleCamera.Toggle();
                    var skin=hero.GetComponent<SkinnedMeshRenderer>();if(wait<12){var top=cam.WorldToViewportPoint(new Vector3(skin.bounds.center.x,skin.bounds.max.y,skin.bounds.center.z));topMost=Mathf.Max(topMost,top.y);}
                    if(--wait>-60)return;log.AppendLine("switch to close-up mid-ultimate: highest point reached viewport y="+topMost.ToString("F2"));
                    if(topMost>.97f)throw new Exception("Switching to close-up mid-ultimate crops Guan Yu");
                    BattleCamera.Toggle();File.WriteAllText(Report,"PASS\n"+log);Debug.Log("SAMPLE130_CHECK_PASS\n"+log);Finish(0);return;}
                }
            }catch(Exception ex){File.WriteAllText(Report,"FAIL: "+ex.Message+"\n"+log);Debug.LogException(ex);Finish(1);}
        }
        static void Finish(int code){Time.captureFramerate=0;Campaign.EndTest();SessionState.SetBool("Sango3SampleCheck",false);EditorApplication.update-=Tick;EditorApplication.Exit(code);}
    }
}
