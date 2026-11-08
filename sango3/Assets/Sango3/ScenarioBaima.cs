using System.Collections.Generic;
using UnityEngine;
namespace Sango3
{
    // Stage 16 白马破阵, the production sample stage. Baima stood on the south bank of the Yellow River;
    // Yan Liang crossed from the north to besiege it, and Guan Yu cut him down before his own command
    // parasol. The road runs from Yan Liang's bridgehead camp at the river to Baima's rammed-earth gate.
    public static partial class ScenarioScenery
    {
        static readonly Color YuanBanner=new Color(.78f,.58f,.16f),YuanDark=new Color(.42f,.26f,.10f),CaoBanner=new Color(.13f,.18f,.34f),Lacquer=new Color(.52f,.08f,.05f),Silt=new Color(.56f,.47f,.32f);
        static System.Collections.IEnumerator Baima(Vector3 start,Vector3 end,Transform parent)
        {
            // The Yellow River along the north edge: a meandering silt-laden shore, a sloped bank that darkens
            // where it is wet, a foam line, and reed beds in clumps rather than a row.
            River(parent);yield return null;
            UnityEngine.Profiling.Profiler.BeginSample("Sango3.Baima.Ground");GroundVariation(parent);RouteReadability(parent);UnityEngine.Profiling.Profiler.EndSample();yield return null;DistantRidges(parent);yield return null;
            // The far (north) bank: a low silt shore with reed beds and a few willows, so the river has two sides.
            {var silt=SceneArtResources.Own(new Material(Resources.Load<Material>("Nature/GroundMat")){name="Far silt"});silt.color=new Color(.78f,.70f,.54f);
                var far=WorldArt.Shape("Far bank",PrimitiveType.Cube,new Vector3(0,.0f,25.2f),new Vector3(90,.25f,6),Color.white,parent);far.GetComponent<Renderer>().sharedMaterial=silt;
                {UnityEngine.Profiling.Profiler.BeginSample("Sango3.Baima.FarReeds");PropMaterials();var farReeds=new Kit();for(int c=0;c<12;c++){float cx=R(-24,24);for(int k=0;k<10;k++){var rp=new Vector3(cx+R(-1,1),.12f,R(22.3f,23.2f));Reed(farReeds,rp,new Vector3(rp.x-cx,0,.3f),R(.7f,1.2f));}}farReeds.Build("Far reeds",parent,false);UnityEngine.Profiling.Profiler.EndSample();}
                yield return null; // 0.27: far reeds and far trees in separate frames (together they made the 40 ms loading frame)
                for(int k=0;k<7;k++)ScenicArt.Tree(new Vector3(R(-22,22),.12f,R(23.5f,26)),R(2.6f,4f),parent,1);}
            yield return null;
            // Three boats moored by the jetty (one with its sail set) and one further out under sail.
            {UnityEngine.Profiling.Profiler.BeginSample("Sango3.Baima.Boat");Riverboat(new Vector3(-15.6f,.02f,13.0f),96,.82f,false,parent);UnityEngine.Profiling.Profiler.EndSample();}yield return null;{UnityEngine.Profiling.Profiler.BeginSample("Sango3.Baima.Boat");Riverboat(new Vector3(-10.0f,.02f,13.3f),78,.82f,true,parent);UnityEngine.Profiling.Profiler.EndSample();}yield return null;{UnityEngine.Profiling.Profiler.BeginSample("Sango3.Baima.Boat");Riverboat(new Vector3(-18.6f,.02f,14.1f),104,.78f,false,parent);UnityEngine.Profiling.Profiler.EndSample();}yield return null;{UnityEngine.Profiling.Profiler.BeginSample("Sango3.Baima.Boat");Riverboat(new Vector3(-3.5f,.02f,17.2f),70,.74f,true,parent);UnityEngine.Profiling.Profiler.EndSample();}yield return null;
            Jetty(new Vector3(-12.5f,.06f,11.2f),parent);
            Claim(new Vector3(-13,0,11),4);

            {var bank=Riverbank(parent);while(bank.MoveNext())yield return null;}
            for(int k=0;k<6;k++){ContactShadow(new Vector3(-15.5f+k*1.3f,0,Shore(-15.5f+k*1.3f)+.3f),.22f,.22f,0,.35f);var post=WorldArt.Shape("Mooring stake",PrimitiveType.Cylinder,new Vector3(-15.5f+k*1.3f,.25f,Shore(-15.5f+k*1.3f)+.3f),new Vector3(.12f,.4f,.12f),new Color(.22f,.15f,.09f),parent);post.transform.localRotation=Quaternion.Euler(R(-8,8),0,R(-8,8));}
            for(int k=0;k<5;k++){float x=R(0,18);ScenicArt.Rock(new Vector3(x,-.1f,Shore(x)+R(.1f,.6f)),new Vector3(R(.5f,1f),R(.3f,.5f),R(.5f,.9f)),parent);}
            yield return null;
            // Yan Liang's bridgehead camp at the road's start, his command parasol as the landmark.
            var camp=start+new Vector3(-3.2f,0,1.2f);
            for(int i=0;i<3;i++){var p=camp+new Vector3(-1.5f+i*1.7f,0,i==1?2.2f:.6f);p.y=ScenicArt.Height(p);Tent(p,160+i*25,parent);}
            ContactShadow(start+new Vector3(-1.9f,0,-1.6f),.55f,.55f,0,.45f);CommandParasol(start+new Vector3(-1.9f,0,-1.6f),parent);
            // Signs of an army that crossed in the night: a cook fire, weapon racks, grain sacks by the tents.
            ContactShadow(camp+new Vector3(-2.6f,0,-.6f),.9f,.45f,0,.4f);ContactShadow(camp+new Vector3(2.4f,0,1.6f),.9f,.45f,0,.4f);ContactShadow(camp+new Vector3(1.9f,0,-.25f),1.1f,.5f,0,.45f);UnityEngine.Profiling.Profiler.BeginSample("Sango3.Baima.CampFire");CampFire(camp+new Vector3(-.45f,0,-1.45f),parent);UnityEngine.Profiling.Profiler.EndSample();Rack(camp+new Vector3(-2.6f,0,-.6f),parent);Rack(camp+new Vector3(2.4f,0,1.6f),parent);for(int k=0;k<4;k++)Sack(camp+new Vector3(1.2f+k*.45f,0,-.4f+(k%2)*.35f),parent);
            // Flank the tents so the command flags frame the fire from the deployment camera.
            var bannerOffsets = new[] {
                new Vector3(-3.1f, 0, -.8f), new Vector3(-2.3f, 0, -1.6f),
                new Vector3(2.5f, 0, .1f), new Vector3(3.3f, 0, .8f)
            };
            for (int i = 0; i < bannerOffsets.Length; i++) {
                var p = camp + bannerOffsets[i];
                p.y = ScenicArt.Height(p);
                ContactShadow(p, .3f, .3f, 0, .4f);
                ClothBanner(p, i == 1 ? "BannerYan" : "BannerYuan", parent, i * .37f);
            }
            Claim(camp,3.6f);Claim(start+Vector3.left*2,2.4f);
            yield return null;
            // Baima: a rammed-earth city wall closing the east edge, gate tower where the road arrives.
            var gate=end+new Vector3(2.4f,0,0);
            HistoricalArchitecture.Place("Gate",gate,parent,.85f,90);
            for(int side=-1;side<=1;side+=2)for(int k=0;k<6;k++){var p=gate+new Vector3(.2f,0,side*(2.6f+k*2.1f));CityWall(p,parent);}
            for(int k=0;k<3;k++){var p=gate+new Vector3(.2f,0,-2.6f-k*4.2f);p.y=ScenicArt.Height(p)+2.4f;ClothBanner(p+new Vector3(-.2f,0,0),"BannerCao",parent,k*.53f);}
            for(int k=0;k<3;k++){var p=gate+new Vector3(.2f,0,2.6f+k*4.2f);p.y=ScenicArt.Height(p)+2.4f;ClothBanner(p+new Vector3(-.2f,0,0),"BannerCao",parent,k*.53f);}
            Claim(gate,3.6f);Claim(gate+new Vector3(0,0,7),3);Claim(gate+new Vector3(0,0,-7),3);
            yield return null;
            // Earlier skirmish along the road: abandoned shields and broken spears near the river bank.
            for(int i=0;i<8;i++){var p=new Vector3(R(-10,6),0,R(7.5f,9.6f));if(ScenicArt.NearRoad(p,1.3f)||!Free(p,.6f))continue;p.y=ScenicArt.Height(p);ContactShadow(p,.45f,.45f,0,.35f);Debris(p,parent);}
            ContactShadows(parent);
        }
        // North shore line of the Yellow River for x (the floodplain beyond is flat; ScenicArt.Relief).
        // Irregular: the old sine sum plus noise at two scales, a shallow cove east of the jetty and a silt spit further east.
        internal static float Shore(float x)=>10.9f+.55f*Mathf.Sin(x*.23f+1)+.3f*Mathf.Sin(x*.61f)+.15f*Mathf.Sin(x*1.7f+2)
            +(Mathf.PerlinNoise(x*.19f,3.7f)-.5f)*.9f+(Mathf.PerlinNoise(x*.83f,8.1f)-.5f)*.22f
            +.55f*Mathf.Exp(-Mathf.Pow((x+5.5f)/2.2f,2))-.75f*Mathf.Exp(-Mathf.Pow((x-9f)/1.6f,2));
        // Bank band widths vary along the shore so the dry silt, wet mud and water do not run as parallel ribbons.
        static float BankWidth(float x,float seed)=>.65f+.7f*Mathf.PerlinNoise(x*.21f+seed,seed*1.7f);
        static void River(Transform parent)
        {
            const int n=192;const float x0=-32,x1=32;
            var water=SceneArtResources.Own(new Material(Resources.Load<Shader>("Nature/Water")));water.SetColor("_BaseColor",new Color(.42f,.33f,.17f,1));water.SetColor("_FresnelColor",new Color(.56f,.53f,.45f,1));water.SetFloat("_Silt",1);
            var ground=Resources.Load<Material>("Nature/GroundMat");
            var dry=SceneArtResources.Own(new Material(ground){name="Dry silt"});dry.color=new Color(.86f,.78f,.6f);
            var wet=SceneArtResources.Own(new Material(ground){name="Wet mud"});wet.color=new Color(.42f,.36f,.27f);wet.SetFloat("_Smoothness",.45f);
            // Each band is a strip following the shore: (offset from shore at land side, height), (offset at river side, height).
            Strip("Dry bank",parent,dry,n,x0,x1,-1.9f,.02f,-.55f,-.02f,false,null,x=>BankWidth(x,1.3f),x=>BankWidth(x,4.2f));
            Strip("Wet bank",parent,wet,n,x0,x1,-.65f,-.015f,.45f,-.11f,false,null,x=>BankWidth(x,4.2f),null);
            Strip("Yellow River",parent,water,n,x0,x1,-.25f,.02f,11.5f,.02f,true);
            // Foam along the bank is drawn by the water shader itself (broken, noise-driven), not as a uniform ribbon.
        }
        static void Strip(string name,Transform parent,Material material,int n,float x0,float x1,float near,float nearY,float far,float farY,bool uvAcross,Color? tint=null,System.Func<float,float> nearScale=null,System.Func<float,float> farScale=null)
        {
            var v=new Vector3[(n+1)*2];var uv=new Vector2[v.Length];var c=new Color[v.Length];var t=new int[n*6];
            for(int i=0;i<=n;i++){float x=Mathf.Lerp(x0,x1,i/(float)n),z=Shore(x);float nearAt=near*(nearScale?.Invoke(x)??1),farAt=far*(farScale?.Invoke(x)??1);v[i*2]=new Vector3(x,nearY,z+nearAt);v[i*2+1]=new Vector3(x,farY,z+farAt);
                uv[i*2]=uvAcross?new Vector2(x*.05f,0):new Vector2(x*.25f,(z+nearAt)*.25f);uv[i*2+1]=uvAcross?new Vector2(x*.05f,1):new Vector2(x*.25f,(z+farAt)*.25f);c[i*2]=c[i*2+1]=tint??Color.white;
                if(i<n){int k=i*6,b=i*2;t[k]=b;t[k+1]=b+1;t[k+2]=b+2;t[k+3]=b+1;t[k+4]=b+3;t[k+5]=b+2;}}
            var mesh=SceneArtResources.Own(new Mesh{name=name});mesh.vertices=v;mesh.uv=uv;mesh.colors=c;mesh.triangles=t;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;
            if(material.renderQueue>=3000){r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;go.transform.SetParent(parent.parent,true);} // transparent: keep out of the static batch
        }
        // Grain sack: a lathe-turned bag with a tied neck, in undyed linen.
        static void Sack(Vector3 p,Transform parent)
        {
            p.y=ScenicArt.Height(p);var profile=new[]{new Vector2(.05f,0),new Vector2(.22f,.02f),new Vector2(.26f,.14f),new Vector2(.24f,.32f),new Vector2(.16f,.44f),new Vector2(.06f,.5f),new Vector2(.08f,.58f),new Vector2(.02f,.62f)};
            const int seg=14;var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            for(int r=0;r<profile.Length;r++)for(int k=0;k<=seg;k++){float a=k*Mathf.PI*2/seg;float bulge=1+.06f*Mathf.Sin(a*3+r);v.Add(new Vector3(Mathf.Cos(a)*profile[r].x*bulge,profile[r].y,Mathf.Sin(a)*profile[r].x*bulge*.85f));uv.Add(new Vector2(k/(float)seg*2,profile[r].y*2));}
            for(int r=0;r<profile.Length-1;r++)for(int k=0;k<seg;k++){int a=r*(seg+1)+k,b=a+seg+1;t.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            var mesh=SceneArtResources.Own(new Mesh{name="Grain sack"});mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var go=new GameObject("Grain sack",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localRotation=Quaternion.Euler(R(-8,8),R(0,360),R(-8,8));go.transform.localScale=Vector3.one*R(.9f,1.15f);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=Resources.Load<Material>("Architecture/Undyed linen");
        }
        // Silk standard on a pole with a crossbar: a hanging cloth with the army's character that sways in the wind.
        static readonly Dictionary<string,Material> bannerMaterials=new Dictionary<string,Material>();
        static void ClothBanner(Vector3 p,string texture,Transform parent,float phase)
        {
            var timber=Resources.Load<Material>("Architecture/Timber");
            var pole=WorldArt.Shape("Banner pole",PrimitiveType.Cylinder,p+Vector3.up*1.9f,new Vector3(.08f,1.9f,.08f),Color.white,parent);pole.GetComponent<Renderer>().sharedMaterial=timber;
            WorldArt.Shape("Banner finial",PrimitiveType.Sphere,p+Vector3.up*3.85f,Vector3.one*.16f,new Color(.75f,.55f,.2f),parent);
            var bar=WorldArt.Shape("Banner crossbar",PrimitiveType.Cylinder,p+new Vector3(.5f,3.62f,0),new Vector3(.05f,.55f,.05f),Color.white,parent);bar.transform.localRotation=Quaternion.Euler(0,0,90);bar.GetComponent<Renderer>().sharedMaterial=timber;
            if(!bannerMaterials.TryGetValue(texture,out var material)||!material){material=SceneArtResources.Own(new Material(Resources.Load<Material>("BattleLit")){name=texture});material.mainTexture=Resources.Load<Texture2D>("Architecture/"+texture);material.color=Color.white;material.SetFloat("_Smoothness",.25f);material.SetFloat("_Cull",0);bannerMaterials[texture]=material;}
            // Cloth: 6 x 8 quads hanging from the crossbar, with a soft fold and a lift at the fly edge.
            const int cols=6,rows=8;var v=new Vector3[(cols+1)*(rows+1)];var uv=new Vector2[v.Length];var tri=new List<int>();
            for(int r=0;r<=rows;r++)for(int c=0;c<=cols;c++){float u=c/(float)cols,w=r/(float)rows;v[r*(cols+1)+c]=new Vector3(u*1.0f,-w*1.4f,Mathf.Sin(u*Mathf.PI*1.6f+w*1.3f)*.07f*(.3f+u));uv[r*(cols+1)+c]=new Vector2(u,1-w);
                if(r<rows&&c<cols){int a=r*(cols+1)+c;tri.AddRange(new[]{a,a+cols+1,a+1,a+1,a+cols+1,a+cols+2});}}
            var mesh=SceneArtResources.Own(new Mesh{name="Banner cloth"});mesh.vertices=v;mesh.uv=uv;mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            // Cloth sways, so it lives outside the static batch.
            var cloth=new GameObject("Banner cloth "+texture,typeof(MeshFilter),typeof(MeshRenderer));cloth.transform.SetParent(parent.parent,false);cloth.transform.position=parent.TransformPoint(p+new Vector3(.02f,3.6f,0));
            cloth.GetComponent<MeshFilter>().sharedMesh=mesh;cloth.GetComponent<MeshRenderer>().sharedMaterial=material;var motion=cloth.AddComponent<ScenicMotion>();motion.kind=1;
        }
        // 麾盖: a lacquered silk canopy on a tall pole: ribbed dome with a scalloped rim, two tiers of hanging valance and a
        // gilt finial, in crimson silk with gold cloud bands (art_textures.py), marking the commander's position.
        static void CommandParasol(Vector3 p,Transform parent)
        {
            p.y=ScenicArt.Height(p);var root=new GameObject("Yan Liang command parasol").transform;root.SetParent(parent,false);root.localPosition=p;
            var silk=SceneArtResources.Own(new Material(Resources.Load<Material>("BattleLit")){name="Parasol silk"});silk.mainTexture=Resources.Load<Texture2D>("Architecture/ParasolSilk");silk.color=Color.white;silk.SetFloat("_Smoothness",.3f);silk.SetFloat("_Cull",0);
            var timber=Resources.Load<Material>("Architecture/Timber");var gilt=new Color(.85f,.62f,.2f);
            var pole=WorldArt.Shape("Parasol pole",PrimitiveType.Cylinder,new Vector3(0,2.3f,0),new Vector3(.12f,2.3f,.12f),Color.white,root);pole.GetComponent<Renderer>().sharedMaterial=timber;
            // Canopy: 16 ribs; the rim dips between ribs (scallops).
            const int ribs=16,rings=6;var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            for(int r=0;r<=rings;r++)for(int k=0;k<=ribs*4;k++){float u=k/(float)(ribs*4),a=u*Mathf.PI*2,f=r/(float)rings;float radius=Mathf.Lerp(.08f,1.55f,f);float sag=Mathf.Pow(Mathf.Abs(Mathf.Sin(a*ribs*.5f)),.6f)*.10f*f;
                v.Add(new Vector3(Mathf.Cos(a)*radius,4.95f-f*f*.55f-sag,Mathf.Sin(a)*radius));uv.Add(new Vector2(u*4,f));}
            int row=ribs*4+1;for(int r=0;r<rings;r++)for(int k=0;k<ribs*4;k++){int a=r*row+k;t.AddRange(new[]{a,a+1,a+row,a+1,a+row+1,a+row});}
            Mesh(root,"Parasol canopy",v,uv,t,silk);
            for(int k=0;k<ribs;k++){float a=k*Mathf.PI*2/ribs;var rib=WorldArt.Shape("Parasol rib",PrimitiveType.Cylinder,new Vector3(Mathf.Cos(a)*.78f,4.68f,Mathf.Sin(a)*.78f),new Vector3(.03f,.8f,.03f),gilt,root);rib.transform.localRotation=Quaternion.FromToRotation(Vector3.up,new Vector3(Mathf.Cos(a),-.36f,Mathf.Sin(a)).normalized);}
            // Valance: two tiers of hanging silk.
            for(int tier=0;tier<2;tier++){var vv=new List<Vector3>();var vu=new List<Vector2>();var vt=new List<int>();float radius=1.55f-tier*.08f,top=4.38f-tier*.32f,depth=.36f;
                for(int k=0;k<=64;k++){float u=k/64f,a=u*Mathf.PI*2;var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));float hem=depth+.05f*Mathf.Sin(a*32);vv.Add(dir*radius+Vector3.up*top);vv.Add(dir*(radius+.04f)+Vector3.up*(top-hem));vu.Add(new Vector2(u*6,.9f));vu.Add(new Vector2(u*6,.55f));
                    if(k<64){int b=k*2;vt.AddRange(new[]{b,b+2,b+1,b+1,b+2,b+3});}}
                Mesh(root,"Parasol valance",vv,vu,vt,silk);}
            WorldArt.Shape("Gilt finial",PrimitiveType.Sphere,new Vector3(0,5.12f,0),Vector3.one*.24f,gilt,root);
            WorldArt.Shape("Finial spike",PrimitiveType.Capsule,new Vector3(0,5.32f,0),new Vector3(.07f,.18f,.07f),gilt,root);
            Claim(p,1.8f);
        }
        // Ridge tent: canvas slopes that sag between poles, gable ends, an open door flap, guy ropes to stakes.
        static void Tent(Vector3 p,float yaw,Transform parent)
        {
            p.y=ScenicArt.Height(p);var root=new GameObject("Army tent").transform;root.SetParent(parent,false);root.localPosition=p;root.localRotation=Quaternion.Euler(0,yaw,0);
            var canvas=Resources.Load<Material>("Architecture/Undyed linen");var timber=Resources.Load<Material>("Architecture/Timber");
            const float half=1.3f,width=1.05f,ridge=1.55f;const int nx=10,ny=5;
            for(int side=-1;side<=1;side+=2){var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
                for(int j=0;j<=ny;j++)for(int i=0;i<=nx;i++){float u=i/(float)nx,w=j/(float)ny;float x=Mathf.Lerp(-half,half,u);float sag=Mathf.Sin(w*Mathf.PI)*.06f*(1-Mathf.Abs(Mathf.Cos(u*Mathf.PI*2))*.5f);
                    v.Add(new Vector3(x,Mathf.Lerp(ridge,0.02f,w)-sag,side*Mathf.Lerp(0,width,w)));uv.Add(new Vector2(u*2,w));}
                int row=nx+1;for(int j=0;j<ny;j++)for(int i=0;i<nx;i++){int a=j*row+i;if(side>0)t.AddRange(new[]{a,a+row,a+1,a+1,a+row,a+row+1});else t.AddRange(new[]{a,a+1,a+row,a+1,a+row+1,a+row});}
                Mesh(root,"Tent canvas",v,uv,t,canvas);}
            // Back gable closed; front gable with the door flap folded back.
            Mesh(root,"Tent gable",new List<Vector3>{new Vector3(-half,ridge,0),new Vector3(-half,.02f,width),new Vector3(-half,.02f,-width)},new List<Vector2>{new Vector2(.5f,1),new Vector2(0,0),new Vector2(1,0)},new List<int>{0,1,2,0,2,1},canvas);
            Mesh(root,"Tent door flap",new List<Vector3>{new Vector3(half,ridge,0),new Vector3(half+.35f,.02f,width*.9f),new Vector3(half,.02f,.05f)},new List<Vector2>{new Vector2(.5f,1),new Vector2(0,0),new Vector2(1,0)},new List<int>{0,1,2,0,2,1},canvas);
            Mesh(root,"Tent door flap",new List<Vector3>{new Vector3(half,ridge,0),new Vector3(half,.02f,-.05f),new Vector3(half,.02f,-width)},new List<Vector2>{new Vector2(.5f,1),new Vector2(0,0),new Vector2(1,0)},new List<int>{0,1,2,0,2,1},canvas);
            foreach(float x in new[]{-half,half}){var pole=WorldArt.Shape("Tent pole",PrimitiveType.Cylinder,new Vector3(x,ridge*.5f,0),new Vector3(.06f,ridge*.5f+.08f,.06f),Color.white,root);pole.GetComponent<Renderer>().sharedMaterial=timber;}
            var rope=new Color(.55f,.48f,.34f);
            foreach(float x in new[]{-half-.1f,half+.1f})foreach(float z in new[]{-1,1}){var from=new Vector3(x*.98f,ridge*.55f,z*width*.5f);var to=new Vector3(x*1.35f,0,z*(width+.7f));var r=WorldArt.Shape("Guy rope",PrimitiveType.Cylinder,(from+to)*.5f,new Vector3(.015f,Vector3.Distance(from,to)*.5f,.015f),rope,root);r.transform.localRotation=Quaternion.FromToRotation(Vector3.up,to-from);
                var stake=WorldArt.Shape("Tent stake",PrimitiveType.Cylinder,to+Vector3.up*.08f,new Vector3(.04f,.1f,.04f),Color.white,root);stake.GetComponent<Renderer>().sharedMaterial=timber;}
            TentDetail(root,half,width,ridge,nx,ny);ContactShadow(p,2.1f,1.7f,yaw,.55f);
            Claim(p,2.2f);
        }
        // River jetty: uneven planks on two stringers, piles with rope lashings.
        static void Jetty(Vector3 p,Transform parent)
        {
            var timber=Resources.Load<Material>("Architecture/Timber");var rope=new Color(.55f,.48f,.34f);
            for(int side=-1;side<=1;side+=2){var beam=WorldArt.Shape("Jetty stringer",PrimitiveType.Cube,p+new Vector3(side*1.0f,.06f,0),new Vector3(.16f,.14f,4.6f),Color.white,parent);beam.GetComponent<Renderer>().sharedMaterial=timber;}
            for(int i=0;i<9;i++){var plank=WorldArt.Shape("Jetty plank",PrimitiveType.Cube,p+new Vector3(R(-.05f,.05f),.16f+R(-.015f,.015f),-2.1f+i*.52f),new Vector3(2.35f+R(-.12f,.08f),.07f,.44f+R(-.04f,.02f)),Color.white,parent);plank.transform.localRotation=Quaternion.Euler(R(-2,2),R(-3,3),R(-2,2));plank.GetComponent<Renderer>().sharedMaterial=timber;}
            for(int side=-1;side<=1;side+=2)for(int z=0;z<3;z++){var at=p+new Vector3(side*1.1f,0,-1.9f+z*1.9f);var pile=WorldArt.Shape("Jetty pile",PrimitiveType.Cylinder,at+Vector3.up*.05f,new Vector3(.17f,.5f,.17f),Color.white,parent);pile.GetComponent<Renderer>().sharedMaterial=timber;
                WorldArt.Shape("Rope lashing",PrimitiveType.Cylinder,at+Vector3.up*.2f,new Vector3(.2f,.04f,.2f),rope,parent);}
        }
        static void Mesh(Transform parent,string name,List<Vector3> v,List<Vector2> uv,List<int> t,Material material)
        {
            var mesh=SceneArtResources.Own(new Mesh{name=name});mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        // Rammed earth with visible tamped courses (QA/sample-130/art_textures.py).
        static Material courses;static Material Courses(){if(courses)return courses;courses=SceneArtResources.Own(new Material(Resources.Load<Material>("Architecture/Rammed earth")){name="Rammed courses"});courses.mainTexture=Resources.Load<Texture2D>("Architecture/RammedCourses");courses.color=Color.white;courses.mainTextureScale=new Vector2(1,1);return courses;}
        static void CityWall(Vector3 p,Transform parent)
        {
            p.y=ScenicArt.Height(p);var earth=Courses();var tiles=Resources.Load<Material>("Architecture/Fired roof tiles");
            var wall=WorldArt.Shape("Baima city wall",PrimitiveType.Cube,p+Vector3.up*1.2f,new Vector3(1.6f,2.4f,2.15f),Color.white,parent);wall.GetComponent<Renderer>().sharedMaterial=earth;wall.transform.localRotation=Quaternion.Euler(0,R(-1.5f,1.5f),0);
            // Batter: the base flares out like real rammed-earth walls.
            var footing=WorldArt.Shape("Wall batter",PrimitiveType.Cube,p+Vector3.up*.35f,new Vector3(2.1f,.7f,2.2f),Color.white,parent);footing.GetComponent<Renderer>().sharedMaterial=earth;
            for(int m=-1;m<=1;m+=2){var merlon=WorldArt.Shape("Crenel",PrimitiveType.Cube,p+new Vector3(-.55f,2.65f,m*.5f),new Vector3(.35f,.5f,.6f),Color.white,parent);merlon.GetComponent<Renderer>().sharedMaterial=earth;}
            var walk=WorldArt.Shape("Wall walk",PrimitiveType.Cube,p+new Vector3(.1f,2.43f,0),new Vector3(1.5f,.08f,2.15f),Color.white,parent);walk.GetComponent<Renderer>().sharedMaterial=tiles;
        }
        static void Debris(Vector3 p,Transform parent)
        {
            var shield=WorldArt.Shape("Dropped shield",PrimitiveType.Cylinder,p+Vector3.up*.05f,new Vector3(.6f,.03f,.75f),new Color(.36f,.2f,.1f),parent);shield.transform.localRotation=Quaternion.Euler(R(-8,8),R(0,180),R(-8,8));
            var spear=WorldArt.Shape("Broken spear",PrimitiveType.Cylinder,p+new Vector3(.4f,.06f,.2f),new Vector3(.04f,.6f,.04f),new Color(.25f,.15f,.08f),parent);spear.transform.localRotation=Quaternion.Euler(88,R(0,180),0);
            Claim(p,.8f);
        }
    }
}
