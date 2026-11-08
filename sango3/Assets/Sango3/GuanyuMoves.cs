using UnityEngine;
namespace Sango3
{
    // Guan Yu's sample move set. Clip lengths and contact times are shared by the editor that bakes the
    // clips and the battle code that resolves damage, so a blow always lands on the frame the blade connects.
    public static class GuanyuMoves
    {
        public const int Hero=0;
        public struct Move {public string state;public float length,contact;public float damage;}
        public static readonly Move[] Combo={
            new Move{state="Attack1",length=.70f,contact=.32f,damage=.85f},  // 斜劈: diagonal cut, single target
            new Move{state="Attack2",length=.75f,contact=.36f,damage=.75f}, // 横扫: sweep, up to three in the arc
            new Move{state="Attack3",length=.95f,contact=.50f,damage=1.5f}, // 拖刀上挑: rising drag-cut, knockback
        };
        public static readonly Move Ultimate=new Move{state="Ultimate",length=1.5f,contact=.85f,damage=1f};
        public const string UltimateName="青龙偃月·斩";
        public const float SweepAngle=130,SweepTargets=3,KnockBack=1.1f,UltimateLength=8.5f,UltimateWidth=2.6f;
        public static bool Uses(int hero)=>hero==Hero;
    }
}
