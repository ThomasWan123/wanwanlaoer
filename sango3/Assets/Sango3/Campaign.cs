using UnityEngine;
namespace Sango3
{
    public sealed class StageDefinition
    {
        public int Index,Chapter,Layout,Waves,StartingGold;
        public string Name,ChapterName,Boss;
        public Vector3[] Route,Slots;
        public float HealthScale,SpawnInterval;
        public Color GroundTint,Sky;
        public HistoricalScenario Scenario=>HistoricalScenarios.Get(Index);
        public int Count(int wave)=>6+wave*2+Chapter+Scenario.Bonus+(wave==Scenario.SurgeWave?5:0);
        public int EnemyKind(int wave,int ordinal)=>Index==0?0:Scenario.ArmorEvery>0&&ordinal%Scenario.ArmorEvery==0?2:Scenario.RunnerEvery>0&&ordinal%Scenario.RunnerEvery==0?1:0;
    }
    public static class Campaign
    {
        public const int Count=30;
        static readonly string[] Names={"颍川烽火","长社破围","汝南清剿","宛城夜袭","广宗决战","汜水先锋","荥阳追击","虎牢鏖兵","洛阳余烬","长安解围","界桥争锋","北海救援","徐州驰援","下邳水围","白门决战","白马破阵","延津渡口","乌巢断粮","官渡争锋","仓亭追击","新野伏兵","博望火计","长坂护民","江夏水寨","赤壁烈焰","南郡夺隘","合肥坚守","定军夺峰","樊城风雨","夷陵终章"};
        static readonly string[] Chapters={"黄巾烽烟","诸侯讨董","群雄逐鹿","官渡风云","赤壁江潮","三国鼎立"};
        static readonly string[] Bosses={"黄巾渠帅","西凉都督","重甲先锋","袁军上将","铁骑统领","镇关大将"};
        static readonly Color[] Tints={new Color(.85f,1,.82f),new Color(1,.83f,.62f),new Color(.88f,.93f,1),new Color(.84f,.9f,.75f),new Color(.7f,.9f,.92f),new Color(1,.83f,.69f)};
        static readonly StageDefinition[] stages=Build();
        static int selected;
        static string Prefix=>testPrefix??"sango3.campaign.v1.";
        static string testPrefix;
        public static StageDefinition Current=>stages[selected];
        public static StageDefinition Get(int index)=>stages[Mathf.Clamp(index,0,Count-1)];
        public static int Stars(int index)=>Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"stars."+index, index==0&&testPrefix==null?PlayerPrefs.GetInt("sango3.yingchuan.stars",0):0),0,3);
        public static int Unlocked {get{int n=Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"unlocked",Stars(0)>0?2:1),1,Count);return n;}}
        public static int TotalStars {get{int sum=0;for(int i=0;i<Count;i++)sum+=Stars(i);return sum;}}
        public static bool Select(int index){if(index<0||index>=Count||index>=Unlocked)return false;selected=index;return true;}
        public static void Complete(int index,int life){if(index<0||index>=Count||life<=0)return;int stars=life>=10?3:life>=5?2:1;PlayerPrefs.SetInt(Prefix+"stars."+index,Mathf.Max(Stars(index),stars));PlayerPrefs.SetInt(Prefix+"unlocked",Mathf.Max(Unlocked,Mathf.Min(Count,index+2)));PlayerPrefs.Save();}
// Test saves: editor checks, and development builds for the review launch (ReviewLaunch).
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static void BeginTest(string prefix){testPrefix=prefix;selected=0;}
        public static void SelectTest(int index){selected=Mathf.Clamp(index,0,Count-1);}
        public static void EndTest(){for(int i=0;i<Count;i++)PlayerPrefs.DeleteKey(Prefix+"stars."+i);PlayerPrefs.DeleteKey(Prefix+"unlocked");PlayerPrefs.Save();testPrefix=null;selected=0;}
#endif
        static Vector3 V(float x,float z)=>new Vector3(x,0,z);
        static StageDefinition[] Build()
        {
            var routes=new[]{
                new[]{V(-13,7),V(-7,7),V(-7,1),V(2,1),V(2,7),V(9,7),V(9,-6),V(13,-6)},
                new[]{V(-13,7),V(-10,7),V(-10,-4),V(-2,-4),V(-2,6),V(8,6),V(8,-6),V(13,-6)},
                new[]{V(-13,7),V(-5,7),V(-5,-5),V(3,-5),V(3,5),V(10,5),V(10,-6),V(13,-6)},
                new[]{V(-13,7),V(-13,0),V(-4,0),V(-4,7),V(5,7),V(5,-6),V(13,-6)},
                new[]{V(-13,7),V(-8,7),V(-8,-5),V(0,-5),V(0,5),V(8,5),V(8,-6),V(13,-6)}
            };
            var slots=new[]{
                new[]{V(-10,4),V(-4,5),V(-4,-2),V(0,4),V(5,4),V(6,-2),V(12,3),V(5,-5)},
                new[]{V(-13,2),V(-7,3),V(-6,-1),V(1,-2),V(1,2),V(4,3),V(5,-2),V(11,1)},
                new[]{V(-9,4),V(-8,-1),V(-2,3),V(-2,-2),V(0,0),V(6,2),V(7,-2),V(13,1)},
                new[]{V(-10,4),V(-9,-3),V(-7,4),V(-1,3),V(2,4),V(2,-2),V(8,2),V(9,-3)},
                new[]{V(-11,3),V(-5,3),V(-5,-2),V(-3,0),V(3,2),V(5,-2),V(11,2),V(11,-3)}
            };
            var all=new StageDefinition[Count];
            for(int i=0;i<Count;i++){
                int chapter=i/5,layout=i%5;var route=(Vector3[])routes[layout].Clone();var pads=(Vector3[])slots[layout].Clone();
                // Move interior bends and their paired deployment positions, keeping the entrance and gate fixed.
                float shift=(chapter%3-1)*.3f;if(i==0)shift=0;for(int j=1;j<route.Length-1;j++)route[j].x+=shift;for(int j=0;j<pads.Length;j++)pads[j].x+=shift;
                var scenario=HistoricalScenarios.Get(i);route=scenario.Route;if(i!=0)pads=HistoricalScenarios.DeploymentPoints(scenario);
                all[i]=new StageDefinition{Index=i,Chapter=chapter,Layout=layout,Name=Names[i],ChapterName=Chapters[chapter],Boss=scenario.EnemyArmy+"主将",Waves=6+chapter/2,StartingGold=300+chapter*30,Route=route,Slots=pads,HealthScale=1+i*.028f,SpawnInterval=1.15f-chapter*.05f,GroundTint=Tints[chapter],Sky=Color.Lerp(new Color(.3f,.45f,.53f),Tints[chapter],.2f)};
                if(i==BattleController.BaimaStage)all[i].Boss="颜良";
            }
            return all;
        }
    }
}
