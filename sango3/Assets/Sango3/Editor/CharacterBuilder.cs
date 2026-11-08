using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using Object=UnityEngine.Object;
namespace Sango3.Editor
{
    public static class CharacterBuilder
    {
        const string Folder="Assets/Sango3/Characters/Source/";
        const string Output="Assets/Sango3/Resources/Heroes/";
        public static void Build()
        {
            Directory.CreateDirectory(Output);Directory.CreateDirectory("Assets/Sango3/Resources/Skills");AssetDatabase.Refresh();
            var glowPath="Assets/Sango3/Resources/Skills/Glow.mat";if(!AssetDatabase.LoadAssetAtPath<Material>(glowPath))AssetDatabase.CreateAsset(new Material(Shader.Find("Sango3/SkillGlow")),glowPath);
            foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Folder.TrimEnd('/')})){
                var path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(path.Contains("Normals-UnrealEngine"))continue;importer.textureType=path.Contains("Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;importer.maxTextureSize=1024;importer.SaveAndReimport();
            }
            var animImporter=(ModelImporter)AssetImporter.GetAtPath(Folder+"UAL1_Standard.fbx");var clips=animImporter.defaultClipAnimations;
            foreach(var clip in clips){clip.loopTime=clip.name.Contains("Loop")||clip.name.Contains("Idle");clip.lockRootRotation=true;clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;}
            animImporter.clipAnimations=clips;animImporter.SaveAndReimport();
            var motions=AssetDatabase.LoadAllAssetsAtPath(Folder+"UAL1_Standard.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            for(int hero=0;hero<6;hero++){
                bool mage=hero==1||hero==3;var source=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+(mage?"Male_Peasant.fbx":"Male_Ranger.fbx"));var model=Object.Instantiate(source);model.name="Animated warrior";
                var animator=model.GetComponent<Animator>();if(!animator||!animator.avatar||!animator.avatar.isValid)throw new Exception("Invalid warrior avatar");
                var bones=model.GetComponentsInChildren<Transform>().GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
                AddHead(model.transform,bones);
                AddHair(model.transform,animator,hero);
                foreach(var renderer in model.GetComponentsInChildren<Renderer>()){
                    var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++)materials[i]=CreateMaterial(materials[i]?materials[i].name:"skin",hero);renderer.sharedMaterials=materials;
                    if(renderer.name.Contains("Hood")&&hero!=1)renderer.gameObject.SetActive(false);
                }
                DynastyArmorBuilder.Dress(model,animator,hero);
                var controllerPath=Output+"Hero"+hero+".controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                var layers=controller.layers;layers[0].iKPass=true;controller.layers=layers;var machine=controller.layers[0].stateMachine;foreach(var child in machine.states)machine.RemoveState(child.state);
                AddState(machine,"Idle",motions,"Idle_Loop",1);AddState(machine,"Walk",motions,"Walk_Loop",1);
                AddState(machine,"Attack",motions,mage?"Spell_Simple_Shoot":"Sword_Attack",mage?1:2.35f);
                var cast=AddState(machine,"Cast",motions,mage?"Spell_Simple_Enter":"Sword_Attack",mage?1:1.8f);var release=AddState(machine,"Release",motions,"Spell_Simple_Shoot",1);
                var transition=cast.AddTransition(release);transition.mute=!mage;transition.hasExitTime=true;transition.exitTime=1;transition.duration=.06f;
                animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
                var root=new GameObject("Hero "+BattleRules.Names[hero]);model.transform.SetParent(root.transform,false);model.transform.localPosition=Vector3.zero;
                // Human avatars expose reliable attachment points even when source bone names differ.
                var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);if(!hand)throw new Exception("Right hand missing");
                HeroArt.AttachWeapon(hand,BattleRules.Names[hero]);if(hero==0||hero==2||hero==4){var grip=model.AddComponent<PolearmGrip>();grip.shaft=hand.Find("Weapon mount/Spear");}
                int resource=0;foreach(var filter in hand.GetComponentsInChildren<MeshFilter>())if(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(filter.sharedMesh))){var path=Output+hero+"WeaponMesh"+(resource++)+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(filter.sharedMesh,existing);filter.sharedMesh=existing;}else AssetDatabase.CreateAsset(filter.sharedMesh,path);}
                resource=0;foreach(var renderer in hand.GetComponentsInChildren<MeshRenderer>())if(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(renderer.sharedMaterial))){var path=Output+hero+"WeaponMaterial"+(resource++)+".mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing){EditorUtility.CopySerialized(renderer.sharedMaterial,existing);renderer.sharedMaterial=existing;}else{var copy=new Material(renderer.sharedMaterial);AssetDatabase.CreateAsset(copy,path);renderer.sharedMaterial=copy;}}
                var bounds=new Bounds();bool hasBounds=false;foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>()){if(!r.gameObject.activeSelf||!r.name.StartsWith("Male_"))continue;if(!hasBounds){bounds=r.bounds;hasBounds=true;}else bounds.Encapsulate(r.bounds);}
                float scale=2.1f;model.transform.localScale*=scale;model.transform.localPosition-=Vector3.up*bounds.min.y*scale;
                model.transform.localRotation=Quaternion.Euler(0,180,0);
                PrefabUtility.SaveAsPrefabAsset(root,Output+BattleRules.Portraits[hero]+".prefab");Object.DestroyImmediate(root);
            }
            var enemy=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Output+"zhangfei.prefab"));enemy.name="Armored infantry";
            foreach(var part in enemy.GetComponentsInChildren<Transform>())if(part.name=="Long beard"||part.name=="Silk mantle"||part.name=="Hair_Long"||part.name=="Hair_Beard")part.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(enemy,Output+"enemy.prefab");Object.DestroyImmediate(enemy);
            AssetDatabase.SaveAssets();Debug.Log("SANGO3_RIGGED_HEROES_READY");
        }
        static AnimatorState AddState(AnimatorStateMachine machine,string name,AnimationClip[] clips,string suffix,float speed)
        {
            var clip=clips.FirstOrDefault(c=>c.name.EndsWith("|"+suffix));if(!clip)throw new Exception("Missing animation "+suffix);var state=machine.AddState(name);state.motion=clip;state.speed=speed;return state;
        }
        static Material CreateMaterial(string source,int hero)
        {
            string key=source.Replace(" (Instance)","");var path=Output+hero+"_"+key+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            string texture,normal;
            if(key.Contains("Ranger")){texture="Ranger/T_Ranger_BaseColor.png";normal="Ranger/T_Ranger_Normal.png";}
            else if(key.Contains("Peasant")){texture="Peasant/T_Peasant_BaseColor.png";normal="Peasant/T_Peasant_Normal.png";}
            else if(key.Contains("Eyes")){texture="BaseTextures/T_Eye_Brown.png";normal="BaseTextures/T_Eye_Normal.png";}
            else if(key.Contains("Hair")){texture="BaseTextures/T_Hair_1_BaseColor.png";normal="BaseTextures/T_Hair_1_Normal.png";}
            else if(key.Contains("Superhero")||key.Contains("SuperHero")){texture="BaseTextures/T_Superhero_Male_Dark.png";normal="BaseTextures/T_Superhero_Male_Normal.png";}
            else {texture="Ranger/Base Chars/T_Regular_Male_Dark_BaseColor.png";normal="Ranger/Base Chars/T_Regular_Male_Normal.png";}
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+texture);if(!material.mainTexture)throw new Exception("Missing texture "+texture);
            var bump=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+normal);if(bump){material.SetTexture("_BumpMap",bump);material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",.7f);}
            bool outfit=key.Contains("Ranger")||key.Contains("Peasant");material.color=outfit?Color.Lerp(Color.white,BattleRules.Colors[hero],.28f):Color.white;if(key.Contains("Hair")){material.color=new Color(.09f,.065f,.045f);material.SetFloat("_Cull",0);material.SetFloat("_AlphaClip",1);material.EnableKeyword("_ALPHATEST_ON");}material.SetFloat("_Smoothness",outfit?.28f:.2f);material.enableInstancing=true;EditorUtility.SetDirty(material);return material;
        }
        static void AddHair(Transform model,Animator animator,int hero)
        {
            var head=animator.GetBoneTransform(HumanBodyBones.Head);
            var face=model.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name=="SuperHero_Male");var vertices=face.sharedMesh.vertices;float top=face.sharedMesh.triangles.Max(i=>face.transform.TransformPoint(vertices[i]).y);
            foreach(var file in (hero==0||hero==2||hero==3||hero==5)?new[]{"Hair_Long.fbx","Hair_Beard.fbx"}:new[]{"Hair_Long.fbx"}){
                var hair=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+file),model,false);hair.name=file.Replace(".fbx","");
                var hairBounds=hair.GetComponentsInChildren<Renderer>()[0].bounds;float desiredTop=top+(file.Contains("Beard")?-.20f:.025f);hair.transform.position+=Vector3.up*(desiredTop-hairBounds.max.y);
                var extraAnimator=hair.GetComponent<Animator>();if(extraAnimator)Object.DestroyImmediate(extraAnimator);
                hair.transform.SetParent(head,true);
            }
        }
        static void AddHead(Transform target,Dictionary<string,Transform> bones)
        {
            var source=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Superhero_Male_FullBody.fbx"));var sourceAnimator=source.GetComponent<Animator>();var headBone=sourceAnimator.GetBoneTransform(HumanBodyBones.Head);var neckBone=sourceAnimator.GetBoneTransform(HumanBodyBones.Neck);
            foreach(var renderer in source.GetComponentsInChildren<SkinnedMeshRenderer>()){
                Mesh mesh=renderer.sharedMesh;
                if(renderer.name=="SuperHero_Male"){
                    var filtered=Object.Instantiate(mesh);var weights=mesh.boneWeights;var headIndices=new HashSet<int>();for(int i=0;i<renderer.bones.Length;i++)if(renderer.bones[i]==headBone||renderer.bones[i]==neckBone||renderer.bones[i].IsChildOf(headBone))headIndices.Add(i);
                    bool[] keep=new bool[weights.Length];for(int i=0;i<weights.Length;i++){var w=weights[i];float weight=(headIndices.Contains(w.boneIndex0)?w.weight0:0)+(headIndices.Contains(w.boneIndex1)?w.weight1:0)+(headIndices.Contains(w.boneIndex2)?w.weight2:0)+(headIndices.Contains(w.boneIndex3)?w.weight3:0);keep[i]=weight>.25f;}
                    for(int sub=0;sub<mesh.subMeshCount;sub++){var input=mesh.GetTriangles(sub);var output=new List<int>();for(int i=0;i<input.Length;i+=3)if(keep[input[i]]&&keep[input[i+1]]&&keep[input[i+2]]){output.Add(input[i]);output.Add(input[i+1]);output.Add(input[i+2]);}filtered.SetTriangles(output,sub);}
                    var path=Output+"HeadMesh.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(filtered,existing);Object.DestroyImmediate(filtered);mesh=existing;}else{AssetDatabase.CreateAsset(filtered,path);mesh=filtered;}
                }
                var go=new GameObject(renderer.name,typeof(SkinnedMeshRenderer));go.transform.SetParent(target,false);go.transform.localPosition=renderer.transform.localPosition;go.transform.localRotation=renderer.transform.localRotation;go.transform.localScale=renderer.transform.localScale;
                var skin=go.GetComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.sharedMaterials=renderer.sharedMaterials;skin.bones=renderer.bones.Select(b=>bones.TryGetValue(b.name,out var found)?found:target).ToArray();skin.rootBone=bones.TryGetValue(renderer.rootBone.name,out var root)?root:target;skin.updateWhenOffscreen=true;
            }
            Object.DestroyImmediate(source);
        }
        public static void Inspect()
        {
            AssetDatabase.Refresh();var report=new System.Text.StringBuilder();
            foreach(var file in new[]{"Male_Ranger.fbx","Male_Peasant.fbx","UAL1_Standard.fbx","Superhero_Male_FullBody.fbx","Hair_Long.fbx","Hair_Beard.fbx"}){
                var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+file);importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.bakeAxisConversion=true;importer.importCameras=false;importer.importLights=false;importer.SaveAndReimport();
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+file);report.AppendLine(file);
                foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())report.AppendLine("MESH "+renderer.name+" vertices="+renderer.sharedMesh.vertexCount+" materials="+string.Join(",",renderer.sharedMaterials.Select(m=>m?m.name:"NULL")));
                foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(Folder+file).OfType<AnimationClip>())report.AppendLine("CLIP "+clip.name+" "+clip.length);
                var animator=model.GetComponent<Animator>();report.AppendLine("AVATAR "+(animator&&animator.avatar?animator.avatar.isValid+" human="+animator.avatar.isHuman:"missing"));
            }
            File.WriteAllText("QA/character-import.txt",report.ToString());Debug.Log("SANGO3_CHARACTER_IMPORT_READY");
        }
    }
}
