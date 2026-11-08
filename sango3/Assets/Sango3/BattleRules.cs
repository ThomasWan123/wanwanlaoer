using UnityEngine;
namespace Sango3
{
    public static class BattleRules
    {
        public static readonly string[] Names = {"关羽", "诸葛亮", "张飞", "刘备", "赵云", "曹操"};
        public static readonly string[] Portraits = {"guanyu", "zhugeliang", "zhangfei", "liubei", "zhaoyun", "caocao"};
        public static readonly string[] Skills = {"青龙偃月·斩", "八阵图", "当阳怒吼", "仁德", "七进七出", "魏武号令"};
        public static readonly int[] Costs = {80,110,100,95,90,115};
        public static readonly float[] Damage = {34,23,42,20,26,28};
        public static readonly float[] Range = {4.3f,6,3.6f,5.2f,5,5.8f};
        public static readonly Color[] Colors = {new Color(.16f,.4f,.26f),new Color(.2f,.5f,.6f),new Color(.33f,.19f,.14f),new Color(.7f,.53f,.18f),new Color(.65f,.72f,.79f),new Color(.27f,.3f,.55f)};
        public static int UpgradeCost(int level) => 60 * level;
        public static Vector3[] Route=>Campaign.Current.Route;
        public static Vector3[] Slots=>Campaign.Current.Slots;
    }
}
