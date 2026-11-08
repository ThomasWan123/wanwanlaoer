using System.Collections.Generic;
using UnityEngine;
namespace Sango3.Editor
{
    // Baked into prefabs; no mesh generation during deployment.
    public static class RecoveredWeaponBuilder
    {
        static readonly Color Steel=new Color(.64f,.72f,.76f),Gold=new Color(.57f,.37f,.13f),Wood=new Color(.12f,.065f,.035f),Silk=new Color(.48f,.035f,.025f);
        public static Transform Build(Transform hand,string name)
        {
            var mount=new GameObject("Weapon mount").transform;mount.SetParent(hand,false);
            var shaft=new GameObject("Spear").transform;shaft.SetParent(mount,false);
            bool fan=name=="诸葛亮",sword=name=="刘备"||name=="曹操";
            if(fan){
                Part("Fan handle",PrimitiveType.Cylinder,new Vector3(0,.1f,0),new Vector3(.045f,.19f,.045f),Wood,shaft);
                for(int i=0;i<11;i++){var feather=new GameObject("Layered feather").transform;feather.SetParent(shaft,false);feather.localRotation=Quaternion.Euler(0,0,(i-5)*-8);feather.localPosition=new Vector3(0,.18f,0);Blade("Feather vane",new[]{new Vector2(-.025f,0),new Vector2(-.055f,.30f),new Vector2(-.035f,.49f),new Vector2(0,.56f),new Vector2(.035f,.49f),new Vector2(.055f,.30f),new Vector2(.025f,0)},.009f,feather,new Color(.84f,.81f,.67f));Part("Feather rib",PrimitiveType.Cylinder,new Vector3(0,.25f,-.008f),new Vector3(.008f,.25f,.008f),Gold,feather);}
                return shaft;
            }
            Part("Lacquered shaft",PrimitiveType.Cylinder,new Vector3(0,sword?0:.12f,0),new Vector3(.044f,sword?.20f:1.08f,.044f),Wood,shaft);
            for(int i=0;i<12;i++)Part("Braided grip binding",PrimitiveType.Cylinder,new Vector3(0,-.17f+i*.028f,0),new Vector3(.052f,.006f,.052f),i%3==0?Gold:new Color(.26f,.12f,.065f),shaft);
            if(sword){
                Blade("Ridged jian",new[]{new Vector2(-.052f,.22f),new Vector2(-.05f,.92f),new Vector2(0,1.15f),new Vector2(.05f,.92f),new Vector2(.052f,.22f)},.025f,shaft,Steel);
                Part("Winged bronze guard",PrimitiveType.Cube,new Vector3(0,.19f,0),new Vector3(.28f,.07f,.10f),Gold,shaft);
                for(int side=-1;side<=1;side+=2){var wing=Part("Swept quillon",PrimitiveType.Cube,new Vector3(side*.13f,.21f,0),new Vector3(.1f,.045f,.07f),Gold,shaft);wing.transform.localRotation=Quaternion.Euler(0,0,side*25);}
            }else{
                for(int i=0;i<3;i++)Part("Engraved socket ring",PrimitiveType.Cylinder,new Vector3(0,.92f+i*.065f,0),new Vector3(.075f,.018f,.075f),Gold,shaft);
                if(name=="关羽")Blade("Bevelled crescent blade",new[]{new Vector2(-.032f,.98f),new Vector2(-.04f,1.52f),new Vector2(.02f,1.80f),new Vector2(.15f,1.69f),new Vector2(.31f,1.49f),new Vector2(.38f,1.29f),new Vector2(.31f,1.10f),new Vector2(.16f,.99f)},.045f,shaft,Steel);
                else if(name=="张飞")Blade("Serpent spear blade",new[]{new Vector2(-.045f,1.06f),new Vector2(-.09f,1.23f),new Vector2(.015f,1.43f),new Vector2(-.03f,1.62f),new Vector2(.035f,1.79f),new Vector2(.085f,1.58f),new Vector2(.07f,1.43f),new Vector2(-.015f,1.24f),new Vector2(.04f,1.06f)},.035f,shaft,Steel);
                else Blade("Diamond spear point",new[]{new Vector2(-.065f,1.08f),new Vector2(-.095f,1.25f),new Vector2(0,1.75f),new Vector2(.095f,1.25f),new Vector2(.065f,1.08f)},.04f,shaft,Steel);
                for(int i=0;i<7;i++){float a=i*Mathf.PI*2/7;var tassel=Part("Silk tassel strand",PrimitiveType.Capsule,new Vector3(Mathf.Cos(a)*.09f,.89f,Mathf.Sin(a)*.09f),new Vector3(.025f,.16f,.025f),Silk,shaft);tassel.transform.localRotation=Quaternion.Euler(Mathf.Cos(a)*17,0,Mathf.Sin(a)*17);}
            }
            Part("Bronze pommel",PrimitiveType.Sphere,new Vector3(0,sword?-.22f:-.98f,0),new Vector3(.075f,.11f,.075f),Gold,shaft);
            return shaft;
        }
        static Material Surface(Color color,float metal,float smooth){var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);m.enableInstancing=true;return m;}
        static GameObject Part(string name,PrimitiveType type,Vector3 p,Vector3 scale,Color color,Transform parent){var go=WorldArt.Shape(name,type,p,scale,color,parent);go.GetComponent<Renderer>().sharedMaterial=Surface(color,color==Steel?.8f:color==Gold?.65f:.05f,color==Steel?.64f:.32f);return go;}
        static void Blade(string name,Vector2[] outline,float depth,Transform parent,Color color)
        {
            // A raised centre on each face gives the blade a genuine ridge and bevel highlights.
            var v=new List<Vector3>();var t=new List<int>();Vector2 center=Vector2.zero;foreach(var p in outline)center+=p;center/=outline.Length;int n=outline.Length;
            for(int side=0;side<2;side++){
                float sign=side==0?1:-1;int offset=v.Count;
                for(int i=0;i<n;i++){var p=outline[i];v.Add(new Vector3(p.x,p.y,0));}
                v.Add(new Vector3(center.x,center.y,depth*sign));
                for(int i=0;i<n;i++){if(side==0)t.AddRange(new[]{offset+i,offset+(i+1)%n,offset+n});else t.AddRange(new[]{offset+(i+1)%n,offset+i,offset+n});}
            }
            float area=0;for(int i=0;i<n;i++)area+=outline[i].x*outline[(i+1)%n].y-outline[(i+1)%n].x*outline[i].y;if(area<0)for(int i=0;i<t.Count;i+=3){int swap=t[i];t[i]=t[i+1];t[i+1]=swap;}
            // Split ridge facets so their normals retain a real machined edge.
            var facets=new List<Vector3>();var indices=new List<int>();var uv=new List<Vector2>();for(int i=0;i<t.Count;i++){var p=v[t[i]];facets.Add(p);indices.Add(i);uv.Add(new Vector2(p.x,p.y));}
            var mesh=new Mesh{name=name};mesh.SetVertices(facets);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=Surface(color,name=="Feather vane"?0:.85f,name=="Feather vane"?.18f:.65f);
        }
    }
}
