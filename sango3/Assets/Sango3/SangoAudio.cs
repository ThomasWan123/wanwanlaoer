using System.Collections.Generic;
using UnityEngine;
namespace Sango3
{
    // Battle audio: a small voice pool for effects and one looping ambience bed, each with its own bus
    // volume. Delays are given in battle seconds so a swing sound lines up with the blade at 1x and 2x.
    public static class SangoAudio
    {
        public static float EffectsVolume=.9f,AmbienceVolume=.32f;
        static readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        static readonly Dictionary<string,float> lastPlayed=new Dictionary<string,float>();
        static AudioSource[] voices;static AudioSource ambience;static int next;static GameObject host;
        static readonly Dictionary<string,float> Levels=new Dictionary<string,float>{{"swing",.55f},{"swing_heavy",.7f},{"hit",.75f},{"hit_heavy",.95f},{"ult_charge",.8f},{"ult_slam",1f},{"war_drums",.7f}};
        static void Ensure()
        {
            if(host)return;pending.Clear();host=new GameObject("Battle audio");host.AddComponent<Clock>();voices=new AudioSource[14];
            for(int i=0;i<voices.Length;i++){voices[i]=host.AddComponent<AudioSource>();voices[i].playOnAwake=false;voices[i].spatialBlend=0;}
            ambience=host.AddComponent<AudioSource>();ambience.loop=true;ambience.playOnAwake=false;ambience.spatialBlend=0;
        }
        public static AudioClip Clip(string name){if(!clips.TryGetValue(name,out var c)||!c){c=Resources.Load<AudioClip>("Audio/"+name);clips[name]=c;}return c;}
        // Sounds waiting for a moment in battle time (a swing timed to its blade contact). They count down on
        // the battle clock, so pausing, switching speed or hit-stop after queueing keeps them on the frame.
        sealed class Pending{public string name;public Vector3 world;public float volume,remaining;public object owner;public int frame;}
        static readonly List<Pending> pending=new List<Pending>();
        // Runs after BattleController; a sound queued this frame starts counting next frame, as the blow it leads does.
        [DefaultExecutionOrder(100)] sealed class Clock:MonoBehaviour{void Update(){Tick(Fx.Delta);}}
        static void Tick(float dt)
        {
            for(int i=0;i<pending.Count;i++){var p=pending[i];if(p.frame==Time.frameCount)continue;p.remaining-=dt;if(p.remaining>0)continue;pending.RemoveAt(i--);Play(p.name,p.world,p.volume);}
        }
        // Battle times at which sounds actually started, for the sync check (editor and development builds).
        public static readonly List<(string name,float time)> Fired=new List<(string,float)>();
        public static void Cancel(object owner){pending.RemoveAll(p=>p.owner==owner);}
        public static void Play(string name,Vector3 world,float volume=1,float battleDelay=0,object owner=null)
        {
            var clip=Clip(name);if(!clip)return;Ensure();
            if(battleDelay>0){pending.Add(new Pending{name=name,world=world,volume=volume,remaining=battleDelay,owner=owner,frame=Time.frameCount});return;}
            if(Application.isEditor||Debug.isDebugBuild){Fired.Add((name,Fx.Battle?Fx.Battle.BattleTime:Time.time));if(Fired.Count>256)Fired.RemoveAt(0);}
            // Many simultaneous hits in a dense fight would only add noise; keep one per 40 ms per sound.
            if(lastPlayed.TryGetValue(name,out var last)&&Time.unscaledTime-last<.04f)return;lastPlayed[name]=Time.unscaledTime;
            var v=voices[next];next=(next+1)%voices.Length;v.Stop();v.clip=clip;
            float level=Levels.TryGetValue(name,out var l)?l:.8f;v.volume=EffectsVolume*level*volume;v.pitch=Random.Range(.95f,1.05f);
            var cam=Camera.main;v.panStereo=cam?Mathf.Clamp((cam.WorldToViewportPoint(world).x-.5f)*.9f,-.6f,.6f):0;
            v.Play();
        }
        public static void StartAmbience(string name){var clip=Clip(name);if(!clip)return;Ensure();ambience.clip=clip;ambience.volume=AmbienceVolume;ambience.Play();}
        public static void Pause(bool paused){AudioListener.pause=paused;}
    }
}
