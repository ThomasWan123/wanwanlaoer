using UnityEngine;
namespace Sango3
{
    // Shoulder helper bones (Guan Yu, 0.24). When the arm is raised overhead the upper arm turns up to ~170 degrees away
    // from the clavicle, and vertices skinned half to each collapse (linear skinning of two rotations that far apart
    // shrinks them to almost nothing). Each helper sits at the shoulder joint and takes half of the rotation from the
    // clavicle to the upper arm; the vertices that blended clavicle and upper arm now blend through it, so no pair of
    // bones is more than about half that angle apart. Runs after the animator and the polearm grip, before the shoulder
    // correctives and skinning.
    [DefaultExecutionOrder(120)]
    public sealed class ShoulderHelper:MonoBehaviour
    {
        public Transform[] helper=new Transform[2],clavicle=new Transform[2],upper=new Transform[2];
        // Bind-pose rotations of helper relative to clavicle and to upper arm (the helper's bind rotation = upper arm's).
        public Quaternion[] fromClavicle=new Quaternion[2],fromUpper=new Quaternion[2];
        [Range(0,1)] public float share=.5f;
        void LateUpdate()=>Solve();
        public void Solve()
        {
            for(int s=0;s<2;s++){if(!helper[s]||!clavicle[s]||!upper[s])continue;
                var a=clavicle[s].rotation*fromClavicle[s];var b=upper[s].rotation*fromUpper[s];
                helper[s].SetPositionAndRotation(upper[s].position,Quaternion.Slerp(a,b,share));}
        }
    }
}
