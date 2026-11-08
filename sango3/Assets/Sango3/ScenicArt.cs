using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Sango3
{
    // Presentation only: the road and deployment coordinates remain owned by BattleRules.
    public static class ScenicArt
    {
        static readonly Color Stone=new Color(.30f,.34f,.33f), Gold=new Color(.65f,.47f,.22f);
        static System.Random random;
        internal static float R(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
        public static void ConfigureCamera(Camera cam)
        {
            HeroLighting.Apply(Campaign.Current.Scenario.Night);
            QualitySettings.shadows=UnityEngine.ShadowQuality.All;QualitySettings.shadowDistance=65;cam.orthographic=false;cam.fieldOfView=34;cam.nearClipPlane=.3f;cam.farClipPlane=160;
            cam.transform.position=new Vector3(1,25,-27);cam.transform.LookAt(new Vector3(0,0,1));
            cam.backgroundColor=Campaign.Current.Scenario.Night?new Color(.055f,.08f,.15f):Campaign.Current.Sky;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.52f,.62f,.70f);
            RenderSettings.ambientEquatorColor=new Color(.32f,.38f,.40f);
            RenderSettings.ambientGroundColor=new Color(.16f,.19f,.19f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogColor=cam.backgroundColor;RenderSettings.fogStartDistance=46;RenderSettings.fogEndDistance=105;
            var sun=Object.FindObjectOfType<Light>();
            if(sun){sun.transform.rotation=Quaternion.Euler(43,-38,0);sun.color=new Color(1,.95f,.86f);sun.intensity=1.55f;sun.shadowStrength=.75f;sun.shadowBias=.025f;sun.shadowNormalBias=.25f;}
            var fill=new GameObject("Cool sky fill",typeof(Light)).GetComponent<Light>();fill.type=LightType.Directional;fill.color=new Color(.42f,.62f,.82f);fill.intensity=.6f;fill.transform.rotation=Quaternion.Euler(30,140,0);fill.shadows=LightShadows.None;
        }
        public static Material Surface(Color color,float metallic=0,float smooth=.2f)
        {
            var m=SceneArtResources.Own(new Material(Resources.Load<Material>("BattleLit")));m.color=color;m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);m.enableInstancing=true;return m;
        }
        static Material GroundMaterial()
        {
            var scanned=Resources.Load<Material>("Nature/GroundMat");if(scanned){var tinted=SceneArtResources.Own(new Material(scanned));tinted.color=Campaign.Current.Scenario.Night?Campaign.Current.GroundTint*new Color(.44f,.52f,.68f):Campaign.Current.GroundTint;return tinted;}
            var texture=SceneArtResources.Own(new Texture2D(256,256,TextureFormat.RGB24,true));texture.name="Earth grass variation";texture.wrapMode=TextureWrapMode.Repeat;
            var pixels=new Color[256*256];
            for(int y=0;y<256;y++)for(int x=0;x<256;x++){
                float broad=Mathf.PerlinNoise(x*.033f,y*.033f),grain=Mathf.PerlinNoise(x*.65f,y*.65f);
                pixels[y*256+x]=Color.Lerp(new Color(.22f,.27f,.16f),new Color(.49f,.48f,.28f),broad)*Mathf.Lerp(.78f,1.1f,grain);
            }
            texture.SetPixels(pixels);texture.Apply(true,true);var m=Surface(Color.white);m.mainTexture=texture;m.mainTextureScale=new Vector2(8,6);return m;
        }
#if UNITY_EDITOR
        // Editor check: static batching rejects the whole batch when any member has non-finite vertices.
        public static string NonFinite="";
        static string CheckFinite(Transform root){var sb=new System.Text.StringBuilder();foreach(var f in root.GetComponentsInChildren<MeshFilter>()){if(!f.sharedMesh)continue;var m=f.transform.localToWorldMatrix;foreach(var p in f.sharedMesh.vertices){var w=m.MultiplyPoint3x4(p);if(float.IsNaN(w.x+w.y+w.z)||float.IsInfinity(w.x+w.y+w.z)){sb.AppendLine(f.name+" mesh="+f.sharedMesh.name+" parent="+(f.transform.parent?f.transform.parent.name:"-")+" local="+f.transform.localPosition+" rot="+f.transform.localRotation+" scale="+f.transform.localScale);break;}}}return sb.ToString();}
#endif
        public static void Build(Transform parent){var steps=BuildAsync(parent);while(steps.MoveNext()){} }
        public static System.Collections.IEnumerator BuildAsync(Transform parent)
        {
            random=new System.Random(3917+Campaign.Current.Index*137);
            var fixedRoot=new GameObject("Static scenery").transform;fixedRoot.SetParent(parent,false);
            Terrain(fixedRoot);yield return null;
            // Exposed rock strata break the flat rectangular silhouette.
            for(int i=0;i<34;i++){
                if(i%4==0)yield return null;float x=-18+i*1.12f;
                Rock(new Vector3(x,-1.25f,-10.5f+R(-.6f,.6f)),new Vector3(R(1.7f,2.7f),R(1.5f,2.7f),R(1.4f,2.2f)),fixedRoot);
                if(i%3==0&&R(0,1)>.3f&&Campaign.Current.Index!=BattleController.BaimaStage){var back=new Vector3(x+R(-.6f,.6f),-.5f,R(12.5f,16.5f));Rock(back,new Vector3(R(1.6f,3.2f),R(1.4f,2.8f),R(1.8f,3)),fixedRoot);}
            }
            var water=WorldArt.Shape("River beneath terrace",PrimitiveType.Cube,new Vector3(0,-2.2f,-15),new Vector3(120,.1f,35),new Color(.10f,.29f,.33f),fixedRoot);
            water.GetComponent<Renderer>().sharedMaterial=Surface(new Color(.11f,.29f,.33f),.35f,.8f);
            for(int i=0;i<26;i++){
                var ripple=WorldArt.Shape("River glint",PrimitiveType.Cube,new Vector3(R(-28,28),-2.13f,R(-22,-11)),new Vector3(R(.8f,3),.015f,.035f),new Color(.32f,.52f,.53f),parent);
                ripple.AddComponent<ScenicMotion>().kind=2;
            }
            Road(fixedRoot);if(Campaign.Current.Scenario.Flank!=null)Road(fixedRoot,Campaign.Current.Scenario.Flank);
            for(int i=0;i<60;i++){
                if(i%8==0)yield return null;var p=new Vector3(R(-17,17),0,R(-9.6f,11));
                if(NearRoad(p,2.1f)||NearSlot(p,1.8f)||(Campaign.Current.Scenario.Theme==BattleTheme.River&&Mathf.Abs(p.x)<16&&Mathf.Abs(p.z-1)<10))continue;
                p.y=Height(p);if(i%3==0)Rock(p,new Vector3(R(.4f,1.1f),R(.25f,.7f),R(.4f,1)),fixedRoot);
                else Grass(p,fixedRoot);
            }
            // Irregular clusters and distant ridges instead of an evenly spaced tree fence.
            yield return null;
            // Stage 16 has layered hazy ridges for its distance (ScenarioScenery.DistantRidges) instead of the generic cliff boulders.
            if(Campaign.Current.Index!=BattleController.BaimaStage)for(int i=0;i<10;i++)Rock(new Vector3(-40+i*8.5f,4,R(28,40)),new Vector3(R(9,14),R(11,20),R(9,15)),fixedRoot);
            // Exit structures, standards and landmarks are composed per stage in ScenarioScenery.
            for(int i=0;i<8;i++){
                var p=BattleRules.Slots[i];
                WorldArt.Shape("Carved deployment plinth",PrimitiveType.Cylinder,p+Vector3.up*.04f,new Vector3(2.1f,.11f,2.1f),Stone,fixedRoot);
                var ring=VisualGeometry.Ring(p+Vector3.up*.08f,.88f,Gold);ring.transform.SetParent(fixedRoot,true);
            }
            var scenario=ScenarioScenery.BuildSteps(fixedRoot);while(scenario.MoveNext())yield return null;yield return null;
#if UNITY_EDITOR
            NonFinite=CheckFinite(fixedRoot);
#endif
            float combineStart=Time.realtimeSinceStartup;StaticBatchingUtility.Combine(fixedRoot.gameObject);if(Debug.isDebugBuild&&Application.isPlaying)Debug.Log("SANGO3_BOOT combineMs="+((Time.realtimeSinceStartup-combineStart)*1000).ToString("F1"));
        }
        internal static bool NearSlot(Vector3 p,float radius){foreach(var v in BattleRules.Slots)if(Vector3.Distance(p,v)<radius)return true;return false;}
        internal static bool NearRoad(Vector3 p,float radius)=>HistoricalScenarios.Distance(p,BattleRules.Route)<radius||(Campaign.Current.Scenario.Flank!=null&&HistoricalScenarios.Distance(p,Campaign.Current.Scenario.Flank)<radius);
        static void Road(Transform parent,Vector3[] route=null)
        {
            route=route??BattleRules.Route;bool wooden=Campaign.Current.Scenario.Theme==BattleTheme.River;
            var material=wooden?SceneArtResources.Own(new Material(Resources.Load<Material>("Architecture/Timber"))):Surface(new Color(.85f,.88f,.82f),0,.25f);
            if(!wooden)material.mainTexture=Resources.Load<Texture2D>("Nature/Rock");
            for(int i=1;i<route.Length;i++){
                var a=route[i-1];var b=route[i];var delta=b-a;float length=delta.magnitude;var dir=delta.normalized;var side=Vector3.Cross(Vector3.up,dir);
                var road=WorldArt.Shape("Weathered stone causeway",PrimitiveType.Cube,(a+b)*.5f+Vector3.down*.04f,new Vector3(2.05f,.12f,length+1.6f),Color.white,parent);road.transform.rotation=Quaternion.LookRotation(dir);var roadMaterial=SceneArtResources.Own(new Material(material));roadMaterial.mainTextureScale=new Vector2(1,length/2);road.GetComponent<Renderer>().sharedMaterial=roadMaterial;
                if(wooden){
                    int planks=Mathf.CeilToInt(length/.38f);
                    for(int plank=0;plank<planks;plank++){
                        var deck=WorldArt.Shape("Timber cross plank",PrimitiveType.Cube,a+dir*(plank+.5f)*length/planks+Vector3.up*.035f,new Vector3(2.14f,.09f,length/planks-.025f),Color.white,parent);
                        deck.transform.rotation=Quaternion.LookRotation(dir);deck.GetComponent<Renderer>().sharedMaterial=material;
                    }
                }
                int count=Mathf.CeilToInt(length/1.25f);
                for(int j=0;j<count;j++)for(int s=-1;s<=1;s+=2){
                    var center=a+dir*(j+.5f)*length/count+side*s*1.03f;
                    var curb=WorldArt.Shape("Chipped stone curb",PrimitiveType.Cube,center+Vector3.up*.04f,new Vector3(.15f,.15f,length/count-.035f),new Color(.36f,.39f,.36f),parent);curb.transform.rotation=Quaternion.LookRotation(dir);curb.GetComponent<Renderer>().sharedMaterial=material;
                }
            }
        }
        internal static void Rock(Vector3 p,Vector3 scale,Transform parent)
        {
            var mesh=Resources.Load<Mesh>("Nature/RockMesh");
            if(mesh){var rock=new GameObject("Scanned weathered rock",typeof(MeshFilter),typeof(MeshRenderer));rock.transform.SetParent(parent,false);rock.transform.localPosition=p;rock.transform.localScale=scale;rock.transform.localRotation=Quaternion.Euler(0,R(0,360),R(-12,12));rock.GetComponent<MeshFilter>().sharedMesh=mesh;rock.GetComponent<MeshRenderer>().sharedMaterial=Resources.Load<Material>("Nature/RockMat");return;}
            var go=WorldArt.Shape("Stratified limestone",PrimitiveType.Sphere,p,scale,Color.Lerp(new Color(.22f,.28f,.29f),new Color(.42f,.45f,.39f),R(0,1)),parent);
            go.transform.rotation=Quaternion.Euler(R(-16,16),R(0,180),R(-18,18));
        }
        internal static void Grass(Vector3 p,Transform parent)
        {
            // Stage 16 uses curved grass tufts (0.26), the other stages keep these straight blades.
            if(Campaign.Current.Index==BattleController.BaimaStage){ScenarioScenery.CurvedTuftAt(p,parent);return;}
            for(int i=0;i<5;i++){
                var grass=WorldArt.Shape("Reed",PrimitiveType.Cube,p+new Vector3(R(-.3f,.3f),.2f,R(-.3f,.3f)),new Vector3(.045f,R(.3f,.65f),.05f),new Color(.33f,.37f,.17f),parent);
                grass.transform.localRotation=Quaternion.Euler(R(-25,25),R(0,180),R(-25,25));
            }
        }
        static void Tree(Vector3 p,float height,Transform parent)=>Tree(p,height,parent,0);
        // shape 0: layered conifer; 1: windswept broad crown of separate branch clumps; 2: sparse burned/dead tree.
        // Crowns are built from foliage cards on branches, never a single sphere.
        internal static void Tree(Vector3 p,float height,Transform parent,int shape)
        {
            float lean=R(-6,6),turn=R(0,360);var root=new GameObject(shape==0?"Pine":shape==1?"Windswept tree":"Bare tree").transform;root.SetParent(parent,false);root.localPosition=p;root.localRotation=Quaternion.Euler(lean,turn,R(-4,4));
            var bark=new Color(.23f,.20f,.15f)*R(.8f,1.15f);bark.a=1;var foliage=Resources.Load<Material>("Nature/FoliageMat");
            WorldArt.Shape("Trunk",PrimitiveType.Cylinder,Vector3.up*height*.45f,new Vector3(.2f+height*.015f,height*.45f,.2f+height*.015f),bark,root);
            if(shape==0){
                int cards=16+Mathf.RoundToInt(height*2);for(int i=0;i<cards;i++){float a=i*2.4f;float t=i/(float)cards;var offset=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*Mathf.Lerp(.05f,.75f+R(0,.3f),t);
                    var crown=WorldArt.Shape("Pine branch",PrimitiveType.Quad,offset+Vector3.up*(height-t*height*.65f),new Vector3(1.1f+t*1.5f,1.0f+t*.9f,1)*R(.85f,1.15f),Color.white,root);crown.GetComponent<Renderer>().sharedMaterial=foliage;crown.transform.localRotation=Quaternion.Euler(25+i%3*18,i*137,0);}
                return;
            }
            int limbs=shape==1?4+random.Next(3):3+random.Next(3);
            for(int b=0;b<limbs;b++){
                float a=b*2.4f+R(-.4f,.4f);float h=height*R(.45f,.85f);float reach=height*R(.18f,.32f)*(shape==1?1.2f:1);var dir=new Vector3(Mathf.Cos(a),R(.35f,.8f),Mathf.Sin(a)).normalized;
                var limb=WorldArt.Shape("Branch",PrimitiveType.Cylinder,Vector3.up*h+dir*reach*.5f,new Vector3(.09f,reach*.5f,.09f),bark,root);limb.transform.localRotation=Quaternion.FromToRotation(Vector3.up,dir);
                if(shape==2)continue;var tip=Vector3.up*h+dir*reach;int cards=shape==1?10+random.Next(5):0;
                for(int i=0;i<cards;i++){var card=WorldArt.Shape("Leaf clump",PrimitiveType.Quad,tip+new Vector3(R(-.5f,.5f),R(-.2f,.45f),R(-.5f,.5f))*(height*.18f),Vector3.one*R(1.1f,1.7f)*(height*.24f),Color.white,root);card.GetComponent<Renderer>().sharedMaterial=foliage;card.transform.localRotation=Quaternion.Euler(R(10,70),R(0,360),R(-20,20));}
            }
        }
        // Off-road relief so hill and pass stages read as uneven ground; road and plinths stay level.
        static float Relief(float x,float z)
        {
            var theme=Campaign.Current.Scenario.Theme;float amount=theme==BattleTheme.Mountain?1.9f:theme==BattleTheme.Pass?1.1f:theme==BattleTheme.Forest?.45f:theme==BattleTheme.Ruins?.3f:.2f;
            if(Mathf.Abs(x)>19||z>15||z<-10)return 0;
            // Baima: the Yellow River floodplain is flat from the bank north.
            if(Campaign.Current.Index==BattleController.BaimaStage&&z>ScenarioScenery.Shore(x)-2.4f)return 0;var p=new Vector3(x,0,z);float clear=HistoricalScenarios.Distance(p,BattleRules.Route);if(Campaign.Current.Scenario.Flank!=null)clear=Mathf.Min(clear,HistoricalScenarios.Distance(p,Campaign.Current.Scenario.Flank));
            foreach(var v in BattleRules.Slots)clear=Mathf.Min(clear,Vector3.Distance(p,v)+1.2f);
            float seed=Campaign.Current.Index*13.7f;return Mathf.SmoothStep(0,1,Mathf.InverseLerp(3.2f,7,clear))*amount*(.35f+Mathf.PerlinNoise(x*.16f+seed,z*.16f+seed));
        }
        // Ground height at a point, matching the terrain mesh, for placing props on relief.
        internal static float Height(Vector3 p)=>Mathf.Max(0,Relief(p.x,p.z)-.09f);
        static void Terrain(Transform parent)
        {
            const int nx=80,nz=60;var vertices=new Vector3[(nx+1)*(nz+1)];var uv=new Vector2[vertices.Length];var triangles=new List<int>();
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++){
                float px=-50+x*1.25f,pz=-30+z*1.25f;bool baima=Campaign.Current.Index==BattleController.BaimaStage;/* Baima: the river runs on past the side hills */float edge=Mathf.Max(Mathf.Abs(px)-(baima&&pz>7?42:17),pz-(baima?21:12));float y=-.09f;
                if(edge>0)y+=Mathf.SmoothStep(0,1,edge/8)*(1.2f+Mathf.PerlinNoise(px*.1f+30,pz*.1f+30)*5);
                if(pz<-10)y-=Mathf.SmoothStep(0,1,(-pz-10)/3)*3;
                y+=Relief(px,pz);
                int index=z*(nx+1)+x;vertices[index]=new Vector3(px,y,pz);uv[index]=new Vector2(px/4,pz/4);
                if(x>0&&z>0){int a=index,b=index-1,c=index-nx-1,d=c-1;triangles.AddRange(new[]{a,c,b,b,c,d});}
            }
            var mesh=SceneArtResources.Own(new Mesh{name="River valley terrain"});mesh.vertices=vertices;mesh.uv=uv;mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();
            var ground=new GameObject("Rolling river valley",typeof(MeshFilter),typeof(MeshRenderer));ground.transform.SetParent(parent,false);ground.GetComponent<MeshFilter>().sharedMesh=mesh;ground.GetComponent<Renderer>().sharedMaterial=GroundMaterial();
        }
        static void Gate(Transform parent)
        {
            foreach(float z in new[]{-8.2f,-3.7f}){
                var p=new Vector3(14,0,z);
                WorldArt.Shape("Fortress foundation",PrimitiveType.Cube,p+Vector3.up*1.5f,new Vector3(2.7f,3,2.8f),Stone,parent);
                for(int row=0;row<5;row++)for(int col=0;col<4;col++){
                    WorldArt.Shape("Masonry block",PrimitiveType.Cube,p+new Vector3(-1.37f,.3f+row*.55f,-1.1f+col*.7f),new Vector3(.1f,.49f,.64f),Color.Lerp(Stone,new Color(.49f,.46f,.36f),(row+col)%3*.18f),parent);
                }
                Pavilion(p+Vector3.up*3, .85f,parent);
            }
            WorldArt.Shape("Gate stone lintel",PrimitiveType.Cube,new Vector3(14,2.8f,-6),new Vector3(2.2f,.65f,2.3f),Stone,parent);
            WorldArt.Shape("Heavy wooden gate",PrimitiveType.Cube,new Vector3(14.65f,1.1f,-6),new Vector3(.18f,2.3f,1.8f),new Color(.24f,.12f,.08f),parent);
            for(int i=0;i<5;i++)WorldArt.Shape("Gate brass studs",PrimitiveType.Sphere,new Vector3(14.5f,.4f+i*.4f,-5.5f),Vector3.one*.12f,Gold,parent);
            for(int i=0;i<6;i++){
                WorldArt.Shape("Battlement",PrimitiveType.Cube,new Vector3(16,1.2f,1+i*1.5f),new Vector3(1.3f,2.4f,1.5f),Stone,parent);
                WorldArt.Shape("Merlon",PrimitiveType.Cube,new Vector3(16,2.7f,1+i*1.5f),new Vector3(1.3f,.6f,.7f),Stone,parent);
            }
        }
        static void Pavilion(Vector3 p,float size,Transform parent)
        {
            var root=new GameObject("Chinese gate pavilion").transform;root.SetParent(parent,false);root.localPosition=p;root.localScale=Vector3.one*size;
            WorldArt.Shape("Pavilion platform",PrimitiveType.Cube,new Vector3(0,.12f,0),new Vector3(3.5f,.24f,3.5f),Stone,root);
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)WorldArt.Shape("Red cedar column",PrimitiveType.Cylinder,new Vector3(x*1.2f,1.15f,z*1.2f),new Vector3(.18f,1.05f,.18f),new Color(.36f,.12f,.08f),root);
            WorldArt.Shape("Pavilion room",PrimitiveType.Cube,new Vector3(0,1.0f,0),new Vector3(1.9f,1.8f,1.9f),new Color(.48f,.36f,.24f),root);
            for(int x=-1;x<=1;x+=2)for(int i=0;i<5;i++)WorldArt.Shape("Window lattice",PrimitiveType.Cube,new Vector3(x*.98f,1.3f,-.7f+i*.35f),new Vector3(.035f,.85f,.055f),new Color(.14f,.16f,.14f),root);
            Roof(new Vector3(0,2.1f,0),root);
        }
        static void Roof(Vector3 p,Transform parent)
        {
            // Curved, overhanging ceramic tile courses; repeated fine detail survives the close camera.
            for(int side=-1;side<=1;side+=2)for(int row=0;row<7;row++){
                float x=side*(row*.27f+.14f);float y=.75f-row*.15f+Mathf.Pow(row/6f,4)*.3f;
                var strip=WorldArt.Shape("Curved tiled eave",PrimitiveType.Cube,p+new Vector3(x,y,0),new Vector3(.38f,.13f,3.8f),new Color(.16f,.25f,.25f),parent);strip.transform.localRotation=Quaternion.Euler(0,0,side*(-29+row*5));
                for(int tile=0;tile<12;tile++)WorldArt.Shape("Tile seam",PrimitiveType.Sphere,p+new Vector3(x,y+.07f,-1.75f+tile*.32f),new Vector3(.38f,.045f,.055f),new Color(.25f,.34f,.32f),parent);
            }
            WorldArt.Shape("Roof gilded ridge",PrimitiveType.Cube,p+Vector3.up*.88f,new Vector3(.16f,.16f,4),Gold,parent);
        }
        internal static void Banner(Vector3 p,Color color,Transform parent)
        {
            WorldArt.Shape("Banner mast",PrimitiveType.Cylinder,p+Vector3.up*1.7f,new Vector3(.07f,1.7f,.07f),Gold,parent);
            var flag=WorldArt.Shape("Silk battle standard",PrimitiveType.Cube,p+new Vector3(.48f,2.65f,0),new Vector3(.95f,1.25f,.04f),color,parent);
            flag.AddComponent<ScenicMotion>().kind=1;
            WorldArt.Shape("Banner finial",PrimitiveType.Sphere,p+Vector3.up*3.45f,Vector3.one*.15f,Gold,parent);
        }
    }
    public class ScenicMotion : MonoBehaviour
    {
        public int kind;Vector3 origin,scale;float phase;Quaternion baseRotation;
        void Start(){origin=transform.localPosition;scale=transform.localScale;phase=origin.x;baseRotation=transform.localRotation;}
        void Update(){float t=Time.time+phase;
            // 2: a moored boat: slow heave, roll and pitch on the river.
            if(kind==2){transform.localPosition=origin+Vector3.up*Mathf.Sin(t*.9f)*.03f;transform.localRotation=baseRotation*Quaternion.Euler(Mathf.Sin(t*.7f)*.8f,0,Mathf.Sin(t*.55f+1)*1.6f);return;}
            if(kind==1)transform.localRotation=Quaternion.Euler(Mathf.Sin(t*1.7f)*6,Mathf.Sin(t)*9,Mathf.Sin(t*1.2f)*3);else{transform.localPosition=origin+Vector3.right*Mathf.Sin(t*.4f)*.5f;transform.localScale=new Vector3(scale.x*(.8f+Mathf.Sin(t)*.2f),scale.y,scale.z);}}
    }
}
