using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
namespace Sango3
{
    public enum BattleObjective { Defend, Escort, Supplies, Commanders, Siege }
    public enum BattleTheme { Village, Night, Pass, Ruins, River, Camp, Forest, Mountain }
    public enum BattleTactic { Volley, Fire, Flood, Rally, Supplies, Breach, Beacon, Repair }
    public sealed class HistoricalScenario
    {
        public string Briefing,EnemyArmy,Rule,Title;
        public BattleObjective Objective;public BattleTheme Theme;public BattleTactic Tactic;
        public int Target,RunnerEvery,ArmorEvery,Bonus,SurgeWave;public float EnemySpeed=1;
        public Vector3[] Route,Flank;public Vector3 ActionPoint;
        public bool NavalSiege=>Objective==BattleObjective.Siege&&Theme==BattleTheme.River&&Tactic==BattleTactic.Fire;
        public bool Night=>Theme==BattleTheme.Night;
        public string ObjectiveText=>Objective==BattleObjective.Escort?"护送 "+Target+" 支队伍离场":Objective==BattleObjective.Supplies?"截获 "+Target+" 辆粮车":Objective==BattleObjective.Commanders?"击败 "+Target+" 名敌将":Objective==BattleObjective.Siege?(NavalSiege?"引燃 "+Target+" 艘敌船":"破坏 "+Target+" 处敌营工事"):"守住全部波次";
        public string TacticName=>new[]{"齐射压阵","引燃火阵","借水阻敌","鼓舞护送","接应粮秣","破坏工事","举烽照明","抢修防线"}[(int)Tactic];
    }
    public static class HistoricalScenarios
    {
        // Strategic fiction inspired by the campaigns, not a claim to reconstruct historical battle maps.
        static HistoricalScenario S(string title,string army,string briefing,string rule,BattleTheme theme,BattleObjective goal,BattleTactic tactic,int target,string route,int runner=5,int armor=7,int bonus=0,int surge=0,float speed=1,string flank=null)
        {var r=Parse(route);return new HistoricalScenario{Title=title,EnemyArmy=army,Briefing=briefing,Rule=rule,Theme=theme,Objective=goal,Tactic=tactic,Target=target,Route=r,ActionPoint=r[r.Length/2],RunnerEvery=runner,ArmorEvery=armor,Bonus=bonus,SurgeWave=surge,EnemySpeed=speed,Flank=flank==null?null:Parse(flank)};}
        static readonly HistoricalScenario[] data={
            S("乡垒初战","黄巾军","黄巾起事波及颍川，先稳住乡垒。","步卒为主，熟悉部署与齐射。",BattleTheme.Village,BattleObjective.Defend,BattleTactic.Volley,0,"-13,7;-7,7;-7,1;2,1;2,7;9,7;9,-6;13,-6",0,0),
            S("草营借火","黄巾军","长社战事以火攻为灵感，诱敌进入草营。","第3波密集来袭；火阵在中央弯道生效。",BattleTheme.Camp,BattleObjective.Defend,BattleTactic.Fire,0,"-13,7;-9,7;-9,0;-2,0;-2,6;6,6;6,-6;13,-6",6,0,1,3),
            S("林间清剿","黄巾游兵","残部散入汝南林地，斥候沿两路穿插。","偶数波斥候走侧路；不能只守正面。",BattleTheme.Forest,BattleObjective.Defend,BattleTactic.Beacon,0,"-13,7;-10,7;-10,-3;-3,-3;-3,5;8,5;8,-6;13,-6",3,9,0,4,1.08f,"-13,-6;-7,-6;-7,0;-3,0;-3,5;8,5;8,-6;13,-6"),
            S("夜撤伤兵","黄巾残部","黄巾宛城攻防为背景，改编为夜间护送伤兵；非张绣袭曹操。","夜间射程降低；保护蓝色队伍到出口。",BattleTheme.Night,BattleObjective.Escort,BattleTactic.Rally,2,"-13,7;-5,7;-5,2;-10,2;-10,-5;2,-5;2,4;10,4;10,-6;13,-6",4,8),
            S("渠帅决战","黄巾精锐","广宗攻坚进入决战，敌方渠帅分批压阵。","敌将不只出现在终波；保留绝技应对重甲。",BattleTheme.Camp,BattleObjective.Commanders,BattleTactic.Volley,2,"-13,7;-8,7;-8,-4;0,-4;0,6;7,6;7,-6;13,-6",8,4,1),
            S("关前搦战","西凉军","诸侯联军逼近关隘，先锋迎战守将。","狭长关道与重甲敌将；齐射集中破阵。",BattleTheme.Pass,BattleObjective.Commanders,BattleTactic.Volley,2,"-13,7;-11,7;-11,-5;-5,-5;-5,6;3,6;3,-3;9,-3;9,-6;13,-6",9,3),
            S("截辎追击","西凉辎重军","追击董卓军的道路上，截断撤退辎重。","每波黄色粮车经过，至少截获3辆。",BattleTheme.Ruins,BattleObjective.Supplies,BattleTactic.Volley,3,"-13,7;-6,7;-6,-2;1,-2;1,5;10,5;10,-6;13,-6",3,8,0,4,1.12f),
            S("虎牢攻坚","西凉铁甲","以虎牢关前对峙为战役改编。","三轮敌将压阵；重甲比例更高。",BattleTheme.Pass,BattleObjective.Commanders,BattleTactic.Breach,3,"-13,7;-9,7;-9,-4;-1,-4;-1,7;5,7;5,-2;10,-2;10,-6;13,-6",8,3,1,5),
            S("废都护民","劫掠游兵","洛阳焚毁后的撤离行动，保护流民出城。","废墟长路；伤亡队伍不能被城防治疗替代。",BattleTheme.Ruins,BattleObjective.Escort,BattleTactic.Rally,3,"-13,7;-4,7;-4,3;-10,3;-10,-4;3,-4;3,5;9,5;9,-6;13,-6",5,8),
            S("破垒解围","长安守军","长安争夺改编为守线与破垒并行。","击退敌军之外，还需使用3次破坏工事。",BattleTheme.Pass,BattleObjective.Siege,BattleTactic.Breach,3,"-13,7;-7,7;-7,-5;0,-5;0,4;7,4;7,-1;11,-1;11,-6;13,-6",6,4),
            S("强弩拒骑","北方骑兵","界桥战事中的步骑对抗，强调迟滞与火力。","骑兵密集且更快；第2波即有突袭。",BattleTheme.Village,BattleObjective.Defend,BattleTactic.Volley,0,"-13,7;-11,7;-11,1;-4,1;-4,6;4,6;4,-5;10,-5;13,-6",2,8,2,2,1.15f),
            S("北海接应","黄巾围军","北海受围，以接应队伍穿越封锁为目标。","三队接应队伍与敌军交错行进。",BattleTheme.Forest,BattleObjective.Escort,BattleTactic.Rally,3,"-13,7;-8,7;-8,0;0,0;0,7;8,7;8,-4;3,-4;3,-7;13,-6",4,7),
            S("徐州运粮","围城军","徐州战局中的后勤压力，改编为截粮支援。","截获粮车补充军资；普通击杀收入减少。",BattleTheme.Village,BattleObjective.Supplies,BattleTactic.Supplies,3,"-13,7;-10,7;-10,-5;-2,-5;-2,3;5,3;5,7;10,7;10,-6;13,-6",5,5,1),
            S("泗水破垒","下邳守军","下邳围城与水攻背景下，借水势削弱守军。","放水减速中央河道，并计入破垒目标。",BattleTheme.River,BattleObjective.Siege,BattleTactic.Flood,3,"-13,7;-6,7;-6,0;-10,0;-10,-5;1,-5;1,5;9,5;9,-6;13,-6",6,3),
            S("白门收束","吕布残军","下邳战事末段，拦截试图突围的部将。","三名敌将必须被击败，放走会影响目标。",BattleTheme.Pass,BattleObjective.Commanders,BattleTactic.Volley,3,"-13,7;-3,7;-3,1;-8,1;-8,-5;4,-5;4,5;10,5;10,-6;13,-6",3,4,0,4),
            S("白马破阵","袁军先锋","建安五年，颜良渡河围白马。关羽望见颜良麾盖，策马刺之于万众之中。击败袁军先锋将与颜良。","高速先锋掩护敌将；齐射打击选中武将附近。",BattleTheme.Village,BattleObjective.Commanders,BattleTactic.Volley,2,"-13,7;-8,7;-8,-2;-1,-2;-1,6;6,6;6,-5;11,-5;13,-6",3,5,2),
            S("渡口夺粮","袁军辎重","延津战场的辎重与渡口，改编为截粮战。","双入口粮队；兼顾渡口和侧路。",BattleTheme.River,BattleObjective.Supplies,BattleTactic.Flood,3,"-13,7;-9,7;-9,2;-2,2;-2,-5;5,-5;5,6;10,6;10,-6;13,-6",4,7,0,4,1,"-13,-5;-9,-5;-9,2;-2,2;-2,-5;5,-5;5,6;10,6;10,-6;13,-6"),
            S("乌巢截粮","袁军护粮队","官渡相持期间，乌巢粮秣成为关键目标。","至少截获4辆粮车；火攻对中央粮道有效。",BattleTheme.Night,BattleObjective.Supplies,BattleTactic.Fire,4,"-13,7;-5,7;-5,-4;2,-4;2,2;-1,2;-1,7;10,7;10,-6;13,-6",6,3,1),
            S("官渡破垒","袁军主力","官渡长期相持，以破坏营垒结束对峙。","普通击杀赏金较少；破垒与守线同时完成。",BattleTheme.Camp,BattleObjective.Siege,BattleTactic.Breach,4,"-13,7;-10,7;-10,-4;-4,-4;-4,5;3,5;3,-1;9,-1;9,-6;13,-6",7,3,2,5),
            S("仓亭追击","袁军残部","仓亭战事改编为追击残部的拦截战。","连续疾行兵，波间休整只有1秒。",BattleTheme.Forest,BattleObjective.Defend,BattleTactic.Volley,0,"-13,7;-7,7;-7,0;-1,0;-1,6;7,6;7,-5;12,-5;13,-6",2,9,3,3,1.18f),
            S("新野设伏","南下先锋","以新野周边战事为背景的伏击改编。","偶数波由林间侧路进入；烽火扩大夜间射程。",BattleTheme.Night,BattleObjective.Defend,BattleTactic.Beacon,0,"-13,7;-8,7;-8,-4;0,-4;0,3;6,3;6,7;11,7;11,-6;13,-6",3,6,1,4,1,"-13,-6;-4,-6;-4,-4;0,-4;0,3;6,3;6,7;11,7;11,-6;13,-6"),
            S("博望火伏","曹军先锋","博望战事结合火伏题材进行游戏改编。","第4波大队入谷；火攻时机比一到冷却就放更重要。",BattleTheme.Forest,BattleObjective.Defend,BattleTactic.Fire,0,"-13,7;-11,7;-11,-3;-5,-3;-5,5;2,5;2,-4;8,-4;8,-6;13,-6",4,5,1,4),
            S("长坂护民","曹军追骑","长坂撤退与民众随行为背景，护送队伍离场。","护送4队；追骑很快，鼓舞可治疗并加速友军。",BattleTheme.Village,BattleObjective.Escort,BattleTactic.Rally,4,"-13,7;-6,7;-6,2;1,2;1,7;8,7;8,-3;3,-3;3,-6;13,-6",2,8,0,3,1.12f),
            S("江夏水寨","水寨守军","江夏水域战事改编为渡口粮道争夺。","河道交错；截获4辆粮车，放水迟滞护卫。",BattleTheme.River,BattleObjective.Supplies,BattleTactic.Flood,4,"-13,7;-9,7;-9,-5;-1,-5;-1,1;5,1;5,6;10,6;10,-6;13,-6",5,5,1),
            S("赤壁火船","曹军水师","赤壁火攻与水上营阵是本关的历史灵感。","引燃4艘敌船；中央火区可同时烧伤密集敌军。",BattleTheme.River,BattleObjective.Siege,BattleTactic.Fire,4,"-13,7;-7,7;-7,2;-2,2;-2,-5;4,-5;4,5;11,5;11,-6;13,-6",7,4,3,5),
            S("南郡攻坚","南郡守军","赤壁之后的南郡争夺，转入营垒攻坚。","重甲掩护工事；完成3次破垒再清场。",BattleTheme.Pass,BattleObjective.Siege,BattleTactic.Breach,3,"-13,7;-10,7;-10,0;-4,0;-4,-5;3,-5;3,6;9,6;9,-6;13,-6",6,3,2),
            S("合肥突袭","突袭骑军","合肥战事中的突击与守备，改编为多轮敌将冲阵。","疾行兵与敌将交替，需连续击败3名指挥官。",BattleTheme.Pass,BattleObjective.Commanders,BattleTactic.Volley,3,"-13,7;-5,7;-5,0;1,0;1,-5;7,-5;7,6;11,6;11,-6;13,-6",2,5,2,2,1.15f),
            S("定军夺峰","山地守军","定军山争夺以山道与高地压制为灵感。","折返山路，重甲敌将缓慢但耐打。",BattleTheme.Mountain,BattleObjective.Commanders,BattleTactic.Volley,3,"-13,7;-10,7;-10,-5;-5,-5;-5,6;0,6;0,-4;6,-4;6,5;11,5;11,-6;13,-6",9,3,1,4,.94f),
            S("樊城秋汛","樊城援军","樊城战事中洪水改变军势，本关强调水势控制。","秋汛漫堤；借水势迟滞中央重甲，非人为制造洪水。",BattleTheme.River,BattleObjective.Defend,BattleTactic.Flood,0,"-13,7;-8,7;-8,-4;-2,-4;-2,4;4,4;4,-1;10,-1;10,-6;13,-6",4,3,3,6),
            S("夷陵撤营","吴军追兵","夷陵战败与营地火灾背景，改编为撤营护送。","护送4队，偶数波追兵从侧路切入；抢救队伍优先。",BattleTheme.Forest,BattleObjective.Escort,BattleTactic.Rally,4,"-13,7;-9,7;-9,1;-3,1;-3,6;4,6;4,-5;10,-5;13,-6",3,4,2,5,1.08f,"-13,-6;-9,-6;-9,1;-3,1;-3,6;4,6;4,-5;10,-5;13,-6")
        };
        public static HistoricalScenario Get(int i)=>data[i];
        static Vector3[] Parse(string value){var chunks=value.Split(';');var r=new Vector3[chunks.Length];for(int i=0;i<r.Length;i++){var p=chunks[i].Split(',');r[i]=new Vector3(float.Parse(p[0],CultureInfo.InvariantCulture),0,float.Parse(p[1],CultureInfo.InvariantCulture));}return r;}
        public static float Distance(Vector3 p,Vector3[] route){float best=float.MaxValue;for(int i=1;i<route.Length;i++){var d=route[i]-route[i-1];best=Mathf.Min(best,Vector3.Distance(p,route[i-1]+d*Mathf.Clamp01(Vector3.Dot(p-route[i-1],d)/d.sqrMagnitude)));}return best;}
        public static Vector3[] DeploymentPoints(HistoricalScenario s){
            var candidates=new List<Vector3>();for(float x=-12;x<=12;x+=1)for(float z=-6;z<=8;z+=1){var p=new Vector3(x,0,z);float d=Distance(p,s.Route);if(d>=1.65f&&d<=3.4f&&(s.Flank==null||Distance(p,s.Flank)>=1.65f))candidates.Add(p);}
            var result=new List<Vector3>();float length=0;for(int i=1;i<s.Route.Length;i++)length+=Vector3.Distance(s.Route[i-1],s.Route[i]);
            for(int k=0;k<8;k++){float walk=length*(.08f+k*.115f);var target=s.Route[0];for(int i=1;i<s.Route.Length;i++){float d=Vector3.Distance(s.Route[i-1],s.Route[i]);if(walk<=d){target=Vector3.Lerp(s.Route[i-1],s.Route[i],walk/d);break;}walk-=d;}
                int best=-1;float score=float.MaxValue;for(int i=0;i<candidates.Count;i++){bool blocked=false;foreach(var p in result)if(Vector3.Distance(p,candidates[i])<2.8f){blocked=true;break;}if(blocked)continue;float value=Vector3.Distance(target,candidates[i]);if(value<score){score=value;best=i;}}if(best<0)throw new InvalidOperationException("No deployment point for "+s.Title);result.Add(candidates[best]);candidates.RemoveAt(best);
            }return result.ToArray();
        }
    }
}
