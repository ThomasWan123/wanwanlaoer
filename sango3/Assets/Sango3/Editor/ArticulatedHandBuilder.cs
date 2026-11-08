using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Sango3.Editor
{
    public static class ArticulatedHandBuilder
    {
        const string Source="Assets/Sango3/Characters/Source/Superhero_Male_FullBody.fbx";
        public struct Grip {public Vector3 center,axis;}
        struct Corner {public Vector3 p,n;public Vector2 uv;public static Corner Lerp(Corner a,Corner b,float t)=>new Corner{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t)};}
        public static void TrimOriginal(Mesh mesh,Vector3 wrist,Vector3 direction){
            var input=mesh.vertices;var ns=mesh.normals;var uv=mesh.uv;var triangles=mesh.triangles;var output=new List<int>();var vertices=new List<Vector3>();var normals=new List<Vector3>();var uvs=new List<Vector2>();var unique=new Dictionary<(Vector3,Vector3,Vector2),int>();
            float Distance(Vector3 p)=>Vector3.Dot(new Vector3(Mathf.Abs(p.x),p.y,p.z)-wrist,direction)+.025f;
            for(int i=0;i<triangles.Length;i+=3){
                var polygon=new List<Corner>();for(int k=0;k<3;k++){int j=triangles[i+k];polygon.Add(new Corner{p=input[j],n=ns[j],uv=uv[j]});}
                var center=(polygon[0].p+polygon[1].p+polygon[2].p)/3;var q=new Vector3(Mathf.Abs(center.x),center.y,center.z)-wrist;float along=Vector3.Dot(q,direction);
                if(along>-.30f&&along<.8f&&(q-direction*along).magnitude<.43f&&Mathf.Abs(center.x)>wrist.x*.78f){
                    var clipped=new List<Corner>();for(int k=0;k<polygon.Count;k++){var a=polygon[k];var b=polygon[(k+1)%polygon.Count];float da=Distance(a.p),db=Distance(b.p);if(da<=0)clipped.Add(a);if((da<=0)!=(db<=0))clipped.Add(Corner.Lerp(a,b,da/(da-db)));}polygon=clipped;
                }
                if(polygon.Count<3)continue;var polyIndices=new List<int>();foreach(var c in polygon){var key=(c.p,c.n,c.uv);if(!unique.TryGetValue(key,out int index)){index=vertices.Count;unique.Add(key,index);vertices.Add(c.p);normals.Add(c.n);uvs.Add(c.uv);}polyIndices.Add(index);}for(int k=1;k<polygon.Count-1;k++){output.Add(polyIndices[0]);output.Add(polyIndices[k]);output.Add(polyIndices[k+1]);}
            }
            mesh.Clear();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uvs);mesh.SetTriangles(output,0);mesh.RecalculateBounds();
        }
        public static Grip Attach(GameObject root,Transform targetWrist,Vector3 direction,bool right,bool grasp,string folder){
            var importer=(ModelImporter)AssetImporter.GetAtPath(Source);if(!importer.isReadable){importer.isReadable=true;importer.SaveAndReimport();}
            var source=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source));var animator=source.GetComponent<Animator>();var renderer=source.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name=="SuperHero_Male");
            HumanBodyBones Bone(HumanBodyBones r,HumanBodyBones l)=>right?r:l;
            var wrist=animator.GetBoneTransform(Bone(HumanBodyBones.RightHand,HumanBodyBones.LeftHand));var middle=animator.GetBoneTransform(Bone(HumanBodyBones.RightMiddleProximal,HumanBodyBones.LeftMiddleProximal));var mid=animator.GetBoneTransform(Bone(HumanBodyBones.RightMiddleIntermediate,HumanBodyBones.LeftMiddleIntermediate));var distal=animator.GetBoneTransform(Bone(HumanBodyBones.RightMiddleDistal,HumanBodyBones.LeftMiddleDistal));var index=animator.GetBoneTransform(Bone(HumanBodyBones.RightIndexProximal,HumanBodyBones.LeftIndexProximal));var little=animator.GetBoneTransform(Bone(HumanBodyBones.RightLittleProximal,HumanBodyBones.LeftLittleProximal));
            if(!wrist||!middle||!mid||!distal||!index||!little)throw new Exception("Complete finger bones required");
            var sourceDirection=(middle.position-wrist.position).normalized;var sourceSide=(index.position-little.position).normalized;var sourceNormal=Vector3.Cross(sourceDirection,sourceSide).normalized*(right?1:-1);var normal=Vector3.ProjectOnPlane(Vector3.forward,direction).normalized;
            var rotation=Quaternion.LookRotation(normal,direction)*Quaternion.Inverse(Quaternion.LookRotation(sourceNormal,sourceDirection));const float scale=1.9f;
            var handler=new HumanPoseHandler(animator.avatar,source.transform);var pose=new HumanPose();handler.GetHumanPose(ref pose);var names=HumanTrait.MuscleName;string prefix=right?"Right ":"Left ";
            for(int i=0;i<names.Length;i++)if(names[i].StartsWith(prefix)&&(names[i].Contains("Thumb")||names[i].Contains("Index")||names[i].Contains("Middle")||names[i].Contains("Ring")||names[i].Contains("Little")))pose.muscles[i]=names[i].Contains("Spread")?0:grasp?(names[i].Contains("Thumb")?-.55f:-.85f):.35f;
            handler.SetHumanPose(ref pose);var origin=wrist.position;Vector3 Map(Vector3 p)=>targetWrist.position+rotation*(p-origin)*scale;
            var sourceBones=renderer.bones;var handSources=wrist.GetComponentsInChildren<Transform>();var mapping=new Dictionary<Transform,Transform>{{wrist,targetWrist}};
            foreach(var bone in handSources){if(bone==wrist)continue;var target=new GameObject("Grip_"+bone.name).transform;target.SetParent(mapping[bone.parent],false);target.position=Map(bone.position);target.rotation=rotation*bone.rotation;mapping.Add(bone,target);}
            var targetBones=mapping.Values.ToArray();var boneMap=new Dictionary<int,int>();for(int i=0;i<sourceBones.Length;i++)boneMap[i]=mapping.TryGetValue(sourceBones[i],out var mapped)?Array.IndexOf(targetBones,mapped):0;
            var sourceMesh=renderer.sharedMesh;var bw=sourceMesh.boneWeights;var baked=new Mesh();renderer.BakeMesh(baked);var input=baked.vertices;var normals=baked.normals;var uv=sourceMesh.uv;
            bool Keep(int n){var w=bw[n];float total=0;if(mapping.ContainsKey(sourceBones[w.boneIndex0]))total+=w.weight0;if(mapping.ContainsKey(sourceBones[w.boneIndex1]))total+=w.weight1;if(mapping.ContainsKey(sourceBones[w.boneIndex2]))total+=w.weight2;if(mapping.ContainsKey(sourceBones[w.boneIndex3]))total+=w.weight3;return total>.01f;}
            var ids=new Dictionary<int,int>();var vertices=new List<Vector3>();var outputNormals=new List<Vector3>();var uvs=new List<Vector2>();var weights=new List<BoneWeight>();var triangles=new List<int>();
            int Add(int n){if(ids.TryGetValue(n,out int found))return found;int next=vertices.Count;ids.Add(n,next);var point=Map(renderer.transform.TransformPoint(input[n]));float along=Vector3.Dot(point-targetWrist.position,direction);if(along<-.08f)point+=direction*(-.08f-along);vertices.Add(point);outputNormals.Add(rotation*renderer.transform.TransformDirection(normals[n]).normalized);uvs.Add(uv[n]);var w=bw[n];w.boneIndex0=boneMap[w.boneIndex0];w.boneIndex1=boneMap[w.boneIndex1];w.boneIndex2=boneMap[w.boneIndex2];w.boneIndex3=boneMap[w.boneIndex3];if(along<-.08f)w=new BoneWeight{boneIndex0=0,weight0=1};weights.Add(w);return next;}
            foreach(var sub in Enumerable.Range(0,sourceMesh.subMeshCount)){var tri=sourceMesh.GetTriangles(sub);for(int i=0;i<tri.Length;i+=3)if(Keep(tri[i])&&Keep(tri[i+1])&&Keep(tri[i+2])){triangles.Add(Add(tri[i]));triangles.Add(Add(tri[i+1]));triangles.Add(Add(tri[i+2]));}}
            if(vertices.Count<100)throw new Exception("Incomplete extracted hand");var mesh=new Mesh{name=right?"Articulated right hand":"Articulated left hand"};mesh.SetVertices(vertices);mesh.SetNormals(outputNormals);mesh.SetUVs(0,uvs);mesh.SetTriangles(triangles,0);mesh.boneWeights=weights.ToArray();mesh.bindposes=targetBones.Select(b=>b.worldToLocalMatrix*root.transform.localToWorldMatrix).ToArray();mesh.RecalculateBounds();mesh.RecalculateTangents();string meshPath=folder+(right?"right":"left")+"-hand.asset";RecoveredHeroBuilder.SaveAsset(mesh,meshPath);
            string materialPath=folder+"hand-skin.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,materialPath);}material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sango3/Characters/Source/BaseTextures/T_Superhero_Male_Dark.png");material.SetFloat("_Smoothness",.30f);material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sango3/Characters/Source/BaseTextures/Normals Unity - Godot/T_Superhero_Male_Normal.png"));material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",.6f);EditorUtility.SetDirty(material);
            var view=new GameObject(right?"Articulated right hand":"Articulated left hand").AddComponent<SkinnedMeshRenderer>();view.transform.SetParent(root.transform,false);view.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);view.sharedMaterial=material;view.bones=targetBones;view.rootBone=targetWrist;view.localBounds=view.sharedMesh.bounds;view.updateWhenOffscreen=true;
            var a=mid.position-middle.position;var b=distal.position-middle.position;var cross=Vector3.Cross(a,b);var center=cross.sqrMagnitude>1e-9f?middle.position+(a.sqrMagnitude*Vector3.Cross(b,cross)+b.sqrMagnitude*Vector3.Cross(cross,a))/(2*cross.sqrMagnitude):(middle.position+distal.position)*.5f;
            var axis=rotation*sourceSide;if(axis.y<0)axis=-axis;var grip=new Grip{center=Map(center)-targetWrist.position,axis=axis};handler.Dispose();Object.DestroyImmediate(baked);Object.DestroyImmediate(source);return grip;
        }
    }
}
