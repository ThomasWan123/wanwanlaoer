using UnityEngine;
namespace Sango3
{
    // The battle camera: tactical overview (the configured stage view) or a combat close-up that follows
    // a hero, plus feedback on top. The ultimate's push runs on the battle clock, so it freezes with the
    // fight when paused and holds through hit-stop; shake is real time so it reads during hit-stop, but it
    // also stops while paused. Both end when the battle ends.
    public sealed class BattleCamera : MonoBehaviour
    {
        public enum View { Tactical, Close }
        public static View Mode {get;private set;}=View.Tactical;
        Vector3 tacticalPosition;Quaternion tacticalRotation;Vector3 basePosition;bool ready;
        float shakeAmplitude,shakeLength,shakeAge;float seed;
        Vector3 pushTarget;float pushAmount,pushCloseAmount,pushLength,pushAge=-1;
        BattleController battle;
        // Close-up framing: same viewing angle as the overview, this far from the focus point.
        const float CloseDistance=15.5f;
        public static BattleCamera Attach(Camera cam,BattleController owner)
        {
            var rig=cam.GetComponent<BattleCamera>();if(!rig)rig=cam.gameObject.AddComponent<BattleCamera>();
            rig.battle=owner;rig.tacticalPosition=cam.transform.position;rig.tacticalRotation=cam.transform.rotation;rig.basePosition=rig.tacticalPosition;rig.seed=Random.value*100;rig.ready=true;return rig;
        }
        static BattleCamera Rig(Camera cam)=>cam?cam.GetComponent<BattleCamera>():null;
        public static void Toggle()=>Mode=Mode==View.Tactical?View.Close:View.Tactical;
        public static void Shake(Camera cam,float strength,float seconds)
        {
            var rig=Rig(cam);if(!rig)return;float remaining=rig.shakeLength>0?rig.shakeAmplitude*Mathf.Max(0,1-rig.shakeAge/rig.shakeLength):0;
            rig.shakeAmplitude=Mathf.Max(remaining,strength);rig.shakeLength=seconds;rig.shakeAge=0;
        }
        // Move a fraction of the way toward a point, hold, and ease back; length in battle seconds.
        // Move a fraction of the way toward a point, hold, and ease back; length in battle seconds. The close-up is already
        // tight, so it uses its own fraction (negative = pull back) — also when the view is switched while a push runs.
        public static void Push(Camera cam,Vector3 point,float fraction,float closeFraction,float battleSeconds){var rig=Rig(cam);if(!rig)return;rig.pushTarget=point;rig.pushAmount=fraction;rig.pushCloseAmount=closeFraction;rig.pushLength=battleSeconds;rig.pushAge=0;}
        public bool Pushing=>pushAge>=0;
        void LateUpdate()
        {
            if(!ready)return;bool paused=battle&&battle.Paused,finished=battle&&battle.Finished;
            if(finished){pushAge=-1;shakeLength=0;}
            // Base pose: overview, or follow the focus hero at the overview's angle.
            var focus=battle&&Mode==View.Close?battle.CameraFocus:null;
            var goal=focus?battle.CameraFocusPoint-tacticalRotation*Vector3.forward*CloseDistance:tacticalPosition;
            basePosition=Vector3.Lerp(basePosition,goal,1-Mathf.Exp(-Time.unscaledDeltaTime*(focus?11:6)));
            var offset=Vector3.zero;
            if(pushAge>=0){if(!paused)pushAge+=battle?battle.BattleDeltaTime:Time.deltaTime;float u=pushAge/pushLength;
                if(u>=1)pushAge=-1;else{float k=u<.2f?Mathf.SmoothStep(0,1,u/.2f):u>.65f?Mathf.SmoothStep(1,0,(u-.65f)/.35f):1;offset+=(pushTarget-basePosition)*(Mode==View.Close?pushCloseAmount:pushAmount)*k;}}
            if(shakeLength>0&&!paused){shakeAge+=Time.unscaledDeltaTime;float u=shakeAge/shakeLength;
                if(u>=1)shakeLength=0;else{float k=shakeAmplitude*(1-u)*(1-u);float t=Time.unscaledTime*38+seed;offset+=transform.right*(Mathf.PerlinNoise(t,0)-.5f)*2*k+transform.up*(Mathf.PerlinNoise(0,t)-.5f)*2*k;}}
            transform.SetPositionAndRotation(basePosition+offset,tacticalRotation);
        }
    }
}
