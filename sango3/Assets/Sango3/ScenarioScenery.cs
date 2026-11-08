using System.Collections.Generic;
using UnityEngine;
namespace Sango3
{
    public static partial class ScenarioScenery
    {
        static readonly List<GameObject> works=new List<GameObject>();static readonly HashSet<GameObject> burned=new HashSet<GameObject>();
        public static void Build(Transform parent){var steps=BuildSteps(parent);while(steps.MoveNext()){}}
        // One composition step per frame so stage scenery does not land in a single long loading frame.
        public static System.Collections.IEnumerator BuildSteps(Transform parent)
        {
            works.Clear();burned.Clear();var s=Campaign.Current.Scenario;
            if(s.Theme==BattleTheme.River){
                var water=WorldArt.Shape("Battlefield river",PrimitiveType.Cube,new Vector3(0,-.035f,1),new Vector3(90,.025f,23),new Color(.12f,.36f,.42f),parent);water.GetComponent<Renderer>().sharedMaterial=SceneArtResources.Own(new Material(Resources.Load<Shader>("Nature/Water")));water.GetComponent<Renderer>().sharedMaterial.SetFloat("_Width",23);
                if(!s.NavalSiege)for(int i=0;i<4;i++)HistoricalArchitecture.Place("Riverboat",new Vector3(-9+i*6.5f,.1f,11.5f+(i%2)*.5f),parent,.80f,i%2==0?8:-12);
            }
            var compose=Compose(parent);while(compose.MoveNext())yield return null;
            if(s.Night){RenderSettings.ambientSkyColor=new Color(.18f,.24f,.34f);RenderSettings.ambientEquatorColor=new Color(.13f,.17f,.22f);var sun=Object.FindObjectOfType<Light>();if(sun){sun.color=new Color(.45f,.55f,.8f);sun.intensity=.48f;}foreach(var p in new[]{Campaign.Current.Route[1],s.ActionPoint,Campaign.Current.Route[Campaign.Current.Route.Length-2]}){WorldArt.Shape("Beacon pole",PrimitiveType.Cylinder,p+new Vector3(1.5f,1,0),new Vector3(.12f,1,.12f),new Color(.25f,.17f,.08f),parent);var flame=WorldArt.Shape("Beacon flame",PrimitiveType.Sphere,p+new Vector3(1.5f,2.2f,0),new Vector3(.32f,.65f,.32f),new Color(1,.4f,.05f),parent);var glow=ScenicArt.Surface(new Color(1,.4f,.05f));glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(3,1,.1f));flame.GetComponent<Renderer>().sharedMaterial=glow;var lamp=new GameObject("Amber beacon").AddComponent<Light>();lamp.transform.SetParent(parent,false);lamp.transform.position=p+new Vector3(1.5f,2,0);lamp.type=LightType.Point;lamp.color=new Color(1,.55f,.18f);lamp.intensity=3;lamp.range=8;lamp.shadows=LightShadows.None;}}
            if(s.NavalSiege){for(int i=0;i<s.Target;i++)works.Add(HistoricalArchitecture.Place("Riverboat",new Vector3(-9+i*6.5f,.1f,11.5f),parent.parent,.85f,i%2==0?8:-8));}
            else if(s.Objective==BattleObjective.Siege)for(int i=0;i<s.Target;i++)works.Add(WorldArt.Shape("Enemy work "+i,PrimitiveType.Cube,new Vector3(10+i,1.1f,9.5f),new Vector3(.7f,2.2f,.7f),new Color(.45f,.24f,.13f),parent.parent));
            if(s.Tactic==BattleTactic.Fire)for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;WorldArt.Shape("Prepared fire bundle",PrimitiveType.Cylinder,s.ActionPoint+new Vector3(Mathf.Cos(a)*2,.3f,Mathf.Sin(a)*2),new Vector3(.5f,.3f,.5f),new Color(.64f,.45f,.14f),parent);}
        }
        // Each stage gets its own composition: a defended exit fitting the theme, the invading army's
        // standards at the entry, one landmark set piece and clustered dressing on free ground.
        // Placement avoids the road, the deployment plinths and earlier pieces so nothing hides the play space.
        static System.Random rng;static readonly List<Vector3> claimed=new List<Vector3>();
        static float R(float a,float b)=>Mathf.Lerp(a,b,(float)rng.NextDouble());
        static Vector3 Ground(float x,float z){var p=new Vector3(x,0,z);p.y=ScenicArt.Height(p);return p;}
        static bool Free(Vector3 p,float radius){
            if(ScenicArt.NearRoad(p,1.6f+radius)||ScenicArt.NearSlot(p,1.5f+radius))return false;
            if(Campaign.Current.Index==BattleController.BaimaStage&&p.z+radius>10.2f)return false; // Yellow River
            foreach(var c in claimed)if(new Vector2(p.x-c.x,p.z-c.z).magnitude<radius+c.y)return false;return true;
        }
        static void Claim(Vector3 p,float radius)=>claimed.Add(new Vector3(p.x,radius,p.z));
        static bool Spot(Rect area,float radius,out Vector3 p){
            for(int i=0;i<60;i++){p=Ground(R(area.xMin,area.xMax),R(area.yMin,area.yMax));if(Free(p,radius)){Claim(p,radius);return true;}}
            p=default;return false;
        }
        static readonly Rect Back=new Rect(-18,9.5f,36,5),Left=new Rect(-20,-9,5.5f,17),Right=new Rect(15.5f,-1,4.5f,13);
        static System.Collections.IEnumerator Compose(Transform parent)
        {
            var stage=Campaign.Current;var s=stage.Scenario;rng=new System.Random(7411+stage.Index*389);claimed.Clear();
            var start=stage.Route[0];var end=stage.Route[stage.Route.Length-1];
            Claim(end+Vector3.right*1.5f,3.4f);
            if(stage.Index==BattleController.BaimaStage){var baima=Baima(start,end,parent);while(baima.MoveNext())yield return null;yield return null;Dressing(s,parent);yield break;}
            Exit(s,end,parent);
            Standards(s,start,parent);yield return null;
            Landmark(s,stage.Index,parent);yield return null;
            Dressing(s,parent);
        }
        static Color Faction(string army)=>army.Contains("黄巾")?new Color(.78f,.62f,.12f):army.Contains("西凉")||army.Contains("吕布")?new Color(.32f,.12f,.10f):army.Contains("袁")?new Color(.62f,.55f,.42f):army.Contains("曹")?new Color(.12f,.16f,.30f):army.Contains("吴")||army.Contains("水寨")?new Color(.55f,.14f,.10f):new Color(.30f,.30f,.26f);
        static void Exit(HistoricalScenario s,Vector3 end,Transform parent)
        {
            var at=end+Vector3.right*1.2f;
            switch(s.Theme){
                case BattleTheme.Pass:
                    // Alternate between the walled gate and a cliff-side stockade so pass stages do not share one exit.
                    if(Campaign.Current.Index%2==0){HistoricalArchitecture.Place("Gate",at,parent,.8f,90);Crag(at+new Vector3(2.5f,0,5.5f),3.2f,parent);Crag(at+new Vector3(2.8f,0,-5),2.6f,parent);}
                    else{Crag(at+new Vector3(1.6f,0,4.2f),3.8f,parent);Crag(at+new Vector3(1.8f,0,-4.4f),3.4f,parent);HistoricalArchitecture.Place("Palisade",at+new Vector3(1.1f,0,0),parent,.9f,90);HistoricalArchitecture.Place("Watchtower",at+new Vector3(3.6f,0,1.2f),parent,.66f,-90);}break;
                case BattleTheme.Night:HistoricalArchitecture.Place("Gate",at,parent,.8f,90);break;
                case BattleTheme.Ruins:var broken=HistoricalArchitecture.Place("Gate",at+Vector3.down*.35f,parent,.8f,90);broken.transform.localRotation*=Quaternion.Euler(0,0,4);for(int i=0;i<7;i++)ScenicArt.Rock(at+new Vector3(R(-1.5f,1.5f),0,R(-4,4)),new Vector3(R(.5f,1.1f),R(.3f,.6f),R(.5f,1)),parent);break;
                case BattleTheme.River:HistoricalArchitecture.Place("Watchtower",at+new Vector3(1.5f,0,3.2f),parent,.75f,-80);Pier(at+new Vector3(.5f,0,-3.4f),parent);break;
                case BattleTheme.Mountain:Crag(at+new Vector3(1.5f,0,3.8f),3.6f,parent);HistoricalArchitecture.Place("Watchtower",at+new Vector3(1.2f,0,-3.4f),parent,.72f,-95);break;
                case BattleTheme.Village:
                    // Rammed-earth hamlet wall with a gate gap, granary behind it.
                    for(int side=-1;side<=1;side+=2)for(int k=0;k<3;k++)EarthWall(at+new Vector3(.9f,0,side*(2+k*1.9f)),parent);HistoricalArchitecture.Place("Granary",at+new Vector3(3.6f,0,3.4f),parent,.72f,-90);break;
                case BattleTheme.Camp:
                    for(int side=-1;side<=1;side+=2)HistoricalArchitecture.Place("Palisade",at+new Vector3(1.1f,0,side*3.6f),parent,.85f,90);HistoricalArchitecture.Place("Watchtower",at+new Vector3(2.4f,0,6.6f),parent,.62f,-90);Rack(at+new Vector3(2.6f,0,-5.8f),parent);break;
                default:
                    // Forest stockade: felled-log barricade between palisade sections.
                    for(int side=-1;side<=1;side+=2)HistoricalArchitecture.Place("Palisade",at+new Vector3(1.2f,0,side*4.2f),parent,.8f,90);
                    for(int k=0;k<4;k++)Log(at+new Vector3(1.6f+k*.15f,k*.25f,side2(k)*1.3f),82+k*9,parent);
                    for(int k=0;k<3;k++)ScenicArt.Tree(at+new Vector3(3.6f+k*.8f,0,-6.5f+k*5.5f),R(3.6f,5),parent,k%2);break;
            }
        }
        static float side2(int k)=>k%2==0?-1:1;
        static void EarthWall(Vector3 p,Transform parent){var wall=WorldArt.Shape("Rammed earth wall",PrimitiveType.Cube,p+Vector3.up*.75f,new Vector3(.7f,1.5f+R(-.2f,.15f),1.85f),Color.white,parent);wall.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Architecture/Rammed earth");var cap=WorldArt.Shape("Wall tile cap",PrimitiveType.Cube,p+Vector3.up*1.6f,new Vector3(.95f,.14f,1.95f),Color.white,parent);cap.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Architecture/Fired roof tiles");}
        static void Standards(HistoricalScenario s,Vector3 start,Transform parent)
        {
            var color=Faction(s.EnemyArmy);int count=2+rng.Next(2);
            for(int i=0;i<count;i++){var p=start+new Vector3(R(-2.6f,-1.2f),0,R(-2,2.4f));if(ScenicArt.NearRoad(p,1.2f))p.x-=1;p.y=ScenicArt.Height(p);ScenicArt.Banner(p,Color.Lerp(color,Color.black,i*.12f),parent);}
            Claim(start+Vector3.left*2,2);
        }
        static void Landmark(HistoricalScenario s,int index,Transform parent)
        {
            var regions=new[]{new Rect(-18,9.5f,14,5),Left,new Rect(4,9.5f,14,5),Back,Right};
            Vector3 c=default;bool found=false;for(int k=0;k<regions.Length&&!found;k++)found=Spot(regions[(index+k)%regions.Length],4.2f,out c);
            if(!found)return;float yaw=R(-25,25)+(index%2==0?0:180);bool alt=index%3==1;
            var root=new GameObject("Landmark · "+s.Title).transform;root.SetParent(parent,false);root.localPosition=c;root.localRotation=Quaternion.Euler(0,yaw,0);
            switch(s.Theme){
                case BattleTheme.Village:
                    HistoricalArchitecture.Place(alt?"Granary":"House",new Vector3(0,0,1.8f),root,.82f,180);HistoricalArchitecture.Place("House",new Vector3(-2.4f,0,-.2f),root,.72f,90);HistoricalArchitecture.Place(alt?"House":"Granary",new Vector3(2.4f,0,-.3f),root,.7f,-90);
                    Well(new Vector3(0,0,-1.2f),root);for(int i=0;i<4;i++)Stack(new Vector3(-1.6f+i*1.1f,0,-2.6f+R(-.3f,.3f)),root);break;
                case BattleTheme.Camp:
                    HistoricalArchitecture.Place("Tent",Vector3.zero,root,.9f,0);for(int i=0;i<3;i++){float a=(i*120+30)*Mathf.Deg2Rad;HistoricalArchitecture.Place("Tent",new Vector3(Mathf.Cos(a)*3,0,Mathf.Sin(a)*2.4f),root,.58f,90-i*120-30);}
                    ScenicArt.Banner(new Vector3(0,0,-1.9f),Faction(s.EnemyArmy),root);Rack(new Vector3(-2.6f,0,0),root);Rack(new Vector3(2.6f,0,0),root);break;
                case BattleTheme.Forest:
                    for(int i=0;i<9;i++){var p=new Vector3(R(-3.6f,3.6f),0,R(-2.6f,2.6f));if(p.magnitude<1.3f)continue;ScenicArt.Tree(p,R(3.2f,5.6f),root,i%3);}
                    Log(new Vector3(.2f,0,-.4f),R(0,180),root);ScenicArt.Rock(new Vector3(1,0,.8f),new Vector3(1.1f,.6f,.9f),root);break;
                case BattleTheme.Night:
                    for(int i=0;i<3;i++)HistoricalArchitecture.Place("Tent",new Vector3(-2.6f+i*2.6f,0,1.4f+R(-.4f,.4f)),root,.72f,180+R(-15,15));HistoricalArchitecture.Place("Palisade",new Vector3(0,0,3.2f),root,.85f,0);
                    Fire(new Vector3(0,0,-1),root);break;
                case BattleTheme.Pass:
                    Crag(new Vector3(-1.5f,0,.5f),3.4f,root);Crag(new Vector3(2.2f,0,.9f),2.4f,root);HistoricalArchitecture.Place("Watchtower",new Vector3(.4f,0,-1.6f),root,.7f,180);break;
                case BattleTheme.Ruins:
                    Hall(root,alt);break;
                case BattleTheme.River:
                    Pier(new Vector3(0,0,-1),root);HistoricalArchitecture.Place("Riverboat",new Vector3(1.8f,.1f,1.6f),root,.7f,75);for(int i=0;i<4;i++)ScenicArt.Grass(new Vector3(R(-3,3),0,R(-2.5f,2.5f)),root);break;
                case BattleTheme.Mountain:
                    Crag(new Vector3(-1.8f,0,.4f),4.2f,root);Crag(new Vector3(1.9f,0,1.2f),3.2f,root);Crag(new Vector3(.2f,0,-1.6f),2,root);
                    ScenicArt.Tree(new Vector3(-2.2f,2.6f,1.4f),3.2f,root,1);ScenicArt.Tree(new Vector3(2.2f,2,1.6f),2.8f,root,0);ScenicArt.Banner(new Vector3(-1.4f,3.4f,.2f),new Color(.55f,.12f,.09f),root);break;
            }
        }
        static void Dressing(HistoricalScenario s,Transform parent)
        {
            int groups=s.Theme==BattleTheme.Forest?7:s.Theme==BattleTheme.Mountain||s.Theme==BattleTheme.Pass?6:4;
            for(int g=0;g<groups;g++){
                var area=g%3==0?Left:g%3==1?Back:Right;if(!Spot(area,1.8f,out var c))continue;int items=2+rng.Next(3);
                for(int i=0;i<items;i++){var p=c+new Vector3(R(-1.8f,1.8f),0,R(-1.6f,1.6f));if(ScenicArt.NearRoad(p,2)||ScenicArt.NearSlot(p,1.8f))continue;p.y=ScenicArt.Height(p);
                    switch(s.Theme){
                        case BattleTheme.Forest:case BattleTheme.Night:ScenicArt.Tree(p,R(2.6f,5),parent,rng.Next(3));break;
                        case BattleTheme.Mountain:case BattleTheme.Pass:if(i==0)Crag(p,R(1.4f,2.6f),parent);else ScenicArt.Tree(p,R(2.4f,4),parent,rng.Next(2));break;
                        case BattleTheme.Village:if(i==0&&g%2==0)HistoricalArchitecture.Place(rng.Next(2)==0?"House":"Granary",p,parent,R(.6f,.75f),R(0,360));else Stack(p,parent);break;
                        case BattleTheme.Camp:if(i==0)HistoricalArchitecture.Place("Tent",p,parent,R(.5f,.62f),R(0,360));else Rack(p,parent);break;
                        case BattleTheme.Ruins:if(i%2==0)BrokenWall(p,R(0,180),parent);else ScenicArt.Rock(p,new Vector3(R(.5f,1.2f),R(.3f,.7f),R(.5f,1.1f)),parent);break;
                        default:ScenicArt.Grass(p,parent);break;
                    }
                }
            }
            // A few trees break the distant edge without forming a fence.
            for(int i=0;i<5;i++)if(Spot(Back,1.4f,out var p))ScenicArt.Tree(p,R(3,4.8f),parent,s.Theme==BattleTheme.Ruins?2:rng.Next(2));
        }
        // Scanned rock pieces stacked into an irregular outcrop; no smooth ellipsoids.
        static void Crag(Vector3 p,float size,Transform parent){
            int pieces=3+Mathf.RoundToInt(size);for(int i=0;i<pieces;i++){float t=i/(float)pieces;var offset=new Vector3(R(-.55f,.55f)*size,(1-t)*size*.35f-.3f,R(-.45f,.45f)*size);var scale=new Vector3(R(.7f,1.2f),R(.8f,1.5f)*(1.1f-t*.4f),R(.6f,1.1f))*size*(1-t*.35f);ScenicArt.Rock(p+offset,scale,parent);}
        }
        static void Pier(Vector3 p,Transform parent){var timber=Resources.Load<Material>("Architecture/Timber");for(int i=0;i<7;i++){var plank=WorldArt.Shape("Pier plank",PrimitiveType.Cube,p+new Vector3(0,.12f,-1.8f+i*.6f),new Vector3(2.4f,.08f,.52f),Color.white,parent);plank.GetComponent<Renderer>().sharedMaterial=timber;}for(int x=-1;x<=1;x+=2)for(int z=0;z<3;z++){var post=WorldArt.Shape("Pier post",PrimitiveType.Cylinder,p+new Vector3(x*1.1f,.05f,-1.8f+z*1.8f),new Vector3(.16f,.45f,.16f),Color.white,parent);post.GetComponent<Renderer>().sharedMaterial=timber;}}
        static void Well(Vector3 p,Transform parent){for(int i=0;i<10;i++){float a=i*Mathf.PI/5;WorldArt.Shape("Well stone",PrimitiveType.Cube,p+new Vector3(Mathf.Cos(a)*.55f,.22f,Mathf.Sin(a)*.55f),new Vector3(.32f,.44f,.22f),new Color(.36f,.37f,.33f),parent).transform.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);}WorldArt.Shape("Well beam",PrimitiveType.Cube,p+Vector3.up*1.2f,new Vector3(1.4f,.08f,.08f),new Color(.3f,.2f,.11f),parent);for(int x=-1;x<=1;x+=2)WorldArt.Shape("Well post",PrimitiveType.Cylinder,p+new Vector3(x*.65f,.6f,0),new Vector3(.08f,.6f,.08f),new Color(.3f,.2f,.11f),parent);}
        // Haystack: a lathe-turned rounded stook with a lumpy outline, a tied waist band and a twisted top knot, in straw texture.
        static Material straw;
        static void Stack(Vector3 p,Transform parent)
        {
            if(!straw){straw=SceneArtResources.Own(new Material(Resources.Load<Material>("BattleLit")){name="Straw"});straw.mainTexture=Resources.Load<Texture2D>("Architecture/Straw");straw.color=Color.white;straw.SetFloat("_Smoothness",.08f);}
            var profile=new[]{new Vector2(.62f,0),new Vector2(.70f,.12f),new Vector2(.66f,.38f),new Vector2(.56f,.55f),new Vector2(.60f,.66f),new Vector2(.44f,.92f),new Vector2(.22f,1.14f),new Vector2(.06f,1.26f),new Vector2(0,1.3f)};
            const int seg=18;var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();float seed=R(0,10);
            for(int r=0;r<profile.Length;r++)for(int k=0;k<=seg;k++){float a=k*Mathf.PI*2/seg;float lump=1+.07f*Mathf.Sin(a*5+seed+r)+.04f*Mathf.Sin(a*11+seed);v.Add(new Vector3(Mathf.Cos(a)*profile[r].x*lump,profile[r].y,Mathf.Sin(a)*profile[r].x*lump));uv.Add(new Vector2(k/(float)seg*3,profile[r].y));}
            for(int r=0;r<profile.Length-1;r++)for(int k=0;k<seg;k++){int a=r*(seg+1)+k,b=a+seg+1;t.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            var mesh=SceneArtResources.Own(new Mesh{name="Haystack"});mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            p.y=ScenicArt.Height(p);var go=new GameObject("Haystack",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=Vector3.one*R(.8f,1.05f);go.transform.localRotation=Quaternion.Euler(0,R(0,360),0);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=straw;
            var band=WorldArt.Shape("Straw rope band",PrimitiveType.Cylinder,new Vector3(0,.56f,0),new Vector3(1.16f,.03f,1.16f),new Color(.42f,.32f,.16f),go.transform);
        }
        static void Rack(Vector3 p,Transform parent){var wood=new Color(.28f,.18f,.1f);var root=new GameObject("Weapon rack").transform;root.SetParent(parent,false);root.localPosition=p;root.localRotation=Quaternion.Euler(0,R(0,180),0);WorldArt.Shape("Rack rail",PrimitiveType.Cube,Vector3.up*.9f,new Vector3(1.3f,.06f,.06f),wood,root);for(int i=0;i<5;i++){var spear=WorldArt.Shape("Racked spear",PrimitiveType.Cylinder,new Vector3(-.5f+i*.25f,.8f,.08f),new Vector3(.035f,.8f,.035f),wood,root);spear.transform.localRotation=Quaternion.Euler(-10,0,0);WorldArt.Shape("Spear head",PrimitiveType.Cube,new Vector3(-.5f+i*.25f,1.62f,.22f),new Vector3(.05f,.18f,.02f),new Color(.45f,.5f,.52f),root);}}
        static void Log(Vector3 p,float yaw,Transform parent){var log=WorldArt.Shape("Fallen trunk",PrimitiveType.Cylinder,p+Vector3.up*.22f,new Vector3(.42f,1.5f,.42f),new Color(.24f,.19f,.13f),parent);log.transform.localRotation=Quaternion.Euler(0,yaw,88);}
        static void Fire(Vector3 p,Transform parent){for(int i=0;i<6;i++){float a=i*Mathf.PI/3;WorldArt.Shape("Fire stone",PrimitiveType.Sphere,p+new Vector3(Mathf.Cos(a)*.5f,.08f,Mathf.Sin(a)*.5f),new Vector3(.24f,.16f,.2f),new Color(.2f,.2f,.2f),parent);}var flame=WorldArt.Shape("Camp fire",PrimitiveType.Sphere,p+Vector3.up*.35f,new Vector3(.4f,.7f,.4f),new Color(1,.4f,.05f),parent);var glow=ScenicArt.Surface(new Color(1,.4f,.05f));glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(3,1,.1f));flame.GetComponent<Renderer>().sharedMaterial=glow;}
        // Masonry stub with an uneven broken top instead of a single box.
        static void BrokenWall(Vector3 p,float yaw,Transform parent){var root=new GameObject("Broken wall").transform;root.SetParent(parent,false);root.localPosition=p;root.localRotation=Quaternion.Euler(0,yaw,0);int columns=4+rng.Next(3);for(int x=0;x<columns;x++){int rows=1+rng.Next(4);for(int y=0;y<rows;y++)WorldArt.Shape("Scorched brick",PrimitiveType.Cube,new Vector3(x*.48f-columns*.24f+(y%2)*.12f,.14f+y*.27f,0),new Vector3(.46f,.25f,.38f),Color.Lerp(new Color(.24f,.22f,.19f),new Color(.36f,.31f,.25f),(float)rng.NextDouble()),root).transform.localRotation=Quaternion.Euler(R(-3,3),R(-4,4),R(-3,3));}}
        static void Hall(Transform root,bool alt){
            WorldArt.Shape("Hall platform",PrimitiveType.Cube,new Vector3(0,.15f,0),new Vector3(6,.3f,4),Color.white,root).GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Architecture/Stone footing");
            for(int x=0;x<4;x++)for(int z=0;z<2;z++){float h=R(.4f,2.8f);var column=WorldArt.Shape("Charred column",PrimitiveType.Cylinder,new Vector3(-2.2f+x*1.45f,.3f+h*.5f,-1.3f+z*2.6f),new Vector3(.32f,h*.5f,.32f),new Color(.17f,.11f,.08f),root);if(h<.8f&&(x+z)%2==0){column.transform.localPosition=new Vector3(-2.2f+x*1.45f+.9f,.5f,-1.3f+z*2.6f);column.transform.localScale=new Vector3(.32f,1.1f,.32f);column.transform.localRotation=Quaternion.Euler(0,R(0,180),90);}}
            for(int i=0;i<(alt?14:9);i++){var tile=WorldArt.Shape("Fallen roof tile",PrimitiveType.Cube,new Vector3(R(-3,3),.34f,R(-2,2)),new Vector3(R(.4f,.9f),.06f,R(.25f,.5f)),new Color(.16f,.22f,.22f),root);tile.transform.localRotation=Quaternion.Euler(R(-20,20),R(0,180),R(-20,20));}
            BrokenWall(new Vector3(0,.3f,2.3f),0,root);
        }
        public static void Breach(int count){for(int i=0;i<Mathf.Min(count,works.Count);i++)if(works[i]&&burned.Add(works[i])){if(Campaign.Current.Scenario.NavalSiege){BurningShip.Ignite(works[i]);works[i].transform.localRotation*=Quaternion.Euler(7,0,5);works[i].transform.localPosition+=Vector3.down*.18f;}else{works[i].transform.localScale=new Vector3(.7f,.25f,.7f);var p=works[i].transform.localPosition;p.y=.125f;works[i].transform.localPosition=p;}}}
        public static void Pulse(BattleTactic tactic,Vector3 center,BattleController battle){
            if(tactic!=BattleTactic.Fire&&tactic!=BattleTactic.Flood)return;
            var root=new GameObject(tactic+" field");root.transform.position=center;var effect=root.AddComponent<ScenarioPulse>();effect.battle=battle;effect.fire=tactic==BattleTactic.Fire;
            for(int i=0;i<12;i++){float angle=i*Mathf.PI*2/12;var go=WorldArt.Shape("Tactical column",PrimitiveType.Sphere,new Vector3(Mathf.Cos(angle)*2,.25f,Mathf.Sin(angle)*2),effect.fire?new Vector3(.4f,1.2f,.4f):new Vector3(.7f,.15f,.7f),effect.fire?new Color(1,.35f,.035f):new Color(.2f,.65f,.8f),root.transform);}
        }
    }
    public sealed class ScenarioPulse:MonoBehaviour
    {
        public BattleController battle;public bool fire;float age;
        void Update(){if(battle&&(battle.Paused||battle.Finished))return;age+=battle?battle.BattleDeltaTime:Time.deltaTime;transform.localScale=Vector3.one*(1+age*.4f);if(age>2)Destroy(gameObject);}
    }
}
