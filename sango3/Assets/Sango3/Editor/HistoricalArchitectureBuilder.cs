using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Sango3.Editor
{
    // Original game meshes informed by Han architectural models; not archaeological reconstructions.
    public static class HistoricalArchitectureBuilder
    {
        const string Folder="Assets/Sango3/Resources/Architecture/";
        static Material wood,plaster,earth,tile,stone,cloth,iron;
        public static void Build(){
            Directory.CreateDirectory(Folder);Textures();AssetDatabase.Refresh();
            wood=Mat("Timber",new Color(.53f,.39f,.25f),"Timber",.21f);
            plaster=Mat("Lime render",new Color(.65f,.59f,.45f),"Earth",.10f);
            earth=Mat("Rammed earth",new Color(.48f,.38f,.26f),"Earth",.08f);
            tile=Mat("Fired roof tiles",new Color(.23f,.26f,.25f),"Earth",.24f);
            stone=Mat("Stone footing",new Color(.50f,.51f,.47f),"Earth",.15f);
            cloth=Mat("Undyed linen",new Color(.69f,.62f,.44f),"Linen",.05f);
            cloth.SetFloat("_Cull",0);iron=Mat("Forged fittings",new Color(.16f,.18f,.17f),"Earth",.35f);iron.SetFloat("_Metallic",.55f);
            Bake("House",r=>House(r,false));Bake("Granary",r=>House(r,true));Bake("Watchtower",Watchtower);Bake("Gate",Gate);Bake("Palisade",Palisade);Bake("Tent",Tent);Bake("Riverboat",Boat);AssetDatabase.SaveAssets();Debug.Log("HISTORICAL_ARCHITECTURE_READY");
        }
        static void Textures(){
            const int size=512;
            foreach(string name in new[]{"Timber","Earth","Linen"}){
                var pixels=new Color[size*size];var heights=new float[size*size];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++){
                    float noise=Mathf.PerlinNoise(x*.07f+41,y*.07f+12),grain=Mathf.PerlinNoise(x*.7f+3,y*.7f+9);
                    float h=name=="Timber"?.55f+.12f*Mathf.Sin(x*.42f+Mathf.PerlinNoise(x*.018f,y*.025f)*7)+.12f*Mathf.PerlinNoise(x*.12f,y*.009f):name=="Linen"?.55f+(x%4<2?.045f:-.045f)+(y%4<2?.045f:-.045f):.52f+noise*.18f+grain*.09f;
                    if(name=="Timber"&&x%128<3)h*=.45f;if(name=="Earth"&&y%96<2)h*=.88f;
                    heights[y*size+x]=h;pixels[y*size+x]=new Color(h,h,h,1);
                }
                WriteTexture(name,pixels,size,false);var normal=new Color[pixels.Length];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++){float dx=(heights[y*size+(x+1)%size]-heights[y*size+(x+size-1)%size])*1.4f;float dy=(heights[((y+1)%size)*size+x]-heights[((y+size-1)%size)*size+x])*1.4f;var n=new Vector3(-dx,-dy,1).normalized;normal[y*size+x]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);}
                WriteTexture(name+"Normal",normal,size,true);
            }
        }
        static void WriteTexture(string name,Color[] pixels,int size,bool normal){var texture=new Texture2D(size,size,TextureFormat.RGB24,false);texture.SetPixels(pixels);texture.Apply();string path=Folder+name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.wrapMode=TextureWrapMode.Repeat;importer.maxTextureSize=512;importer.SaveAndReimport();}
        static Material Mat(string name,Color color,string texture,float smooth){string path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+texture+".png");m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+texture+"Normal.png"));m.SetFloat("_BumpScale",.45f);m.EnableKeyword("_NORMALMAP");m.SetFloat("_Smoothness",smooth);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;}
        static Transform Root(string name,Transform parent,Vector3 p){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;return t;}
        static GameObject Box(string name,Vector3 p,Vector3 size,Material mat,Transform root){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
        static void Beam(Vector3 a,Vector3 b,float width,Material mat,Transform root){var go=Box("Mortised timber",(a+b)*.5f,new Vector3(width,(b-a).magnitude,width),mat,root);go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
        static void Surface(string name,List<Vector3> v,List<int> triangles,List<Vector2> uv,Material mat,Transform root){var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateTangents();var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=mat;}
        static void Roof(Transform root,float width,float depth,float baseY,float rise){
            int cols=Mathf.CeilToInt(width/.18f),rows=8;var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();
            for(int side=-1;side<=1;side+=2){int offset=v.Count;
                for(int row=0;row<=rows;row++)for(int col=0;col<=cols*4;col++){
                    float t=row/(float)rows,x=-width*.5f+col/(cols*4f)*width,z=side*t*depth*.5f;
                    float y=baseY+rise*(1-t)+.12f*Mathf.Pow(t,4)+Mathf.Sin(col*Mathf.PI*.5f)*.025f;
                    v.Add(new Vector3(x,y,z));uv.Add(new Vector2(col/(cols*4f)*4,t*2));
                    if(row>0&&col>0){int a=offset+row*(cols*4+1)+col,b=a-1,c=a-(cols*4+1),d=c-1;if(side==1)tr.AddRange(new[]{a,c,b,b,c,d});else tr.AddRange(new[]{a,b,c,b,d,c});}
                }
                Beam(new Vector3(-width*.5f,baseY+.10f,side*depth*.5f),new Vector3(width*.5f,baseY+.10f,side*depth*.5f),.12f,wood,root);
                for(int col=0;col<=cols;col++)Box("Tile drip end",new Vector3(-width*.5f+col/(float)cols*width,baseY+.10f,side*(depth*.5f+.035f)),new Vector3(.115f,.08f,.075f),tile,root);
            }
            Surface("Continuous tile roof",v,tr,uv,tile,root);Beam(new Vector3(-width*.5f-.08f,baseY+rise+.06f,0),new Vector3(width*.5f+.08f,baseY+rise+.06f,0),.14f,tile,root);
        }
        static void Rail(Transform root,Vector3 a,Vector3 b){Beam(a+Vector3.up*.6f,b+Vector3.up*.6f,.10f,wood,root);Beam(a+Vector3.up*.12f,b+Vector3.up*.12f,.08f,wood,root);int n=Mathf.CeilToInt(Vector3.Distance(a,b)/.38f);for(int i=0;i<=n;i++){var p=Vector3.Lerp(a,b,i/(float)n);Beam(p,p+Vector3.up*.69f,.085f,wood,root);}}
        static void Brackets(Transform root,Vector3 p){Box("Bracket block",p,new Vector3(.22f,.16f,.22f),wood,root);Box("Transverse bearing",p+Vector3.up*.17f,new Vector3(.67f,.13f,.23f),wood,root);Box("Eave arm",p+Vector3.up*.28f,new Vector3(.21f,.12f,.8f),wood,root);}
        static void House(Transform root,bool raised){
            float floor=raised?.75f:.2f;Box("Stone platform",new Vector3(0,.12f,0),new Vector3(3.7f,.24f,2.9f),stone,root);
            if(raised)for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Box("Granary footing",new Vector3(x*1.2f,.4f,z*.85f),new Vector3(.3f,.8f,.3f),stone,root);
            Box("Plastered wall",new Vector3(0,floor+.9f,0),new Vector3(3.2f,1.8f,2.35f),raised?earth:plaster,root);
            for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z+=2){Beam(new Vector3(x*1.48f,floor,z*1.2f),new Vector3(x*1.48f,floor+2.02f,z*1.2f),.16f,wood,root);Brackets(root,new Vector3(x*1.48f,floor+1.88f,z*1.2f));}
            for(int side=-1;side<=1;side+=2)Beam(new Vector3(-1.65f,floor+1.7f,side*1.2f),new Vector3(1.65f,floor+1.7f,side*1.2f),.15f,wood,root);
            Box("Recessed doorway",new Vector3(0,floor+.65f,-1.19f),new Vector3(.72f,1.3f,.07f),wood,root);
            for(int i=0;i<7;i++)Box("Door plank",new Vector3(-.31f+i*.103f,floor+.65f,-1.24f),new Vector3(.09f,1.26f,.035f),wood,root);
            for(int side=-1;side<=1;side+=2){Box("Window recess",new Vector3(side*.98f,floor+1.14f,-1.2f),new Vector3(.57f,.57f,.04f),iron,root);for(int n=-2;n<=2;n++)Box("Window lattice",new Vector3(side*.98f+n*.10f,floor+1.14f,-1.24f),new Vector3(.035f,.57f,.04f),wood,root);}
            Roof(root,3.9f,3.15f,floor+2.1f,.85f);
        }
        static void Watchtower(Transform root){
            Box("Earth podium",new Vector3(0,.55f,0),new Vector3(3.2f,1.1f,3.2f),earth,root);
            for(int level=0;level<2;level++){float y=1.05f+level*2.15f;
                Box("Timber storey",new Vector3(0,y+.64f,0),new Vector3(2.05f,1.3f,2.05f),plaster,root);Box("Cantilever platform",new Vector3(0,y-.04f,0),new Vector3(3.3f,.18f,3.3f),wood,root);
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2){Beam(new Vector3(x*1.15f,y,z*1.15f),new Vector3(x*1.15f,y+1.56f,z*1.15f),.19f,wood,root);Brackets(root,new Vector3(x*1.15f,y+1.47f,z*1.15f));}
                Rail(root,new Vector3(-1.48f,y,-1.48f),new Vector3(1.48f,y,-1.48f));Rail(root,new Vector3(-1.48f,y,1.48f),new Vector3(1.48f,y,1.48f));Rail(root,new Vector3(-1.48f,y,-1.48f),new Vector3(-1.48f,y,1.48f));Rail(root,new Vector3(1.48f,y,-1.48f),new Vector3(1.48f,y,1.48f));
                for(int side=-1;side<=1;side+=2){Box("Watch window",new Vector3(side*.54f,y+.95f,-1.04f),new Vector3(.5f,.55f,.05f),wood,root);}
                Roof(root,3.7f,3.7f,y+1.85f,.73f);
            }
        }
        static void Gate(Transform root){
            for(int side=-1;side<=1;side+=2){var tower=Root("Flanking watchtower",root,new Vector3(side*3.15f,0,0));Watchtower(tower);}
            Beam(new Vector3(-1.65f,2.7f,0),new Vector3(1.65f,2.7f,0),.4f,wood,root);Roof(root,3.8f,2.5f,2.85f,.65f);
            for(int side=-1;side<=1;side+=2){var door=Root("Open gate leaf",root,new Vector3(side*1.55f,0,0));door.localRotation=Quaternion.Euler(0,side*68,0);for(int p=0;p<6;p++)Box("Gate plank",new Vector3(side*(p*.21f+.1f),1.2f,0),new Vector3(.20f,2.4f,.13f),wood,door);for(float y=.5f;y<=2;y+=.7f)Box("Iron gate strap",new Vector3(side*.63f,y,-.085f),new Vector3(1.27f,.07f,.04f),iron,door);}
        }
        static void Palisade(Transform root){for(int i=0;i<11;i++){float x=-1.5f+i*.3f;Box("Sharpened stake",new Vector3(x,1,0),new Vector3(.21f,2,.22f),wood,root);Beam(new Vector3(x,0,.6f),new Vector3(x,1.3f,0),.13f,wood,root);}Beam(new Vector3(-1.65f,.6f,-.13f),new Vector3(1.65f,.6f,-.13f),.16f,wood,root);Beam(new Vector3(-1.65f,1.4f,-.13f),new Vector3(1.65f,1.4f,-.13f),.16f,wood,root);}
        // Faces keep their own vertices and wind outward; shared, inward-wound corners rendered the canvas black.
        static void Tent(Transform root){
            var a=new Vector3(-1.5f,0,-1.2f);var b=new Vector3(0,1.9f,-1.2f);var c=new Vector3(0,1.9f,1.2f);var d=new Vector3(-1.5f,0,1.2f);var e=new Vector3(1.5f,0,-1.2f);var f=new Vector3(1.5f,0,1.2f);
            var v=new List<Vector3>{a,d,b,b,d,c, e,b,f,b,c,f, a,b,e, d,f,c};var t=Enumerable.Range(0,v.Count).ToList();
            var uv=v.Select(p=>Mathf.Abs(p.z)>1.19f&&(p.y<.01f||p.x==0)?new Vector2(p.x*.5f+.5f,p.y*.5f):new Vector2(p.z*.5f+.5f,Vector2.Distance(new Vector2(p.x,p.y),Vector2.zero)*.4f)).ToList();
            Surface("Pitched linen tent",v,t,uv,cloth,root);Beam(new Vector3(0,0,-1.3f),new Vector3(0,2.02f,-1.3f),.1f,wood,root);for(int side=-1;side<=1;side+=2)Beam(new Vector3(0,1.9f,side*1.3f),new Vector3(0,.06f,side*2),.025f,wood,root);}
        public static void RebuildTent(){wood=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Timber.mat");cloth=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Undyed linen.mat");Bake("Tent",Tent);AssetDatabase.SaveAssets();Debug.Log("TENT_REBUILT");}
        static void Boat(Transform root){
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();const int sections=12;
            for(int i=0;i<=sections;i++){float x=-2.4f+i*4.8f/sections;float w=.85f*Mathf.Pow(Mathf.Sin(i*Mathf.PI/sections),.55f)+.06f;float end=Mathf.Pow(Mathf.Abs(x)/2.4f,4)*.5f;foreach(var p in new[]{new Vector3(x,.3f+end,-w),new Vector3(x,-.35f,-w*.45f),new Vector3(x,-.35f,w*.45f),new Vector3(x,.3f+end,w)}){v.Add(p);uv.Add(new Vector2(i*.4f,v.Count%4*.4f));}if(i>0)for(int n=0;n<3;n++){int a=i*4+n,b=a-4;t.AddRange(new[]{a,a+1,b,b,a+1,b+1});}}
            Surface("Planked curved hull",v,t,uv,wood,root);for(int i=0;i<19;i++){float x=-2.1f+i*.23f;float w=.8f*Mathf.Pow(Mathf.Sin((x+2.4f)*Mathf.PI/4.8f),.55f);Box("Deck plank",new Vector3(x,.16f,0),new Vector3(.21f,.09f,w*2),wood,root);}
            Beam(new Vector3(0,.2f,0),new Vector3(0,4.1f,0),.12f,wood,root);
            var sail=new List<Vector3>();var sailUv=new List<Vector2>();var sailTri=new List<int>();for(int y=0;y<=6;y++)for(int x=0;x<=8;x++){float u=x/8f,f=y/6f;sail.Add(new Vector3((u-.5f)*2.5f,1.5f+f*2.2f,Mathf.Sin(u*Mathf.PI)*Mathf.Sin(f*Mathf.PI)*.33f));sailUv.Add(new Vector2(u,f));if(y>0&&x>0){int a=y*9+x;sailTri.AddRange(new[]{a,a-9,a-1,a-1,a-9,a-10});}}
            Surface("Bowed woven sail",sail,sailTri,sailUv,cloth,root);Beam(new Vector3(-1.35f,3.7f,0),new Vector3(1.35f,3.7f,0),.055f,wood,root);Beam(new Vector3(-1.35f,1.5f,0),new Vector3(1.35f,1.5f,0),.055f,wood,root);
            for(int side=-1;side<=1;side+=2)Beam(new Vector3(0,4,0),new Vector3(side*2,.3f,0),.022f,cloth,root);
        }
        static void Bake(string name,Action<Transform> build){var root=new GameObject(name);build(root.transform);var filters=root.GetComponentsInChildren<MeshFilter>();var groups=filters.GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial).ToArray();int index=0;foreach(var group in groups){var mesh=new Mesh{name=name+" "+group.Key.name};mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.transform.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());string path=Folder+name+"-"+(index++)+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);var child=new GameObject(group.Key.name,typeof(MeshFilter),typeof(MeshRenderer));child.transform.SetParent(root.transform,false);child.GetComponent<MeshFilter>().sharedMesh=mesh;child.GetComponent<MeshRenderer>().sharedMaterial=group.Key;}
            foreach(var f in filters)if(f){var mesh=f.sharedMesh;if(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh)))Object.DestroyImmediate(mesh);Object.DestroyImmediate(f.gameObject);}foreach(var tr in root.GetComponentsInChildren<Transform>().Reverse())if(tr!=root.transform&&tr.childCount==0&&!tr.GetComponent<Renderer>())Object.DestroyImmediate(tr.gameObject);
            PrefabUtility.SaveAsPrefabAsset(root,Folder+name+".prefab");Object.DestroyImmediate(root);
        }
    }
}
