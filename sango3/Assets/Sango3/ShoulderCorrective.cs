using UnityEngine;
namespace Sango3
{
    // Shoulder and sleeve corrective shapes. Linear skinning loses volume where the shoulder turns far from the bind pose
    // (the pauldron caves in, the sleeve folds into a hard block, the armpit collapses). The editor bakes, for each arm,
    // blend shapes at several arm directions (Editor/ShoulderCorrectives); each one moves the shoulder region from the
    // linear result to a dual-quaternion result, which keeps the volume. At runtime the shapes are blended by the upper
    // arm's direction in chest space (inverse-angle weights, with the bind pose as a zero shape), after the animator,
    // the polearm grip and the sleeve drapes have posed the arm.
    [DefaultExecutionOrder(150)]
    public sealed class ShoulderCorrective:MonoBehaviour
    {
        [System.Serializable] public struct Shape{public int index;public int side;public Vector3 direction;}
        public Transform chest;public Transform[] upper=new Transform[2],lower=new Transform[2];
        // Bind-pose upper-arm direction per side, in chest space (the zero shape).
        public Vector3[] rest=new Vector3[2];
        public Shape[] shapes=new Shape[0];
        public bool active=true;public static bool Disabled;
        SkinnedMeshRenderer skin;readonly float[] weights=new float[32];
        void LateUpdate(){if(Disabled){if(!skin)skin=GetComponent<SkinnedMeshRenderer>();if(skin)for(int i=0;i<skin.sharedMesh.blendShapeCount;i++)skin.SetBlendShapeWeight(i,0);enabled=false;return;}Solve();}
        public void Solve()
        {
            if(!skin)skin=GetComponent<SkinnedMeshRenderer>();if(!skin||!chest)return;
            for(int side=0;side<2;side++){if(!upper[side]||!lower[side])continue;
                var dir=chest.InverseTransformDirection((lower[side].position-upper[side].position).normalized);
                float total=Weight(Vector3.Angle(dir,rest[side]));int count=0;
                for(int k=0;k<shapes.Length&&k<weights.Length;k++){if(shapes[k].side!=side){weights[k]=-1;continue;}weights[k]=Weight(Vector3.Angle(dir,shapes[k].direction));total+=weights[k];count++;}
                // A shape with index -1 is a zero node: an arm direction that should get no correction (it only takes weight).
                for(int k=0;k<shapes.Length&&k<weights.Length;k++)if(shapes[k].side==side&&shapes[k].index>=0)skin.SetBlendShapeWeight(shapes[k].index,active&&total>0?weights[k]/total*100:0);
            }
        }
        // Inverse-angle weight: dominant at the node itself, falling off steeply, zero beyond 110 degrees.
        // Continuous through the node (an earlier version jumped to a fixed value within 0.5 degrees, which made the shape
        // pop when the arm passed exactly through a node; found by the 0.23 trial acceptance check).
        static float Weight(float angle){float fall=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(70,110,angle));return fall*Mathf.Pow(30f/Mathf.Max(angle,.5f),3);}
    }
}
