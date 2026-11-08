using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Sango3.Editor
{
    public static class RecoveredHeroBuilder
    {
        const string Folder="Assets/Sango3/Characters/Recovered/";
        static string id="guanyu"; static string Output=>"Assets/Sango3/Characters/Recovered/Processed/"+(id=="guanyu"?"":id+"/");
        static readonly string[] Roster={"guanyu","zhugeliang","zhangfei","liubei","zhaoyun","caocao","lvbu","zhouyu","sunquan","simayi","zhangliao","xiahoudun"};
        public static void PreviewAll(){foreach(var name in Roster){id=name;Preview();}}
        public static void RebuildLiu(){id="liubei";Build();}
        public static void RebuildRobes(){foreach(var n in new[]{"liubei","zhugeliang"}){id=n;Build();}}
        public static void RebuildGuan(){id="guanyu";Build();}
        public static void BuildYuanSoldier(){id="yuansoldier";Build();}
        public static void BuildPlayableOthers(){foreach(var n in new[]{"zhugeliang","zhangfei","liubei","zhaoyun","caocao"}){id=n;Build();}}
        public static void ReviewHands(){RebuildGuan();RebuildLiu();HandStudy.Review();}
        public static void BuildAll(){foreach(var name in Roster){id=name;Build();}}
        public static void RenderAccepted(){foreach(var name in Roster){id=name;var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sango3/Resources/Heroes/Final/"+id+".prefab"));var animator=root.GetComponent<Animator>();var grip=root.GetComponent<RecoveredPolearmGrip>();foreach(var state in new[]{"Idle","Cast"}){animator.Rebind();animator.Play(state,0,0);animator.Update(0);animator.Update(.25f);if(grip)grip.Solve();RenderRig(root,"accepted-"+state);}Object.DestroyImmediate(root);}}
        public static void RefineRestPoses(){foreach(var name in Roster){id=name;profile=Profile(id);var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sango3/Resources/Heroes/Final/"+id+".prefab"));bones.Clear();bones.AddRange(root.GetComponent<SkinnedMeshRenderer>().bones);foreach(string state in new[]{"Idle","Walk","Attack","Cast"})SaveAsset(CreateClip(state,root.transform),Output+state+".anim");Object.DestroyImmediate(root);}AssetDatabase.SaveAssets();RenderAccepted();}
        static readonly List<Transform> bones=new List<Transform>();
        sealed class RigProfile
        {
            public Vector2 shoulder=new Vector2(.45f,3.37f),elbow=new Vector2(.89f,2.90f),wrist=new Vector2(1.23f,2.48f);
            public float hips=2.2f,neck=3.61f,head=3.83f;public bool robe,cape,polearm=true;public string weapon="张飞";
            public float elbowDepth,wristDepth;
            public Vector3 fingerDirection;
        }
        // Playable heroes get clavicles (Guan Yu standard). Liu Bei's robe uses bones 18/19 for sleeve drapes instead.
        static bool Clavicled=>id=="guanyu"||id=="zhaoyun"||id=="zhangfei"||id=="caocao"||id=="zhugeliang";
        static RigProfile profile;// Every hero holds a weapon, fan or sword: all get the articulated five-finger grip, not a curled source mesh.
        static bool UseArticulatedHands=>true;static float PalmLength=>id=="guanyu"?.18f:.13f;
        static RigProfile Profile(string hero){var p=new RigProfile();switch(hero){
            case "guanyu":p.weapon="关羽";break;
            case "zhugeliang":p.robe=true;p.polearm=false;p.weapon="诸葛亮";p.shoulder=new Vector2(.42f,3.45f);p.elbow=new Vector2(.98f,2.92f);p.wrist=new Vector2(1.34f,2.60f);break;
            case "liubei":p.robe=true;p.polearm=false;p.weapon="刘备";p.shoulder=new Vector2(.46f,3.38f);p.elbow=new Vector2(1.05f,3.07f);p.wrist=new Vector2(1.64f,3.015f);break;
            case "zhangfei":p.shoulder=new Vector2(.53f,3.40f);p.elbow=new Vector2(.96f,2.98f);p.wrist=new Vector2(1.28f,2.51f);break;
            case "zhaoyun":p.cape=true;p.shoulder=new Vector2(.44f,3.42f);p.elbow=new Vector2(.89f,2.98f);p.wrist=new Vector2(1.20f,2.43f);p.weapon="赵云";break;
            case "caocao":p.cape=true;p.polearm=false;p.weapon="曹操";p.shoulder=new Vector2(.45f,3.38f);p.elbow=new Vector2(.88f,2.86f);p.wrist=new Vector2(1.10f,2.27f);break;
            case "lvbu":p.cape=true;p.shoulder=new Vector2(.47f,3.20f);p.elbow=new Vector2(.94f,2.78f);p.wrist=new Vector2(1.23f,2.33f);p.neck=3.38f;p.head=3.58f;p.hips=2.02f;break;
            case "zhouyu":p.robe=true;p.polearm=false;p.weapon="刘备";p.wrist=new Vector2(1.08f,2.43f);break;
            case "sunquan":p.robe=true;p.polearm=false;p.weapon="刘备";p.elbow=new Vector2(.75f,2.85f);p.wrist=new Vector2(.99f,2.58f);break;
            case "simayi":p.robe=true;p.polearm=false;p.weapon="诸葛亮";p.elbow=new Vector2(.80f,2.85f);p.wrist=new Vector2(1.04f,2.50f);break;
            case "zhangliao":p.cape=true;p.shoulder=new Vector2(.48f,3.30f);p.wrist=new Vector2(1.20f,2.40f);p.neck=3.52f;p.head=3.72f;break;
            case "xiahoudun":p.polearm=false;p.weapon="曹操";p.shoulder=new Vector2(.50f,3.42f);p.wrist=new Vector2(1.25f,2.43f);break;
            // Stage 16 Yuan spearman: the xiahoudun body recoloured (QA/sample-130/recolor.py), two-handed spear grip.
            case "yuansoldier":p.weapon="士卒";p.shoulder=new Vector2(.50f,3.42f);p.wrist=new Vector2(1.25f,2.43f);break;
        }return p;}
        static Transform Bone(string name,int parent,Vector3 position,Transform root){var t=new GameObject(name).transform;t.SetParent(parent<0?root:bones[parent],false);t.position=position;bones.Add(t);return t;}
        public static void Build()
        {
            profile=Profile(id);Directory.CreateDirectory(Output);Directory.CreateDirectory("Assets/Sango3/Resources/Heroes/Final");AssetDatabase.Refresh();bones.Clear();
            var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+id+".obj");if(!importer.isReadable){importer.isReadable=true;importer.SaveAndReimport();}
            var source=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+id+".obj"));var filter=source.GetComponentInChildren<MeshFilter>();var mesh=Object.Instantiate(filter.sharedMesh);mesh.name="Guanyu skinned mesh";if(!mesh.isReadable||mesh.vertexCount<100)throw new Exception("Source mesh is not readable or empty: "+id);
            var sourceNormals=mesh.normals.Select(n=>filter.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(n).normalized).ToArray();var vertices=mesh.vertices.Select(v=>filter.transform.TransformPoint(v)).ToArray();var min=vertices.Aggregate(Vector3.Min);var max=vertices.Aggregate(Vector3.Max);float scale=4.4f/(max.y-min.y);var center=(max+min)*.5f;
            for(int i=0;i<vertices.Length;i++)vertices[i]=(vertices[i]-new Vector3(center.x,min.y,center.z))*scale;mesh.vertices=vertices;mesh.normals=sourceNormals;mesh.RecalculateTangents();
            profile.elbowDepth=id=="liubei"?.15f:JointDepth(vertices,profile.elbow);profile.wristDepth=id=="liubei"?.25f:JointDepth(vertices,profile.wrist);
            var handPosition=new Vector3(profile.wrist.x,profile.wrist.y,profile.wristDepth);var handDirection=(handPosition-new Vector3(profile.elbow.x,profile.elbow.y,profile.elbowDepth)).normalized;
            
            handDirection=FingerDirection(mesh.vertices,handPosition,handDirection);profile.fingerDirection=handDirection;
            if(UseArticulatedHands)ArticulatedHandBuilder.TrimOriginal(mesh,handPosition,handDirection);else HandTopology.Refine(mesh,handPosition,handDirection);vertices=mesh.vertices;sourceNormals=mesh.normals;
            Object.DestroyImmediate(source);var root=new GameObject("Guanyu recovered animated");
            Bone("Hips",-1,new Vector3(0,profile.hips,0),root.transform); // 0
            Bone("Spine",0,new Vector3(0,2.65f,0),root.transform); // 1
            Bone("Chest",1,new Vector3(0,3.13f,0),root.transform); // 2
            Bone("Neck",2,new Vector3(0,profile.neck,0),root.transform); // 3
            Bone("Head",3,new Vector3(0,profile.head,0),root.transform); // 4
            for(int side=0;side<2;side++){float s=side==0?-1:1;int start=bones.Count;string prefix=side==0?"Right":"Left";
                Bone(prefix+"UpperArm",2,new Vector3(s*profile.shoulder.x,profile.shoulder.y,0),root.transform);
                Bone(prefix+"Forearm",start,new Vector3(s*profile.elbow.x,profile.elbow.y,profile.elbowDepth),root.transform);
                Bone(prefix+"Hand",start+1,new Vector3(s*profile.wrist.x,profile.wrist.y,profile.wristDepth),root.transform);
            }
            for(int side=0;side<2;side++){float s=side==0?-1:1;int start=bones.Count;string prefix=side==0?"Right":"Left";
                Bone(prefix+"Thigh",0,new Vector3(s*.28f,2.10f,0),root.transform);
                Bone(prefix+"Shin",start,new Vector3(s*.32f,1.02f,0),root.transform);
                Bone(prefix+"Foot",start+1,new Vector3(s*.37f,.20f,.06f),root.transform);
            }
            Bone("Skirt",0,new Vector3(0,2.2f,0),root.transform); // 17 independent cloth from legs
            if(Clavicled)for(int side=0;side<2;side++){float sx=side==0?-1:1;
                // 18/19: clavicles between chest and upper arm. Raising the arm overhead shares its lift with the
                // shoulder girdle instead of folding the pauldron and sleeve at a single joint.
                var clavicle=Bone((side==0?"Right":"Left")+"Clavicle",2,new Vector3(sx*.16f,profile.shoulder.y-.04f,0),root.transform);bones[side==0?5:8].SetParent(clavicle,true);}
            if(id=="liubei"){
                var drape=root.AddComponent<RecoveredSleeveDrape>();drape.body=root.transform;
                drape.rightSleeve=Bone("RightSleeveDrape",6,new Vector3(-1.36f,2.96f,.17f),root.transform);
                drape.leftSleeve=Bone("LeftSleeveDrape",9,new Vector3(1.36f,2.96f,.17f),root.transform);
            }
            var weights=new BoneWeight[vertices.Length];
            for(int i=0;i<vertices.Length;i++){
                var v=vertices[i];int[] candidates;
                if(v.y>profile.neck && Mathf.Abs(v.x)<.34f)candidates=new[]{3,4};
                else if(Mathf.Abs(v.x)>profile.wrist.x*.91f&&v.y>profile.wrist.y-.60f&&v.y<profile.wrist.y+.12f)candidates=v.x<0?new[]{6,7}:new[]{9,10};
                else if(profile.robe&&Mathf.Abs(v.x)>.60f&&v.y>1.5f)candidates=v.x<0?new[]{1,2,5,6,7}:new[]{1,2,8,9,10};
                else if(profile.cape&&v.z<-.16f&&v.y<3.05f)candidates=new[]{0,17};
                else if(profile.robe&&v.y<2.15f)candidates=new[]{0,17};
                else if(v.y>2.0f && Mathf.Abs(v.x)>.20f)candidates=v.x<0?new[]{0,1,2,3,5,6,7}:new[]{0,1,2,3,8,9,10};
                else if(v.y<1.12f)candidates=v.x<0?new[]{11,12,13}:new[]{14,15,16};
                else if(v.y<2.20f)candidates=new[]{0,17};
                else if(v.y>3.05f&&v.z>.20f&&Mathf.Abs(v.x)<.30f)candidates=new[]{4,2};
                else candidates=new[]{0,1,2,3};
                float boundary=profile.shoulder.x+(profile.shoulder.y-v.y)*(profile.wrist.x-profile.shoulder.x)/Mathf.Max(.3f,profile.shoulder.y-profile.wrist.y);if(profile.robe)boundary=.64f;float blend=profile.robe?.34f:.18f; // robes: a wider chest-to-sleeve blend so the shoulder seam bends instead of creasing
                float armMix=Mathf.SmoothStep(0,1,Mathf.InverseLerp(boundary-blend,boundary+blend,Mathf.Abs(v.x)));bool mixed=candidates.Length>=5;var nearest=candidates.Select(k=>new {k,w=(mixed?(k>=5?armMix:1-armMix):1)/Mathf.Pow(Distance(v,k)+.08f,3)}).OrderByDescending(p=>p.w).Take(4).ToArray();var values=nearest.Select(p=>p.w).ToArray();float sum=values.Sum();var w=new BoneWeight{boneIndex0=nearest[0].k,boneIndex1=nearest[1].k,weight0=values[0]/sum,weight1=values[1]/sum};if(nearest.Length>2){w.boneIndex2=nearest[2].k;w.weight2=values[2]/sum;}if(nearest.Length>3){w.boneIndex3=nearest[3].k;w.weight3=values[3]/sum;}weights[i]=w;
            }
            SmoothWeights(vertices,mesh.triangles,weights);if(id=="guanyu")RefineGuanArms(vertices,weights);RefineGripAndShoulders(vertices,weights,sourceNormals);if(Clavicled)RefineClavicles(vertices,weights);if(id=="liubei")RefineSleeves(vertices,weights);ClothWeights.Soften(vertices,mesh.triangles,weights,bones.Count);mesh.vertices=vertices;mesh.normals=sourceNormals;mesh.RecalculateTangents();
            mesh.boneWeights=weights;mesh.bindposes=bones.Select(t=>t.worldToLocalMatrix*root.transform.localToWorldMatrix).ToArray();mesh.RecalculateBounds();SaveAsset(mesh,Output+"guanyu-skin.asset");
            var skin=root.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Output+"guanyu-skin.asset");skin.bones=bones.ToArray();skin.rootBone=bones[0];skin.localBounds=new Bounds(Vector3.up*2.2f,new Vector3(8,7,8));skin.updateWhenOffscreen=true;
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Guanyu recovered jade and bronze"};material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+id+"-albedo.jpg");material.SetTexture("_MetallicGlossMap",RecoveredSurfaceMap.Build(skin.sharedMesh,(Texture2D)material.mainTexture,profile.robe,handPosition,Output+"surface-mask.png"));material.EnableKeyword("_METALLICSPECGLOSSMAP");material.SetFloat("_Smoothness",1);material.SetFloat("_Cull",0);SaveAsset(material,Output+"guanyu.mat");skin.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Output+"guanyu.mat");
            ArticulatedHandBuilder.Grip rightGrip=default,leftGrip=default;if(UseArticulatedHands){rightGrip=ArticulatedHandBuilder.Attach(root,bones[7],new Vector3(-profile.fingerDirection.x,profile.fingerDirection.y,profile.fingerDirection.z),true,true,Output);leftGrip=ArticulatedHandBuilder.Attach(root,bones[10],profile.fingerDirection,false,profile.polearm,Output);}
            var shaft=RecoveredWeaponBuilder.Build(bones[7],profile.weapon);shaft.localRotation=profile.polearm?Quaternion.Euler(70,0,-18):Quaternion.Euler(35,0,0);shaft.localScale=Vector3.one*(profile.polearm?1.5f:1.2f);var gripDirection=new Vector3(-profile.fingerDirection.x,profile.fingerDirection.y,profile.fingerDirection.z);var gripCurl=Vector3.ProjectOnPlane(Vector3.forward,gripDirection).normalized;shaft.localPosition=gripDirection*PalmLength+gripCurl*.065f;var gripAxis=Vector3.Cross(gripDirection,gripCurl).normalized;if(gripAxis.y<0)gripAxis=-gripAxis;if(profile.polearm)gripAxis=-gripAxis;shaft.localRotation=Quaternion.FromToRotation(Vector3.up,gripAxis);
            if(UseArticulatedHands){shaft.localPosition=rightGrip.center;shaft.localRotation=Quaternion.FromToRotation(Vector3.up,profile.polearm?-rightGrip.axis:rightGrip.axis);}
            int resource=0;foreach(var f in shaft.GetComponentsInChildren<MeshFilter>())if(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(f.sharedMesh))){string p=Output+"weapon-mesh-"+(resource++)+".asset";SaveAsset(Object.Instantiate(f.sharedMesh),p);f.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(p);}
            resource=0;foreach(var r in shaft.GetComponentsInChildren<Renderer>())if(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(r.sharedMaterial))){string p=Output+"weapon-material-"+(resource++)+".mat";SaveAsset(new Material(r.sharedMaterial),p);r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(p);}
            RecoveredPolearmGrip grip=null;if(profile.polearm){grip=root.AddComponent<RecoveredPolearmGrip>();grip.upper=bones[8];grip.lower=bones[9];grip.hand=bones[10];grip.shaft=shaft;grip.rightHand=bones[7];grip.gripOffset=new Vector3(0,-.35f,0);var supportDirection=profile.fingerDirection;grip.palmOffset=supportDirection*PalmLength+Vector3.ProjectOnPlane(Vector3.forward,supportDirection).normalized*.065f;grip.palmAxis=Vector3.Cross(Vector3.forward,supportDirection).normalized;}
            if(UseArticulatedHands&&grip){grip.palmOffset=leftGrip.center;grip.palmAxis=leftGrip.axis;}
            var controllerPath=Output+"Guanyu.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);var machine=controller.layers[0].stateMachine;foreach(var s in machine.states)machine.RemoveState(s.state);
            foreach(string state in new[]{"Idle","Walk","Attack","Cast"}){var clip=CreateClip(state,root.transform);SaveAsset(clip,Output+state+".anim");var s=machine.AddState(state);s.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(Output+state+".anim");if(state=="Idle")machine.defaultState=s;}
            root.AddComponent<Animator>().runtimeAnimatorController=controller;
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Sango3/Resources/Heroes/Final/"+id+".prefab");AssetDatabase.SaveAssets();if(id=="guanyu")GuanyuSample.ApplyShoulderHelpersFor(id);if(id=="liubei")GuanyuSample.ApplyLiubeiSleeve();if(System.Array.IndexOf(BattleRules.Portraits,id)>=0)ShoulderCorrectives.Build(id);
            var report=new System.Text.StringBuilder();report.AppendLine("bones="+bones.Count+" vertices="+skin.sharedMesh.vertexCount+" triangles="+skin.sharedMesh.triangles.Length/3);
            foreach(string state in new[]{"Idle","Attack","Cast"}){var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Output+state+".anim");foreach(float time in new[]{0f,.25f,.55f,.85f}){clip.SampleAnimation(root,time);if(grip)grip.Solve();var baked=new Mesh();skin.BakeMesh(baked);if(baked.vertices.Any(v=>float.IsNaN(v.x)||float.IsInfinity(v.x)))throw new Exception("Invalid deformation");report.AppendLine(state+" t="+time+" bounds="+baked.bounds.size);Object.DestroyImmediate(baked);RenderRig(root,state+"-"+time.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture));}}
            File.WriteAllText("QA/"+id+"-rig-validation.txt",report.ToString());Object.DestroyImmediate(root);Debug.Log("RECOVERED_GUANYU_RIG_READY");
        }
        // Trapezius and the top of the pauldron move partly with the clavicle (child upper-arm weights already follow it).
        static void RefineClavicles(Vector3[] vertices,BoneWeight[] weights){
            for(int i=0;i<vertices.Length;i++){var v=vertices[i];float x=Mathf.Abs(v.x);if(x<.10f||v.y<profile.shoulder.y-.40f||v.y>profile.neck+.05f)continue;
                int clavicle=v.x<0?18:19;float influence=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.10f,profile.shoulder.x-.02f,x))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(profile.shoulder.y-.40f,profile.shoulder.y-.10f,v.y))*.75f;
                var w=weights[i];var data=new float[bones.Count];data[w.boneIndex0]+=w.weight0;data[w.boneIndex1]+=w.weight1;data[w.boneIndex2]+=w.weight2;data[w.boneIndex3]+=w.weight3;
                // Only the torso share (spine, chest, neck) moves to the clavicle; arm weights stay with the arm.
                float torso=data[1]+data[2]+data[3];if(torso<=0)continue;float moved=torso*influence;for(int k=1;k<=3;k++)data[k]*=1-influence;data[clavicle]+=moved;
                var top=Enumerable.Range(0,bones.Count).OrderByDescending(k=>data[k]).Take(4).ToArray();float sum=top.Sum(k=>data[k]);weights[i]=new BoneWeight{boneIndex0=top[0],boneIndex1=top[1],boneIndex2=top[2],boneIndex3=top[3],weight0=data[top[0]]/sum,weight1=data[top[1]]/sum,weight2=data[top[2]]/sum,weight3=data[top[3]]/sum};}
        }
        static void RefineGuanArms(Vector3[] vertices,BoneWeight[] weights){
            for(int i=0;i<vertices.Length;i++){
                var p=vertices[i];int upper=p.x<0?5:8,lower=upper+1,hand=upper+2;if(p.y<2.1f||p.y>3.65f)continue;
                float distance=Mathf.Min(Distance(p,upper),Distance(p,lower));float influence=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.22f,.34f,distance)))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.38f,.62f,Mathf.Abs(p.x)));if(influence<=0)continue;
                var axis=(bones[hand].position-bones[lower].position).normalized;float fore=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.12f,.12f,Vector3.Dot(p-bones[lower].position,axis)));
                var w=weights[i];var data=new float[bones.Count];data[w.boneIndex0]+=w.weight0*(1-influence);data[w.boneIndex1]+=w.weight1*(1-influence);data[w.boneIndex2]+=w.weight2*(1-influence);data[w.boneIndex3]+=w.weight3*(1-influence);data[upper]+=influence*(1-fore);data[lower]+=influence*fore;
                var top=Enumerable.Range(0,bones.Count).OrderByDescending(k=>data[k]).Take(4).ToArray();float sum=top.Sum(k=>data[k]);weights[i]=new BoneWeight{boneIndex0=top[0],boneIndex1=top[1],boneIndex2=top[2],boneIndex3=top[3],weight0=data[top[0]]/sum,weight1=data[top[1]]/sum,weight2=data[top[2]]/sum,weight3=data[top[3]]/sum};
            }
        }
        static void RefineGripAndShoulders(Vector3[] vertices,BoneWeight[] weights,Vector3[] normals)
        {
            for(int i=0;i<vertices.Length;i++){
                var v=vertices[i];float side=v.x<0?-1:1;int hand=side<0?7:10;
                // Curl only beyond the palm; the wrist and forearm remain unchanged.
                if(UseArticulatedHands||side<0||profile.polearm){
                    var wrist=bones[hand].position;var direction=new Vector3(side*profile.fingerDirection.x,profile.fingerDirection.y,profile.fingerDirection.z);var q=v-wrist;float along=Vector3.Dot(q,direction);var radial=q-direction*along;
                    if(along>(UseArticulatedHands?-.25f:-.06f)&&along<.65f&&radial.magnitude<.40f&&Mathf.Abs(v.x)>profile.wrist.x*.85f){float handBlend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(UseArticulatedHands?-.25f:-.06f,UseArticulatedHands?-.08f:.04f,along))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.28f,.40f,radial.magnitude)));var w=weights[i];var data=new float[bones.Count];data[w.boneIndex0]+=w.weight0*(1-handBlend);data[w.boneIndex1]+=w.weight1*(1-handBlend);data[w.boneIndex2]+=w.weight2*(1-handBlend);data[w.boneIndex3]+=w.weight3*(1-handBlend);data[hand]+=handBlend;var handTop=Enumerable.Range(0,bones.Count).OrderByDescending(k=>data[k]).Take(4).ToArray();float handSum=handTop.Sum(k=>data[k]);weights[i]=new BoneWeight{boneIndex0=handTop[0],boneIndex1=handTop[1],boneIndex2=handTop[2],boneIndex3=handTop[3],weight0=data[handTop[0]]/handSum,weight1=data[handTop[1]]/handSum,weight2=data[handTop[2]]/handSum,weight3=data[handTop[3]]/handSum};}
                    if(!UseArticulatedHands&&along>PalmLength&&along<.65f&&radial.magnitude<.40f&&Mathf.Abs(v.x)>profile.wrist.x*.90f){
                        float length=along-PalmLength;float radius=.065f;float angle=Mathf.Min(length/radius,4.4f);var curl=Vector3.ProjectOnPlane(Vector3.forward,direction).normalized;
                        var curled=wrist+direction*(PalmLength+Mathf.Sin(angle)*radius)+curl*((1-Mathf.Cos(angle))*radius)+radial-curl*Vector3.Dot(radial,curl)+(curl*Mathf.Cos(angle)-direction*Mathf.Sin(angle))*Vector3.Dot(radial,curl);
                        float blend=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.28f,.40f,radial.magnitude));
                        vertices[i]=Vector3.Lerp(v,curled,blend);normals[i]=Vector3.Slerp(normals[i],Quaternion.AngleAxis(angle*Mathf.Rad2Deg,Vector3.Cross(direction,curl))*normals[i],blend).normalized;
                    }
                }
                if(profile.robe)continue;
                // Shoulder cap core follows the upper arm; feather the boundary into the existing skin.
                var shoulder=bones[side<0?5:8].position;var d=v-shoulder;float ellipsoid=d.x*d.x/(.23f*.23f)+d.y*d.y/(.18f*.18f)+d.z*d.z/(.30f*.30f);
                float influence=id=="guanyu"?Mathf.SmoothStep(0,1,Mathf.InverseLerp(1,.25f,ellipsoid)):Mathf.SmoothStep(0,1,Mathf.Clamp01(1-ellipsoid))*.65f;
                if(influence<=0)continue;
                var old=weights[i];var values=new float[bones.Count];values[old.boneIndex0]+=old.weight0*(1-influence);values[old.boneIndex1]+=old.weight1*(1-influence);values[old.boneIndex2]+=old.weight2*(1-influence);values[old.boneIndex3]+=old.weight3*(1-influence);values[side<0?5:8]+=influence;
                var top=Enumerable.Range(0,bones.Count).OrderByDescending(k=>values[k]).Take(4).ToArray();float sum=top.Sum(k=>values[k]);weights[i]=new BoneWeight{boneIndex0=top[0],boneIndex1=top[1],boneIndex2=top[2],boneIndex3=top[3],weight0=values[top[0]]/sum,weight1=values[top[1]]/sum,weight2=values[top[2]]/sum,weight3=values[top[3]]/sum};
            }
        }
        static float JointDepth(Vector3[] vertices,Vector2 joint){var depths=vertices.Where(v=>Mathf.Abs(Mathf.Abs(v.x)-joint.x)<.10f&&Mathf.Abs(v.y-joint.y)<.10f).Select(v=>v.z).OrderBy(z=>z).ToArray();if(depths.Length<4)throw new Exception("Insufficient joint surface samples "+id+" "+joint);return (depths[depths.Length/10]+depths[depths.Length*9/10])*.5f;}
        static Vector3 FingerDirection(Vector3[] vertices,Vector3 wrist,Vector3 forearm){
            var distal=vertices.Select(v=>new Vector3(Mathf.Abs(v.x),v.y,v.z)-wrist).Where(q=>Vector3.Dot(q,forearm)>.20f&&Vector3.Dot(q,forearm)<.62f&&(q-forearm*Vector3.Dot(q,forearm)).magnitude<.30f).OrderByDescending(q=>Vector3.Dot(q,forearm)).ToArray();
            if(distal.Length<8)return forearm;var average=Vector3.zero;int count=Mathf.Max(8,distal.Length/4);for(int i=0;i<count;i++)average+=distal[i];return average.normalized;
        }
        static void RefineSleeves(Vector3[] vertices,BoneWeight[] weights){
            for(int i=0;i<vertices.Length;i++){var v=vertices[i];float x=Mathf.Abs(v.x);if(x<.72f||x>1.77f||v.y<1.1f||v.y>2.90f)continue;
                float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.72f,1.1f,x))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.50f,3.08f-v.y));
                var w=weights[i];var values=new float[bones.Count];values[w.boneIndex0]+=w.weight0*(1-blend);values[w.boneIndex1]+=w.weight1*(1-blend);values[w.boneIndex2]+=w.weight2*(1-blend);values[w.boneIndex3]+=w.weight3*(1-blend);values[v.x<0?18:19]+=blend;
                var top=Enumerable.Range(0,bones.Count).OrderByDescending(k=>values[k]).Take(4).ToArray();float sum=top.Sum(k=>values[k]);weights[i]=new BoneWeight{boneIndex0=top[0],boneIndex1=top[1],boneIndex2=top[2],boneIndex3=top[3],weight0=values[top[0]]/sum,weight1=values[top[1]]/sum,weight2=values[top[2]]/sum,weight3=values[top[3]]/sum};
            }
        }
        static float Distance(Vector3 v,int bone){var a=bones[bone].position;var b=a+Vector3.up*.2f;int next=bone==0?1:bone==1?2:bone==2?3:bone==3?4:bone==5?6:bone==6?7:bone==8?9:bone==9?10:bone==11?12:bone==12?13:bone==14?15:bone==15?16:-1;if(next>=0)b=bones[next].position;if(bone==17)b=new Vector3(0,1.3f,0);var d=b-a;return Vector3.Distance(v,a+d*Mathf.Clamp01(Vector3.Dot(v-a,d)/d.sqrMagnitude));}
        static void SmoothWeights(Vector3[] vertices,int[] triangles,BoneWeight[] weights){
            var groups=new Dictionary<Vector3Int,int>();var map=new int[vertices.Length];int count=0;
            for(int i=0;i<vertices.Length;i++){var key=Vector3Int.RoundToInt(vertices[i]*10000);if(!groups.TryGetValue(key,out int g)){g=count++;groups.Add(key,g);}map[i]=g;}
            var adjacency=Enumerable.Range(0,count).Select(_=>new HashSet<int>()).ToArray();var data=new float[count,bones.Count];var samples=new int[count];
            for(int i=0;i<vertices.Length;i++){int g=map[i];var w=weights[i];data[g,w.boneIndex0]+=w.weight0;data[g,w.boneIndex1]+=w.weight1;data[g,w.boneIndex2]+=w.weight2;data[g,w.boneIndex3]+=w.weight3;samples[g]++;}
            for(int g=0;g<count;g++)for(int k=0;k<bones.Count;k++)data[g,k]/=samples[g];
            for(int i=0;i<triangles.Length;i+=3)for(int e=0;e<3;e++){int a=map[triangles[i+e]],b=map[triangles[i+(e+1)%3]];adjacency[a].Add(b);adjacency[b].Add(a);}
            for(int pass=0;pass<8;pass++){var next=(float[,])data.Clone();for(int g=0;g<count;g++){if(adjacency[g].Count==0)continue;for(int k=0;k<bones.Count;k++){float sum=0;foreach(int n in adjacency[g])sum+=data[n,k];next[g,k]=data[g,k]*.5f+sum/adjacency[g].Count*.5f;}}data=next;}
            for(int i=0;i<vertices.Length;i++){int g=map[i];var top=Enumerable.Range(0,bones.Count).OrderByDescending(k=>data[g,k]).Take(4).ToArray();float sum=top.Sum(k=>data[g,k]);weights[i]=new BoneWeight{boneIndex0=top[0],boneIndex1=top[1],boneIndex2=top[2],boneIndex3=top[3],weight0=data[g,top[0]]/sum,weight1=data[g,top[1]]/sum,weight2=data[g,top[2]]/sum,weight3=data[g,top[3]]/sum};}
        }
        internal static void SaveAsset(Object obj,string path){var old=AssetDatabase.LoadAssetAtPath<Object>(path);if(old){
            if(obj is Mesh source && old is Mesh target){if(!source.isReadable||source.vertexCount<3)throw new Exception("Refusing to replace mesh with unreadable or empty data: "+path);target.Clear();target.indexFormat=source.indexFormat;target.vertices=source.vertices;target.normals=source.normals;target.tangents=source.tangents;target.uv=source.uv;target.subMeshCount=source.subMeshCount;for(int i=0;i<source.subMeshCount;i++)target.SetTriangles(source.GetTriangles(i),i);target.bindposes=source.bindposes;target.boneWeights=source.boneWeights;target.bounds=source.bounds;}
            else EditorUtility.CopySerialized(obj,old);Object.DestroyImmediate(obj);EditorUtility.SetDirty(old);
        }else AssetDatabase.CreateAsset(obj,path);}
        static AnimationClip CreateClip(string name,Transform root){var clip=new AnimationClip{name=name,frameRate=60};float duration=name=="Cast"?1.15f:name=="Attack"?.65f:2f;
            for(int b=0;b<bones.Count;b++)for(int axis=0;axis<3;axis++){
                var keys=new List<Keyframe>();int samples=24;for(int i=0;i<=samples;i++){float t=i/(float)samples;Vector3 angle=Vector3.zero;
                    if(b==5)angle=new Vector3(-12,0,55);if(b==6)angle=new Vector3(-35,0,-8);if(b==7)angle=new Vector3(0,0,-25);
                    bool action=name=="Attack"||name=="Cast";
                    // Anticipation (0-40%), strike reaching contact at ~52%, follow-through hold, eased recovery.
                    float coil=Mathf.Sin(Mathf.Clamp01(t/.40f)*Mathf.PI*.5f)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.40f,.50f,t)));
                    float hitCurve=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.40f,.52f,t))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.64f,1,t)));
                    float act=action?hitCurve-.3f*coil:0;
                    if(name=="Attack"||name=="Cast"){float wind=Mathf.Sin(Mathf.Clamp01(t/.40f)*Mathf.PI*.5f);float strike=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.40f,.58f,t));float recover=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.66f,1,t));float sweep=(-30*wind+80*strike)*recover;float lift=Mathf.Sin(Mathf.Clamp01(t/.50f)*Mathf.PI)*recover*(name=="Cast"?1.3f:1f);
                        if(b==1)angle=new Vector3(8*strike*recover,sweep*.45f,0);if(b==2)angle=new Vector3(-8*lift,sweep*.55f,0);if(b==5)angle+=new Vector3(-55*lift,30*lift,-20*strike*recover);if(b==6)angle+=new Vector3(35*strike*recover,0,-15*lift);if(b==4)angle=new Vector3(0,-sweep*.45f,0);if(b==17)angle=new Vector3(4*strike*recover,0,0);
                    }else{if(b==1)angle.x=Mathf.Sin(t*Mathf.PI*2)*1.3f;if(name=="Walk"&&(b==11||b==14))angle.x=Mathf.Sin(t*Mathf.PI*4+(b==14?Mathf.PI:0))*12;}
                    if(profile.robe){
                        float gesture=act*(name=="Cast"?1:.7f);
                        angle=Vector3.zero;float rest=id=="liubei"?65:30;
                        if(b==5)angle=new Vector3(-8-gesture*24,gesture*8,rest-gesture*15);
                        if(b==8)angle=new Vector3(-5-gesture*18,-gesture*8,-rest+gesture*12);
                        if(b==6)angle=new Vector3(-10-gesture*22,0,0);if(b==9)angle=new Vector3(-7-gesture*15,0,0);
                        if(b==1)angle=new Vector3(Mathf.Sin(t*Mathf.PI*2)*1.0f,gesture*5,0);if(b==4)angle=new Vector3(-gesture*5,-gesture*5,0);
                    }else if(!profile.polearm){
                        float gesture=act;
                        if(b==5)angle=new Vector3(-15-gesture*(name=="Cast"?45:30),gesture*15,25);
                        if(b==6)angle=new Vector3(-25-gesture*25,0,-8);if(b==8)angle=new Vector3(0,0,-20);
                    }else if(id=="zhaoyun"&&(name=="Attack"||name=="Cast")){
                        float thrust=act;if(b==5)angle=new Vector3(-12-thrust*30,0,55-thrust*12);if(b==6)angle=new Vector3(-35+thrust*28,0,-8);if(b==2)angle=new Vector3(thrust*12,0,0);
                    }else if(id=="zhangfei"&&name=="Cast"){
                        float roar=Mathf.Max(0,act);if(b==2)angle=new Vector3(-roar*15,roar*8,0);if(b==5)angle=new Vector3(-12-roar*25,0,55);if(b==4)angle=new Vector3(-roar*12,0,0);
                    }
                    angle+=Secondary(name,b,t,coil,hitCurve);
                    keys.Add(new Keyframe(t*duration,angle[axis]));
                }
                var curve=new AnimationCurve(keys.ToArray());for(int i=0;i<curve.length;i++)AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);clip.SetCurve(AnimationUtility.CalculateTransformPath(bones[b],root),typeof(Transform),"localEulerAnglesRaw."+"xyz"[axis],curve);
            }
            // Weight shift: the pelvis dips into a lunge on the strike and bobs with each step.
            var hipsPath=AnimationUtility.CalculateTransformPath(bones[0],root);var hipsRest=bones[0].localPosition;
            for(int axis=0;axis<3;axis++){var keys=new List<Keyframe>();for(int i=0;i<=24;i++){float t=i/24f;var offset=Vector3.zero;
                    if(name=="Walk")offset.y=-Mathf.Abs(Mathf.Sin(t*Mathf.PI*2))*.06f;
                    else if(name=="Idle")offset.y=Mathf.Sin(t*Mathf.PI*2)*.012f;
                    else{float coil=Mathf.Sin(Mathf.Clamp01(t/.40f)*Mathf.PI*.5f)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.40f,.50f,t)));float hit=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.40f,.52f,t))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.64f,1,t)));offset=new Vector3(0,-.05f*coil-.12f*hit,-.05f*coil+.16f*hit);}
                    keys.Add(new Keyframe(t*duration,hipsRest[axis]+offset[axis]));}
                var curve=new AnimationCurve(keys.ToArray());for(int i=0;i<curve.length;i++)AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);clip.SetCurve(hipsPath,typeof(Transform),"m_LocalPosition."+"xyz"[axis],curve);}
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=name=="Idle"||name=="Walk";AnimationUtility.SetAnimationClipSettings(clip,settings);return clip;
        }
        // Overlapping motion added on top of each profile's pose: breathing, counter-rotation, stepping legs and a lunge.
        static Vector3 Secondary(string name,int b,float t,float coil,float hit){
            float w=t*Mathf.PI*2;
            if(name=="Idle"){
                if(b==1)return new Vector3(Mathf.Sin(w)*1.4f,0,0);if(b==2)return new Vector3(Mathf.Sin(w+.6f)*1.6f,0,Mathf.Sin(w*.5f)*.8f);
                if(b==4)return new Vector3(Mathf.Sin(w+1.2f)*1.5f,Mathf.Sin(w*.5f)*3,0);if(b==0)return new Vector3(0,Mathf.Sin(w*.5f)*1.5f,Mathf.Sin(w)*.8f);
                if(b==17)return new Vector3(Mathf.Sin(w+2)*1.2f,0,0);return Vector3.zero;
            }
            if(name=="Walk"){
                float step=Mathf.Sin(w*2);
                if(b==12)return new Vector3(Mathf.Max(0,-Mathf.Sin(w*2))*28,0,0);if(b==15)return new Vector3(Mathf.Max(0,Mathf.Sin(w*2))*28,0,0);
                if(b==13)return new Vector3(-step*8,0,0);if(b==16)return new Vector3(step*8,0,0);
                if(b==0)return new Vector3(0,step*5,0);if(b==2)return new Vector3(0,-step*6,0);if(b==4)return new Vector3(0,step*2,0);
                if(b==17)return new Vector3(Mathf.Sin(w*2+1)*4,0,0);return Vector3.zero;
            }
            // Attack and Cast: coil the hips away, then drive the hips and front leg through the strike.
            if(b==0)return new Vector3(4*hit,-14*coil+12*hit,0);
            if(b==11)return new Vector3(-22*hit+6*coil,0,0);if(b==12)return new Vector3(26*hit,0,0);
            if(b==14)return new Vector3(14*hit-4*coil,0,0);if(b==15)return new Vector3(10*hit,0,0);
            if(b==4)return new Vector3(5*coil-4*hit,10*coil-6*hit,0);
            if(b==17)return new Vector3(-6*hit+3*coil,0,0);
            return Vector3.zero;
        }
        static void RenderRig(GameObject root,string name){
            HeroLighting.Apply();
            var drape=root.GetComponent<RecoveredSleeveDrape>();if(drape)drape.Solve();
            var skin=root.GetComponent<SkinnedMeshRenderer>();var baked=new Mesh();skin.BakeMesh(baked);var visible=new GameObject("Evaluated pose",typeof(MeshFilter),typeof(MeshRenderer));visible.GetComponent<MeshFilter>().sharedMesh=baked;visible.GetComponent<MeshRenderer>().sharedMaterial=skin.sharedMaterial;skin.enabled=false;
            if(name=="Idle-0.25"){
                var rest=skin.sharedMesh.vertices;var deformed=baked.vertices;var tri=skin.sharedMesh.triangles;var bw=skin.sharedMesh.boneWeights;var lines=new List<string>();
                for(int i=0;i<tri.Length;i+=3)for(int e=0;e<3;e++){int a=tri[i+e],b=tri[i+(e+1)%3];if(Vector3.Distance(deformed[a],deformed[b])>.5f&&Vector3.Distance(rest[a],rest[b])<.12f)lines.Add(a+" "+rest[a]+" bone="+bw[a].boneIndex0+" w="+bw[a].weight0+" -> "+b+" "+rest[b]+" bone="+bw[b].boneIndex0+" w="+bw[b].weight0);}
                File.WriteAllLines("QA/"+id+"-stretched-edges.txt",lines.Take(50));
            }
            var light=new GameObject("key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(35,-30,0);RenderSettings.ambientLight=new Color(.6f,.6f,.65f);var camera=new GameObject("camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.10f,.14f);camera.orthographic=true;camera.orthographicSize=3.5f;camera.transform.position=new Vector3(5,3.5f,9);camera.transform.LookAt(new Vector3(0,2.4f,0));Save(camera,"QA/"+id+"-rig-"+name+".png");Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(visible);Object.DestroyImmediate(baked);skin.enabled=true;}
        public static void Preview()
        {
            AssetDatabase.Refresh();
            var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+id+".obj"));
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+id+"-albedo.jpg");mat.SetFloat("_Smoothness",.24f);mat.SetFloat("_Cull",0);
            foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterial=mat;
            var bounds=model.GetComponentsInChildren<Renderer>()[0].bounds;float scale=4.4f/bounds.size.y;model.transform.localScale*=scale;model.transform.position=new Vector3(-bounds.center.x*scale,-bounds.min.y*scale,-bounds.center.z*scale);
            var light=new GameObject("Preview key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(35,-30,0);RenderSettings.ambientLight=new Color(.6f,.6f,.65f);
            var camera=new GameObject("Preview camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.10f,.14f);camera.orthographic=true;camera.orthographicSize=2.7f;
            for(int i=0;i<4;i++){float angle=i*90*Mathf.Deg2Rad;camera.transform.position=new Vector3(Mathf.Sin(angle)*9,2.6f,Mathf.Cos(angle)*9);camera.transform.LookAt(new Vector3(0,2.2f,0));Save(camera,"QA/"+id+"-source-"+i+".png");}
            Debug.Log("RECOVERED_PREVIEW_READY");Object.DestroyImmediate(model);Object.DestroyImmediate(mat);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(light.gameObject);
        }
        static void Save(Camera camera,string path){var target=new RenderTexture(768,768,24);camera.targetTexture=target;camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(768,768,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,768,768),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(texture);Object.DestroyImmediate(target);}
    }
}
