using UnityEngine;
namespace Sango3
{
    // Generic rigs do not receive Humanoid OnAnimatorIK callbacks.
    public sealed class RecoveredPolearmGrip : MonoBehaviour
    {
        public Transform upper,lower,hand,shaft,rightHand;
        public Vector3 gripOffset=new Vector3(0,.45f,0);
        public Vector3 palmOffset,palmAxis=Vector3.up;
        // When set, the authored right hand is never turned to bring the shaft within the left hand's reach;
        // in a wide swing the supporting hand lets go and reaches toward the shaft instead (Guan Yu's sample moves).
        public bool keepRightHand;
        // When the shaft is turned into the support hand's reach, keep the point end up (spearmen at rest).
        public bool pointUp;
        // During a named move (keepRightHand), release, regrip and sliding happen over ReleaseSeconds / at SlideSpeed; StepSeconds overrides the frame time when baking.
        public bool continuous=true;public const float ReleaseSeconds=.16f,SlideSpeed=4.5f;public static float StepSeconds;
        float smoothAlong,smoothFree;bool smoothed;
        public void ResetContinuity(){smoothed=false;}
        public float SupportGap {get;private set;}
        // 0 holding the shaft, 1 fully let go; in between while releasing or taking hold again.
        public float Release {get;private set;} // how far the shaft lies beyond the support hand's reach
        public Vector3 PalmTarget {get;private set;}
        public Vector3 CurrentGripTarget {get;private set;}
        Quaternion restShaftRotation;bool initialized;Quaternion restHand;bool restHandCaptured;
        Quaternion authoredRightRotation,lastRightRotation;bool rightInitialized;
        void LateUpdate(){Solve();}
        // Forget the cached authored right-hand pose (editor baking poses the hand directly between solves).
        public void ResetAuthoring(){rightInitialized=false;}
        public void Solve()
        {
            if(!upper||!lower||!hand||!shaft)return;
            if(rightHand){
                if(rightInitialized&&Quaternion.Angle(rightHand.localRotation,lastRightRotation)<.001f)rightHand.localRotation=authoredRightRotation;
                else authoredRightRotation=rightHand.localRotation;
                rightInitialized=true;
            }
            if(!initialized){restShaftRotation=shaft.localRotation;initialized=true;}shaft.localRotation=restShaftRotation;
            Quaternion palmRotation=shaft.rotation*Quaternion.FromToRotation(palmAxis,Vector3.up);
            Vector3 offset=palmRotation*palmOffset,origin=shaft.position-offset;
            Vector3 a=upper.position,b=lower.position,c=hand.position,target=shaft.TransformPoint(gripOffset)-offset;
            float l1=Vector3.Distance(a,b),l2=Vector3.Distance(b,c);var delta=target-a;float distance=Mathf.Clamp(delta.magnitude,.001f,l1+l2-.001f);var direction=delta.normalized;
            // Let the supporting hand slide along the shaft during wide swings.
            // Choose the closest reachable grip instead of leaving the hand in mid-air.
            var axis=shaft.up;float preferred=Vector3.Dot(target-origin,axis);float closest=Vector3.Dot(a-origin,axis);
            float perpendicularDistance=Vector3.Distance(a,origin+axis*closest);float reach=l1+l2-.005f;
            if(!keepRightHand&&perpendicularDistance>reach*.92f){
                var reachable=a+transform.TransformDirection(new Vector3(0,-.35f,.35f));var adjusted=(reachable-shaft.position).normalized;if(pointUp?adjusted.y<0:Vector3.Dot(adjusted,axis)<0)adjusted=-adjusted;
                var turn=Quaternion.FromToRotation(axis,adjusted);
                if(rightHand)rightHand.rotation=turn*rightHand.rotation;else shaft.rotation=turn*shaft.rotation;
                axis=shaft.up;palmRotation=shaft.rotation*Quaternion.FromToRotation(palmAxis,Vector3.up);offset=palmRotation*palmOffset;origin=shaft.position-offset;target=shaft.TransformPoint(gripOffset)-offset;preferred=Vector3.Dot(target-origin,axis);closest=Vector3.Dot(a-origin,axis);perpendicularDistance=Vector3.Distance(a,origin+axis*closest);
            }
            float span=Mathf.Sqrt(Mathf.Max(0,reach*reach-perpendicularDistance*perpendicularDistance));
            float alongShaft=Mathf.Clamp(preferred,closest-span,closest+span);alongShaft=Mathf.Clamp(alongShaft,-.75f,.95f);
            // Continuity: the grip slides along the shaft and lets go / takes hold over time, never in one frame.
            float step=StepSeconds>0?StepSeconds:Time.deltaTime;
            if(continuous&&keepRightHand&&smoothed){alongShaft=Mathf.MoveTowards(smoothAlong,alongShaft,SlideSpeed*step);}smoothAlong=alongShaft;
            target=origin+axis*alongShaft;CurrentGripTarget=target;PalmTarget=target+offset;
            // A shaft out of reach: rather than a hand hovering beside it, the support arm swings free to the side for balance.
            float gap=Mathf.Max(0,Vector3.Distance(target,a)-reach);SupportGap=gap;float free=keepRightHand?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.02f,.14f,gap)):0;
            if(continuous&&keepRightHand&&smoothed)free=Mathf.MoveTowards(smoothFree,free,step/ReleaseSeconds);smoothFree=free;smoothed=true;Release=free;
            if(!restHandCaptured){restHand=hand.localRotation;restHandCaptured=true;}
            if(free>0)target=Vector3.Lerp(target,a+transform.TransformDirection(new Vector3(.38f,-.92f,-.12f)).normalized*reach*.93f,free);
            delta=target-a;distance=Mathf.Clamp(delta.magnitude,.001f,l1+l2-.001f);direction=delta.normalized;
            var pole=transform.TransformDirection(new Vector3(1,-.45f,-.2f));var perpendicular=Vector3.ProjectOnPlane(pole,direction).normalized;
            float along=(l1*l1-l2*l2+distance*distance)/(2*distance);float height=Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));var elbow=a+direction*along+perpendicular*height;
            upper.rotation=Quaternion.FromToRotation(b-a,elbow-a)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,a+direction*distance-lower.position)*lower.rotation;
            hand.rotation=free>0?Quaternion.Slerp(palmRotation,lower.rotation*restHand,free):palmRotation;
            if(rightHand)lastRightRotation=rightHand.localRotation;
        }
    }
}
