using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Sango3.Editor
{
    // Stage 16: deploys each playable hero in turn, casts its ultimate once enemies are near, and captures the close-up
    // at the cast, at contact and just after, into one review sheet per hero. Also asserts each ultimate resolves.
    [InitializeOnLoad]
    public static class UltimateShowcase
    {
        const string Out="QA/sample-130/ultimates/";
        static double deadline;static int phase,hero,wait,slot,startFrame,lastFrame,casts;static BattleController battle;static Texture2D sheet;static int shot;
        static UltimateShowcase(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){EditorSceneManager.OpenScene("Assets/Sango3/Generated/Yingchuan.unity");SessionState.SetBool("Sango3Ultimates",true);EditorApplication.isPlaying=true;}
        static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("Sango3Ultimates",false))return;
            if(Directory.Exists(Out))Directory.Delete(Out,true);Directory.CreateDirectory(Out);
            Campaign.BeginTest("sango3.qa.ultimates.");Campaign.SelectTest(BattleController.BaimaStage);deadline=EditorApplication.timeSinceStartup+300;phase=0;hero=1;
            UnityEngine.Object.FindObjectOfType<BattleController>().Restart();startFrame=Time.frameCount;EditorApplication.update+=Tick;
        }
        static void Tick()
        {
            if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
            try{
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timed out at hero "+hero+" phase "+phase);
                if(Time.frameCount<startFrame+5)return;
                switch(phase){
                case 0:
                    battle=UnityEngine.Object.FindObjectOfType<BattleController>();if(!battle||!battle.Ready||battle.Stage.Index!=BattleController.BaimaStage)return;
                    var route=battle.Stage.Route;slot=Enumerable.Range(0,BattleRules.Slots.Length).OrderBy(i=>Vector3.Distance(BattleRules.Slots[i],route[1])).First();
                    battle.DebugAddGold(2000);battle.SelectHero(hero);if(!battle.Deploy(slot))throw new Exception("Deploy failed for "+hero);
                    Time.captureFramerate=30;battle.StartBattle();BattleCamera.Toggle();wait=150;phase=1;return;
                case 1:
                    if(--wait>0)return;casts=battle.CastsResolved;battle.FillMorale();battle.Deploy(slot);battle.DebugClearCooldown();battle.Skill();
                    sheet=new Texture2D(960*3,540,TextureFormat.RGB24,false);shot=0;wait=0;phase=2;return;
                case 2:{
                    var set=HeroMoves.For(hero);int contactFrame=Mathf.RoundToInt(set.Ultimate.contact*30);wait++;
                    if(wait==6||wait==contactFrame+3||wait==contactFrame+12){Grab(shot++);}
                    if(wait<contactFrame+14)return;
                    if(battle.CastsResolved<=casts)throw new Exception("Ultimate did not resolve for hero "+hero);
                    sheet.Apply();File.WriteAllBytes(Out+BattleRules.Portraits[hero]+"-ultimate.jpg",sheet.EncodeToJPG(85));UnityEngine.Object.DestroyImmediate(sheet);
                    hero++;if(hero>=BattleRules.Names.Length){Debug.Log("SAMPLE170_ULTIMATES_DONE");Finish(0);return;}
                    // Next hero: restart the stage so each ultimate is captured on its own.
                    BattleCamera.Toggle();Time.captureFramerate=0;battle.Restart();startFrame=Time.frameCount;phase=0;return;}
                }
            }catch(Exception ex){Debug.LogException(ex);Finish(1);}
        }
        static void Grab(int index)
        {
            var cam=Camera.main;var rt=RenderTexture.GetTemporary(960,540,24);var prev=cam.targetTexture;cam.targetTexture=rt;cam.Render();cam.targetTexture=prev;var active=RenderTexture.active;RenderTexture.active=rt;sheet.ReadPixels(new Rect(0,0,960,540),index*960,0);RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);
        }
        static void Finish(int code){Time.captureFramerate=0;if(BattleCamera.Mode==BattleCamera.View.Close)BattleCamera.Toggle();Campaign.EndTest();SessionState.SetBool("Sango3Ultimates",false);EditorApplication.update-=Tick;EditorApplication.Exit(code);}
    }
}
