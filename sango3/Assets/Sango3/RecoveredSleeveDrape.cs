using UnityEngine;
namespace Sango3
{
    // Liu Bei's wide sleeves: each sleeve tip bone rides on the forearm (translation) and hangs toward gravity. Until
    // 0.20 it kept the body's orientation exactly, so when the forearm turned or swung across the chest the sleeve stayed
    // square to the body and folded into a hard slab where it met the forearm. It now takes part of the forearm's own
    // turn (follow, measured from the bind pose: the sleeve opening turns with the arm) while its hanging side stays
    // pointed at the ground. This is a controlled cloth rig, not a cloth simulation; ClothFollow adds the lag on top.
    [DefaultExecutionOrder(110)]
    public sealed class RecoveredSleeveDrape:MonoBehaviour
    {
        public Transform body,rightSleeve,leftSleeve;
        [Range(0,1)] public float follow=.55f;
        [Range(0,1)] public float followForward=.3f;public static float turnFrom=70;
        Transform[] forearm,hand;Vector3[] restAxis;bool ready;
        void Prepare()
        {
            ready=true;var sleeves=new[]{rightSleeve,leftSleeve};forearm=new Transform[2];hand=new Transform[2];restAxis=new Vector3[2];
            var skin=body?body.GetComponent<SkinnedMeshRenderer>():null;
            for(int s=0;s<2;s++){if(!sleeves[s])continue;forearm[s]=sleeves[s].parent;foreach(Transform c in forearm[s])if(c.name.EndsWith("Hand"))hand[s]=c;
                restAxis[s]=Vector3.zero;if(!skin||!hand[s])continue;
                // Forearm direction in the bind pose (body space), from the skin's bind poses.
                int f=System.Array.IndexOf(skin.bones,forearm[s]),h=System.Array.IndexOf(skin.bones,hand[s]);if(f<0||h<0)continue;
                var bind=skin.sharedMesh.bindposes;restAxis[s]=((Vector3)bind[h].inverse.GetColumn(3)-(Vector3)bind[f].inverse.GetColumn(3)).normalized;}
        }
        public void Solve()
        {
            if(!body)return;if(!ready)Prepare();var sleeves=new[]{rightSleeve,leftSleeve};
            for(int s=0;s<2;s++){var sleeve=sleeves[s];if(!sleeve)continue;
                if(restAxis[s]==Vector3.zero||follow<=0){sleeve.rotation=body.rotation;continue;}
                var axis=body.InverseTransformDirection(hand[s].position-forearm[s].position).normalized;var down=body.InverseTransformDirection(Vector3.down);
                var restUp=Vector3.ProjectOnPlane(Vector3.up,restAxis[s]);var up=Vector3.ProjectOnPlane(-down,axis);
                if(restUp.sqrMagnitude<1e-4f||up.sqrMagnitude<1e-4f){sleeve.rotation=body.rotation;continue;}
                var turn=Quaternion.LookRotation(axis,up)*Quaternion.Inverse(Quaternion.LookRotation(restAxis[s],restUp));
                // 0.25: when the forearm has turned far from its rest direction (the thrust), turning the sleeve with it pushes the
                // cloth into a mass in front of the body; there the sleeve follows less (followForward). Measured per move in 0.25.
                float forward=Mathf.SmoothStep(0,1,Mathf.InverseLerp(turnFrom,turnFrom+40,Quaternion.Angle(Quaternion.identity,turn)));
                sleeve.rotation=body.rotation*Quaternion.Slerp(Quaternion.identity,turn,Mathf.Lerp(follow,followForward,forward));}
        }
        void LateUpdate()=>Solve();
    }
}
