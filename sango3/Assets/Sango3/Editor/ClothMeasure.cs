using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace Sango3.Editor
{
    // Cloth fold measurement for the review: every move of every playable hero is baked at 1/30 s and, for each mesh
    // edge in the cloth regions (sleeves, hem/skirt, cape), the fold angle between its two triangles is compared with the
    // rest pose. A "hard fold" is an edge that folds more than 55 degrees beyond its rest angle, which shows up on screen as
    // a crease line. Results: QA/sample-130/cloth-folds.txt, plus a close render of each hero's worst frame.
    public static partial class GuanyuSample
    {
        const float HardFold=55f;
        public static void RenderDetailAll(){foreach(var id in BattleRules.Portraits){heroId=id;RenderDetail();}heroId="guanyu";}
        enum Region{None,Sleeve,Hem,Cape}
        public static void MeasureCloth()
        {
            var report=new StringBuilder();report.AppendLine("Hard fold = an edge folding more than "+HardFold+" degrees beyond its rest angle. Counts are per frame (average over all frames of all moves / worst frame).");
            Directory.CreateDirectory(Evidence+"cloth/");
            foreach(var id in BattleRules.Portraits){heroId=id;Load();
                var skin=root.GetComponent<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;var rest=mesh.vertices;var tris=mesh.triangles;
                // Weld by position so seams count as edges.
                var weld=new Dictionary<Vector3Int,int>();var map=new int[rest.Length];for(int i=0;i<rest.Length;i++){var k=Vector3Int.RoundToInt(rest[i]*10000);if(!weld.TryGetValue(k,out int g)){g=weld.Count;weld.Add(k,g);}map[i]=g;}
                var edges=new Dictionary<long,List<int>>();for(int t=0;t<tris.Length/3;t++)for(int e=0;e<3;e++){int a=map[tris[t*3+e]],b=map[tris[t*3+(e+1)%3]];if(a==b)continue;long key=a<b?(long)a<<32|(uint)b:(long)b<<32|(uint)a;if(!edges.TryGetValue(key,out var list))edges[key]=list=new List<int>();list.Add(t);}
                var pairs=new List<(int t0,int t1,Region r,Vector3 at)>();
                foreach(var kv in edges){if(kv.Value.Count!=2)continue;int a=(int)(kv.Key>>32);var tri=kv.Value[0];var p=(rest[tris[tri*3]]+rest[tris[tri*3+1]]+rest[tris[tri*3+2]])/3;var r=Classify(p);if(r!=Region.None)pairs.Add((kv.Value[0],kv.Value[1],r,p));}
                float[] restAngle=pairs.Select(q=>Angle(rest,tris,q.t0,q.t1)).ToArray();
                // Measured as the game runs it: the cloth follow-through is stepped at 1/30 s, and each hero is measured with the
                // shoulder correctives off and on.
                ClothFollow.Attach(root);var cloth=root.GetComponent<ClothFollow>();var corrective=root.GetComponent<ShoulderCorrective>();
                for(int pass=0;pass<2;pass++){if(corrective)corrective.active=pass==1;else if(pass==1)break;
                var where=new Dictionary<string,int>();var perMove=new Dictionary<string,(int n,int f)>();var sums=new Dictionary<Region,float>();var worst=new Dictionary<Region,int>();int frames=0;string worstAt="";int worstCount=-1;
                foreach(var move in HeroMoves.For(HeroIndex).All.Concat(new[]{new GuanyuMoves.Move{state="Idle",length=1.9f}})){
                    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(HeroFolder+move.state+".anim");if(!clip)continue;
                    for(float t=0;t<=move.length+1e-4f;t+=1/30f){clip.SampleAnimation(root,t);if(grip)grip.Solve();SolveDrape();if(cloth)cloth.Step(1/30f);
                        var baked=new Mesh();skin.BakeMesh(baked);var v=baked.vertices;Object.DestroyImmediate(baked);frames++;
                        var count=new Dictionary<Region,int>();int total=0;
                        for(int i=0;i<pairs.Count;i++){float d=Angle(v,tris,pairs[i].t0,pairs[i].t1)-restAngle[i];if(d>HardFold){count.TryGetValue(pairs[i].r,out int c);count[pairs[i].r]=c+1;total++;var at=pairs[i].at;string cell=pairs[i].r+" x"+(Mathf.Floor(Mathf.Abs(at.x)*5)/5).ToString("0.0")+" y"+(Mathf.Floor(at.y*5)/5).ToString("0.0")+(at.z<0?" back":" front");where.TryGetValue(cell,out int h);where[cell]=h+1;}}
                        foreach(var kv in count){sums.TryGetValue(kv.Key,out float s);sums[kv.Key]=s+kv.Value;worst.TryGetValue(kv.Key,out int w);if(kv.Value>w)worst[kv.Key]=kv.Value;}
                        perMove.TryGetValue(move.state,out var pm);perMove[move.state]=(pm.n+total,pm.f+1);if(total>worstCount){worstCount=total;worstAt=move.state+" t="+t.ToString("0.000");}
                    }
                }
                report.Append(id+(corrective?(pass==1?" correctives on":" correctives off"):"")+" ("+frames+" frames):");foreach(Region r in new[]{Region.Sleeve,Region.Hem,Region.Cape}){if(!pairs.Any(q=>q.r==r))continue;sums.TryGetValue(r,out float s);worst.TryGetValue(r,out int w);report.Append(" "+r+" "+(s/frames).ToString("0.0")+" / "+w+" of "+pairs.Count(q=>q.r==r)+" edges;");}
                report.AppendLine(" worst frame "+worstAt+" ("+worstCount+")");report.AppendLine("   per move: "+string.Join(", ",perMove.Select(kv=>kv.Key+" "+(kv.Value.n/(float)kv.Value.f).ToString("0"))));report.AppendLine("   most folds at: "+string.Join(", ",where.OrderByDescending(kv=>kv.Value).Take(8).Select(kv=>kv.Key+" "+(kv.Value/(float)frames).ToString("0.0"))));}
                Object.DestroyImmediate(root);
            }
            heroId="guanyu";File.WriteAllText(Evidence+"cloth-folds.txt",report.ToString());Debug.Log("SANGO3_CLOTH\n"+report);
        }
        // Rest-pose regions (hero space, 4.4 units tall): sleeves hang outside the shoulders, the hem/skirt below the belt, the
        // cape behind the back.
        static Region Classify(Vector3 p)
        {
            if(p.z<-.16f&&p.y>.6f&&p.y<3.05f&&Mathf.Abs(p.x)<.75f)return Region.Cape;
            if(Mathf.Abs(p.x)>.50f&&p.y>2.15f&&p.y<3.55f)return Region.Sleeve;
            if(p.y>.55f&&p.y<2.15f)return Region.Hem;
            return Region.None;
        }
        static float Angle(Vector3[] v,int[] tris,int a,int b)
        {
            var na=Vector3.Cross(v[tris[a*3+1]]-v[tris[a*3]],v[tris[a*3+2]]-v[tris[a*3]]);var nb=Vector3.Cross(v[tris[b*3+1]]-v[tris[b*3]],v[tris[b*3+2]]-v[tris[b*3]]);
            if(na.sqrMagnitude<1e-14f||nb.sqrMagnitude<1e-14f)return 0;return Vector3.Angle(na,nb);
        }
    }
}
