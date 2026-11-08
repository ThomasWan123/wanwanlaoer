using System.Collections.Generic;
using UnityEngine;
namespace Sango3
{
    // Stage 16 props at close-up detail: Yellow River boats, the river bank (pebbles, grass, reeds, driftwood), tent
    // details and soft contact shadows under the props. Each prop is built into one mesh with a sub-mesh per material.
    public static partial class ScenarioScenery
    {
        // Mesh builder: geometry is collected per material and emitted as one mesh with sub-meshes.
        sealed class Kit
        {
            sealed class Part{public readonly List<Vector3> v=new List<Vector3>(),n=new List<Vector3>();public readonly List<Vector2> uv=new List<Vector2>();public readonly List<int> t=new List<int>();}
            readonly Dictionary<Material,Part> parts=new Dictionary<Material,Part>();readonly List<Material> order=new List<Material>();
            Part For(Material m){if(!parts.TryGetValue(m,out var p)){p=new Part();parts[m]=p;order.Add(m);}return p;}
            // Flat quad a-b-c-d; the front face is the side Cross(b-a,c-a) points to. twoSided adds the back face with its own normal.
            // translucent: the back face keeps the front normal, so thin cloth lit from one side reads lit from both.
            public Vector3? normalOverride;
            public void Quad(Material m,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector2 ua,Vector2 ub,Vector2 uc,Vector2 ud,bool twoSided=false,bool translucent=false)
            {
                var p=For(m);var n=Vector3.Cross(b-a,c-a);if(n.sqrMagnitude<1e-12f)n=Vector3.Cross(c-a,d-a);n.Normalize();if(normalOverride.HasValue)n=normalOverride.Value;int i=p.v.Count;
                p.v.Add(a);p.v.Add(b);p.v.Add(c);p.v.Add(d);p.n.Add(n);p.n.Add(n);p.n.Add(n);p.n.Add(n);p.uv.Add(ua);p.uv.Add(ub);p.uv.Add(uc);p.uv.Add(ud);p.t.Add(i);p.t.Add(i+1);p.t.Add(i+2);p.t.Add(i);p.t.Add(i+2);p.t.Add(i+3);
                if(twoSided){i=p.v.Count;var back=translucent?n:-n;p.v.Add(a);p.v.Add(b);p.v.Add(c);p.v.Add(d);p.n.Add(back);p.n.Add(back);p.n.Add(back);p.n.Add(back);p.uv.Add(ua);p.uv.Add(ub);p.uv.Add(uc);p.uv.Add(ud);p.t.Add(i);p.t.Add(i+2);p.t.Add(i+1);p.t.Add(i);p.t.Add(i+3);p.t.Add(i+2);}
            }
            public void Tri(Material m,Vector3 a,Vector3 b,Vector3 c,bool twoSided=false,bool translucent=false)=>Quad(m,a,b,c,c,new Vector2(0,0),new Vector2(1,0),new Vector2(.5f,1),new Vector2(.5f,1),twoSided,translucent);
            public void Box(Material m,Vector3 center,Quaternion rotation,Vector3 size)
            {
                var h=size*.5f;void Q(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector2 ua,Vector2 ub,Vector2 uc,Vector2 ud)=>Quad(m,a,d,c,b,ua,ud,uc,ub);Vector3 P(float x,float y,float z)=>center+rotation*new Vector3(x*h.x,y*h.y,z*h.z);
                Q(P(-1,-1,1),P(-1,1,1),P(1,1,1),P(1,-1,1),new Vector2(0,0),new Vector2(0,size.y),new Vector2(size.x,size.y),new Vector2(size.x,0));
                Q(P(1,-1,-1),P(1,1,-1),P(-1,1,-1),P(-1,-1,-1),new Vector2(0,0),new Vector2(0,size.y),new Vector2(size.x,size.y),new Vector2(size.x,0));
                Q(P(-1,-1,-1),P(-1,1,-1),P(-1,1,1),P(-1,-1,1),new Vector2(0,0),new Vector2(0,size.y),new Vector2(size.z,size.y),new Vector2(size.z,0));
                Q(P(1,-1,1),P(1,1,1),P(1,1,-1),P(1,-1,-1),new Vector2(0,0),new Vector2(0,size.y),new Vector2(size.z,size.y),new Vector2(size.z,0));
                Q(P(-1,1,1),P(-1,1,-1),P(1,1,-1),P(1,1,1),new Vector2(0,0),new Vector2(0,size.z),new Vector2(size.x,size.z),new Vector2(size.x,0));
                Q(P(-1,-1,-1),P(-1,-1,1),P(1,-1,1),P(1,-1,-1),new Vector2(0,0),new Vector2(0,size.z),new Vector2(size.x,size.z),new Vector2(size.x,0));
            }
            // A plank or beam laid from a to b, with the given width (side) and thickness, its width along `across`.
            public void Beam(Material m,Vector3 a,Vector3 b,float width,float thickness,Vector3 up)
            {
                var dir=b-a;if(dir.sqrMagnitude<1e-8f)return;Box(m,(a+b)*.5f,Quaternion.LookRotation(dir,up),new Vector3(width,thickness,dir.magnitude));
            }
            // Tapered round tube from a to b with smooth normals.
            public void Tube(Material m,Vector3 a,Vector3 b,float ra,float rb,int seg=8)
            {
                var p=For(m);var axis=(b-a).normalized;if(axis.sqrMagnitude<1e-8f)return;var side=Vector3.Cross(axis,Mathf.Abs(axis.y)<.95f?Vector3.up:Vector3.right).normalized;var side2=Vector3.Cross(axis,side);
                int i=p.v.Count;float length=Vector3.Distance(a,b);
                for(int k=0;k<=seg;k++){float ang=k*Mathf.PI*2/seg;var d=side*Mathf.Cos(ang)+side2*Mathf.Sin(ang);p.v.Add(a+d*ra);p.v.Add(b+d*rb);p.n.Add(d);p.n.Add(d);p.uv.Add(new Vector2(k/(float)seg,0));p.uv.Add(new Vector2(k/(float)seg,length));}
                for(int k=0;k<seg;k++){int q=i+k*2;p.t.Add(q);p.t.Add(q+2);p.t.Add(q+1);p.t.Add(q+2);p.t.Add(q+3);p.t.Add(q+1);}
            }
            // A pebble: an octahedron-based lump, jittered and flattened, with direction normals for a rounded look.
            public void Pebble(Material m,Vector3 at,Vector3 size,float yaw,System.Random random)
            {
                var p=For(m);var rot=Quaternion.Euler(0,yaw,0);int i=p.v.Count;const int rings=4,seg=7;
                for(int r=0;r<=rings;r++)for(int k=0;k<=seg;k++){float lat=Mathf.PI*(r/(float)rings-.5f),lon=k*Mathf.PI*2/seg;var d=new Vector3(Mathf.Cos(lat)*Mathf.Cos(lon),Mathf.Sin(lat),Mathf.Cos(lat)*Mathf.Sin(lon));
                    float j=(r==0||r==rings||k==seg)?1:.82f+(float)random.NextDouble()*.3f;if(k==seg){p.v.Add(p.v[i+r*(seg+1)]);p.n.Add(p.n[i+r*(seg+1)]);p.uv.Add(new Vector2(1,r/(float)rings));continue;}
                    var local=Vector3.Scale(d*j,size);if(local.y<-size.y*.35f)local.y=-size.y*.35f;p.v.Add(at+rot*local);p.n.Add(rot*new Vector3(d.x/size.x,d.y/size.y,d.z/size.z).normalized);p.uv.Add(new Vector2(k/(float)seg,r/(float)rings));}
                for(int r=0;r<rings;r++)for(int k=0;k<seg;k++){int a=i+r*(seg+1)+k,b=a+seg+1;p.t.Add(a);p.t.Add(b);p.t.Add(a+1);p.t.Add(a+1);p.t.Add(b);p.t.Add(b+1);}
            }
            public GameObject Build(string name,Transform parent,bool shadows=true)
            {
                var mesh=SceneArtResources.Own(new Mesh{name=name});int total=0;foreach(var m in order)total+=parts[m].v.Count;if(total>65000)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
                var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();foreach(var m in order){v.AddRange(parts[m].v);n.AddRange(parts[m].n);uv.AddRange(parts[m].uv);}
                // Non-finite vertices break rendering on device (sin(PI) is slightly negative, Pow of it is NaN); fail loudly here.
                foreach(var q in v)if(float.IsNaN(q.x+q.y+q.z)||float.IsInfinity(q.x+q.y+q.z))throw new System.Exception("Non-finite vertex in "+name);
                mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetUVs(0,uv);mesh.subMeshCount=order.Count;int offset=0;
                for(int s=0;s<order.Count;s++){var t=parts[order[s]].t;var shifted=new int[t.Count];for(int k=0;k<t.Count;k++)shifted[k]=t[k]+offset;mesh.SetTriangles(shifted,s);offset+=parts[order[s]].v.Count;}
                mesh.RecalculateTangents();mesh.RecalculateBounds();
                var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterials=order.ToArray();
                if(!shadows)r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return go;
            }
        }

        // Materials for this stage's props, made from the shared architecture materials and re-made if a stage teardown destroyed them.
        static Material propPlume,propTarred,propBamboo,propStraw,propSail,propRope,propMud,propSeam,propPebble,propPebbleDark,propDrift,propFloor;static Material[] propGrass;
        static Material Tinted(string source,string name,Color color,float smoothness,string texture=null)
        {
            var m=SceneArtResources.Own(new Material(Resources.Load<Material>(source)){name=name});m.color=color;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smoothness);
            if(texture!=null){var tex=Resources.Load<Texture2D>(texture);if(tex){m.mainTexture=tex;if(m.HasProperty("_BaseMap"))m.SetTexture("_BaseMap",tex);}}return m;
        }
        static void PropMaterials()
        {
            if(propTarred)return;
            propTarred=Tinted("Architecture/Timber","Tarred hull",new Color(.50f,.38f,.27f),.28f);
            propBamboo=Tinted("Architecture/Timber","Bamboo",new Color(.78f,.68f,.42f),.35f);
            propStraw=Tinted("Architecture/Undyed linen","Woven mat",new Color(.80f,.68f,.46f),.08f,"Architecture/Straw");
            propSail=Tinted("Architecture/Undyed linen","Tan sail",new Color(.84f,.66f,.45f),.05f);
            // Thin sailcloth passes some light through: a faint emission in the cloth colour keeps the shaded side from going black.
            propSail.EnableKeyword("_EMISSION");propSail.SetColor("_EmissionColor",new Color(.84f,.66f,.45f)*.16f);propSail.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;
            propRope=Tinted("Architecture/Undyed linen","Hemp rope",new Color(.52f,.44f,.31f),.05f);
            propMud=Tinted("Architecture/Undyed linen","Mud-stained canvas",new Color(.30f,.24f,.17f),.06f);
            propSeam=Tinted("Architecture/Undyed linen","Canvas seam",new Color(.40f,.36f,.26f),.06f);
            propPebble=Tinted("Architecture/Stone footing","River pebble",new Color(.62f,.58f,.52f),.30f);
            propPebbleDark=Tinted("Architecture/Stone footing","Wet pebble",new Color(.40f,.36f,.31f),.55f);
            propDrift=Tinted("Architecture/Timber","Driftwood",new Color(.74f,.70f,.62f),.12f);
            propFloor=Tinted("Architecture/Undyed linen","Tent floor",new Color(.30f,.26f,.20f),.05f,"Architecture/Straw");
            propGrass=new[]{Tinted("BattleLit","Dry grass",new Color(.66f,.58f,.33f),.08f),Tinted("BattleLit","Olive grass",new Color(.45f,.47f,.23f),.08f),Tinted("BattleLit","Green grass",new Color(.34f,.43f,.20f),.08f),Tinted("BattleLit","Reed seed",new Color(.42f,.30f,.18f),.05f)};
            foreach(var g in propGrass)g.SetFloat("_Cull",0);
            propPlume=Tinted("BattleLit","Reed plume",new Color(.80f,.72f,.56f),.05f);propPlume.SetFloat("_Cull",0);
        }

        // Yellow River boat: a flat-bottomed hull of lapped strakes with transom ends, a sheer rising to a high stern,
        // gunwale rails, ribs and thwarts, bow and stern decks, a woven-mat cabin, a mast with a battened sail (or the
        // sail furled), and the stern sculling oar. It bobs on the water (ScenicMotion kind 2), so it stays out of the static batch.
        static void Riverboat(Vector3 at,float yaw,float scale,bool sailSet,Transform parent)
        {
            PropMaterials();var timber=Resources.Load<Material>("Architecture/Timber");var kit=new Kit();
            const float L=4.4f,B=.78f,H=.42f,D=.38f;const int stations=16,strakes=5;
            float Beam(float u)=>B*(.40f+.60f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(Mathf.PI*u)),.65f));
            float Sheer(float u)=>H+.20f*Mathf.Pow(2*u-1,2)+.22f*Mathf.Max(0,u-.62f)/.38f;
            float Bottom(float u)=>-D*(.45f+.55f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(Mathf.PI*u)),.45f));
            float Z(float u)=>(.5f-u)*L;
            // Section: gunwale -> chine (s=.72) -> keel centre (s=1).
            Vector3 P(float u,float s,float side){float b=Beam(u),top=Sheer(u),bottom=Bottom(u);float x,y;
                if(s<=.72f){float k=s/.72f;x=Mathf.Lerp(b,b*.80f,k*k);y=Mathf.Lerp(top,bottom+.10f,k);}else{float k=(s-.72f)/.28f;x=Mathf.Lerp(b*.80f,0,k);y=Mathf.Lerp(bottom+.10f,bottom,Mathf.Sqrt(k));}
                return new Vector3(side*x,y,Z(u));}
            float[] edges={0,.15f,.29f,.43f,.57f,.72f,.86f,1};
            for(int side=-1;side<=1;side+=2)for(int i=0;i<stations;i++){float u0=i/(float)stations,u1=(i+1)/(float)stations;
                for(int k=0;k<edges.Length-1;k++){float s0=edges[k],s1=edges[k+1];
                    // Lapped (clinker) strakes: each plank's lower edge sits proud of the next one, so plank lines catch the light.
                    var lap=new Vector3(side*.022f,-.004f,0)*(k<strakes?1:.4f);
                    var a=P(u0,s0,side);var b=P(u1,s0,side);var c=P(u1,s1,side)+lap;var d=P(u0,s1,side)+lap;
                    var ua=new Vector2(u0*L*.5f,k*.5f);var ub=new Vector2(u1*L*.5f,k*.5f);var uc=new Vector2(u1*L*.5f,k*.5f+.45f);var ud=new Vector2(u0*L*.5f,k*.5f+.45f);
                    if(side>0)kit.Quad(propTarred,a,b,c,d,ua,ub,uc,ud,true);else kit.Quad(propTarred,a,d,c,b,ua,ud,uc,ub,true);}}
            // Transom ends, planked across.
            foreach(float u in new[]{0f,1f}){var centre=new Vector3(0,(Sheer(u)+Bottom(u))*.5f,Z(u));
                for(int k=0;k<edges.Length-1;k++)for(int side=-1;side<=1;side+=2)kit.Tri(propTarred,centre,P(u,edges[k],side),P(u,edges[k+1],side),true);
                kit.Beam(timber,P(u,0,-1)+Vector3.up*.03f,P(u,0,1)+Vector3.up*.03f,.09f,.08f,Vector3.up);}
            // Gunwale rails and a rubbing strake.
            for(int side=-1;side<=1;side+=2)for(int i=0;i<stations;i++){float u0=i/(float)stations,u1=(i+1)/(float)stations;
                kit.Beam(timber,P(u0,0,side)+new Vector3(side*.02f,.03f,0),P(u1,0,side)+new Vector3(side*.02f,.03f,0),.08f,.07f,Vector3.up);
                kit.Beam(timber,P(u0,.29f,side)+new Vector3(side*.04f,0,0),P(u1,.29f,side)+new Vector3(side*.04f,0,0),.045f,.05f,Vector3.up);}
            // Ribs inside the hull and thwarts across it.
            foreach(float u in new[]{.18f,.30f,.42f,.54f,.66f,.78f}){var inward=new Vector3(0,.02f,0);
                for(int side=-1;side<=1;side+=2){var g=P(u,0,side)*.94f;g.z=Z(u);var c=P(u,.72f,side)*.94f;c.z=Z(u);var k=P(u,1,side)+inward;kit.Beam(timber,g,c,.05f,.04f,new Vector3(side,0,0));kit.Beam(timber,c,k,.05f,.04f,Vector3.up);}}
            foreach(float u in new[]{.24f,.44f}){float y=Sheer(u)-.12f;kit.Beam(timber,new Vector3(-Beam(u)*.93f,y,Z(u)),new Vector3(Beam(u)*.93f,y,Z(u)),.20f,.05f,Vector3.up);}
            // Bow and stern decks.
            for(int k=0;k<5;k++){float u=.015f+k*.026f;float y=Sheer(u)-.07f;kit.Beam(timber,new Vector3(-Beam(u)*.95f,y,Z(u)),new Vector3(Beam(u)*.95f,y,Z(u)),.11f,.04f,Vector3.up);}
            for(int k=0;k<6;k++){float u=.855f+k*.025f;float y=Sheer(u)-.07f;kit.Beam(timber,new Vector3(-Beam(u)*.95f,y,Z(u)),new Vector3(Beam(u)*.95f,y,Z(u)),.11f,.04f,Vector3.up);}
            // Cabin: bamboo hoops under an arched woven mat.
            {const float c0=.52f,c1=.80f;const int along=6,around=12;
                Vector3 Arch(float u,float a){float r=Beam(u)*.92f,base_=Sheer(u)-.04f;return new Vector3(-Mathf.Cos(a)*r,base_+Mathf.Sin(a)*.78f,Z(u));}
                for(int i=0;i<along;i++)for(int k=0;k<around;k++){float u0=Mathf.Lerp(c0,c1,i/(float)along),u1=Mathf.Lerp(c0,c1,(i+1)/(float)along),a0=Mathf.PI*k/around,a1=Mathf.PI*(k+1)/around;
                    kit.Quad(propStraw,Arch(u0,a0),Arch(u0,a1),Arch(u1,a1),Arch(u1,a0),new Vector2(u0*3,a0),new Vector2(u0*3,a1),new Vector2(u1*3,a1),new Vector2(u1*3,a0),true);}
                foreach(float u in new[]{c0,.66f,c1})for(int k=0;k<around;k++)kit.Tube(propBamboo,Arch(u,Mathf.PI*k/around)+Vector3.up*.015f,Arch(u,Mathf.PI*(k+1)/around)+Vector3.up*.015f,.025f,.025f,5);}
            // Mast and sail.
            {float um=.30f;var foot=new Vector3(0,Bottom(um)+.08f,Z(um));var head=new Vector3(0,Sheer(um)+3.1f,Z(um));kit.Tube(timber,foot,head,.075f,.045f,8);
                kit.Tube(propRope,head+Vector3.down*.1f,P(.02f,0,0)+Vector3.up*.05f,.012f,.012f,4);kit.Tube(propRope,head+Vector3.down*.1f,P(.97f,0,0)+Vector3.up*.05f,.012f,.012f,4);
                if(sailSet){
                    // Battened lug sail: a trapezoid set mostly aft of the mast, five bamboo battens, a slight belly between them.
                    float y0=Sheer(um)+.55f,y1=Sheer(um)+2.95f;float zm=Z(um);const int battens=6,cols=6;var corners=new Vector3[battens+1,cols+1];
                    for(int r=0;r<=battens;r++){float w=r/(float)battens;float y=Mathf.Lerp(y0,y1,w);float fore=Mathf.Lerp(.42f,.30f,w),aft=Mathf.Lerp(1.75f,1.25f,w);
                        for(int c=0;c<=cols;c++){float q=c/(float)cols;float belly=r==0||r==battens||c==0||c==cols?0:Mathf.Sin(q*Mathf.PI)*.10f*(r%1==0?1:1);float panel=Mathf.Sin((w*battens-Mathf.Floor(w*battens))*Mathf.PI);
                            corners[r,c]=new Vector3(.12f+belly+Mathf.Sin(q*Mathf.PI)*.05f,y+(c==cols?-.06f*w:0),zm+fore-(fore+aft)*q);}}
                    // One normal for the whole sail (its plane), so the cloth shades smoothly instead of showing its triangles.
                    kit.normalOverride=Vector3.right;
                    for(int r=0;r<battens;r++)for(int c=0;c<cols;c++){var a=corners[r,c];var b=corners[r,c+1];var cc=corners[r+1,c+1];var d=corners[r+1,c];
                        var mid=(a+b+cc+d)*.25f+new Vector3(.05f,0,0);kit.Tri(propSail,a,b,mid,true,true);kit.Tri(propSail,b,cc,mid,true,true);kit.Tri(propSail,cc,d,mid,true,true);kit.Tri(propSail,d,a,mid,true,true);}
                    kit.normalOverride=null;
                    for(int r=0;r<=battens;r++){for(int c=0;c<cols;c++)kit.Tube(propBamboo,corners[r,c]+Vector3.right*.02f,corners[r,c+1]+Vector3.right*.02f,.022f,.022f,5);
                        kit.Tube(propRope,corners[r,cols],new Vector3(.25f,Sheer(.9f)+.1f,Z(.9f)),.008f,.008f,3);}
                }else{
                    // Sail lowered and bundled along the boom.
                    var a=new Vector3(.1f,Sheer(um)+.62f,Z(um)+.35f);var b=new Vector3(.1f,Sheer(.62f)+.55f,Z(.62f)-.1f);kit.Tube(propSail,a,b,.13f,.10f,9);kit.Tube(propBamboo,a+Vector3.down*.12f,b+Vector3.down*.12f,.03f,.03f,5);
                    for(int k=1;k<5;k++){var p=Vector3.Lerp(a,b,k/5f);kit.Tube(propRope,p+new Vector3(-.15f,-.02f,0),p+new Vector3(.15f,-.02f,0),.018f,.018f,4);}}}
            // Stern sculling oar (橹) trailing into the water, and a spare oar on the thwarts.
            {var pivot=new Vector3(.15f,Sheer(1)+.12f,Z(1)+.05f);var blade=new Vector3(.35f,-.25f,Z(1)-1.7f);kit.Tube(timber,pivot+new Vector3(-.05f,.35f,.55f),blade,.035f,.03f,6);kit.Beam(timber,blade+new Vector3(0,.05f,.25f),blade+new Vector3(0,-.05f,-.25f),.20f,.03f,Vector3.right);
                kit.Tube(timber,new Vector3(-.45f,Sheer(.24f)-.06f,Z(.24f)+.1f),new Vector3(.40f,Sheer(.44f)-.06f,Z(.44f)-.1f),.03f,.03f,6);}
            var root=new GameObject("Yellow River boat").transform;root.SetParent(parent.parent,false);root.position=parent.TransformPoint(at);root.rotation=parent.rotation*Quaternion.Euler(0,yaw,0);root.localScale=Vector3.one*scale;
            kit.Build("Yellow River boat hull",root);root.gameObject.AddComponent<ScenicMotion>().kind=2;
            // Where the hull meets the water: a darker patch of shade under it and a ring of small waves around it.
            WaterCard("Boat water shade",at,yaw,(B+.45f)*scale,(L*.5f+.55f)*scale,.03f,new Color(.08f,.06f,.03f,.55f),"fx_soft",parent);
            WaterCard("Boat ripple",at,yaw,(B+.5f)*scale,(L*.5f+.6f)*scale,.045f,new Color(.95f,.9f,.8f,.20f),"fx_ring",parent);
        }

        // A flat transparent card on the water (kept out of the static batch with the other transparent scenery).
        static void WaterCard(string name,Vector3 at,float yaw,float halfWidth,float halfLength,float y,Color colour,string texture,Transform parent)
        {
            var r=Quaternion.Euler(0,yaw,0);var v=new List<Vector3>();foreach(var q in new[]{new Vector2(-1,-1),new Vector2(-1,1),new Vector2(1,1),new Vector2(1,-1)})v.Add(new Vector3(at.x,y,at.z)+r*new Vector3(q.x*halfWidth,0,q.y*halfLength));
            var mesh=SceneArtResources.Own(new Mesh{name=name});mesh.SetVertices(v);mesh.SetUVs(0,new List<Vector2>{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)});mesh.SetColors(new List<Color>{colour,colour,colour,colour});mesh.SetTriangles(new[]{0,1,2,0,2,3},0);mesh.RecalculateBounds();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.SetParent(parent.parent,true);go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var rr=go.GetComponent<MeshRenderer>();rr.sharedMaterial=Fx.Mat(texture,false,1);rr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;rr.receiveShadows=false;
        }

        // Large-scale variation over the floodplain so the tiled ground texture does not read as one repeating pattern:
        // broad darker (damp, trampled) and lighter (dry silt) patches, following the terrain, kept off the road, the
        // hero pads and the river bank. One transparent mesh with vertex colours.
        public static bool SkipGroundVariation,SkipRidges;
        static void GroundVariation(Transform parent)
        {
            if(SkipGroundVariation)return;
            const float x0=-27,x1=27,z0=-11,z1=12,step=.5f;int nx=Mathf.RoundToInt((x1-x0)/step),nz=Mathf.RoundToInt((z1-z0)/step);
            var v=new List<Vector3>((nx+1)*(nz+1));var c=new List<Color>(v.Capacity);var uv=new List<Vector2>(v.Capacity);var t=new List<int>(nx*nz*6);
            var dark=new Color(.20f,.19f,.09f);var light=new Color(.74f,.65f,.44f);
            for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++){var p=new Vector3(x0+i*step,0,z0+j*step);p.y=ScenicArt.Height(p)+.04f;v.Add(p);uv.Add(new Vector2(.5f,.5f));
                float n=Mathf.PerlinNoise(p.x*.11f+3.1f,p.z*.11f+1.7f)*.7f+Mathf.PerlinNoise(p.x*.43f+7,p.z*.43f+2)*.3f;
                float keep=1;if(ScenicArt.NearRoad(p,1.25f))keep=0;foreach(var s in BattleRules.Slots)if(Vector3.Distance(new Vector3(p.x,0,p.z),new Vector3(s.x,0,s.z))<1.3f)keep=0;
                keep*=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(Shore(p.x)-3f,Shore(p.x)-1.6f,p.z));
                c.Add(n<.52f?new Color(dark.r,dark.g,dark.b,Mathf.Min(.38f,(.52f-n)*2.2f)*keep):n>.58f?new Color(light.r,light.g,light.b,Mathf.Min(.13f,(n-.58f)*.8f)*keep):new Color(0,0,0,0));
                if(i<nx&&j<nz){int a=j*(nx+1)+i;t.Add(a);t.Add(a+nx+1);t.Add(a+1);t.Add(a+1);t.Add(a+nx+1);t.Add(a+nx+2);}}
            var mesh=SceneArtResources.Own(new Mesh{name="Ground variation"});mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(v);mesh.SetColors(c);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateBounds();
            var go=new GameObject("Ground variation",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.SetParent(parent.parent,true);go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=Fx.Mat(null,false,1);r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
        }
        // Distance beyond the far bank: three low ridgelines, each further one lighter and closer to the sky colour (aerial
        // perspective), with a broken top line, so the horizon has depth instead of ending at a flat band.
        static void DistantRidges(Transform parent)
        {
            if(SkipRidges)return;
            // 0.26: each ridge is shaded by height (a darker, broken crest fading into sky-coloured haze at its foot) and
            // along its length (lighter and darker slopes), and the nearest one carries a tree-line silhouette, instead of
            // one flat colour per layer. Unlit vertex colours: at this distance the haze, not the sun, sets the tone.
            var sky=Campaign.Current.Sky;var land=new Color(.40f,.38f,.27f);
            var layers=new[]{(z:44f,h:10f,haze:.38f,seed:1.3f,trees:true),(z:64f,h:16f,haze:.58f,seed:4.1f,trees:false),(z:92f,h:24f,haze:.78f,seed:8.7f,trees:false)};
            var v=new List<Vector3>();var c=new List<Color>();var uv=new List<Vector2>();var t=new List<int>();
            foreach(var L in layers){int start=v.Count;const int samples=300;
                for(int k=0;k<=samples;k++){float x=-150+k*(300f/samples);
                    float top=.6f+L.h*(.35f+.65f*Mathf.PerlinNoise(x*.035f+L.seed,L.seed))+L.h*.25f*(Mathf.PerlinNoise(x*.14f+L.seed*2,1)-.5f);
                    if(L.trees)top+=.55f*Mathf.PerlinNoise(x*1.3f+L.seed,3)+.35f*Mathf.PerlinNoise(x*3.1f,7); // canopy bumps
                    float slope=.82f+.36f*Mathf.PerlinNoise(x*.09f+L.seed*3,5);
                    var crest=Color.Lerp(land*slope,sky,L.haze);crest.a=1;var foot=Color.Lerp(land,sky,Mathf.Min(1,L.haze+.38f));foot.a=1;var mid=Color.Lerp(crest,foot,.45f);
                    v.Add(new Vector3(x,top,L.z));v.Add(new Vector3(x,top*.55f,L.z));v.Add(new Vector3(x,-2,L.z));// Vertex colours go to the shader as linear values: convert from the display (sRGB) colours chosen here.
                    c.Add(crest.linear);c.Add(mid.linear);c.Add(foot.linear);uv.Add(new Vector2(.5f,.5f));uv.Add(new Vector2(.5f,.5f));uv.Add(new Vector2(.5f,.5f));
                    if(k>0){int a=start+(k-1)*3,b=start+k*3;for(int row=0;row<2;row++){t.Add(a+row);t.Add(b+row);t.Add(a+row+1);t.Add(b+row);t.Add(b+row+1);t.Add(a+row+1);}}}}
            var mesh=SceneArtResources.Own(new Mesh{name="Distant ridges"});mesh.SetVertices(v);mesh.SetColors(c);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateBounds();
            var go=new GameObject("Distant ridges",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.SetParent(parent.parent,true);go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=Fx.Mat(null,false,1);r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
        }
        // River bank detail along the shore: wet and dry pebbles at the waterline, grass tufts on the dry bank, reed beds
        // with seed heads, and bleached driftwood. One static mesh per kind.
        static System.Collections.IEnumerator Riverbank(Transform parent)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Sango3.Baima.Pebbles");
            PropMaterials();var kit=new Kit();
            for(int k=0;k<420;k++){float x=R(-27,27);if(Mathf.Abs(x+12.5f)<1.6f)continue;
                // Stones gather in patches (gravel bars) with bare silt between them, not an even line.
                float patch=Mathf.PerlinNoise(x*.31f,2.3f);if(R(0,1)>patch*patch*2.2f-.15f)continue;float off=R(-.55f,.45f);var p=new Vector3(x,0,Shore(x)+off);p.y=off<-.1f?.0f:-.02f-.08f*Mathf.InverseLerp(-.1f,.45f,off);
                float s=R(.06f,.18f);if(rng.Next(9)==0)s*=2.2f;kit.Pebble(off>-.15f?propPebbleDark:propPebble,p,new Vector3(s*R(1,1.5f),s*R(.45f,.7f),s),R(0,360),rng);}
            for(int k=0;k<70;k++){float x=R(-27,27);if(Mathf.Abs(x+12.5f)<1.8f)continue;if(R(0,1)>Mathf.PerlinNoise(x*.31f,2.3f)*1.6f-.2f)continue;var p=new Vector3(x,.02f,Shore(x)-R(.6f,1.6f));float s=R(.18f,.36f);kit.Pebble(propPebble,p,new Vector3(s*R(1,1.6f),s*R(.4f,.6f),s),R(0,360),rng);}
            kit.Build("River pebbles",parent);UnityEngine.Profiling.Profiler.EndSample();yield return null;UnityEngine.Profiling.Profiler.BeginSample("Sango3.Baima.Grass");
            var grass=new Kit();
            for(int c=0;c<520;c++){float x=R(-27,27);Vector3 p;
                if(c<170){p=new Vector3(x,0,Shore(x)-R(.7f,2.6f));}else{p=new Vector3(R(-17,13),0,R(-8,9.5f));if(ScenicArt.NearRoad(p,1.1f)||!Free(p,.3f))continue;
                    // Floodplain grass grows in patches, not evenly.
                    if(R(0,1)>Mathf.PerlinNoise(p.x*.22f+5,p.z*.22f)*1.5f-.25f)continue;}
                if(Mathf.Abs(p.x+12.5f)<1.6f&&p.z>9)continue;p.y=ScenicArt.Height(p);Tuft(grass,p,propGrass[rng.Next(3)],R(.22f,.48f),5+rng.Next(6));}
            grass.Build("Bank grass",parent,false);UnityEngine.Profiling.Profiler.EndSample();yield return null;UnityEngine.Profiling.Profiler.BeginSample("Sango3.Baima.Reeds");
            var reeds=new Kit();
            for(int c=0;c<22;c++){float cx=R(-24,24);if(Mathf.Abs(cx+12.5f)<2.2f)continue;int n=8+rng.Next(12);var centre=new Vector3(cx,0,Shore(cx));
                for(int k=0;k<n;k++){float x=cx+R(-1f,1f);var p=new Vector3(x,-.02f,Shore(x)+R(-.8f,.35f));Reed(reeds,p,(p-centre).normalized,R(.8f,1.45f));}}
            reeds.Build("Reed beds",parent,false);UnityEngine.Profiling.Profiler.EndSample();yield return null;UnityEngine.Profiling.Profiler.BeginSample("Sango3.Baima.Driftwood");
            var drift=new Kit();
            for(int k=0;k<5;k++){float x=R(-8,24);var a=new Vector3(x,.04f,Shore(x)-R(.1f,.6f));var dir=Quaternion.Euler(0,R(0,360),0)*Vector3.forward;float len=R(1.2f,2.4f);var b=a+dir*len;b.y=.06f;
                drift.Tube(propDrift,a,b,R(.08f,.12f),R(.04f,.06f),7);var stub=Vector3.Lerp(a,b,R(.3f,.7f));drift.Tube(propDrift,stub,stub+Quaternion.Euler(0,R(30,70),0)*dir*R(.3f,.5f)+Vector3.up*.08f,.04f,.015f,5);}
            drift.Build("Driftwood",parent);UnityEngine.Profiling.Profiler.EndSample();yield return null;
        }
        // One curved grass tuft as its own small mesh, for the generic landscape grass on stage 16.
        internal static void CurvedTuftAt(Vector3 p,Transform parent){PropMaterials();var keep=rng;rng=new System.Random(Mathf.RoundToInt(p.x*1000)*31+Mathf.RoundToInt(p.z*1000)); /* own generator: called before the scenery seed is set */
            try{var kit=new Kit();Tuft(kit,p,propGrass[1+rng.Next(2)],R(.3f,.55f),7+rng.Next(5));kit.Build("Grass tuft",parent,false);}finally{rng=keep;}}
        // Campfire for Yan Liang's camp (0.27): a ring of stones, charred logs leaning together over an ash bed, layered
        // flame cards with a flame-shaped texture (not a glowing ball), embers, a warm pool of light on the ground and a
        // flickering point light that lights the tents, sacks and ground around it.
        static void CampFire(Vector3 at,Transform parent)
        {
            PropMaterials();at.y=ScenicArt.Height(at);var kit=new Kit();
            var charred=Tinted("Architecture/Timber","Charred log",new Color(.22f,.16f,.11f),.08f);var ash=Tinted("Architecture/Stone footing","Ash bed",new Color(.30f,.28f,.26f),.05f);
            for(int i=0;i<10;i++){float a=i*Mathf.PI*2/10+R(-.1f,.1f);var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));float s=R(.11f,.17f);kit.Pebble(propPebbleDark,at+d*.58f+Vector3.up*.03f,new Vector3(s*1.3f,s*.75f,s),R(0,360),rng);}
            kit.Pebble(ash,at+Vector3.up*.01f,new Vector3(.45f,.05f,.45f),0,rng);
            for(int k=0;k<5;k++){float a=k*Mathf.PI*2/5+R(-.2f,.2f);var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));kit.Tube(charred,at+d*.48f+Vector3.up*.05f,at+Vector3.up*.38f-d*.04f,.065f,.045f,6);}
            kit.Build("Campfire",parent);
            var root=new GameObject("Campfire light").transform;root.SetParent(parent,false);root.localPosition=at;root.SetParent(parent.parent,true);
            var flameMat=SceneArtResources.Own(new Material(Resources.Load<Shader>("Fx/FxAdditive")){name="Campfire flame"});flameMat.mainTexture=CampFireFlicker.Texture();flameMat.SetFloat("_Boost",1.5f);flameMat.renderQueue=3100;
            var flames=new System.Collections.Generic.List<Transform>();
            Transform Card(string name,Material m,Vector3 local,Quaternion rot,Vector2 size,Color c,bool pivotBottom)
            {
                var mesh=SceneArtResources.Own(new Mesh{name=name});float y0=pivotBottom?0:-.5f;
                mesh.SetVertices(new System.Collections.Generic.List<Vector3>{new Vector3(-.5f,y0,0),new Vector3(-.5f,y0+1,0),new Vector3(.5f,y0+1,0),new Vector3(.5f,y0,0)});
                mesh.SetUVs(0,new System.Collections.Generic.List<Vector2>{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)});mesh.SetColors(new System.Collections.Generic.List<Color>{c,c,c,c});mesh.SetTriangles(new[]{0,1,2,0,2,3},0);mesh.RecalculateBounds();
                var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.transform.localPosition=local;go.transform.localRotation=rot;go.transform.localScale=new Vector3(size.x,size.y,1);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=m;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;return go.transform;
            }
            // Outer flames (orange) and inner, brighter, shorter ones; crossed so they read from any camera angle.
            for(int i=0;i<3;i++)flames.Add(Card("Flame",flameMat,Vector3.up*.08f,Quaternion.Euler(0,i*60+R(-8,8),0),new Vector2(1.0f,R(1.1f,1.3f)),new Color(1,.75f,.5f,.95f),true));
            for(int i=0;i<2;i++)flames.Add(Card("Flame core",flameMat,Vector3.up*.08f,Quaternion.Euler(0,30+i*90,0),new Vector2(.6f,.75f),new Color(1,1,.9f,1),true));
            Card("Embers",Fx.Mat("fx_soft",true,1.6f),Vector3.up*.05f,Quaternion.Euler(90,0,0),new Vector2(.95f,.95f),new Color(1,.42f,.12f,.9f),false);
            // Warm light pooled on the ground around the fire (independent of the per-pixel light, so it also shows where
            // the device limits lights).
            Card("Fire glow",Fx.Mat("fx_soft",true,1f),Vector3.up*.06f,Quaternion.Euler(90,0,0),new Vector2(5.2f,5.2f),new Color(1,.55f,.25f,.30f),false);
            var light=new GameObject("Fire point light").AddComponent<Light>();light.transform.SetParent(root,false);light.transform.localPosition=Vector3.up*.9f;
            light.type=LightType.Point;light.color=new Color(1,.62f,.32f);light.range=8f*root.lossyScale.y;light.intensity=4f;light.shadows=LightShadows.None;
            var flicker=root.gameObject.AddComponent<CampFireFlicker>();flicker.fireLight=light;flicker.flames=flames.ToArray();flicker.baseIntensity=4f;
        }
        // Tactical readability of the march route (0.27): from the battle camera the cobbled route and the olive ground had
        // nearly the same brightness. A light, warm dust band lies on the route and a soft dark trampled verge runs along
        // both edges, so the route's outline reads at a glance. Transparent vertex-coloured strips (colours converted to
        // linear for the shader).
        static void RouteReadability(Transform parent)
        {
            var route=BattleRules.Route;var v=new List<Vector3>();var c=new List<Color>();var uv=new List<Vector2>();var t=new List<int>();
            void Band(Vector3 a,Vector3 b,float from,float to,Color inner,Color outer,float extend)
            {
                var dir=(b-a).normalized;var side=Vector3.Cross(Vector3.up,dir);var a2=a-dir*extend;var b2=b+dir*extend;
                foreach(float sgn in new[]{-1f,1f}){int i=v.Count;
                    var p0=a2+side*sgn*from;var p1=b2+side*sgn*from;var p2=b2+side*sgn*to;var p3=a2+side*sgn*to;
                    foreach(var q in new[]{p0,p1,p2,p3}){var w=q;w.y=Mathf.Max(ScenicArt.Height(q),0)+.075f;v.Add(w);uv.Add(new Vector2(.5f,.5f));}
                    c.Add(inner.linear);c.Add(inner.linear);c.Add(outer.linear);c.Add(outer.linear);
                    if(sgn<0){t.Add(i);t.Add(i+2);t.Add(i+1);t.Add(i);t.Add(i+3);t.Add(i+2);}else{t.Add(i);t.Add(i+1);t.Add(i+2);t.Add(i);t.Add(i+2);t.Add(i+3);}}
            }
            var dust=new Color(.95f,.86f,.66f,.10f);var dustEdge=new Color(.95f,.86f,.66f,.04f);var verge=new Color(.10f,.08f,.05f,.58f);var clear=new Color(.12f,.10f,.06f,0);
            for(int i=1;i<route.Length;i++){var a=route[i-1];var b=route[i];a.y=b.y=0;
                Band(a,b,0,.85f,dust,dustEdge,-.85f); // dusty, lighter route surface (stops short of corners: no double blending)
                Band(a,b,1.02f,1.9f,verge,clear,.6f);} // dark trampled verge just outside the curbs
            var mesh=SceneArtResources.Own(new Mesh{name="Route readability"});mesh.SetVertices(v);mesh.SetColors(c);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateBounds();
            var go=new GameObject("Route readability",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.SetParent(parent.parent,true);go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=Fx.Mat(null,false,1);r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
        }
        // A grass tuft: tapered, bent blades fanning out from one point.
        static void Tuft(Kit kit,Vector3 p,Material m,float height,int blades)
        {
            for(int b=0;b<blades;b++){float a=R(0,Mathf.PI*2);var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));float h=height*R(.6f,1.1f);var root=p+dir*R(0,.06f);
                // An arc: up first, then bending outward and over under its own weight.
                CurvedBlade(kit,m,root,dir,h,R(.25f,.7f),.034f,4);}
        }
        // A blade curving from its root: rises with height h and bends out along dir by bend*h (quadratic), tapering to a
        // point; one consistent face direction so the segments join.
        static void CurvedBlade(Kit kit,Material m,Vector3 root,Vector3 dir,float h,float bend,float width,int segments)
        {
            var side=Vector3.Cross(Vector3.up,dir).normalized;if(side.sqrMagnitude<.01f)side=Vector3.right;
            Vector3 P(float t)=>root+Vector3.up*h*(t-.35f*bend*t*t)+dir*h*bend*t*t;
            for(int k=0;k<segments;k++){float t0=k/(float)segments,t1=(k+1)/(float)segments;var a=P(t0);var b=P(t1);float wa=width*Mathf.Pow(1-t0,.8f),wb=width*Mathf.Pow(1-t1,.8f)+.001f;
                kit.Quad(m,a-side*wa*.5f,b-side*wb*.5f,b+side*wb*.5f,a+side*wa*.5f,new Vector2(0,t0),new Vector2(0,t1),new Vector2(1,t1),new Vector2(1,t0),false);}
        }
        // A Yellow River reed: a slightly curved stem leaning away from its clump, long leaves that rise and droop, and a
        // soft pale plume hanging from the tip (most stems), instead of a straight stick with a cone.
        static void Reed(Kit kit,Vector3 p,Vector3 away,float h)
        {
            if(away.sqrMagnitude<.01f)away=Vector3.forward;away.y=0;away.Normalize();var lean=(away*.7f+new Vector3(R(-.3f,.3f),0,R(-.3f,.3f))).normalized;float bend=R(.05f,.22f);
            Vector3 S(float t)=>p+Vector3.up*h*t+lean*h*bend*t*t;
            for(int k=0;k<4;k++){float t0=k/4f,t1=(k+1)/4f;kit.Tube(propGrass[k<2?1:0],S(t0),S(t1),Mathf.Lerp(.013f,.006f,t0),Mathf.Lerp(.013f,.006f,t1),4);}
            int leaves=2+rng.Next(3);for(int l=0;l<leaves;l++){float t=R(.15f,.6f);var from=S(t);float a=R(0,Mathf.PI*2);var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));float L=R(.3f,.55f);
                var side=Vector3.Cross(Vector3.up,d).normalized;
                for(int k=0;k<4;k++){float u0=k/4f,u1=(k+1)/4f;Vector3 Lp(float u)=>from+d*L*u+Vector3.up*L*(.45f*u-.75f*u*u);var a0=Lp(u0);var a1=Lp(u1);float w0=.035f*Mathf.Sin(Mathf.PI*(.15f+u0*.85f)),w1=.035f*Mathf.Sin(Mathf.PI*(.15f+u1*.85f))+.001f;
                    kit.Quad(propGrass[l%2==0?1:2],a0-side*w0*.5f,a1-side*w1*.5f,a1+side*w1*.5f,a0+side*w0*.5f,new Vector2(0,u0),new Vector2(0,u1),new Vector2(1,u1),new Vector2(1,u0),false);}}
            if(rng.Next(4)>0){var tip=S(1);for(int k=0;k<6;k++){float a=R(0,Mathf.PI*2);var d=(lean*1.2f+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.6f).normalized;
                    // Plume strands: short, soft, hanging over toward the lean.
                    CurvedBlade(kit,propPlume,tip-Vector3.up*.02f,d,R(.12f,.2f),R(.9f,1.6f),.02f,3);}}
        }
        // One flat blade segment, its face turned toward the horizontal so it reads from any battle camera angle.
        static void BladeStrip(Kit kit,Material m,Vector3 a,Vector3 b,float wa,float wb)
        {
            var axis=(b-a).normalized;var side=Vector3.Cross(axis,Quaternion.Euler(0,R(0,180),0)*Vector3.forward).normalized;if(side.sqrMagnitude<.01f)side=Vector3.right;
            kit.Quad(m,a-side*wa*.5f,b-side*wb*.5f,b+side*wb*.5f,a+side*wa*.5f,new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0),false);
        }

        // Tent details on top of the canvas: ridge pole, darker seam strips down the slopes, a mud-stained hem, stones
        // holding the hem, and a floor mat visible through the open door. Built in the tent's own space.
        static void TentDetail(Transform root,float half,float width,float ridge,int nx,int ny)
        {
            PropMaterials();var kit=new Kit();var timber=Resources.Load<Material>("Architecture/Timber");
            Vector3 Canvas(float u,float w,int side){float x=Mathf.Lerp(-half,half,u);float sag=Mathf.Sin(w*Mathf.PI)*.06f*(1-Mathf.Abs(Mathf.Cos(u*Mathf.PI*2))*.5f);return new Vector3(x,Mathf.Lerp(ridge,0.02f,w)-sag,side*Mathf.Lerp(0,width,w));}
            Vector3 Out(int side){return new Vector3(0,width,side*ridge).normalized;}
            kit.Tube(timber,new Vector3(-half-.12f,ridge+.03f,0),new Vector3(half+.12f,ridge+.03f,0),.045f,.045f,7);
            for(int side=-1;side<=1;side+=2){var o=Out(side);
                foreach(float u in new[]{.2f,.4f,.6f,.8f})for(int j=0;j<ny;j++){float w0=j/(float)ny,w1=(j+1)/(float)ny;var a=Canvas(u,w0,side)+o*.012f;var b=Canvas(u,w1,side)+o*.012f;var s=Vector3.right*.022f;
                    if(side>0)kit.Quad(propSeam,a-s,b-s,b+s,a+s,new Vector2(0,w0),new Vector2(0,w1),new Vector2(1,w1),new Vector2(1,w0));else kit.Quad(propSeam,a+s,b+s,b-s,a-s,new Vector2(0,w0),new Vector2(0,w1),new Vector2(1,w1),new Vector2(1,w0));}
                for(int i=0;i<nx;i++){float u0=i/(float)nx,u1=(i+1)/(float)nx;var a=Canvas(u0,.82f,side)+o*.008f;var b=Canvas(u1,.82f,side)+o*.008f;var c=Canvas(u1,1,side)+o*.008f;var d=Canvas(u0,1,side)+o*.008f;
                    if(side>0)kit.Quad(propMud,a,d,c,b,new Vector2(u0,1),new Vector2(u0,0),new Vector2(u1,0),new Vector2(u1,1));else kit.Quad(propMud,a,b,c,d,new Vector2(u0,1),new Vector2(u1,1),new Vector2(u1,0),new Vector2(u0,0));}
                for(int k=0;k<4;k++){var p=Canvas(.12f+k*.25f+R(-.05f,.05f),1,side)+new Vector3(0,.02f,side*.08f);float s=R(.09f,.14f);kit.Pebble(propPebble,p,new Vector3(s*1.3f,s*.6f,s),R(0,360),rng);}}
            kit.Quad(propFloor,new Vector3(-half+.1f,.03f,-width*.8f),new Vector3(-half+.1f,.03f,width*.8f),new Vector3(half-.05f,.03f,width*.8f),new Vector3(half-.05f,.03f,-width*.8f),new Vector2(0,0),new Vector2(0,1.6f),new Vector2(2.4f,1.6f),new Vector2(2.4f,0));
            kit.Build("Tent detail",root);
        }

        // Soft dark ellipses on the ground under props, so tents, racks, sacks and poles sit on the ground instead of
        // floating on it. Collected while the stage is built, emitted as one transparent mesh.
        static readonly List<(Vector3 at,float rx,float rz,float yaw,float alpha)> contactShadows=new List<(Vector3,float,float,float,float)>();
        static void ContactShadow(Vector3 at,float rx,float rz,float yaw=0,float alpha=.5f){contactShadows.Add((at,rx,rz,yaw,alpha));}
        static void ContactShadows(Transform parent)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var c=new List<Color>();var t=new List<int>();
            foreach(var s in contactShadows){var rot=Quaternion.Euler(0,s.yaw,0);var p=s.at;p.y=ScenicArt.Height(p)+.035f;int i=v.Count;
                // A 3x3 grid so the patch follows the ground's relief.
                for(int z=0;z<=2;z++)for(int x=0;x<=2;x++){var q=p+rot*new Vector3((x-1)*s.rx,0,(z-1)*s.rz);q.y=ScenicArt.Height(q)+.05f;v.Add(q);uv.Add(new Vector2(x*.5f,z*.5f));c.Add(new Color(0,0,0,Mathf.Min(1,s.alpha*1.7f)));}
                for(int z=0;z<2;z++)for(int x=0;x<2;x++){int a=i+z*3+x;t.AddRange(new[]{a,a+3,a+1,a+1,a+3,a+4});}}
            contactShadows.Clear();if(v.Count==0)return;
            var mesh=SceneArtResources.Own(new Mesh{name="Contact shadows"});mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetColors(c);mesh.SetTriangles(t,0);mesh.RecalculateBounds();
            var go=new GameObject("Contact shadows",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.SetParent(parent.parent,true);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=Fx.Mat("fx_soft",false,1);r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
            if(false)Debug.Log("SANGO3_CONTACT "+v.Count/9+" bounds "+r.bounds+" parent "+go.transform.parent.name+" mat "+r.sharedMaterial.shader.name+" q "+r.sharedMaterial.renderQueue);
        }
    }
}
