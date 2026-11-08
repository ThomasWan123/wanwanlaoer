using System.Collections.Generic;
using UnityEngine;
namespace Sango3
{
    public partial class BattleController
    {
        sealed class Convoy {public Transform view;public int next=1;public float hp=18,damageTimer;}
        readonly List<Convoy> convoys=new List<Convoy>();
        int escorted,captured,commanders,breached,lostConvoys,tacticCharges=1;
        float tacticCooldown,beaconTime,rallyTime;public int TacticsUsed {get;private set;}
        public bool Won {get;private set;}
        public int ObjectiveProgress=>Stage.Scenario.Objective==BattleObjective.Escort?escorted:Stage.Scenario.Objective==BattleObjective.Supplies?captured:Stage.Scenario.Objective==BattleObjective.Commanders?commanders:Stage.Scenario.Objective==BattleObjective.Siege?breached:wave;
        public bool ObjectiveSatisfied=>Stage.Scenario.Objective==BattleObjective.Defend||ObjectiveProgress>=Stage.Scenario.Target;
        bool ObjectiveCanWait=>Stage.Scenario.Objective==BattleObjective.Siege&&tacticCharges>0||Stage.Scenario.Objective==BattleObjective.Escort&&convoys.Count>0;
        float VisionScale=>Stage.Scenario.Night&&beaconTime<=0?.82f:1;
        public string ObjectiveStatus=>Stage.Scenario.Objective==BattleObjective.Defend?Stage.Scenario.Title+" · 守线 "+wave+"/"+MaxWaves:Stage.Scenario.ObjectiveText+"  "+ObjectiveProgress+"/"+Stage.Scenario.Target+(Stage.Scenario.Objective==BattleObjective.Escort?" · 损失 "+lostConvoys:"");
        public string TacticLabel=>Stage.Scenario.TacticName+(tacticCooldown>0?" · "+Mathf.CeilToInt(tacticCooldown)+"秒":" · "+tacticCharges+"次");
        public bool TacticReady=>started&&!paused&&!finished&&tacticCharges>0&&tacticCooldown<=0;
        public void UseTactic()
        {
            if(!TacticReady)return;var s=Stage.Scenario;
            // Offensive actions require a visible target unless they also advance a siege objective.
            if(enemies.Count==0&&s.Objective!=BattleObjective.Siege&&(s.Tactic==BattleTactic.Fire||s.Tactic==BattleTactic.Flood||s.Tactic==BattleTactic.Volley))return;
            tacticCharges--;tacticCooldown=10;TacticsUsed++;Vector3 center=s.ActionPoint;
            if(s.Tactic==BattleTactic.Volley&&Selected!=null)center=Selected.view.transform.position;
            if(s.Objective==BattleObjective.Siege){breached=Mathf.Min(s.Target,breached+1);ScenarioScenery.Breach(breached);}
            if(s.Tactic==BattleTactic.Supplies)gold+=85;
            if(s.Tactic==BattleTactic.Repair)life=Mathf.Min(12,life+2);
            if(s.Tactic==BattleTactic.Beacon){beaconTime=12;morale=Mathf.Min(100,morale+18);}
            if(s.Tactic==BattleTactic.Rally){rallyTime=8;foreach(var c in convoys)c.hp=Mathf.Min(18,c.hp+9);}
            int visual=s.Tactic==BattleTactic.Fire?2:s.Tactic==BattleTactic.Flood?1:s.Tactic==BattleTactic.Rally?3:s.Tactic==BattleTactic.Volley?4:5;
            SkillVfx.Create(visual,center,6,this,true);ScenarioScenery.Pulse(s.Tactic,center,this);
            foreach(var e in enemies)if(Vector3.Distance(e.view.transform.position,center)<6){
                if(s.Tactic==BattleTactic.Fire)e.hp-=190*Stage.HealthScale;
                else if(s.Tactic==BattleTactic.Volley)e.hp-=160*Stage.HealthScale;
                else if(s.Tactic==BattleTactic.Flood){e.slow=6;e.hp-=70*Stage.HealthScale;}
                else if(s.Tactic==BattleTactic.Breach)e.hp-=100*Stage.HealthScale;
            }
            Message=s.TacticName+"！"+ObjectiveStatus;
        }
        void BeginScenarioWave()
        {
            if(wave>1)tacticCharges=Mathf.Min(4,tacticCharges+1);
            if(Stage.Scenario.Objective==BattleObjective.Escort&&wave<=Stage.Scenario.Target+1){
                var root=new GameObject("Evacuation convoy "+wave).transform;root.SetParent(actors.transform,false);root.position=Stage.Route[0]+Vector3.up*.1f;MakeConvoy(root);convoys.Add(new Convoy{view=root});
            }
        }
        void TickScenario(float dt)
        {
            tacticCooldown=Mathf.Max(0,tacticCooldown-dt);beaconTime=Mathf.Max(0,beaconTime-dt);rallyTime=Mathf.Max(0,rallyTime-dt);
            for(int i=convoys.Count-1;i>=0;i--){var c=convoys[i];var destination=Stage.Route[c.next];var delta=destination-c.view.position;delta.y=0;if(delta.sqrMagnitude>.001f)c.view.rotation=Quaternion.LookRotation(delta);
                c.view.position=Vector3.MoveTowards(c.view.position,destination,(rallyTime>0?3.1f:2.15f)*dt);c.damageTimer-=dt;
                if(c.damageTimer<=0){int threats=0;foreach(var e in enemies)if(e.hp>0&&Vector3.Distance(c.view.position,e.view.transform.position)<2.1f)threats+=e.boss?2:1;c.hp-=Mathf.Min(threats,3);c.damageTimer=1;}
                if(c.hp<=0){lostConvoys++;Destroy(c.view.gameObject);convoys.RemoveAt(i);if(lostConvoys>1){Finish(false);return;}continue;}
                if(Vector3.Distance(c.view.position,destination)<.05f)c.next++;if(c.next>=Stage.Route.Length){escorted++;gold+=35;hud.ShowFloating(c.view.position+Vector3.up*2,"护送抵达 +35金",new Color(.4f,.9f,1));Destroy(c.view.gameObject);convoys.RemoveAt(i);}
            }
        }
        void OnScenarioKill(Enemy enemy){if(enemy.cargo)captured++;if(enemy.boss)commanders++;}
        static void MakeCargo(Transform root){WorldArt.Shape("Grain cargo",PrimitiveType.Cube,new Vector3(0,1.6f,-.5f),new Vector3(.9f,.8f,.7f),new Color(.7f,.48f,.15f),root);var ring=VisualGeometry.Ring(root.position,1,new Color(1,.75f,.1f));ring.transform.SetParent(root,true);}
        static void MakeConvoy(Transform root){WorldArt.Shape("Cart bed",PrimitiveType.Cube,new Vector3(0,.5f,0),new Vector3(.9f,.25f,1.4f),new Color(.35f,.22f,.1f),root);WorldArt.Shape("Blue evacuation cover",PrimitiveType.Cube,new Vector3(0,1,0),new Vector3(.85f,.7f,1.25f),new Color(.2f,.55f,.7f),root);for(int side=-1;side<=1;side+=2){var wheel=WorldArt.Shape("Cart wheel",PrimitiveType.Cylinder,new Vector3(side*.55f,.35f,0),new Vector3(.7f,.1f,.7f),new Color(.2f,.17f,.1f),root);wheel.transform.localRotation=Quaternion.Euler(0,0,90);}var r=VisualGeometry.Ring(root.position,1.1f,new Color(.2f,.75f,1));r.transform.SetParent(root,true);}
    }
}
