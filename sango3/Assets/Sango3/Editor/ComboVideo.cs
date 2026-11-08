using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Sango3.Editor
{
    // Continuous in-game recordings for animation review: each playable hero on stage 16, 30 fps, from the front and
    // the side with the whole weapon in frame. Covers the real Animator cross-fades: combo 1→2→3→idle while it fights,
    // an ultimate cast in the middle of a swing (which cancels the swing's queued blow), and the return to attacking.
    // Frames go to QA/sample-130/video/<hero>-<view>/; QA/sample-130/encode_video.sh makes the mp4 files.
    [InitializeOnLoad]
    public static class ComboVideo
    {
        const string Out="QA/sample-130/video/";const int Seconds=9,CastAt=150,W=960,H=720;
        static double deadline;static int phase,hero,frame,slot,startFrame,lastFrame,castFrame;static BattleController battle;static Camera front,side;static Vector3 facing;static RenderTexture target;static Texture2D read;static string log;
        static ComboVideo(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){EditorSceneManager.OpenScene("Assets/Sango3/Generated/Yingchuan.unity");SessionState.SetBool("Sango3ComboVideo",true);EditorApplication.isPlaying=true;}
        static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("Sango3ComboVideo",false))return;
            if(Directory.Exists(Out))Directory.Delete(Out,true);Directory.CreateDirectory(Out);log="";
            Campaign.BeginTest("sango3.qa.video.");Campaign.SelectTest(BattleController.BaimaStage);deadline=EditorApplication.timeSinceStartup+900;phase=0;hero=0;
            UnityEngine.Object.FindObjectOfType<BattleController>().Restart();startFrame=Time.frameCount;EditorApplication.update+=Tick;
        }
        static void Tick()
        {
            if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
            try{
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timed out at hero "+hero);
                if(Time.frameCount<startFrame+5)return;
                if(phase==0){
                    battle=UnityEngine.Object.FindObjectOfType<BattleController>();if(!battle||!battle.Ready||battle.Stage.Index!=BattleController.BaimaStage)return;
                    var route=battle.Stage.Route;slot=Enumerable.Range(0,BattleRules.Slots.Length).OrderBy(i=>Vector3.Distance(BattleRules.Slots[i],route[1])).First();
                    battle.DebugAddGold(2000);battle.SelectHero(hero);if(!battle.Deploy(slot))throw new Exception("Deploy failed for "+hero);
                    Time.captureFramerate=30;battle.StartBattle();frame=-75;castFrame=-1;phase=1;
                    Directory.CreateDirectory(Out+BattleRules.Portraits[hero]+"-front");Directory.CreateDirectory(Out+BattleRules.Portraits[hero]+"-side");return;
                }
                frame++;if(frame<0)return; // let the first soldiers arrive
                var view=UnityEngine.Object.FindObjectsOfType<Animator>().First(a=>a.name==BattleRules.Names[hero]&&a.gameObject.activeInHierarchy&&a.GetComponent<CombatPresentation>()).transform;
                // Cast the ultimate while a blow is queued but not yet landed (from CastAt on), and check the ultimate cancelled it.
                if(castFrame<0&&frame>=CastAt&&battle.DebugPendingHits(hero)>0){battle.FillMorale();battle.Deploy(slot);battle.DebugClearCooldown();battle.Skill();castFrame=frame;
                    if(battle.DebugPendingHits(hero)!=0)throw new Exception("Ultimate did not cancel the queued blow for hero "+hero);}
                if(castFrame<0&&frame>=CastAt+90)throw new Exception("No queued blow to cancel for hero "+hero);
                Shoot(view,hero,frame);var cloth=view.GetComponent<ClothFollow>();if(cloth){swing=Mathf.Max(swing,cloth.LastSwing);swingSum+=cloth.LastSwing;}
                if(frame<Seconds*30)return;
                log+=BattleRules.Portraits[hero]+": "+frame+" frames, ultimate cast at frame "+castFrame+" during a queued blow (cancelled); frames with body or weapon outside the picture: front "+outFront+", side "+outSide+"; frames with the chest behind a soldier: front "+hidFront+", side "+hidSide+"; cloth follow-through mean "+(swingSum/frame).ToString("0.0")+" deg, max "+swing.ToString("0.0")+" deg"+(cloth?"":" (no cloth component)")+"\n";
                if(outFront+outSide+hidFront+hidSide>0)throw new Exception("Hero or weapon left the picture or was hidden: "+log);outFront=outSide=hidFront=hidSide=0;swing=swingSum=0;
                hero++;if(hero>=BattleRules.Names.Length){File.WriteAllText(Out+"frames.txt",log);Debug.Log("SAMPLE190_VIDEO_DONE");Finish(0);return;}
                Time.captureFramerate=0;battle.Restart();startFrame=Time.frameCount;phase=0;
            }catch(Exception ex){Debug.LogException(ex);Finish(1);}
        }
        // Cameras follow the hero at a fixed offset (front three-quarter, side), far enough that a raised polearm stays in frame.
        static void Shoot(Transform view,int heroIndex,int f)
        {
            if(!front){front=new GameObject("Video front").AddComponent<Camera>();side=new GameObject("Video side").AddComponent<Camera>();foreach(var c in new[]{front,side}){c.enabled=false;c.fieldOfView=37;c.nearClipPlane=.3f;c.farClipPlane=120;c.clearFlags=CameraClearFlags.Skybox;}target=new RenderTexture(W,H,24);read=new Texture2D(W,H,TextureFormat.RGB24,false);}
            var main=Camera.main;front.backgroundColor=side.backgroundColor=main.backgroundColor;front.clearFlags=side.clearFlags=main.clearFlags;
            var focus=view.position+Vector3.up*2.4f;
            // The camera frame is the hero's facing when recording starts (toward the road), so views stay steady while
            // he turns to follow targets: front three-quarter from the road side, side view from his right.
            if(f==0){facing=view.forward;facing.y=0;facing.Normalize();}var right=Vector3.Cross(Vector3.up,facing);
            // Framed for the whole motion envelope, not the standing pose: ground to ~7.5 units (a leap with the guandao raised).
            var aim=view.position+Vector3.up*3.2f;
            front.transform.position=aim+facing*15f+right*6f+Vector3.up*5.5f;front.transform.LookAt(aim);
            side.transform.position=aim+right*16f+facing*3f+Vector3.up*5f;side.transform.LookAt(aim);
            // Every renderer of the hero (body, hands, weapon) must stay inside the picture.
            if(!Inside(front,view,f,"front"))outFront++;if(Blocked(front,view))hidFront++;if(Blocked(side,view))hidSide++;if(!Inside(side,view,f,"side"))outSide++;
            Save(front,Out+BattleRules.Portraits[heroIndex]+"-front/"+f.ToString("0000")+".jpg");Save(side,Out+BattleRules.Portraits[heroIndex]+"-side/"+f.ToString("0000")+".jpg");
        }
        static int outFront,outSide,hidFront,hidSide;static float swing,swingSum;
        // The hero's chest is hidden when another character's renderer bounds cross the line from the camera to it.
        static bool Blocked(Camera c,Transform hero)
        {
            var chest=hero.position+Vector3.up*2.6f;var ray=new Ray(c.transform.position,chest-c.transform.position);float d=Vector3.Distance(c.transform.position,chest)-1f;
            foreach(var a in UnityEngine.Object.FindObjectsOfType<Animator>()){if(a.transform==hero||!a.gameObject.activeInHierarchy)continue;
                foreach(var r in a.GetComponentsInChildren<SkinnedMeshRenderer>()){var b=r.bounds;b.Expand(-.3f);if(b.IntersectRay(ray,out var t)&&t<d)return true;}}
            return false;
        }
        static bool Inside(Camera c,Transform hero,int f,string tag)
        {
            foreach(var r in hero.GetComponentsInChildren<Renderer>()){if(!r.enabled||r is ParticleSystemRenderer||r is LineRenderer)continue;var b=r.bounds;
                for(int i=0;i<8;i++){var p=new Vector3((i&1)==0?b.min.x:b.max.x,(i&2)==0?b.min.y:b.max.y,(i&4)==0?b.min.z:b.max.z);var v=c.WorldToViewportPoint(p);
                    // Inside means in front of the camera between the clip planes, finite, and within the picture.
                    if(float.IsNaN(v.x+v.y+v.z)||float.IsInfinity(v.x+v.y+v.z)||v.z<c.nearClipPlane||v.z>c.farClipPlane||v.x<0||v.x>1||v.y<0||v.y>1){Debug.Log("SANGO3_OUT "+tag+" f"+f+" "+r.name+" size "+b.size+" vp "+v);return false;}}}
            return true;
        }
        static void Save(Camera c,string path){c.targetTexture=target;c.Render();c.targetTexture=null;var active=RenderTexture.active;RenderTexture.active=target;read.ReadPixels(new Rect(0,0,W,H),0,0);RenderTexture.active=active;File.WriteAllBytes(path,read.EncodeToJPG(88));}
        static void Finish(int code){Time.captureFramerate=0;Campaign.EndTest();SessionState.SetBool("Sango3ComboVideo",false);EditorApplication.update-=Tick;EditorApplication.Exit(code);}
    }
}
