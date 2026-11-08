using System.Collections.Generic;
using UnityEngine;
namespace Sango3
{
    // Guan Yu's layered effects: telegraph, blade trail that follows the real blade, impact flash, slash
    // mark, sparks, dust, and for the cleave a dragon crescent, ground crack, shockwave and debris.
    public static class GuanyuVfx
    {
        public static readonly Color Jade=new Color(.35f,1f,.62f),HotJade=new Color(.75f,1f,.86f),Gold=new Color(1f,.78f,.32f),Spark=new Color(1f,.72f,.35f),Dust=new Color(.55f,.48f,.38f,.75f);

        public static void Swing(Transform hero,int heroIndex,int combo,BattleController battle)
        {
            var set=HeroMoves.For(heroIndex);var move=set.Combo[combo];
            Trail(hero,set).Open(move.contact-(combo==2?.16f:.13f),move.contact+.07f,combo==2?1f:combo==1?1.1f:1f,combo==2?set.Hot:set.Trail);
        }
        public static GuanyuTrail Trail(Transform hero)=>Trail(hero,HeroMoves.For(0));
        public static GuanyuTrail Trail(Transform hero,MoveSet set){var t=hero.GetComponent<GuanyuTrail>();if(!t){t=hero.gameObject.AddComponent<GuanyuTrail>();t.inner=set.BladeInner;t.outer=set.BladeOuter;}return t;}
        // World position of the weapon's tip (fan edge, spear point).
        public static Vector3 WeaponTip(Transform hero,MoveSet set){foreach(var t in hero.GetComponentsInChildren<Transform>())if(t.name=="Spear")return t.TransformPoint(set.BladeOuter);return hero.position+Vector3.up*2.5f;}
        // A forked bolt from the fan to its target with a flash at both ends.
        public static void Bolt(Vector3 from,Vector3 to,MoveSet set)
        {
            var go=new GameObject("Fan bolt",typeof(LineRenderer));var line=go.GetComponent<LineRenderer>();const int n=9;line.positionCount=n;line.useWorldSpace=true;
            for(int i=0;i<n;i++){float u=i/(float)(n-1);var p=Vector3.Lerp(from,to,u);if(i>0&&i<n-1)p+=Random.insideUnitSphere*.28f*Mathf.Sin(u*Mathf.PI);line.SetPosition(i,p);}
            line.sharedMaterial=Fx.Mat("fx_trail",true,2f);line.widthMultiplier=.16f;line.startColor=set.Hot;line.endColor=set.Trail;go.AddComponent<FxFade>().life=.14f;
            var flash=Fx.Card("Bolt flash",Fx.Mat("fx_soft",true,1.6f),from,Quaternion.identity,Vector2.one*1.1f,set.Hot,.12f,true);flash.alpha=u=>(1-u)*(1-u);
        }

        public static void Impact(Vector3 at,Vector3 forward,int move,BattleController battle,int heroIndex=0)
        {
            var set=HeroMoves.For(heroIndex);var HotJade=Color.Lerp(set.Trail,Color.white,.5f);var Gold=set.Hot;
            var chest=at+Vector3.up*1.3f;bool heavy=move==2;
            var flash=Fx.Card("Impact flash",Fx.Mat("fx_soft",true,1.6f),chest,Quaternion.identity,Vector2.one*(heavy?3.2f:2.2f),new Color(1,.92f,.75f),heavy?.16f:.11f,true);flash.scale=u=>.5f+u*.9f;flash.alpha=u=>(1-u)*(1-u);
            // The slash mark is drawn at the swing's angle: diagonal, flat, rising.
            float roll=move==0?-38:move==1?4:72;
            var slash=Fx.Card("Slash mark",Fx.Mat("fx_slash",true,1.5f),chest,Quaternion.identity,new Vector2(heavy?3.8f:3f,heavy?1.9f:1.5f),heavy?Gold:HotJade,heavy?.2f:.15f,true);slash.roll=roll;slash.scale=u=>.75f+u*.45f;slash.alpha=u=>u<.25f?1:1-(u-.25f)/.75f;
            Fx.Emit(chest,Quaternion.LookRotation(forward+Vector3.up*.35f),new Fx.Burst{texture="fx_spark",additive=true,count=heavy?26:move==1?12:14,speed=new Vector2(5,heavy?13:10),life=new Vector2(.14f,.32f),size=new Vector2(.05f,.1f),start=Spark,end=new Color(1,.3f,.05f),gravity=1.6f,stretch=.022f,spread=48,radius=.1f,boost=1.8f});
            if(!heavy&&move!=1)return;
            var ring=Fx.Card("Dust ring",Fx.Mat("fx_ring",false),Fx.OnGround(at,.12f),Fx.Ground(forward),Vector2.one,new Color(.86f,.78f,.62f,.6f),.32f);ring.scale=u=>.6f+u*(heavy?3.2f:2f);ring.alpha=u=>1-u;
            Fx.Emit(Fx.OnGround(at,.2f),Quaternion.Euler(-90,0,0),new Fx.Burst{texture="fx_smoke",additive=false,count=heavy?9:5,speed=new Vector2(.8f,2.2f),life=new Vector2(.5f,.9f),size=new Vector2(.8f,1.4f),start=Dust,end=new Color(.5f,.45f,.38f,0),gravity=-.05f,spread=70,radius=.5f});
        }

        // Ground strip showing where the cleave will land; its fill reaches the far end at the contact frame.
        public static void Telegraph(Vector3 origin,Vector3 direction,BattleController battle)
        {
            float length=GuanyuMoves.UltimateLength,width=GuanyuMoves.UltimateWidth,life=GuanyuMoves.Ultimate.contact;
            var center=Fx.OnGround(origin+direction*length*.5f,.1f);
            var frame=Fx.Card("Cleave telegraph",Fx.Mat("fx_strip",true,2f),center,Fx.Ground(direction),new Vector2(width,length),new Color(1,.45f,.15f,1),life);frame.alpha=u=>.55f+.45f*Mathf.Sin(u*Mathf.PI*6);
            var fill=Fx.Card("Cleave fill",Fx.Mat("fx_soft",true,1f),Fx.OnGround(origin,.11f),Fx.Ground(direction),new Vector2(width*1.1f,length),new Color(1,.55f,.2f,.8f),life);
            fill.keepScale=true;
            fill.gameObject.AddComponent<FxGrow>().Begin(Fx.OnGround(origin,.11f),direction,width,length,life);
        }

        public static void Cleave(Vector3 origin,Vector3 direction,BattleController battle)
        {
            float length=GuanyuMoves.UltimateLength;var land=origin+direction*1.9f;var rotation=Quaternion.LookRotation(direction);
            // Dragon crescent: a giant vertical blade arc sweeping down onto the strip.
            var arc=Fx.Card("Dragon crescent",Fx.Mat("fx_slash",true,2f),land+Vector3.up*2.6f,rotation*Quaternion.Euler(0,90,0),new Vector2(7.5f,3.8f),Gold,.32f);arc.scale=u=>.85f+u*.3f;arc.alpha=u=>u<.3f?1:1-(u-.3f)/.7f;
            var inner=Fx.Card("Dragon crescent core",Fx.Mat("fx_slash",true,2.4f),land+Vector3.up*2.4f,rotation*Quaternion.Euler(0,90,0),new Vector2(6.4f,3.1f),HotJade,.24f);inner.scale=u=>.8f+u*.3f;
            // Ground crack: a dark scar that stays a while, with a hot core that cools quickly.
            var crackCenter=Fx.OnGround(origin+direction*(length*.5f+.4f),.09f);
            var scar=Fx.Card("Ground crack",Fx.Mat("fx_crack",false),crackCenter,Fx.Ground(direction)*Quaternion.Euler(0,0,90),new Vector2(length+.8f,1.6f),new Color(1,1,1,1),2.6f);scar.scale=u=>1;scar.alpha=u=>u<.7f?1:1-(u-.7f)/.3f;
            var glow=Fx.Card("Ground crack glow",Fx.Mat("fx_crack",true,2.2f),crackCenter+Vector3.up*.01f,Fx.Ground(direction)*Quaternion.Euler(0,0,90),new Vector2(length+.8f,1.6f),Gold,.9f);glow.scale=u=>1;glow.alpha=u=>(1-u)*(1-u);
            var wave=Fx.Card("Shockwave",Fx.Mat("fx_ring",true,1.4f),Fx.OnGround(land,.14f),Fx.Ground(direction),Vector2.one,HotJade,.42f);wave.scale=u=>1+u*9;wave.alpha=u=>1-u;
            var dust=Fx.Card("Dust ring",Fx.Mat("fx_ring",false),Fx.OnGround(land,.12f),Fx.Ground(direction),Vector2.one,new Color(.82f,.74f,.6f,.7f),.6f);dust.scale=u=>1+u*6;dust.alpha=u=>1-u;
            var flash=Fx.Card("Cleave flash",Fx.Mat("fx_soft",true,1.8f),land+Vector3.up*1.2f,Quaternion.identity,Vector2.one*6,new Color(1,.9f,.6f),.2f,true);flash.scale=u=>.6f+u;flash.alpha=u=>(1-u)*(1-u);
            var mid=origin+direction*length*.5f;
            Fx.Emit(mid+Vector3.up*.2f,rotation*Quaternion.Euler(-90,0,0),new Fx.Burst{texture="fx_smoke",additive=false,count=22,speed=new Vector2(1,3.2f),life=new Vector2(.8f,1.5f),size=new Vector2(1.2f,2.4f),start=Dust,end=new Color(.5f,.45f,.38f,0),gravity=-.04f,box=new Vector3(1.6f,length,.2f)});
            Fx.Emit(mid+Vector3.up*.3f,rotation*Quaternion.Euler(-90,0,0),new Fx.Burst{texture="fx_smoke",additive=false,count=20,speed=new Vector2(4,8),life=new Vector2(.5f,.9f),size=new Vector2(.15f,.32f),start=new Color(.22f,.18f,.14f,1),end=new Color(.2f,.16f,.12f,0),gravity=2.4f,box=new Vector3(1.4f,length,.2f)});
            Fx.Emit(land+Vector3.up*.5f,Quaternion.Euler(-90,0,0),new Fx.Burst{texture="fx_spark",additive=true,count=34,speed=new Vector2(6,15),life=new Vector2(.2f,.45f),size=new Vector2(.07f,.13f),start=Spark,end=new Color(1,.3f,.05f),gravity=1.8f,stretch=.025f,spread=70,radius=.6f,boost=1.8f});
            var light=new GameObject("Cleave light").AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.85f,.5f);light.range=12;light.transform.position=land+Vector3.up*2;light.gameObject.AddComponent<FxLight>().peak=6;
        }
    }

    // Grows the telegraph fill from Guan Yu's feet to the far end of the strip by the contact frame.
    // Fades a line renderer out over its life on the battle clock.
    public sealed class FxFade : MonoBehaviour
    {
        public float life=.15f;float age;LineRenderer line;Color a,b;
        void Start(){line=GetComponent<LineRenderer>();a=line.startColor;b=line.endColor;}
        void Update(){age+=Fx.Delta;float k=Mathf.Clamp01(1-age/life);var c0=a;c0.a*=k;var c1=b;c1.a*=k;line.startColor=c0;line.endColor=c1;if(age>=life)Destroy(gameObject);}
    }
    public sealed class FxGrow : MonoBehaviour
    {
        Vector3 origin,direction;float width,length,life,age;
        public void Begin(Vector3 o,Vector3 d,float w,float l,float t){origin=o;direction=d;width=w;length=l;life=t;}
        void Update(){age+=Fx.Delta;float u=Mathf.Clamp01(age/life);float reach=Mathf.Lerp(.3f,length,u*u*(3-2*u));transform.position=origin+direction*reach*.5f;transform.localScale=new Vector3(width,reach,1);}
    }
    public sealed class FxLight : MonoBehaviour
    {
        public float peak=5,life=.35f;float age;Light view;
        void Awake(){view=GetComponent<Light>();}
        void Update(){age+=Fx.Delta;float u=age/life;view.intensity=peak*(1-u)*(1-u);if(u>=1)Destroy(gameObject);}
    }

    // A ribbon traced by the real guandao blade over the strike window, fading from the tail.
    [DefaultExecutionOrder(200)]
    public sealed class GuanyuTrail : MonoBehaviour
    {
        // Blade inner and outer points in the shaft's local space (RecoveredWeaponBuilder crescent blade).
        public Vector3 inner=new Vector3(.04f,1.02f,0),outer=new Vector3(.30f,1.72f,0);
        struct Sample{public Vector3 inner,outer;public float time;}
        readonly List<Sample> samples=new List<Sample>();
        Transform shaft;Mesh mesh;GameObject view;float clock,open,close,width;Color color;const float Life=.16f;
        public void Open(float start,float end,float w,Color c){open=clock+start;close=clock+end;width=w;color=c;}
        void Start()
        {
            foreach(var t in GetComponentsInChildren<Transform>())if(t.name=="Spear")shaft=t;
            view=new GameObject("Blade trail",typeof(MeshFilter),typeof(MeshRenderer));mesh=new Mesh{name="Blade trail"};mesh.MarkDynamic();view.GetComponent<MeshFilter>().sharedMesh=mesh;
            var r=view.GetComponent<MeshRenderer>();r.sharedMaterial=Fx.Mat("fx_trail",true,1.5f);r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
        }
        void OnDestroy(){if(view)Destroy(view);if(mesh)Destroy(mesh);}
        void LateUpdate()
        {
            if(!shaft)return;float dt=Fx.Delta;clock+=dt;
            if(clock>=open&&clock<=close&&dt>0){var inner=shaft.TransformPoint(this.inner);var outer=shaft.TransformPoint(this.outer);inner=Vector3.LerpUnclamped(outer,inner,width);samples.Add(new Sample{inner=inner,outer=outer,time=clock});}
            while(samples.Count>0&&clock-samples[0].time>Life)samples.RemoveAt(0);
            mesh.Clear();if(samples.Count<2)return;
            int n=samples.Count;var v=new Vector3[n*2];var c=new Color[n*2];var uv=new Vector2[n*2];var tri=new int[(n-1)*6];
            for(int i=0;i<n;i++){float age=(clock-samples[i].time)/Life;float a=Mathf.Pow(Mathf.Clamp01(1-age),1.6f)*(i==n-1?1:.95f);
                v[i*2]=samples[i].inner;v[i*2+1]=samples[i].outer;var edge=color;edge.a=a*.35f;var tip=Color.Lerp(color,Color.white,.25f);tip.a=a*.9f;c[i*2]=edge;c[i*2+1]=tip;float uu=(clock-samples[i].time)/Life;uv[i*2]=new Vector2(uu,0);uv[i*2+1]=new Vector2(uu,1);
                if(i<n-1){int k=i*6,b=i*2;tri[k]=b;tri[k+1]=b+1;tri[k+2]=b+2;tri[k+3]=b+1;tri[k+4]=b+3;tri[k+5]=b+2;}}
            mesh.vertices=v;mesh.colors=c;mesh.uv=uv;mesh.triangles=tri;mesh.RecalculateBounds();
        }
    }
}
