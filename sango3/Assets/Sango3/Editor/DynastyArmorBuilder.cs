using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Sango3.Editor
{
    // Authored profiles are in the source humanoid's metre-scale bind pose, before prefab normalization.
    public static class DynastyArmorBuilder
    {
        const string Folder="Assets/Sango3/Resources/Heroes/";
        public static void Dress(GameObject model,Animator animator,int hero)
        {
            bool scholar=hero==1,robe=scholar||hero==3;
            var bones=new[]{animator.GetBoneTransform(HumanBodyBones.Hips),animator.GetBoneTransform(HumanBodyBones.Spine),animator.GetBoneTransform(HumanBodyBones.Chest),animator.GetBoneTransform(HumanBodyBones.Head),animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg),animator.GetBoneTransform(HumanBodyBones.RightUpperLeg)};
            if(!bones[2])bones[2]=bones[1];
            var armor=Material(hero,"Lamellar",hero==4?new Color(.59f,.68f,.73f):new Color(.18f,.23f,.24f),.7f,true);
            var silk=Material(hero,"Silk",Color.Lerp(BattleRules.Colors[hero],Color.white,.12f),.05f,false);
            var trim=Material(hero,"Gilt",new Color(.64f,.43f,.15f),.72f,false);
            var dark=Material(hero,"Beard",new Color(.035f,.024f,.017f),0,false);
            foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())if(renderer.name.Contains("Belt_")||renderer.name.Contains("Pauldron"))renderer.gameObject.SetActive(false);
            Shell(model,bones,hero,"Cuirass",new[]{new Vector3(.24f,1.01f,.17f),new Vector3(.265f,1.18f,.18f),new Vector3(.29f,1.40f,.20f),new Vector3(.235f,1.49f,.17f)},robe?silk:armor,0,false);
            Shell(model,bones,hero,"War skirt",new[]{new Vector3(robe?.39f:.34f,robe?.28f:.57f,.245f),new Vector3(.33f,.78f,.23f),new Vector3(.255f,1.045f,.18f)},robe?silk:armor,1,true);
            Shell(model,bones,hero,"Brocade sash",new[]{new Vector3(.265f,.985f,.185f),new Vector3(.267f,1.075f,.187f)},trim,1,false);
            var head=model.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name=="SuperHero_Male");var vertices=head.sharedMesh.vertices;float top=head.sharedMesh.triangles.Max(i=>head.transform.TransformPoint(vertices[i]).y);
            var crown=scholar?new[]{new Vector3(.11f,top-.07f,.12f),new Vector3(.12f,top+.15f,.105f),new Vector3(.075f,top+.23f,.09f)}:
                hero==0?new[]{new Vector3(.145f,top-.12f,.145f),new Vector3(.14f,top+.015f,.14f),new Vector3(.035f,top+.09f,.035f)}:
                new[]{new Vector3(.15f,top-.12f,.15f),new Vector3(.14f,top+.025f,.135f),new Vector3(.04f,top+.15f,.045f)};
            Shell(model,bones,hero,"Crown",crown,scholar||hero==0?silk:armor,3,false);
            Shell(model,bones,hero,"Crown rim",new[]{new Vector3(.152f,top-.12f,.152f),new Vector3(.153f,top-.085f,.153f)},trim,3,false);
            if(hero==0||hero==2||hero==5){
                foreach(var t in model.GetComponentsInChildren<Transform>())if(t.name=="Hair_Beard")t.gameObject.SetActive(false);
                Beard(model,bones,hero,top,dark);
            }
            Cloak(model,bones,hero,silk);
        }
        static Material Material(int hero,string name,Color color,float metal,bool plates)
        {
            string path=Folder+hero+"Dynasty"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.color=color;mat.SetFloat("_Metallic",metal);mat.SetFloat("_Smoothness",plates?.46f:.29f);mat.SetFloat("_Cull",0);
            if(plates){var pathTex=Folder+"LamellarDetail.asset";var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(pathTex);if(!tex){tex=new Texture2D(128,128,TextureFormat.RGBA32,true);tex.wrapMode=TextureWrapMode.Repeat;var pixels=new Color[128*128];for(int y=0;y<128;y++)for(int x=0;x<128;x++){int row=y/32,xx=(x+(row%2)*16)%32,yy=y%32;float rim=xx<2||xx>29||yy<2||yy>28?.5f:1;float rivet=(Mathf.Pow(xx-6,2)+Mathf.Pow(yy-6,2)<5||Mathf.Pow(xx-26,2)+Mathf.Pow(yy-6,2)<5)?1.7f:1;float grain=.94f+Mathf.PerlinNoise(x*.8f,y*.8f)*.12f;pixels[y*128+x]=Color.white*rim*rivet*grain;}tex.SetPixels(pixels);tex.Apply();AssetDatabase.CreateAsset(tex,pathTex);}mat.mainTexture=tex;mat.mainTextureScale=new Vector2(5,2);}
            EditorUtility.SetDirty(mat);return mat;
        }
        static void Shell(GameObject model,Transform[] bones,int hero,string name,Vector3[] profile,Material material,int binding,bool skirt)
        {
            const int segments=64,steps=16;var v=new List<Vector3>();var uv=new List<Vector2>();var weights=new List<BoneWeight>();var tris=new List<int>();
            for(int row=0;row<=steps;row++){
                float t=row/(float)steps,p=t*(profile.Length-1);int k=Mathf.Min(profile.Length-2,(int)p);var shape=Vector3.Lerp(profile[k],profile[k+1],p-k);
                for(int col=0;col<=segments;col++){
                    float a=col/(float)segments*Mathf.PI*2,fold=skirt?Mathf.Sin(a*16)*.012f:0;float x=Mathf.Cos(a)*(shape.x+fold),z=Mathf.Sin(a)*(shape.z+fold);
                    v.Add(new Vector3(x,shape.y,z));uv.Add(new Vector2(col/(float)segments,t));
                    BoneWeight weight;if(binding==0){float blend=Mathf.InverseLerp(1.08f,1.37f,shape.y);weight=new BoneWeight{boneIndex0=1,weight0=1-blend,boneIndex1=2,weight1=blend};}
                    else if(skirt){float leg=Mathf.Clamp01((.9f-shape.y)*.65f);weight=new BoneWeight{boneIndex0=0,weight0=1-leg,boneIndex1=x>0?4:5,weight1=leg};}
                    else weight=new BoneWeight{boneIndex0=binding==1?0:binding,weight0=1};weights.Add(weight);
                    if(row>0&&col>0){int n=v.Count-1;tris.AddRange(new[]{n,n-1,n-segments-1,n-1,n-segments-2,n-segments-1});}
                }
            }
            SaveSkin(model,bones,hero,name,v,uv,tris,weights,material);
        }
        static void Beard(GameObject model,Transform[] bones,int hero,float top,Material material)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();var w=new List<BoneWeight>();
            for(int y=0;y<=12;y++)for(int x=0;x<=12;x++){
                float f=y/12f,width=Mathf.Lerp(.105f,.018f,f),xx=(x/12f*2-1)*width;
                v.Add(new Vector3(xx,top-.22f-f*(hero==0?.35f:.18f),-.125f-Mathf.Sin(f*Mathf.PI)*.055f+Mathf.Abs(xx)*.22f+Mathf.Sin(x*2.5f)*.004f));uv.Add(new Vector2(x/12f,f));w.Add(new BoneWeight{boneIndex0=3,weight0=1});if(x>0&&y>0){int n=v.Count-1;t.AddRange(new[]{n,n-1,n-13,n-1,n-14,n-13});}
            }
            SaveSkin(model,bones,hero,"Long beard",v,uv,t,w,material);
        }
        static void Cloak(GameObject model,Transform[] bones,int hero,Material material)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();var w=new List<BoneWeight>();
            for(int y=0;y<=16;y++)for(int x=0;x<=24;x++){
                float f=y/16f,xx=x/24f;v.Add(new Vector3((xx*2-1)*Mathf.Lerp(.23f,.39f,f),1.48f-f*(hero==1?1.15f:1.05f),.29f+f*.22f+Mathf.Sin(xx*Mathf.PI*10)*.025f*f));uv.Add(new Vector2(xx,f));float hip=Mathf.Min(.65f,f*.65f);w.Add(new BoneWeight{boneIndex0=2,weight0=1-hip,boneIndex1=0,weight1=hip});if(x>0&&y>0){int n=v.Count-1;t.AddRange(new[]{n,n-1,n-25,n-1,n-26,n-25});}
            }
            SaveSkin(model,bones,hero,"Silk mantle",v,uv,t,w,material);
        }
        static void SaveSkin(GameObject model,Transform[] bones,int hero,string name,List<Vector3> v,List<Vector2> uv,List<int> triangles,List<BoneWeight> weights,Material material)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.boneWeights=weights.ToArray();mesh.bindposes=bones.Select(b=>b.worldToLocalMatrix*model.transform.localToWorldMatrix).ToArray();mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            string path=Folder+hero+name.Replace(" ","")+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;}else AssetDatabase.CreateAsset(mesh,path);
            var go=new GameObject(name,typeof(SkinnedMeshRenderer));go.transform.SetParent(model.transform,false);var skin=go.GetComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.bones=bones;skin.rootBone=bones[0];skin.sharedMaterial=material;skin.localBounds=new Bounds(Vector3.up,Vector3.one*4);
        }
    }
}
