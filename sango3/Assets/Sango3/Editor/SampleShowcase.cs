using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Sango3.Editor
{
    // Plays stage 16 with Guan Yu and records fixed-step frames of a full combo and the cleave from the
    // game camera and a close camera. Frames feed the review sheets; timing is logged per blow.
    [InitializeOnLoad]
    public static class SampleShowcase
    {
        const string Out="QA/sample-130/frames/";
        static double deadline;static int phase;static BattleController battle;static int slot=-1;static int frame,recorded;static float windowEnd;static string label;static int startFrame;
        static Camera close;static RenderTexture target;static Texture2D read;
        static SampleShowcase(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Sango3/Generated/Yingchuan.unity");
            SessionState.SetBool("Sango3Showcase",true);EditorApplication.isPlaying=true;
        }
        static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("Sango3Showcase",false))return;
            if(Directory.Exists(Out))Directory.Delete(Out,true);Directory.CreateDirectory(Out);
            Campaign.BeginTest("sango3.qa.showcase.");Campaign.SelectTest(BattleController.BaimaStage);deadline=EditorApplication.timeSinceStartup+240;phase=0;
            UnityEngine.Object.FindObjectOfType<BattleController>().Restart();startFrame=Time.frameCount;EditorApplication.update+=Tick;
        }
        static void Tick()
        {
            try{
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timed out in phase "+phase);
                if(Time.frameCount<startFrame+5)return;
                if(phase==0){
                    battle=UnityEngine.Object.FindObjectOfType<BattleController>();if(!battle||!battle.Ready||battle.Stage.Index!=BattleController.BaimaStage)return;
                    // Slot closest to the start of the road so Guan Yu meets the first enemies quickly.
                    var route=battle.Stage.Route;slot=Enumerable.Range(0,BattleRules.Slots.Length).OrderBy(i=>Vector3.Distance(BattleRules.Slots[i],route[1])).First();
                    battle.SelectHero(0);if(!battle.Deploy(slot))throw new Exception("Deploy failed");
                    Time.captureFramerate=30;battle.StartBattle();Capture(Out+"ready");phase=1;return;
                }
                var hero=UnityEngine.Object.FindObjectsOfType<Animator>().FirstOrDefault(a=>a.name=="关羽"&&a.gameObject.activeInHierarchy&&a.GetComponent<CombatPresentation>());
                if(phase==1&&hero&&hero.GetCurrentAnimatorStateInfo(0).IsName("Attack1")){Begin("combo",3.4f);phase=2;}
                if(phase==2&&!Recording){battle.FillMorale();battle.Deploy(slot);battle.Skill();Begin("cleave",2.4f);phase=3;}
                if(Recording){Record(hero);return;}
                if(phase==3){Debug.Log("SAMPLE130_SHOWCASE_DONE frames="+recorded);Finish(0);}
            }catch(Exception ex){Debug.LogException(ex);Finish(1);}
        }
        static bool Recording=>label!=null;
        static void Begin(string name,float seconds){label=name;windowEnd=Time.time+seconds;frame=0;}
        static int lastFrame;
        static void Record(Animator hero)
        {
            if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
            if(Time.time>windowEnd){label=null;return;}
            string n=label+"-"+frame.ToString("000");
            var info=hero?hero.GetCurrentAnimatorStateInfo(0):default;
            File.AppendAllText(Out+"timeline.txt",n+" t="+Time.time.ToString("F3")+" state="+(hero?(info.IsName("Attack1")?"Attack1":info.IsName("Attack2")?"Attack2":info.IsName("Attack3")?"Attack3":info.IsName("Ultimate")?"Ultimate":"other"):"none")+" norm="+info.normalizedTime.ToString("F2")+" scale="+battle.TimeScale.ToString("F2")+"\n");
            if(frame%1==0){Capture(Out+"game-"+n);CloseCapture(hero?hero.transform:null,Out+"close-"+n);}
            frame++;recorded++;
        }
        static void Capture(string path)=>Shot(Camera.main,path+".jpg",960,540);
        static void CloseCapture(Transform hero,string path)
        {
            if(!hero)return;if(!close){close=new GameObject("Showcase camera").AddComponent<Camera>();close.fieldOfView=38;close.enabled=false;}
            var main=Camera.main;close.CopyFrom(main);close.enabled=false;close.fieldOfView=34;
            var focus=hero.position+Vector3.up*1.6f+hero.forward*1.8f;close.transform.position=focus+hero.forward*7.5f+hero.right*3.5f+Vector3.up*5.5f;close.transform.LookAt(focus);
            Shot(close,path+".jpg",960,540);
        }
        static void Shot(Camera camera,string path,int w,int h)
        {
            if(!target||target.width!=w){target=new RenderTexture(w,h,24);read=new Texture2D(w,h,TextureFormat.RGB24,false);}
            var previous=camera.targetTexture;camera.targetTexture=target;camera.Render();var active=RenderTexture.active;RenderTexture.active=target;read.ReadPixels(new Rect(0,0,w,h),0,0);read.Apply();File.WriteAllBytes(path,read.EncodeToJPG(88));RenderTexture.active=active;camera.targetTexture=previous;
        }
        static void Finish(int code){Time.captureFramerate=0;Campaign.EndTest();SessionState.SetBool("Sango3Showcase",false);EditorApplication.update-=Tick;EditorApplication.Exit(code);}
    }
}
