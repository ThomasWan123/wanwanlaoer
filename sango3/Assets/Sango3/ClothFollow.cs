using System.Collections.Generic;
using UnityEngine;
namespace Sango3
{
    // Follow-through for the cloth bones of a hero (the Skirt bone, which carries the hem and the cape, and Liu Bei's
    // sleeve drapes). Each one hangs from its pivot like a damped pendulum chasing the pose the animation gives it, so
    // when the body lunges, leaps or turns, the hem and cape trail behind and settle instead of moving as one rigid
    // block with the hips. Runs after the Animator, the polearm grip and the sleeve drape; follows the animator's speed,
    // so it freezes on pause and slows with the battle speed.
    [DefaultExecutionOrder(200)]
    public sealed class ClothFollow:MonoBehaviour
    {
        sealed class Piece{public Transform bone;public Vector3 localHang;public float length,maxAngle,stiffness,damping;public Vector3 tip,velocity;public bool ready,collide;}
        readonly List<Piece> pieces=new List<Piece>();Animator animator;
        // Largest swing away from the animated pose in the last frame, in degrees (for the review recordings).
        public float LastSwing{get;private set;}
        public static void Attach(GameObject hero)
        {
            var skin=hero.GetComponent<SkinnedMeshRenderer>();if(!skin||skin.bones==null)return;
            ClothFollow follow=null;
            foreach(var b in skin.bones){if(!b)continue;
                if(b.name=="Skirt"){follow=follow??hero.AddComponent<ClothFollow>();follow.Add(b,hero.transform,1.4f,14,55,8.5f);}
                else if(b.name.EndsWith("SleeveDrape")){follow=follow??hero.AddComponent<ClothFollow>();follow.Add(b,hero.transform,.8f,16,150,17,true);}
                // 0.25: Liu Bei's sleeves are two-bone chains; the lower bone swings more freely than the upper one.
                else if(b.name.EndsWith("SleeveTip")){follow=follow??hero.AddComponent<ClothFollow>();follow.Add(b,hero.transform,.9f,30,70,9,true);}}
            // Sleeves are kept out of the torso: a capsule from hip to chest height (hero space, 4.4 units tall).
            if(follow&&follow.pieces.Exists(p=>p.collide)){follow.body=hero.transform;follow.capsuleA=new Vector3(0,1.5f,0);follow.capsuleB=new Vector3(0,3.1f,0);follow.capsuleRadius=.42f;}
        }
        // Hem and cape: soft and slow. Sleeves hang from a fast-moving arm, so they are stiffer and only trail it a little.
        Transform body;Vector3 capsuleA,capsuleB;float capsuleRadius;
        public static bool NoCollision; // measurement switch (editor comparisons)
        void Add(Transform bone,Transform body,float length,float maxAngle,float stiffness,float damping,bool collide=false)
        {
            // The hanging direction in the bone's own space, taken from the prefab's rest pose (straight down the body).
            pieces.Add(new Piece{bone=bone,localHang=Quaternion.Inverse(bone.rotation)*(-body.up),length=length,maxAngle=maxAngle,stiffness=stiffness,damping=damping,collide=collide});
        }
        void Awake(){animator=GetComponent<Animator>();}
        void LateUpdate()=>Step(Time.deltaTime*(animator?animator.speed:1));
        // One simulation step; editor review reels call it directly with 1/30 s while sampling clips.
        public void Step(float dt)
        {
            if(dt<=0)return;dt=Mathf.Min(dt,1/20f);
            float scale=transform.lossyScale.y;LastSwing=0;
            foreach(var p in pieces){
                var pivot=p.bone.position;var hang=p.bone.rotation*p.localHang;float length=p.length*scale;var target=pivot+hang*length;
                if(!p.ready||(p.tip-target).sqrMagnitude>length*length*4){p.tip=target;p.velocity=Vector3.zero;p.ready=true;continue;}
                p.velocity+=((target-p.tip)*p.stiffness-p.velocity*p.damping)*dt;p.tip+=p.velocity*dt;
                var dir=(p.tip-pivot).normalized;float angle=Vector3.Angle(hang,dir);
                if(angle>p.maxAngle){dir=Vector3.Slerp(hang,dir,p.maxAngle/angle);p.velocity*=.5f;}
                if(p.collide&&body&&!NoCollision){var a=body.TransformPoint(capsuleA);var b=body.TransformPoint(capsuleB);var tipNow=pivot+dir*length;var ab=b-a;var c=a+ab*Mathf.Clamp01(Vector3.Dot(tipNow-a,ab)/ab.sqrMagnitude);float r=capsuleRadius*scale;var away=tipNow-c;
                    // A sleeve tip inside the torso is pushed out to its surface (the arm may cross the chest; the cloth may not).
                    if(away.sqrMagnitude<r*r){if(away.sqrMagnitude<1e-8f)away=body.forward;var outside=c+away.normalized*r;dir=(outside-pivot).normalized;p.velocity*=.5f;}}
                p.tip=pivot+dir*length;LastSwing=Mathf.Max(LastSwing,Vector3.Angle(hang,dir));
                p.bone.rotation=Quaternion.FromToRotation(hang,dir)*p.bone.rotation;
            }
        }
    }
}
