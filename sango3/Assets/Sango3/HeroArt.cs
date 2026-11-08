using System.Collections.Generic;
using UnityEngine;
namespace Sango3
{
    public static class HeroArt
    {
        static readonly Color Skin=new Color(.67f,.43f,.29f),Dark=new Color(.075f,.09f,.10f),Brass=new Color(.66f,.46f,.20f),Steel=new Color(.43f,.52f,.57f);
        static Material steel,brass;
        public static void ResetMaterialCache(){steel=null;brass=null;}
        public static void AttachWeapon(Transform hand,string heroName)
        {
            var holder=new GameObject("Weapon mount").transform;holder.SetParent(hand,false);
            Weapon(holder,heroName,true);var weapon=holder.Find("Spear");weapon.localPosition=Vector3.zero;weapon.localRotation=Quaternion.Euler(0,0,90);
        }
        static GameObject Part(string name,PrimitiveType type,Vector3 p,Vector3 scale,Color color,Transform parent,bool metal=false)
        {
            var go=WorldArt.Shape(name,type,p,scale,color,parent);
            if(metal){if(!steel)steel=ScenicArt.Surface(Steel,.65f,.56f);if(!brass)brass=ScenicArt.Surface(Brass,.6f,.4f);go.GetComponent<Renderer>().sharedMaterial=color==Brass?brass:steel;}
            return go;
        }
        // The baked prefab for a hero or the shared enemy, or null when only procedural art exists.
        static int soldierVariant;
        public static GameObject Prefab(string name,bool hero)
        {
            // Stage 16: the Yuan army uses recovered hero models (Guan Yu's detail level); Yan Liang has his own.
            if(!hero&&Campaign.Current.Index==BattleController.BaimaStage)return Resources.Load<GameObject>(name=="颜良"?"Heroes/Baima/yanliang":"Heroes/Baima/yuan-soldier-"+(soldierVariant++%3));
            if(!hero)return Resources.Load<GameObject>("Heroes/enemy");
            int index=System.Array.IndexOf(BattleRules.Names,name);if(index<0)return null;
            return Resources.Load<GameObject>("Heroes/Final/"+BattleRules.Portraits[index])??Resources.Load<GameObject>("Heroes/"+BattleRules.Portraits[index]);
        }
        // Shared setup for an instance of Prefab(), whether created now or prepared during loading.
        public static GameObject Configure(GameObject instance,string name,bool hero)
        {
            instance.name=name;
            if(hero)instance.transform.rotation=Quaternion.Euler(0,175,0);else instance.transform.localScale=Vector3.one*.65f;
            instance.AddComponent<CombatPresentation>().Initialize(!hero);if(hero)ClothFollow.Attach(instance);return instance;
        }
        public static GameObject Build(string name,Vector3 position,Color color,bool hero)
        {
            var source=Prefab(name,hero);if(source)return Configure(Object.Instantiate(source,position,Quaternion.identity),name,hero);
            var root=new GameObject(name);root.transform.position=position;
            var body=new GameObject("Body rig").transform;body.SetParent(root.transform,false);
            bool scholar=name=="诸葛亮",guanyu=name=="关羽",zhangfei=name=="张飞",zhaoyun=name=="赵云";
            var cloth=Color.Lerp(color,Color.black,.22f);
            Profile("Fitted tunic",new[]{new Vector2(.21f,.85f),new Vector2(.24f,1.0f),new Vector2(.20f,1.2f),new Vector2(.30f,1.60f),new Vector2(.24f,1.73f)},.68f,cloth,body);
            Profile("Layered robe",new[]{new Vector2(.34f,.40f),new Vector2(.31f,.55f),new Vector2(.23f,1.05f)},.73f,cloth,body);
            for(int s=-1;s<=1;s+=2){
                var leg=Part("Boot",PrimitiveType.Capsule,new Vector3(s*.14f,.36f,.015f),new Vector3(.21f,.36f,.24f),Dark,body);
                Part("Boot toe",PrimitiveType.Sphere,new Vector3(s*.14f,.08f,.10f),new Vector3(.22f,.17f,.35f),Dark,leg.transform).transform.localPosition=new Vector3(0,-.65f,.24f);
                Part("Knee guard",PrimitiveType.Sphere,new Vector3(s*.15f,.65f,.12f),new Vector3(.23f,.26f,.12f),Brass,body,true);
                var shoulder=Part("Engraved pauldron",PrimitiveType.Sphere,new Vector3(s*.34f,1.59f,0),new Vector3(.29f,.18f,.32f),Steel,body,true);shoulder.transform.localRotation=Quaternion.Euler(0,0,s*-20);
                Part("Upper arm",PrimitiveType.Capsule,new Vector3(s*.37f,1.36f,.01f),new Vector3(.19f,.25f,.20f),cloth,body);
                Part("Arm bracer",PrimitiveType.Capsule,new Vector3(s*.40f,1.12f,.08f),new Vector3(.18f,.19f,.20f),Steel,body,true);
                Part("Hand",PrimitiveType.Sphere,new Vector3(s*.40f,.94f,.09f),new Vector3(.14f,.18f,.14f),Skin,body);
            }
            Part("Collar",PrimitiveType.Cylinder,new Vector3(0,1.76f,0),new Vector3(.29f,.06f,.26f),Brass,body,true);
            Part("Face",PrimitiveType.Sphere,new Vector3(0,1.99f,.02f),new Vector3(.30f,.40f,.29f),Skin,body);
            Part("Nose",PrimitiveType.Sphere,new Vector3(0,1.99f,.17f),new Vector3(.065f,.11f,.08f),Skin,body);
            for(int s=-1;s<=1;s+=2){
                Part("Eyebrow",PrimitiveType.Cube,new Vector3(s*.075f,2.055f,.15f),new Vector3(.09f,.025f,.025f),Dark,body).transform.localRotation=Quaternion.Euler(0,0,s*-9);
                Part("Eye",PrimitiveType.Sphere,new Vector3(s*.073f,2.02f,.154f),new Vector3(.035f,.02f,.018f),Dark,body);
            }
            Part("Helmet",PrimitiveType.Sphere,new Vector3(0,2.17f,-.025f),new Vector3(.36f,.24f,.34f),scholar?Dark:Steel,body,!scholar);
            Part("Helmet brow",PrimitiveType.Cube,new Vector3(0,2.105f,.14f),new Vector3(.34f,.065f,.055f),Brass,body,true);
            if(hero){
                if(scholar){Profile("Scholar crown",new[]{new Vector2(.14f,2.20f),new Vector2(.13f,2.45f),new Vector2(.06f,2.53f)},.65f,Dark,body);}
                else if(guanyu){Part("Jade headwrap",PrimitiveType.Sphere,new Vector3(0,2.23f,0),new Vector3(.34f,.23f,.32f),cloth,body);}
                else{var crest=Part("Silk plume",PrimitiveType.Capsule,new Vector3(0,2.41f,-.08f),new Vector3(.10f,.27f,.16f),zhaoyun?new Color(.77f,.81f,.79f):new Color(.52f,.08f,.06f),body);crest.transform.localRotation=Quaternion.Euler(-28,0,0);}
                if(guanyu||zhangfei||name=="曹操"){
                    Profile("Sculpted beard",new[]{new Vector2(.03f,guanyu?1.40f:1.66f),new Vector2(.09f,1.78f),new Vector2(.115f,1.88f)},.6f,Dark,body,new Vector3(0,0,.14f));
                }
                Cape(body,cloth);
            }
            // Lamellar plates follow the torso rather than decorating a rectangular body.
            if(!scholar)for(int row=0;row<5;row++)for(int col=-2;col<=2;col++){
                float x=col*.096f,z=.19f-Mathf.Abs(col)*.018f;
                Part("Lamellar scale",PrimitiveType.Sphere,new Vector3(x,1.15f+row*.105f,z),new Vector3(.09f,.135f,.055f),row==4?Brass:Steel,body,true);
            }
            Part("Waist sash",PrimitiveType.Cylinder,new Vector3(0,1.07f,0),new Vector3(.49f,.065f,.35f),cloth,body);
            Part("Belt seal",PrimitiveType.Sphere,new Vector3(0,1.08f,.19f),new Vector3(.15f,.16f,.05f),Brass,body,true);
            Weapon(body,name,hero);
            CombineArmor(body);
            root.transform.localScale=Vector3.one*(hero?1.65f:1.1f);
            root.transform.rotation=Quaternion.Euler(0,175,0);
            var animation=root.AddComponent<CombatPresentation>();animation.Initialize(!hero);
            return root;
        }
        static void CombineArmor(Transform body)
        {
            var groups=new Dictionary<Material,List<CombineInstance>>();
            foreach(var filter in body.GetComponentsInChildren<MeshFilter>()){
                bool articulated=false;for(var p=filter.transform;p!=body;p=p.parent)if(p.name=="Spear"||p.name=="Boot"||p.name=="Cape"){articulated=true;break;}
                if(articulated)continue;var renderer=filter.GetComponent<MeshRenderer>();if(!renderer)continue;
                if(!groups.TryGetValue(renderer.sharedMaterial,out var meshes)){meshes=new List<CombineInstance>();groups.Add(renderer.sharedMaterial,meshes);}
                meshes.Add(new CombineInstance{mesh=filter.sharedMesh,transform=body.worldToLocalMatrix*filter.transform.localToWorldMatrix});renderer.enabled=false;
            }
            foreach(var group in groups){var go=new GameObject("Combined armor",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(body,false);var mesh=SceneArtResources.Own(new Mesh{name="Combined armor"});mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.CombineMeshes(group.Value.ToArray());go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=group.Key;}
        }
        static void Weapon(Transform body,string name,bool hero)
        {
            var weapon=new GameObject("Spear").transform;weapon.SetParent(body,false);weapon.localPosition=new Vector3(.44f,1.05f,.1f);
            bool blade=name=="关羽",fan=name=="诸葛亮",sword=name=="刘备"||name=="曹操";
            if(fan){
                for(int i=0;i<8;i++){var feather=Part("Feather fan",PrimitiveType.Capsule,new Vector3((i-3.5f)*.055f,.27f,0),new Vector3(.075f,.28f,.025f),new Color(.78f,.77f,.63f),weapon);feather.transform.localRotation=Quaternion.Euler(0,0,(i-3.5f)*-9);}
            }else{
                Part("Weapon shaft",PrimitiveType.Cylinder,new Vector3(0,sword?.05f:.12f,0),new Vector3(.045f,sword?.23f:1.08f,.045f),new Color(.20f,.13f,.08f),weapon);
                if(blade){
                    var vertices=new[]{new Vector3(-.025f,.95f,0),new Vector3(.03f,1.65f,0),new Vector3(.26f,1.45f,0),new Vector3(.36f,1.17f,0),new Vector3(.18f,.91f,0)};
                    Extrude("Crescent guandao",vertices,.045f,weapon);
                }else if(sword){
                    Part("Sword blade",PrimitiveType.Cube,new Vector3(0,.67f,0),new Vector3(.10f,.9f,.035f),Steel,weapon,true);
                    Part("Sword guard",PrimitiveType.Cube,new Vector3(0,.20f,0),new Vector3(.29f,.06f,.075f),Brass,weapon,true);
                }else{
                    Extrude("Spear point",new[]{new Vector3(-.08f,1.08f,0),new Vector3(0,1.57f,0),new Vector3(.08f,1.08f,0)},.035f,weapon);
                }
                if(hero){var tassel=Part("Weapon tassel",PrimitiveType.Capsule,new Vector3(.08f,.95f,0),new Vector3(.08f,.18f,.07f),new Color(.59f,.07f,.04f),weapon);tassel.transform.localRotation=Quaternion.Euler(0,0,-20);}
            }
        }
        static void Extrude(string name,Vector3[] outline,float depth,Transform parent)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();int n=outline.Length;
            for(int side=0;side<2;side++)for(int i=0;i<n;i++)vertices.Add(outline[i]+Vector3.forward*(side==0?-depth:depth));
            for(int i=1;i<n-1;i++){triangles.AddRange(new[]{0,i+1,i,n,n+i,n+i+1});}
            for(int i=0;i<n;i++){int next=(i+1)%n;triangles.AddRange(new[]{i,next,n+next,i,n+next,n+i});}
            var mesh=SceneArtResources.Own(new Mesh{name=name});mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;if(!steel)steel=ScenicArt.Surface(Steel,.65f,.56f);go.GetComponent<Renderer>().sharedMaterial=steel;
        }
        static void Profile(string name,Vector2[] profile,float depth,Color color,Transform parent,Vector3 offset=default(Vector3))
        {
            const int segments=16;var v=new Vector3[profile.Length*segments];var uv=new Vector2[v.Length];var t=new List<int>();
            for(int row=0;row<profile.Length;row++)for(int i=0;i<segments;i++){
                float a=i*2*Mathf.PI/segments;int index=row*segments+i;v[index]=new Vector3(Mathf.Cos(a)*profile[row].x,profile[row].y,Mathf.Sin(a)*profile[row].x*depth)+offset;uv[index]=new Vector2(i/(float)segments,row/(float)profile.Length);
                if(row>0){int a0=(row-1)*segments+i,b=(row-1)*segments+(i+1)%segments,c=row*segments+i,d=row*segments+(i+1)%segments;t.AddRange(new[]{a0,c,b,b,c,d});}
            }
            var mesh=SceneArtResources.Own(new Mesh{name=name});mesh.vertices=v;mesh.uv=uv;mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=WorldArt.Material(color);
        }
        static void Cape(Transform parent,Color color)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int y=0;y<6;y++)for(int x=0;x<9;x++){
                float width=Mathf.Lerp(.25f,.43f,y/5f);vertices.Add(new Vector3((x/8f*2-1)*width,1.7f-y*.25f,-.18f-y*.065f+Mathf.Sin(x*Mathf.PI*.75f)*.035f));
                if(y>0&&x>0){int a=y*9+x;triangles.AddRange(new[]{a,a-1,a-9,a-1,a-10,a-9,a,a-9,a-1,a-1,a-9,a-10});}
            }
            var mesh=SceneArtResources.Own(new Mesh{name="Folded cloak"});mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
            var go=new GameObject("Cape",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=WorldArt.Material(color);
        }
    }
}
