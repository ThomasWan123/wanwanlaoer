using UnityEngine;
namespace Sango3
{
    // Layered impact effects for the other five generals' ultimates, built from the same parts as Guan Yu's cleave
    // (flash, shockwave, ground mark, particles, light) on top of each hero's existing skill rings.
    public static class HeroUltimateVfx
    {
        public static void Impact(int hero,Vector3 center,BattleController battle)
        {
            var set=HeroMoves.For(hero);var ground=Fx.OnGround(center,.14f);
            Flash(center+Vector3.up*1.4f,set.Hot,hero==2?7:5.5f);
            switch(hero){
            case 1: // 八阵图: a glowing formation on the ground, lightning from the sky, a blue shockwave
                Ring(ground,set.Trail,8,.6f,true);Ring(ground+Vector3.up*.01f,set.Hot,5.5f,.45f,true);
                for(int i=0;i<6;i++){float a=i*Mathf.PI/3;var p=center+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*3.1f;GuanyuVfx.Bolt(p+Vector3.up*9,p+Vector3.up*.2f,set);Sparks(p,set,10,8);}
                Dust(center,3.4f,14,new Color(.6f,.7f,.8f,.5f));Light(center,set.Trail,6);break;
            case 2: // 当阳怒吼: concentric roar waves and a dust blast
                for(int i=0;i<3;i++){var w=Fx.Card("Roar wave",Fx.Mat("fx_ring",true,1.3f),ground+Vector3.up*(.02f*i),Fx.Ground(Vector3.forward),Vector2.one,set.Hot,.45f+i*.12f);float delay=i*.18f;w.scale=u=>1+Mathf.Max(0,u-delay)*12;w.alpha=u=>u<delay?0:1-u;}
                Dust(center,2.5f,26,new Color(.62f,.52f,.40f,.8f));Sparks(center,set,24,12);Light(center,set.Hot,5);break;
            case 3: // 仁德: a column of light with rising motes over the gate's defenders
                Column(center,new Color(.6f,1f,.65f),6);Rising(center,new Color(.7f,1f,.7f),30,2.5f);Ring(ground,new Color(.55f,1f,.6f),6,.7f,true);Light(center,new Color(.6f,1f,.6f),5);break;
            case 4: // 七进七出: spinning crescent slashes around the target and a dust ring
                for(int i=0;i<3;i++){var s=Fx.Card("Spin slash",Fx.Mat("fx_slash",true,2f),center+Vector3.up*(1+i*.4f),Quaternion.Euler(90,i*120,0),new Vector2(6.5f,3.2f),i==1?set.Hot:set.Trail,.32f+i*.06f);s.spin=720;s.scale=u=>.8f+u*.5f;s.alpha=u=>u<.3f?1:1-(u-.3f)/.7f;}
                Ring(ground,set.Hot,7,.45f,true);Dust(center,3,18,new Color(.6f,.55f,.46f,.7f));Sparks(center,set,30,14);Light(center,set.Trail,5);break;
            case 5: // 魏武号令: a burst (gold flare and column), a spread (two waves and banner-light sheets), and an afterglow
                Column(center,set.Hot,7);Flash(center+Vector3.up*2.2f,new Color(1f,.85f,.45f),8);
                Ring(ground,set.Hot,12,.55f,true);
                {var second=Fx.Card("Command wave",Fx.Mat("fx_ring",true,1.5f),ground+Vector3.up*.02f,Fx.Ground(Vector3.forward),Vector2.one,set.Trail,.85f);second.scale=u=>u<.2f?1:1+(u-.2f)*14;second.alpha=u=>u<.2f?0:1-(u-.2f)/.8f;}
                for(int i=0;i<6;i++){float a=i*Mathf.PI/3;var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var sheet=Fx.Card("Banner light",Fx.Mat("fx_soft",true,1.5f),center+dir*1.6f+Vector3.up*1.8f,Quaternion.LookRotation(dir),new Vector2(.9f,3.4f),set.Hot,.9f);sheet.drift=dir*4.5f;sheet.alpha=u=>Mathf.Sin(u*Mathf.PI)*.85f;}
                Rising(center,set.Hot,40,3.5f);Sparks(center,set,26,10);
                {var glow=Fx.Card("Command afterglow",Fx.Mat("fx_ring",true,1.2f),ground+Vector3.up*.01f,Fx.Ground(Vector3.forward),Vector2.one*5,set.Trail,1.6f);glow.scale=u=>1+.1f*Mathf.Sin(u*12);glow.alpha=u=>u<.15f?u/.15f:(1-u)*.9f;}
                Light(center,set.Hot,8);break;
            }
        }
        // Smaller version of the heal / command burst for each buffed ally: a soft column, a ground ring and rising motes.
        public static void Buff(int hero,Vector3 at)
        {
            var c=hero==3?new Color(.6f,1f,.65f):HeroMoves.For(hero).Hot;
            var col=Fx.Card("Buff column",Fx.Mat("fx_soft",true,1.4f),at+Vector3.up*1.6f,Quaternion.identity,new Vector2(1.2f,3.6f),c,.7f,true);col.scale=u=>.7f+.3f*Mathf.Sin(u*Mathf.PI);col.alpha=u=>Mathf.Sin(u*Mathf.PI)*.8f;
            Ring(Fx.OnGround(at,.13f),c,2.6f,.5f,true);Rising(at,c,10,.8f);
        }
        static void Flash(Vector3 at,Color c,float size){var f=Fx.Card("Ultimate flash",Fx.Mat("fx_soft",true,1.8f),at,Quaternion.identity,Vector2.one*size,c,.22f,true);f.scale=u=>.6f+u;f.alpha=u=>(1-u)*(1-u);}
        static void Ring(Vector3 at,Color c,float size,float life,bool additive){var r=Fx.Card("Ultimate ring",Fx.Mat("fx_ring",additive,1.4f),at,Fx.Ground(Vector3.forward),Vector2.one,c,life);r.scale=u=>1+u*size;r.alpha=u=>1-u;}
        static void Column(Vector3 at,Color c,float height){var col=Fx.Card("Light column",Fx.Mat("fx_soft",true,1.6f),at+Vector3.up*height*.5f,Quaternion.identity,new Vector2(2.4f,height*1.4f),c,.9f,true);col.scale=u=>.7f+.3f*Mathf.Sin(u*Mathf.PI);col.alpha=u=>Mathf.Sin(u*Mathf.PI);}
        static void Sparks(Vector3 at,MoveSet set,int count,float speed)=>Fx.Emit(at+Vector3.up*.6f,Quaternion.Euler(-90,0,0),new Fx.Burst{texture="fx_spark",additive=true,count=count,speed=new Vector2(speed*.4f,speed),life=new Vector2(.2f,.45f),size=new Vector2(.07f,.13f),start=set.Hot,end=set.Trail,gravity=1.6f,stretch=.025f,spread=70,radius=.6f,boost=1.8f});
        static void Dust(Vector3 at,float radius,int count,Color c)=>Fx.Emit(Fx.OnGround(at,.2f),Quaternion.Euler(-90,0,0),new Fx.Burst{texture="fx_smoke",additive=false,count=count,speed=new Vector2(1.5f,4),life=new Vector2(.6f,1.2f),size=new Vector2(1,2),start=c,end=new Color(c.r,c.g,c.b,0),gravity=-.04f,spread=85,radius=radius});
        static void Rising(Vector3 at,Color c,int count,float radius)=>Fx.Emit(Fx.OnGround(at,.2f),Quaternion.Euler(-90,0,0),new Fx.Burst{texture="fx_soft",additive=true,count=count,speed=new Vector2(1.5f,3.5f),life=new Vector2(.8f,1.4f),size=new Vector2(.15f,.32f),start=c,end=new Color(c.r,c.g,c.b,0),gravity=-.25f,spread=10,radius=radius,boost=1.6f});
        static void Light(Vector3 at,Color c,float peak){var l=new GameObject("Ultimate light").AddComponent<Light>();l.type=LightType.Point;l.color=c;l.range=11;l.transform.position=at+Vector3.up*2;l.gameObject.AddComponent<FxLight>().peak=peak;}
    }
}
