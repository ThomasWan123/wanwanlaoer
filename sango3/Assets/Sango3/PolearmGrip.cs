using UnityEngine;
namespace Sango3
{
    public class PolearmGrip:MonoBehaviour
    {
        public Transform shaft;Animator animator;
        void Awake(){animator=GetComponent<Animator>();}
        void OnAnimatorIK(int layer){if(!animator||!shaft)return;animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,.8f);animator.SetIKPosition(AvatarIKGoal.LeftHand,shaft.TransformPoint(new Vector3(0,.32f,0)));}
    }
}
