using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace Sango3.Editor
{
    // Blender sleeve rebuild trial (0.26, one round, approved by Thomas): Liu Bei's sword-side (right) sleeve with a
    // denser, regular edge layout from the sleeve root through the elbow to the cuff (QA/sleeve-retopo/retopo_sleeve.py),
    // as a separate copy of Liu Bei; the 0.25 assets are not touched.
    //  BuildTrial: reads QA/sleeve-retopo/rest-out.obj, takes every new vertex's bone weights, normal and blend-shape
    //   deltas from the point of the original surface it lies on (the original triangle containing it, matched by UV at
    //   seams), then rebuilds the right sleeve's weights along the new loops (forearm / upper arm / sleeve bone smoothed
    //   from root to cuff). Writes Processed/liubei-sleevetrial/skin.asset and liubei-sleevetrial.prefab.
    //  Reels: the sweep (whole clip, 60 fps), idle and thrust, baseline (0.25) and trial side by side, same camera and
    //   light, front / side / back, corrective on, cloth follow-through stepped; plus the bunching measure.
    public static partial class GuanyuSample
    {
        const string RetopoFolder="Assets/Sango3/Characters/Recovered/Processed/liubei-sleevetrial/";
        const string TrialHeroPrefab=RetopoFolder+"liubei-sleevetrial.prefab";
        public static void BuildSleeveTrial()
        {
            heroId="liubei";var source=AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefab).GetComponent<SkinnedMeshRenderer>().sharedMesh;
            if(source.subMeshCount!=1)throw new System.Exception("Liu Bei mesh has "+source.subMeshCount+" sub-meshes");
            var sv=source.vertices;var suv=source.uv;var sn=source.normals;var sw=source.boneWeights;var st=source.triangles;int nb=source.bindposes.Length;
            var shapes=Enumerable.Range(0,source.blendShapeCount).Select(i=>{var d=new Vector3[sv.Length];source.GetBlendShapeFrameVertices(i,0,d,null,null);return (name:source.GetBlendShapeName(i),w:source.GetBlendShapeFrameWeight(i,0),d);}).ToList();
            // Read the Blender output (x mirrored back to Unity; winding reversed accordingly).
            var ci=CultureInfo.InvariantCulture;var pos=new List<Vector3>();var tex=new List<Vector2>();var faces=new List<(int v,int t)[]>();
            foreach(var line in File.ReadLines("QA/sleeve-retopo/rest-out.obj")){var p=line.Split(new[]{' '},System.StringSplitOptions.RemoveEmptyEntries);if(p.Length==0)continue;
                if(p[0]=="v")pos.Add(new Vector3(-float.Parse(p[1],ci),float.Parse(p[2],ci),float.Parse(p[3],ci)));
                else if(p[0]=="vt")tex.Add(new Vector2(float.Parse(p[1],ci),float.Parse(p[2],ci)));
                else if(p[0]=="f")faces.Add(p.Skip(1).Select(q=>{var r=q.Split('/');return (int.Parse(r[0])-1,int.Parse(r[1])-1);}).ToArray());}
            // New vertices: one per (position, uv) pair.
            var key=new Dictionary<(int,int),int>();var nv=new List<Vector3>();var nuv=new List<Vector2>();var tris=new List<int>();
            foreach(var f in faces){if(f.Length!=3)throw new System.Exception("non-triangle face");foreach(var c in new[]{f[0],f[2],f[1]}){if(!key.TryGetValue(c,out int k)){k=nv.Count;key[c]=k;nv.Add(pos[c.v]);nuv.Add(tex[c.t]);}tris.Add(k);}}
            // Transfer from the original surface: nearest original triangle (by distance, then by UV agreement).
            var weightsOut=new BoneWeight[nv.Count];var normalsOut=new Vector3[nv.Count];var shapeOut=shapes.Select(_=>new Vector3[nv.Count]).ToList();
            var grid=new Dictionary<Vector3Int,List<int>>();const float cell=.15f;
            for(int t=0;t<st.Length;t+=3){var mn=Vector3.Min(Vector3.Min(sv[st[t]],sv[st[t+1]]),sv[st[t+2]]);var mx=Vector3.Max(Vector3.Max(sv[st[t]],sv[st[t+1]]),sv[st[t+2]]);
                for(int x=Mathf.FloorToInt(mn.x/cell);x<=Mathf.FloorToInt(mx.x/cell);x++)for(int y=Mathf.FloorToInt(mn.y/cell);y<=Mathf.FloorToInt(mx.y/cell);y++)for(int z=Mathf.FloorToInt(mn.z/cell);z<=Mathf.FloorToInt(mx.z/cell);z++){var g=new Vector3Int(x,y,z);if(!grid.TryGetValue(g,out var l))grid[g]=l=new List<int>();l.Add(t);}}
            float worstDistance=0;int far=0;
            for(int i=0;i<nv.Count;i++){var p=nv[i];var g=Vector3Int.FloorToInt(p/cell);float best=float.MaxValue;int bt=-1;Vector3 bb=Vector3.zero;
                for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++){if(!grid.TryGetValue(g+new Vector3Int(x,y,z),out var list))continue;
                    foreach(int t in list){var (q,bc)=Closest(p,sv[st[t]],sv[st[t+1]],sv[st[t+2]]);float d=(q-p).magnitude;var uvAt=suv[st[t]]*bc.x+suv[st[t+1]]*bc.y+suv[st[t+2]]*bc.z;float score=d+(uvAt-nuv[i]).magnitude*.5f;if(score<best){best=score;bt=t;bb=bc;}}}
                if(bt<0)throw new System.Exception("No source triangle for vertex "+i);var (qq,_)=Closest(p,sv[st[bt]],sv[st[bt+1]],sv[st[bt+2]]);float dist=(qq-p).magnitude;worstDistance=Mathf.Max(worstDistance,dist);if(dist>.002f)far++;
                var acc=new float[nb];int[] idx={st[bt],st[bt+1],st[bt+2]};float[] bw={bb.x,bb.y,bb.z};
                for(int k=0;k<3;k++){var w=sw[idx[k]];acc[w.boneIndex0]+=w.weight0*bw[k];acc[w.boneIndex1]+=w.weight1*bw[k];acc[w.boneIndex2]+=w.weight2*bw[k];acc[w.boneIndex3]+=w.weight3*bw[k];normalsOut[i]+=sn[idx[k]]*bw[k];for(int s=0;s<shapes.Count;s++)shapeOut[s][i]+=shapes[s].d[idx[k]]*bw[k];}
                normalsOut[i]=normalsOut[i].normalized;weightsOut[i]=Top4(acc);}
            // Rebuild the right sleeve's weights along the new loops: forearm (6), upper arm (5) and sleeve bone (18)
            // smoothed over the new surface from the sleeve root to the cuff, total share per vertex kept.
            var adj=Enumerable.Range(0,nv.Count).Select(_=>new HashSet<int>()).ToArray();var weldNew=new Dictionary<Vector3,int>();var wmap=new int[nv.Count];
            for(int i=0;i<nv.Count;i++){if(!weldNew.TryGetValue(nv[i],out int gi)){gi=weldNew.Count;weldNew[nv[i]]=gi;}wmap[i]=gi;}
            int gc=weldNew.Count;var gadj=Enumerable.Range(0,gc).Select(_=>new HashSet<int>()).ToArray();
            for(int t=0;t<tris.Count;t+=3)for(int e=0;e<3;e++){int a=wmap[tris[t+e]],b=wmap[tris[t+(e+1)%3]];if(a!=b){gadj[a].Add(b);gadj[b].Add(a);}}
            var data=new float[gc,nb];var gp=new Vector3[gc];for(int i=0;i<nv.Count;i++){int g=wmap[i];var w=weightsOut[i];data[g,w.boneIndex0]=w.weight0;data[g,w.boneIndex1]=w.weight1;data[g,w.boneIndex2]=w.weight2;data[g,w.boneIndex3]=w.weight3;gp[g]=nv[i];}
            int[] set={5,6,18};int smoothed=0;
            for(int pass=0;pass<24;pass++){var next=(float[,])data.Clone();
                for(int g=0;g<gc;g++){var p=gp[g];if(!(p.x<-.5f&&p.y>1.2f&&p.y<3.45f))continue;float total=set.Sum(b=>data[g,b]);if(total<.5f)continue;
                    foreach(int b in set){float sum=0;int n=0;foreach(int k in gadj[g]){sum+=data[k,b];n++;}if(n>0)next[g,b]=data[g,b]*.5f+sum/n*.5f;}
                    float after=set.Sum(b=>next[g,b]);if(after>0)foreach(int b in set)next[g,b]*=total/after;if(pass==0)smoothed++;}
                data=next;}
            for(int i=0;i<nv.Count;i++){var acc=new float[nb];int g=wmap[i];for(int b=0;b<nb;b++)acc[b]=data[g,b];weightsOut[i]=Top4(acc);}
            // Build the trial mesh and prefab copy.
            var mesh=new Mesh{name="liubei-sleevetrial"};mesh.indexFormat=nv.Count>65000?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(nv);mesh.SetUVs(0,nuv);mesh.SetTriangles(tris,0);mesh.SetNormals(normalsOut.ToList());mesh.RecalculateTangents();mesh.boneWeights=weightsOut;mesh.bindposes=source.bindposes;
            for(int s=0;s<shapes.Count;s++)mesh.AddBlendShapeFrame(shapes[s].name,shapes[s].w,shapeOut[s],null,null);mesh.RecalculateBounds();
            Directory.CreateDirectory(RetopoFolder);AssetDatabase.DeleteAsset(RetopoFolder+"skin.asset");AssetDatabase.CreateAsset(mesh,RetopoFolder+"skin.asset");
            AssetDatabase.DeleteAsset(TrialHeroPrefab);AssetDatabase.CopyAsset(HeroPrefab,TrialHeroPrefab);
            {var c=PrefabUtility.LoadPrefabContents(TrialHeroPrefab);try{var sk=c.GetComponent<SkinnedMeshRenderer>();sk.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(RetopoFolder+"skin.asset");sk.localBounds=new Bounds(Vector3.up*2.2f,new Vector3(8,7,8));PrefabUtility.SaveAsPrefabAsset(c,TrialHeroPrefab);}finally{PrefabUtility.UnloadPrefabContents(c);}}
            AssetDatabase.SaveAssets();heroId="guanyu";
            var log="trial mesh: "+source.vertexCount+" -> "+nv.Count+" vertices, "+st.Length/3+" -> "+tris.Count/3+" triangles; transfer: worst distance to the original surface "+worstDistance.ToString("0.00000")+", vertices farther than 0.002: "+far+"; right-sleeve weights rebuilt on "+smoothed+" positions (24 passes)";
            File.WriteAllText("QA/sleeve-retopo/build.txt",log);Debug.Log("SANGO3_SLEEVE_TRIAL "+log);
        }
        static BoneWeight Top4(float[] acc){var top=Enumerable.Range(0,acc.Length).Where(b=>acc[b]>0).OrderByDescending(b=>acc[b]).Take(4).ToArray();float sum=top.Sum(b=>acc[b]);var w=new BoneWeight();
            for(int k=0;k<top.Length;k++){float x=acc[top[k]]/sum;if(k==0){w.boneIndex0=top[k];w.weight0=x;}if(k==1){w.boneIndex1=top[k];w.weight1=x;}if(k==2){w.boneIndex2=top[k];w.weight2=x;}if(k==3){w.boneIndex3=top[k];w.weight3=x;}}return w;}
        // Closest point on triangle abc to p, with barycentric coordinates.
        static (Vector3,Vector3) Closest(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
        {
            var ab=b-a;var ac=c-a;var ap=p-a;float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);if(d1<=0&&d2<=0)return (a,new Vector3(1,0,0));
            var bp=p-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);if(d3>=0&&d4<=d3)return (b,new Vector3(0,1,0));
            float vc=d1*d4-d3*d2;if(vc<=0&&d1>=0&&d3<=0){float v=d1/(d1-d3);return (a+v*ab,new Vector3(1-v,v,0));}
            var cp=p-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);if(d6>=0&&d5<=d6)return (c,new Vector3(0,0,1));
            float vb=d5*d2-d1*d6;if(vb<=0&&d2>=0&&d6<=0){float w=d2/(d2-d6);return (a+w*ac,new Vector3(1-w,0,w));}
            float va=d3*d6-d5*d4;if(va<=0&&(d4-d3)>=0&&(d5-d6)>=0){float w=(d4-d3)/((d4-d3)+(d5-d6));return (b+w*(c-b),new Vector3(0,1-w,w));}
            float denom=1f/(va+vb+vc);float vv=vb*denom,ww=vc*denom;return (a+ab*vv+ac*ww,new Vector3(1-vv-ww,vv,ww));
        }
        // Side-by-side reels: baseline (0.25) left, trial right; per clip and view, 60 fps sampling.
        public static void SleeveTrialReels()
        {
            const string Out="QA/sleeve-retopo/reels/";if(Directory.Exists(Out))Directory.Delete(Out,true);var report=new StringBuilder();
            var clips=new[]{("Attack2",0f),("Idle",2f),("Attack3",0f)};
            foreach(var variant in new[]{("baseline",(string)null),("trial",TrialHeroPrefab)}){
                trialPrefab=variant.Item2;heroId="liubei";
                // Bunching over all moves for this variant.
                {Load();var m=root.GetComponent<SkinnedMeshRenderer>().sharedMesh;float follow=root.GetComponent<RecoveredSleeveDrape>().follow;report.Append(variant.Item1+" (vertices "+m.vertexCount+"):");
                    foreach(var cl in new[]{"Idle","Attack1","Attack2","Attack3","Ultimate"}){var r=SleeveBunching(m.boneWeights,follow,cl);report.Append(" "+cl+" crushed "+(r.crushed/r.total*100).ToString("0.0")+"% (worst "+(r.worstCrushed*100f/r.total).ToString("0.0")+"%);");}
                    report.AppendLine();Object.DestroyImmediate(root);}
                foreach(var (clipName,fixedLength) in clips){Load();HeroLighting.Apply();ClothFollow.Attach(root);var cloth=root.GetComponent<ClothFollow>();
                    var light=new GameObject("key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(40,-35,0);RenderSettings.ambientLight=new Color(.55f,.56f,.6f);
                    var camera=new GameObject("camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.10f,.12f,.15f);camera.orthographic=true;camera.orthographicSize=1.6f;camera.aspect=1;
                    const int cell=360;var frame=new Texture2D(cell*3,cell,TextureFormat.RGB24,false);var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(HeroFolder+clipName+".anim");
                    float length=fixedLength>0?fixedLength:clip.length;int frames=Mathf.CeilToInt(length*60);var dir=Out+variant.Item1+"-"+clipName+"/";Directory.CreateDirectory(dir);
                    for(int f=0;f<=frames;f++){clip.SampleAnimation(root,Mathf.Min(clip.length,f/60f%Mathf.Max(clip.length,1e-3f)));if(grip)grip.Solve();SolveDrape();if(cloth)cloth.Step(1/60f);
                        var shown=Bake();var chest=bones[Chest].position+Vector3.down*.15f;var views=new[]{new Vector3(2.5f,.4f,7.6f),new Vector3(8f,.4f,.3f),new Vector3(-2.5f,.4f,-7.6f)};
                        for(int v=0;v<3;v++){camera.transform.position=chest+views[v];camera.transform.LookAt(chest);Shot(camera,frame,v*cell,0,cell);}Clear(shown);
                        File.WriteAllBytes(dir+f.ToString("0000")+".jpg",frame.EncodeToJPG(90));}
                    Object.DestroyImmediate(frame);Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(root);}
            }
            trialPrefab=null;heroId="guanyu";File.WriteAllText("QA/sleeve-retopo/bunching.txt",report.ToString());Debug.Log("SANGO3_SLEEVE_REELS "+report);
        }
    }
}
