using UnityEngine;
namespace Sango3
{
    public class CombatPresentation : MonoBehaviour
    {
        Transform[] boots; Transform weapon,cape; BattleController battle; Vector3 previous;float phase,attack,cast,hit;
        Animator animator;bool walking,flashed;Renderer[] bodyRenderers;MaterialPropertyBlock flash;
        Transform bar;Camera viewCamera;GameObject healthRoot;
        public void Initialize(bool health)
        {
            previous=transform.position;viewCamera=Camera.main;battle=FindObjectOfType<BattleController>();
            animator=GetComponentInChildren<Animator>();if(animator){animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;}bodyRenderers=GetComponentsInChildren<Renderer>();flash=new MaterialPropertyBlock();
            var all=GetComponentsInChildren<Transform>();var list=new System.Collections.Generic.List<Transform>();
            foreach(var t in all){if(t.name=="Boot")list.Add(t);if(t.name=="Spear")weapon=t;if(t.name=="Cape")cape=t;}
            boots=list.ToArray();
            if(health){healthRoot=new GameObject("Health");healthRoot.transform.SetParent(transform,false);healthRoot.transform.localPosition=Vector3.up*(animator?4.4f:2.8f);
                WorldArt.Shape("Health background",PrimitiveType.Cube,Vector3.zero,new Vector3(.85f,.1f,.04f),new Color(.12f,.13f,.1f),healthRoot.transform);
                bar=WorldArt.Shape("Health fill",PrimitiveType.Cube,new Vector3(0,0,-.03f),new Vector3(.8f,.07f,.04f),new Color(.8f,.18f,.1f),healthRoot.transform).transform;
            }
        }
        public void Health(float fraction){if(bar){float value=Mathf.Clamp01(fraction);bar.localScale=new Vector3(.8f*value,.07f,.04f);bar.localPosition=new Vector3(-.4f*(1-value),0,-.03f);}}
        public void ResetPosition(){previous=transform.position;}
        // A pooled enemy coming back into play: full health, no lingering flash or action.
        public void ResetForReuse(){previous=transform.position;attack=cast=hit=0;Health(1);if(flashed){flashed=false;foreach(var r in bodyRenderers)if(r)r.SetPropertyBlock(null);}if(animator){animator.Rebind();animator.Play("Walk",0,0);walking=true;}}
        public void Attack(){if(cast>0)return;attack=.65f;if(animator)animator.CrossFadeInFixedTime("Attack",.08f);}
        // A named move clip (Guan Yu's combo and cleave); holds the action state for its full length.
        // During a named move the right hand follows the clip exactly and the support hand may let go of the shaft.
        public void Play(string state,float length){cast=0;attack=length;moving=true;if(animator)animator.CrossFadeInFixedTime(state,.05f);if(!grip)grip=GetComponent<RecoveredPolearmGrip>();if(grip)grip.keepRightHand=true;}
        RecoveredPolearmGrip grip;bool moving;
        public void Cast(){cast=1.15f;attack=0;if(animator)animator.CrossFadeInFixedTime("Cast",.09f);}
        public void Hit(){hit=.14f;}
        void LateUpdate(){if(animator)animator.speed=battle&&(battle.Paused||battle.Finished)?0:battle?battle.TimeScale:1;if(battle&&(battle.Paused||battle.Finished))return;
            float dt=battle?battle.BattleDeltaTime:Time.deltaTime;float oldAction=attack+cast;attack=Mathf.Max(0,attack-dt);cast=Mathf.Max(0,cast-dt);hit=Mathf.Max(0,hit-dt);if(moving&&attack<=0){moving=false;if(grip)grip.keepRightHand=false;}
            bool showFlash=hit>0;if(showFlash!=flashed){flashed=showFlash;if(showFlash)flash.SetColor("_BaseColor",new Color(1.7f,1.5f,1.2f));foreach(var renderer in bodyRenderers)if(renderer)renderer.SetPropertyBlock(showFlash?flash:null);}
            if(cape&&!animator)cape.localRotation=Quaternion.Euler(Mathf.Sin(Time.time*2.2f+transform.position.x)*3,Mathf.Sin(Time.time)*2,0);float moved=Vector3.Distance(previous,transform.position);previous=transform.position;phase+=moved*7;
            if(animator){bool moving=moved>.001f;if(attack+cast<=0&&(oldAction>0||moving!=walking)){animator.CrossFadeInFixedTime(moving?"Walk":"Idle",.12f);walking=moving;}if(healthRoot&&viewCamera)healthRoot.transform.rotation=viewCamera.transform.rotation;return;}
            if(boots!=null)for(int i=0;i<boots.Length;i++)boots[i].localRotation=Quaternion.Euler(moved>.001f?Mathf.Sin(phase+i*Mathf.PI)*28:0,0,0);
            if(attack+cast>0){if(weapon)weapon.localRotation=Quaternion.Euler(Mathf.Sin((attack+cast)/.65f*Mathf.PI)*-65,0,0);}else if(weapon)weapon.localRotation=Quaternion.identity;
            if(healthRoot&&viewCamera)healthRoot.transform.rotation=viewCamera.transform.rotation;
        }
    }
    public class GrowingRing : MonoBehaviour
    {
        float age;public float duration=.7f;public float radius=4;
        void Update(){age+=Time.deltaTime;transform.localScale=Vector3.one*Mathf.Lerp(.2f,radius,age/duration);if(age>=duration)Destroy(gameObject);}
    }
    public static class VisualGeometry
    {
        public static GameObject Cone(string name,Vector3 position,float radius,float height,Color color,Transform parent)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=position;
            const int n=10;var vertices=new Vector3[n*3];var triangles=new int[n*3];
            for(int i=0;i<n;i++){vertices[i*3]=new Vector3(Mathf.Cos(i*2*Mathf.PI/n)*radius,0,Mathf.Sin(i*2*Mathf.PI/n)*radius);vertices[i*3+1]=Vector3.up*height;vertices[i*3+2]=new Vector3(Mathf.Cos((i+1)*2*Mathf.PI/n)*radius,0,Mathf.Sin((i+1)*2*Mathf.PI/n)*radius);triangles[i*3]=i*3;triangles[i*3+1]=i*3+1;triangles[i*3+2]=i*3+2;}
            var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=WorldArt.Material(color);return go;
        }
        public static GameObject Ring(Vector3 position,float radius,Color color)
        {
            var go=new GameObject("Range ring",typeof(LineRenderer));go.transform.position=position;var line=go.GetComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=64;line.widthMultiplier=.055f;line.sharedMaterial=WorldArt.Material(color);
            for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,.09f,Mathf.Sin(a)*radius));}return go;
        }
        public static void Roof(Vector3 center,Color color,Transform parent)
        {
            for(int side=-1;side<=1;side+=2)for(int strip=0;strip<5;strip++){
                var roof=WorldArt.Shape("Sloped tiled roof",PrimitiveType.Cube,center+new Vector3(side*(strip*.28f+.14f),.65f-strip*.12f,0),new Vector3(.4f,.13f,3.3f),color,parent);roof.transform.localRotation=Quaternion.Euler(0,0,side*-23);
            }
            WorldArt.Shape("Roof ridge",PrimitiveType.Cylinder,center+Vector3.up*.7f,new Vector3(.14f,1.75f,.14f),color,parent).transform.localRotation=Quaternion.Euler(90,0,0);
        }
    }
}

