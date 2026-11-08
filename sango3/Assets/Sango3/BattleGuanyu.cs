using UnityEngine;
namespace Sango3
{
    // Guan Yu's combo and cleave resolution. Every blow resolves on the clip's contact frame.
    public partial class BattleController
    {
        void ResolveGuanyuHit(PendingHit h)
        {
            var t=h.tower;var origin=t.view.transform.position;var forward=t.view.transform.forward;forward.y=0;forward.Normalize();
            bool landed=false;var set=HeroMoves.For(t.hero);
            if(set.Ranged){
                // Fan bolts: the target must still be in range at contact; the third blow chills harder.
                if(!h.target.view||h.target.removed||h.target.hp<=0||!InReach(t,h.target,180))return;
                Strike(h.target,h.damage,t,h.move==2?1:0);h.target.slow=Mathf.Max(h.target.slow,h.move==2?2.5f:1.3f);
                var tip=GuanyuVfx.WeaponTip(t.view.transform,set);GuanyuVfx.Bolt(tip,h.target.view.transform.position+Vector3.up*1.3f,set);
                GuanyuVfx.Impact(h.target.view.transform.position,forward,h.move,this,t.hero);SangoAudio.Play(h.move==2?"hit_heavy":"hit",h.target.view.transform.position,.8f);HitStop(h.move==2?.06f:.03f);return;
            }
            if(h.move==1){
                // 横扫: everything in the arc in front of him, up to three.
                float reach=Reach(t);int struck=0;
                // The locked target must still be within the arc at contact, like everyone else the blade passes.
                if(h.target.view&&!h.target.removed&&h.target.hp>0&&InReach(t,h.target,set.SweepAngle*.5f)){Strike(h.target,h.damage,t,1);struck++;landed=true;}
                foreach(var e in enemies.ToArray()){if(struck>=GuanyuMoves.SweepTargets)break;if(e==h.target||e.removed||e.hp<=0)continue;var d=e.view.transform.position-origin;d.y=0;
                    if(d.magnitude<=reach&&Vector3.Angle(forward,d)<=set.SweepAngle*.5f){Strike(e,h.damage,t,1);struck++;landed=true;}}
            }else if(h.target.view&&!h.target.removed&&h.target.hp>0&&InReach(t,h.target,75)){Strike(h.target,h.damage,t,h.move);landed=true;if(h.move==2)KnockBack(h.target,set.KnockBack);}
            if(!landed)return;
            var at=h.target.view&&!h.target.removed?h.target.view.transform.position:origin+forward*2;
            GuanyuVfx.Impact(at,forward,h.move,this,t.hero);SangoAudio.Play(h.move==2?"hit_heavy":"hit",at);
            if(h.move==2){HitStop(.075f);BattleCamera.Shake(cam,.12f,.18f);}else HitStop(.035f);
        }
        // The blow must still meet the target at the contact frame: within weapon reach and in front of him.
        float Reach(Tower t)=>(BattleRules.Range[t.hero]+t.level*.3f)*VisionScale+.9f;
        bool InReach(Tower t,Enemy e,float halfAngle)
        {
            var d=e.view.transform.position-t.view.transform.position;d.y=0;var forward=t.view.transform.forward;forward.y=0;
            return d.magnitude<=Reach(t)&&Vector3.Angle(forward,d)<=halfAngle;
        }
        // During the wind-up Guan Yu keeps turning toward his target, as a swordsman tracks a moving man.
        void TrackTargets(float dt)
        {
            foreach(var h in pendingHits){if(h.move<0||!h.target.view||h.target.removed)continue;var d=h.target.view.transform.position-h.tower.view.transform.position;d.y=0;if(d.sqrMagnitude<.01f)continue;
                h.tower.view.transform.rotation=Quaternion.RotateTowards(h.tower.view.transform.rotation,Quaternion.LookRotation(d),360*dt);}
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Test hooks: move the locked target of a queued Guan Yu blow out of reach, and read an enemy's health.
        public GameObject DebugDisplaceTarget(int move){foreach(var h in pendingHits)if(h.move==move&&h.target.view){h.target.view.transform.position+=(h.target.view.transform.position-h.tower.view.transform.position).normalized*20;return h.target.view;}return null;}
        public int DebugPendingHits(int hero){int n=0;foreach(var h in pendingHits)if(h.tower.hero==hero)n++;return n;}
        public void DebugAddGold(int amount){gold+=amount;}
        public void DebugClearCooldown(){if(Selected!=null)Selected.ultimateCooldown=0;}
        public float DebugHealth(GameObject view){foreach(var e in enemies)if(e.view==view)return e.hp;return -1;}
#endif
        void Strike(Enemy e,float damage,Tower t,int tier)
        {
            e.hp-=damage;e.presentation.Hit();
            hud.ShowDamage(e.view.transform.position+Vector3.up*2.6f,Mathf.RoundToInt(damage),tier==2?1:0);
        }
        // Push an enemy back along the road it came from; never past its spawn point.
        void KnockBack(Enemy e,float distance)
        {
            if(e.boss)distance*=.4f;
            while(distance>0&&e.next>0){var back=e.route[e.next-1];float d=Vector3.Distance(e.view.transform.position,back);
                if(d>=distance){e.view.transform.position=Vector3.MoveTowards(e.view.transform.position,back,distance);break;}
                e.view.transform.position=back;distance-=d;if(e.next>1)e.next--;else break;}
            e.stun=Mathf.Max(e.stun,.35f);
        }
        // 青龙偃月·斩: a long strip ahead of him, heavy damage, knockback.
        void ResolveCleave(PendingSkill cast)
        {
            var t=cast.tower;float damage=150*(1+(t.skillRank-1)*.4f)*(1+t.equipment*.15f);
            GuanyuVfx.Cleave(cast.center,cast.direction,this);SangoAudio.Play("ult_slam",cast.center);BattleCamera.Shake(cam,.38f,.45f);HitStop(.11f);
            foreach(var e in enemies.ToArray()){if(e.removed||e.hp<=0)continue;var d=e.view.transform.position-cast.center;d.y=0;float along=Vector3.Dot(d,cast.direction);float side=Vector3.Cross(cast.direction,d).y;
                if(along<-.5f||along>GuanyuMoves.UltimateLength||Mathf.Abs(side)>GuanyuMoves.UltimateWidth*.5f)continue;
                e.hp-=damage;e.presentation.Hit();KnockBack(e,GuanyuMoves.KnockBack*1.4f);hud.ShowDamage(e.view.transform.position+Vector3.up*2.8f,Mathf.RoundToInt(damage),2);}
        }
    }
}
