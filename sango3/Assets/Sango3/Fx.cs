using System;
using System.Collections.Generic;
using UnityEngine;
namespace Sango3
{
    // Shared building blocks for combat effects. Everything runs on the battle clock: it freezes while
    // paused, doubles at 2x and slows during hit-stop, so effects stay in step with the blows they show.
    public static class Fx
    {
        static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        static Mesh quad;
        static BattleController battle;
        public static BattleController Battle{get{if(!battle)battle=UnityEngine.Object.FindObjectOfType<BattleController>();return battle;}}
        public static float Delta=>Battle?Battle.BattleDeltaTime:Time.deltaTime;
        public static float Scale=>Battle?(Battle.Paused||Battle.Finished?0:Battle.TimeScale):1;

        public static Material Mat(string texture,bool additive,float boost=1)
        {
            string key=texture+additive+boost;if(materials.TryGetValue(key,out var m)&&m)return m;
            m=new Material(Resources.Load<Shader>(additive?"Fx/FxAdditive":"Fx/FxAlpha")){name="Fx "+texture};
            m.mainTexture=texture==null?Texture2D.whiteTexture:Resources.Load<Texture2D>("Fx/"+texture);m.SetFloat("_Boost",boost);m.renderQueue=additive?3100:3050;
            materials[key]=m;return m;
        }
        static Mesh Quad(){if(quad)return quad;quad=new Mesh{name="Fx quad",vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0)},uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)},triangles=new[]{0,2,1,1,2,3},colors=new[]{Color.white,Color.white,Color.white,Color.white}};quad.RecalculateBounds();return quad;}

        // A textured card. Flat cards lie on the ground; billboards face the camera every frame.
        public static FxCard Card(string name,Material material,Vector3 position,Quaternion rotation,Vector2 size,Color color,float life,bool billboard=false)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetPositionAndRotation(position,rotation);go.transform.localScale=new Vector3(size.x,size.y,1);
            go.GetComponent<MeshFilter>().sharedMesh=Quad();var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
            var card=go.AddComponent<FxCard>();card.Begin(color,life,size,billboard);return card;
        }
        public static Vector3 OnGround(Vector3 p,float lift=.08f){p.y=ScenicArt.Height(p)+lift;return p;}
        public static Quaternion Ground(Vector3 forward){forward.y=0;if(forward.sqrMagnitude<1e-4f)forward=Vector3.forward;return Quaternion.LookRotation(Vector3.down,forward.normalized);}

        public struct Burst
        {
            public string texture;public bool additive;public int count;public Vector2 speed,life,size;public Color start,end;public float gravity,stretch,spread,radius;public Vector3 box;public float boost;
        }
        // One-shot particle burst on the battle clock.
        public static ParticleSystem Emit(Vector3 position,Quaternion rotation,Burst b)
        {
            var go=new GameObject("Fx burst "+b.texture);go.transform.SetPositionAndRotation(position,rotation);
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.duration=.1f;main.loop=false;main.playOnAwake=false;main.startSpeed=new ParticleSystem.MinMaxCurve(b.speed.x,b.speed.y);main.startLifetime=new ParticleSystem.MinMaxCurve(b.life.x,b.life.y);
            main.startSize=new ParticleSystem.MinMaxCurve(b.size.x,b.size.y);main.startColor=b.start;main.gravityModifier=b.gravity;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=Mathf.Max(8,b.count);main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
            var emission=ps.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)b.count)});
            var shape=ps.shape;shape.enabled=true;
            if(b.box!=Vector3.zero){shape.shapeType=ParticleSystemShapeType.Box;shape.scale=b.box;}else{shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=b.spread;shape.radius=Mathf.Max(.01f,b.radius);}
            var color=ps.colorOverLifetime;color.enabled=true;var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(b.start,0),new GradientColorKey(b.end,1)},new[]{new GradientAlphaKey(b.start.a,0),new GradientAlphaKey(b.start.a*.8f,.4f),new GradientAlphaKey(0,1)});color.color=g;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,b.stretch>0?AnimationCurve.Linear(0,1,1,.3f):AnimationCurve.EaseInOut(0,.6f,1,1.6f));
            var r=go.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=Mat(b.texture,b.additive,b.boost<=0?1:b.boost);r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
            if(b.stretch>0){r.renderMode=ParticleSystemRenderMode.Stretch;r.velocityScale=b.stretch;r.lengthScale=1;}
            go.AddComponent<FxClock>().life=b.life.y+.2f;ps.Play();return ps;
        }
    }

    // Keeps a particle system on the battle clock and removes it when done.
    public sealed class FxClock : MonoBehaviour
    {
        public float life=1;ParticleSystem ps;float age;
        void Awake(){ps=GetComponent<ParticleSystem>();}
        void Update(){var main=ps.main;main.simulationSpeed=Mathf.Max(0,Fx.Scale);age+=Fx.Delta;if(age>=life)Destroy(gameObject);}
    }

    // An animated card: scale and alpha curves over its life, on the battle clock.
    public sealed class FxCard : MonoBehaviour
    {
        public Func<float,float> scale=u=>1,alpha=u=>1-u;public Vector3 drift;public float spin;
        Color color;float life,age;Vector2 size;bool billboard;MaterialPropertyBlock block;MeshRenderer view;Camera cam;
        public void Begin(Color c,float l,Vector2 s,bool b){color=c;life=Mathf.Max(.01f,l);size=s;billboard=b;block=new MaterialPropertyBlock();view=GetComponent<MeshRenderer>();cam=Camera.main;Apply(0);}
        void Apply(float u)
        {
            if(!keepScale){float k=scale(u);transform.localScale=new Vector3(size.x*k,size.y*k,1);}
            var c=color;c.a*=Mathf.Clamp01(alpha(u));block.SetColor("_BaseColor",c);view.SetPropertyBlock(block);
        }
        void LateUpdate()
        {
            float dt=Fx.Delta;age+=dt;transform.position+=drift*dt;if(spin!=0)transform.Rotate(0,0,spin*dt,Space.Self);
            if(billboard&&cam){transform.rotation=Quaternion.LookRotation(cam.transform.forward,cam.transform.up)*Quaternion.Euler(0,0,roll);}
            Apply(Mathf.Clamp01(age/life));if(age>=life)Destroy(gameObject);
        }
        public float roll;public bool keepScale;
    }
}
