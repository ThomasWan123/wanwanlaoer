using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
namespace Sango3.Editor
{
    public static class ScenarioMechanicsCheck
    {
        const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
        static void Set(BattleController b,string name,object value)=>typeof(BattleController).GetField(name,Flags).SetValue(b,value);
        static object Get(BattleController b,string name)=>typeof(BattleController).GetField(name,Flags).GetValue(b);
        static void Update(BattleController b)=>typeof(BattleController).GetMethod("Update",Flags).Invoke(b,null);
        public static void Run(){
            var report=new StringBuilder();Campaign.BeginTest("sango3.qa.mechanics."+Guid.NewGuid().ToString("N")+".");
            try{
                foreach(int index in new[]{3,6,7,9}){
                    Campaign.SelectTest(index);var s=Campaign.Current.Scenario;string counter=s.Objective==BattleObjective.Escort?"escorted":s.Objective==BattleObjective.Supplies?"captured":s.Objective==BattleObjective.Commanders?"commanders":"breached";
                    for(int complete=0;complete<2;complete++){
                        var root=new GameObject("Controlled objective terminal fixture");root.SetActive(false);var b=root.AddComponent<BattleController>();
                        try{Set(b,"<Ready>k__BackingField",true);Set(b,"started",true);Set(b,"wave",b.MaxWaves);Set(b,"tacticCharges",0);Set(b,counter,s.Target-(complete==0?1:0));Update(b);
                            if(!b.Finished||b.Won!=(complete==1)||b.Life!=12)throw new Exception("Wrong terminal result for "+s.Objective);
                            if(complete==0){int selected=Campaign.Current.Index;b.NextStage();if(Campaign.Current.Index!=selected||Campaign.Stars(index)!=0)throw new Exception("Incomplete objective unlocked stage");}
                            else if(Campaign.Stars(index)!=3)throw new Exception("Completed objective did not save stars");
                            report.AppendLine("PASS "+s.Objective+" target="+s.Target+" progress="+b.ObjectiveProgress+" won="+b.Won+" life=12");
                        }finally{UnityEngine.Object.DestroyImmediate(root);}
                    }
                }
                Campaign.SelectTest(20);var fixture=new GameObject("Night and pause fixture");fixture.SetActive(false);var night=fixture.AddComponent<BattleController>();
                try{
                    var vision=typeof(BattleController).GetProperty("VisionScale",Flags);if(Mathf.Abs((float)vision.GetValue(night)-.82f)>.001f)throw new Exception("Night range unchanged");Set(night,"beaconTime",12f);if((float)vision.GetValue(night)!=1)throw new Exception("Beacon did not restore vision");
                    Set(night,"<Ready>k__BackingField",true);Set(night,"started",true);Set(night,"paused",true);Set(night,"tacticCooldown",5f);Update(night);night.UseTactic();if((float)Get(night,"tacticCooldown")!=5||night.TacticsUsed!=0||(float)Get(night,"beaconTime")!=12)throw new Exception("Paused scenario advanced");report.AppendLine("PASS night range, beacon restoration, paused timers and tactical actions");
                }finally{UnityEngine.Object.DestroyImmediate(fixture);}
                File.WriteAllText("QA/scenario-mechanics-090.txt",report.ToString());Debug.Log("SCENARIO_MECHANICS_PASS");
            }catch(Exception ex){report.AppendLine("FAIL "+ex);File.WriteAllText("QA/scenario-mechanics-090.txt",report.ToString());throw;}
            finally{Campaign.EndTest();}
        }
    }
}
