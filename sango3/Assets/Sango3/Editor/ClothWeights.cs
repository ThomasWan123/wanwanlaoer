using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Sango3.Editor
{
    // Hard cloth creases come from skin weights that change from one bone to the next within one or two rows of
    // vertices (chest to upper arm, upper arm to forearm, belt to thigh, back to cape). This pass spreads those changes
    // over a wider band, but only inside the cloth regions: hands, head and the leg/arm cores keep their weights, so
    // grips and the baked moves are unchanged. RecoveredHeroBuilder calls Soften after its own weight smoothing;
    // SoftenExisting applies it once to the rigs already built (backing them up first).
    public static class ClothWeights
    {
        public static float Width=.25f;public static int LastPasses;
        public static void Soften(Vector3[] vertices,int[] triangles,BoneWeight[] weights,int boneCount)
        {
            var groups=new Dictionary<Vector3Int,int>();var map=new int[vertices.Length];
            for(int i=0;i<vertices.Length;i++){var key=Vector3Int.RoundToInt(vertices[i]*10000);if(!groups.TryGetValue(key,out int g)){g=groups.Count;groups.Add(key,g);}map[i]=g;}
            int count=groups.Count;var adjacency=Enumerable.Range(0,count).Select(_=>new HashSet<int>()).ToArray();var data=new float[count,boneCount];var position=new Vector3[count];var samples=new int[count];
            for(int i=0;i<vertices.Length;i++){int g=map[i];var w=weights[i];data[g,w.boneIndex0]+=w.weight0;data[g,w.boneIndex1]+=w.weight1;data[g,w.boneIndex2]+=w.weight2;data[g,w.boneIndex3]+=w.weight3;samples[g]++;position[g]=vertices[i];}
            for(int g=0;g<count;g++)for(int k=0;k<boneCount;k++)data[g,k]/=samples[g];
            for(int i=0;i<triangles.Length;i+=3)for(int e=0;e<3;e++){int a=map[triangles[i+e]],b=map[triangles[i+(e+1)%3]];if(a==b)continue;adjacency[a].Add(b);adjacency[b].Add(a);}
            // How much each vertex may be smoothed: 1 in the cloth regions, fading to 0 at hands, head and feet.
            var amount=new float[count];
            for(int g=0;g<count;g++){var p=position[g];float hands=data[g,7]+data[g,10],head=data[g,4],feet=data[g,13]+data[g,16];
                float cloth=Mathf.Max(Sleeve(p),Mathf.Max(Hem(p),Cape(p)));amount[g]=cloth*(1-Mathf.Clamp01(hands*2))*(1-Mathf.Clamp01(head*2))*(1-Mathf.Clamp01(feet*2));}
            // Diffusion spreads about one edge length per sqrt(pass), so the pass count follows the mesh density: the softened band
            // is ~0.25 units wide on Guan Yu's dense mesh and on the lighter generals alike.
            double edgeSum=0;int edgeCount=0;for(int g=0;g<count;g++)foreach(int n in adjacency[g]){edgeSum+=Vector3.Distance(position[g],position[n]);edgeCount++;}
            float edge=(float)(edgeSum/Mathf.Max(1,edgeCount));int passes=Mathf.Clamp(Mathf.RoundToInt(Mathf.Pow(Width/edge,2)),20,140);LastPasses=passes;
            for(int pass=0;pass<passes;pass++){var next=(float[,])data.Clone();
                for(int g=0;g<count;g++){float a=amount[g]*.5f;if(a<=0||adjacency[g].Count==0)continue;
                    for(int k=0;k<boneCount;k++){float sum=0;foreach(int n in adjacency[g])sum+=data[n,k];next[g,k]=data[g,k]*(1-a)+sum/adjacency[g].Count*a;}}
                data=next;}
            for(int i=0;i<vertices.Length;i++){int g=map[i];if(amount[g]<=0)continue;var top=Enumerable.Range(0,boneCount).OrderByDescending(k=>data[g,k]).Take(4).ToArray();float sum=top.Sum(k=>data[g,k]);
                weights[i]=new BoneWeight{boneIndex0=top[0],boneIndex1=top[1],boneIndex2=top[2],boneIndex3=top[3],weight0=data[g,top[0]]/sum,weight1=data[g,top[1]]/sum,weight2=data[g,top[2]]/sum,weight3=data[g,top[3]]/sum};}
        }
        // Regions in rest-pose hero space (4.4 units tall), feathered so the softened band has no edge of its own.
        static float Sleeve(Vector3 p)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(.30f,.50f,Mathf.Abs(p.x)))*Band(p.y,2.0f,2.2f,3.55f,3.75f);
        static float Hem(Vector3 p)=>Band(p.y,.45f,.70f,2.10f,2.35f);
        static float Cape(Vector3 p)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.08f,-.18f,p.z))*Band(p.y,.5f,.7f,3.0f,3.25f)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.7f,.85f,Mathf.Abs(p.x))));
        static float Band(float y,float a,float b,float c,float d)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,y))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(c,d,y)));

        public static void SoftenExisting()
        {
            string backup="QA/backup-0.19.0-skins/";Directory.CreateDirectory(backup);
            foreach(var id in BattleRules.Portraits){
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sango3/Resources/Heroes/Final/"+id+".prefab");var skin=prefab.GetComponent<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;var path=AssetDatabase.GetAssetPath(mesh);
                if(!File.Exists(backup+id+"-"+Path.GetFileName(path)))throw new System.Exception("missing backup");
                var original=new Mesh();{var tmp=Path.GetFullPath(backup+id+"-"+Path.GetFileName(path));File.Copy(tmp,"Assets/Sango3/Editor/_restore.asset",true);AssetDatabase.ImportAsset("Assets/Sango3/Editor/_restore.asset");var src=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Sango3/Editor/_restore.asset");mesh.boneWeights=src.boneWeights;AssetDatabase.DeleteAsset("Assets/Sango3/Editor/_restore.asset");}
                var weights=mesh.boneWeights;Soften(mesh.vertices,mesh.triangles,weights,skin.bones.Length);mesh.boneWeights=weights;EditorUtility.SetDirty(mesh);
                Debug.Log("SANGO3_SOFTENED "+id+" "+path+" passes "+LastPasses);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
