using System.Collections.Generic;
using UnityEngine;
namespace Sango3
{
    // Every playable hero's move set, to the standard set by Guan Yu: a three-blow combo and an ultimate, each a
    // baked clip whose contact time drives damage, effects and sound. Lengths and contact times are shared by the
    // editor that bakes the clips (GuanyuSample / HeroMoveKeys) and the battle code that resolves the blows.
    public sealed class MoveSet
    {
        public int Hero;public GuanyuMoves.Move[] Combo;public GuanyuMoves.Move Ultimate;
        public Color Trail,Hot;
        // Blade inner and outer points in the weapon's local space, for the trail that follows the real blade.
        public Vector3 BladeInner,BladeOuter;
        // Ranged heroes (Zhuge Liang's fan) loose a bolt at contact instead of cutting.
        public bool Ranged;public float SweepAngle=130,KnockBack=1.1f;
        public IEnumerable<GuanyuMoves.Move> All{get{foreach(var m in Combo)yield return m;yield return Ultimate;}}
    }
    public static class HeroMoves
    {
        static GuanyuMoves.Move M(string state,float length,float contact,float damage)=>new GuanyuMoves.Move{state=state,length=length,contact=contact,damage=damage};
        // Combo damage keeps each hero's damage per second close to the old single attack (base damage / old cooldown).
        static readonly MoveSet[] sets={
            new MoveSet{Hero=0,Combo=GuanyuMoves.Combo,Ultimate=GuanyuMoves.Ultimate,Trail=GuanyuVfx.Jade,Hot=GuanyuVfx.Gold,BladeInner=new Vector3(.04f,1.02f,0),BladeOuter=new Vector3(.30f,1.72f,0)},
            // 诸葛亮 羽扇: flick, sweeping gesture, raised-then-pointed command; bolts of wind and lightning.
            new MoveSet{Hero=1,Ranged=true,Combo=new[]{M("Attack1",.75f,.34f,.85f),M("Attack2",.80f,.38f,.85f),M("Attack3",1.0f,.52f,1.07f)},Ultimate=M("Ultimate",1.25f,.70f,1),
                Trail=new Color(.45f,.8f,1f),Hot=new Color(.85f,.95f,1f),BladeInner=new Vector3(0,.25f,0),BladeOuter=new Vector3(0,.72f,0)},
            // 张飞 丈八蛇矛: heavy thrust, sweep, overhead smash; the roar plants the spear.
            new MoveSet{Hero=2,Combo=new[]{M("Attack1",.70f,.32f,.80f),M("Attack2",.75f,.36f,.70f),M("Attack3",1.0f,.52f,1.18f)},Ultimate=M("Ultimate",1.25f,.70f,1),
                Trail=new Color(1f,.42f,.18f),Hot=new Color(1f,.78f,.45f),BladeInner=new Vector3(0,1.05f,0),BladeOuter=new Vector3(0,1.78f,0),KnockBack=1.4f},
            // 刘备 双股剑: diagonal cut, backhand sweep, thrust; raises the sword for 仁德.
            new MoveSet{Hero=3,Combo=new[]{M("Attack1",.60f,.28f,.75f),M("Attack2",.65f,.31f,.65f),M("Attack3",.80f,.40f,.90f)},Ultimate=M("Ultimate",1.20f,.70f,1),
                Trail=new Color(.55f,1f,.6f),Hot=new Color(1f,.92f,.55f),BladeInner=new Vector3(0,.25f,0),BladeOuter=new Vector3(0,1.12f,0),SweepAngle=110,KnockBack=.8f},
            // 赵云 龙胆亮银枪: quick thrust, sweep, lunging thrust; a full spinning sweep for 七进七出.
            new MoveSet{Hero=4,Combo=new[]{M("Attack1",.50f,.24f,1.0f),M("Attack2",.55f,.27f,.90f),M("Attack3",.70f,.36f,1.35f)},Ultimate=M("Ultimate",1.30f,.72f,1),
                Trail=new Color(.72f,.78f,1f),Hot=new Color(.95f,.97f,1f),BladeInner=new Vector3(0,1.08f,0),BladeOuter=new Vector3(0,1.75f,0)},
            // 曹操 倚天剑: cut, backhand, thrust; points the sword forward to command 魏武号令.
            new MoveSet{Hero=5,Combo=new[]{M("Attack1",.60f,.28f,.75f),M("Attack2",.65f,.31f,.65f),M("Attack3",.80f,.40f,.90f)},Ultimate=M("Ultimate",1.20f,.70f,1),
                Trail=new Color(.62f,.55f,1f),Hot=new Color(1f,.85f,.4f),BladeInner=new Vector3(0,.25f,0),BladeOuter=new Vector3(0,1.12f,0),SweepAngle=110,KnockBack=.8f},
        };
        public static bool Has(int hero)=>hero>=0&&hero<sets.Length&&sets[hero]!=null;
        public static MoveSet For(int hero)=>sets[hero];
    }
}
