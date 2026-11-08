using UnityEngine;
namespace Sango3
{
    public sealed class BurningShip:MonoBehaviour
    {
        ParticleSystem flames;BattleController battle;
        public static void Ignite(GameObject ship){
            if(ship.GetComponent<BurningShip>())return;var fire=ship.AddComponent<BurningShip>();fire.battle=FindObjectOfType<BattleController>();
            var go=new GameObject("Deck fire");go.transform.SetParent(ship.transform,false);go.transform.localPosition=Vector3.up*.4f;
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.startLifetime=new ParticleSystem.MinMaxCurve(.6f,1.2f);main.startSpeed=new ParticleSystem.MinMaxCurve(.5f,1.7f);main.startSize=new ParticleSystem.MinMaxCurve(.15f,.5f);main.startColor=new ParticleSystem.MinMaxGradient(new Color(1,.18f,.015f,.7f),new Color(1,.65f,.12f,.85f));main.maxParticles=48;main.simulationSpace=ParticleSystemSimulationSpace.Local;
            var emission=ps.emission;emission.rateOverTime=25;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(2.5f,.1f,.7f);shape.rotation=new Vector3(-90,0,0);
            var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(1,.5f,.05f),0),new GradientColorKey(new Color(.3f,.08f,.01f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.75f,.2f),new GradientAlphaKey(0,1)});color.color=gradient;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=Resources.Load<Material>("Skills/Glow");ps.Play();fire.flames=ps;
        }
        void Update(){if(!flames)return;var main=flames.main;main.simulationSpeed=battle?(battle.Paused||battle.Finished?0:battle.Speed):1;}
    }
}
