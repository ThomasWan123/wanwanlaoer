using System;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace Sango3.Editor
{
    public static class Validation090
    {
        public static void Run(){ScenarioMechanicsCheck.Run();CheckFirePause();ArtReview090.Capture();PlayCheck.Run();}
        static void CheckFirePause(){
            const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
            var root=new GameObject("Inactive fire clock fixture");root.SetActive(false);
            var battle=root.AddComponent<BattleController>();var ship=new GameObject("Fire fixture");
            try{
                BurningShip.Ignite(ship);BurningShip.Ignite(ship);
                if(ship.GetComponents<BurningShip>().Length!=1)throw new Exception("Duplicate fire component");
                var fire=ship.GetComponent<BurningShip>();typeof(BurningShip).GetField("battle",flags).SetValue(fire,battle);
                var ps=ship.GetComponentInChildren<ParticleSystem>();var update=typeof(BurningShip).GetMethod("Update",flags);
                typeof(BattleController).GetField("speed",flags).SetValue(battle,2);
                update.Invoke(fire,null);if(ps.main.simulationSpeed!=2)throw new Exception("Fire speed does not follow battle");
                battle.Pause();update.Invoke(fire,null);if(ps.main.simulationSpeed!=0)throw new Exception("Paused fire advances");
                battle.Pause();update.Invoke(fire,null);if(ps.main.simulationSpeed!=2)throw new Exception("Fire does not resume");
                typeof(BattleController).GetField("finished",flags).SetValue(battle,true);update.Invoke(fire,null);if(ps.main.simulationSpeed!=0)throw new Exception("Finished fire advances");
                File.WriteAllText("QA/fire-clock-090.txt","PASS duplicate ignition guard, 2x speed, pause, resume, terminal simulation speed. Controlled fixture, not visual flame quality acceptance.");
            }finally{UnityEngine.Object.DestroyImmediate(ship);UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
