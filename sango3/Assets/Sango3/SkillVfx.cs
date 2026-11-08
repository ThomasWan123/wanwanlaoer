using System.Collections.Generic;
using UnityEngine;
namespace Sango3
{
    public class SkillVfx : MonoBehaviour
    {
        readonly List<LineRenderer> lines=new List<LineRenderer>();
        readonly List<Mesh> ribbons=new List<Mesh>();
        readonly List<Color[]> ribbonColors=new List<Color[]>();
        // How long the telegraph shows before the skill lands (battle seconds).
        public float telegraph=BattleController.SkillImpactDelay;
        BattleController battle;float age;int hero;Vector3 center;float radius;bool impact;
        public static readonly Color[] Colors={new Color(.2f,1,.6f),new Color(.2f,.7f,1),new Color(1,.36f,.12f),new Color(.4f,1,.5f),new Color(.62f,.65f,1),new Color(1,.8f,.25f)};
        public static SkillVfx Create(int hero,Vector3 center,float radius,BattleController owner,bool impact=false)
        {
            var effect=new GameObject(impact?"Skill impact":"Ultimate telegraph").AddComponent<SkillVfx>();effect.hero=hero;effect.center=center;effect.radius=radius;effect.battle=owner;effect.impact=impact;effect.Build();return effect;
        }
        LineRenderer Line(string name,Vector3[] points,float width,Color color,bool loop=false)
        {
            var go=new GameObject(name,typeof(LineRenderer));go.transform.SetParent(transform,false);var line=go.GetComponent<LineRenderer>();line.useWorldSpace=false;line.sharedMaterial=Resources.Load<Material>("Skills/Glow");line.positionCount=points.Length;line.SetPositions(points);line.loop=loop;line.widthMultiplier=width;line.startColor=line.endColor=color;line.numCapVertices=6;line.numCornerVertices=4;if(name.Contains("trail"))line.widthCurve=new AnimationCurve(new Keyframe(0,0),new Keyframe(.3f,1.5f),new Keyframe(.7f,1),new Keyframe(1,0));lines.Add(line);return line;
        }
        Vector3[] Circle(float r,float y,int count=80)
        {
            var pts=new Vector3[count];for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count;pts[i]=new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r);}return pts;
        }
        void BladeRibbon(int layer)
        {
            const int segments=48;var vertices=new Vector3[(segments+1)*2];var uv=new Vector2[vertices.Length];var colors=new Color[vertices.Length];var triangles=new int[segments*6];
            var tilt=Quaternion.Euler(layer==0?32:-24,layer*65,0);
            for(int i=0;i<=segments;i++){
                float t=i/(float)segments,angle=Mathf.Lerp(-135,75,t)*Mathf.Deg2Rad;
                float width=Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.7f)*(layer==0?.75f:.40f);
                var radial=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));
                for(int edge=0;edge<2;edge++){int n=i*2+edge;vertices[n]=tilt*(radial*(radius*.83f+(edge-.5f)*width))+Vector3.up*(1.15f+layer*.25f);uv[n]=new Vector2(t,edge);var c=Colors[hero];c.a=Mathf.Sin(t*Mathf.PI)*.65f;colors[n]=c;}
                if(i<segments){int k=i*6,n=i*2;triangles[k]=n;triangles[k+1]=n+2;triangles[k+2]=n+1;triangles[k+3]=n+1;triangles[k+4]=n+2;triangles[k+5]=n+3;}
            }
            var mesh=new Mesh{name="Tapered blade sweep"};mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateBounds();
            var go=new GameObject("Crescent blade light",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=Resources.Load<Material>("Skills/Glow");renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;ribbons.Add(mesh);ribbonColors.Add(colors);
        }
        void Build()
        {
            transform.position=center+Vector3.up*.2f;var color=Colors[hero];
            Line("Outer luminous ring",Circle(radius,0),impact?.28f:.15f,color,true);
            Line("Inner luminous ring",Circle(radius*.82f,.035f),.08f,color,true);
            if(!impact){for(int i=0;i<8;i++){float a=i*Mathf.PI/4;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));Line("Seal marking",new[]{d*radius*.69f,d*radius*.80f},.16f,color);}return;}
            if(hero==0||hero==4){
                for(int j=0;j<2;j++)BladeRibbon(j);
            }else if(hero==1){
                for(int j=0;j<6;j++){float a=j*Mathf.PI/3;var p=new Vector3(Mathf.Cos(a)*radius*.7f,0,Mathf.Sin(a)*radius*.7f);Line("Lightning strike",new[]{p+Vector3.up*7,p+new Vector3(.3f,4,0),p+new Vector3(-.25f,2,.2f),p},.18f,color);}
                Line("Formation star",new[]{new Vector3(0,.1f,radius),new Vector3(-radius,.1f,-radius*.65f),new Vector3(radius,.1f,-radius*.65f),new Vector3(0,.1f,radius)},.12f,color);
            }else{
                for(int j=0;j<10;j++){float a=j*Mathf.PI/5;var p=new Vector3(Mathf.Cos(a)*radius*.8f,0,Mathf.Sin(a)*radius*.8f);Line("Energy pillar",new[]{p,p+Vector3.up*(hero==2?2:4)},.16f,color);}
            }
        }
        void Update()
        {
            // Battle clock: the telegraph closes exactly when the skill resolves at 1x and 2x.
            float dt=battle?battle.BattleDeltaTime:Time.deltaTime;if(dt<=0)return;age+=dt;float duration=impact?1.65f:telegraph;
            float progress=Mathf.Clamp01(age/duration);transform.localScale=Vector3.one*(impact?Mathf.Lerp(.75f,1.18f,progress):Mathf.Lerp(.85f,1,progress));
            if(hero==0||hero==4||!impact)transform.rotation=Quaternion.Euler(0,age*(hero==4?130:45),0);
            foreach(var line in lines){var c=Colors[hero];c.a=impact?Mathf.Clamp01((1-progress)*1.8f):.7f;line.startColor=line.endColor=c;}
            for(int j=0;j<ribbons.Count;j++){var colors=ribbonColors[j];for(int i=0;i<colors.Length;i++){float t=(i/2)/(float)(colors.Length/2-1);colors[i].a=Mathf.Sin(t*Mathf.PI)*.65f*Mathf.Clamp01((1-progress)*1.8f);}ribbons[j].colors=colors;}
            if(age>=duration)Destroy(gameObject);
        }
        void OnDestroy(){foreach(var mesh in ribbons)if(mesh){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}}
    }
}
