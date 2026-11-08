using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Sango3.Editor
{
    // Unoccluded review reels, 30 frames per move second, every move of every playable hero on a plain background:
    //  - shoulder reel: chest-height close-up, front three-quarter (top) and back three-quarter (bottom), shoulder
    //    correctives off (left) and on (right), same light, same frame;
    //  - body reel: whole body with feet and hem, front three-quarter and side, with the cloth follow-through stepped
    //    at 1/30 s as it runs in the game.
    // Frames go to QA/sample-130/reels/<hero>-shoulder|body/; QA/sample-130/encode_reels.sh makes the mp4 files.
    public static partial class GuanyuSample
    {
        public static void RenderReels()
        {
            const string Out=Evidence+"reels/";var only=System.Environment.GetEnvironmentVariable("SANGO3_REEL");if(only==null&&Directory.Exists(Out))Directory.Delete(Out,true);
            foreach(var id in BattleRules.Portraits){if(only!=null&&only!=id)continue;if(Directory.Exists(Out+id+"-shoulder"))Directory.Delete(Out+id+"-shoulder",true);if(Directory.Exists(Out+id+"-body"))Directory.Delete(Out+id+"-body",true);heroId=id;Load();HeroLighting.Apply();ClothFollow.Attach(root);var cloth=root.GetComponent<ClothFollow>();var corrective=root.GetComponent<ShoulderCorrective>();
                var light=new GameObject("key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(40,-35,0);RenderSettings.ambientLight=new Color(.55f,.56f,.6f);
                var camera=new GameObject("camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.10f,.12f,.15f);camera.orthographic=true;
                const int cell=360;var shoulderFrame=new Texture2D(cell*2,cell*2,TextureFormat.RGB24,false);var bodyFrame=new Texture2D(cell*2,cell*2,TextureFormat.RGB24,false);
                Directory.CreateDirectory(Out+id+"-shoulder");Directory.CreateDirectory(Out+id+"-body");int n=0;
                foreach(var move in HeroMoves.For(HeroIndex).All){var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(HeroFolder+move.state+".anim");int frames=Mathf.CeilToInt(move.length*30)+8;
                    for(int f=0;f<frames;f++,n++){float t=Mathf.Min(move.length,f/30f);clip.SampleAnimation(root,t);if(grip)grip.Solve();var drape=root.GetComponent<RecoveredSleeveDrape>();if(drape)drape.Solve();var helperRig=root.GetComponent<ShoulderHelper>();if(helperRig)helperRig.Solve();
                        // Shoulder close-up, off and on.
                        var chest=bones[Chest].position+Vector3.up*.05f;camera.orthographicSize=1.45f;camera.aspect=1;
                        for(int on=0;on<2;on++){if(corrective){corrective.active=on==1;corrective.Solve();}
                            var shown=Bake();
                            camera.transform.position=chest+new Vector3(2.5f,.5f,7.6f);camera.transform.LookAt(chest);Shot(camera,shoulderFrame,on*cell,cell,cell);
                            camera.transform.position=chest+new Vector3(-2.5f,.5f,-7.6f);camera.transform.LookAt(chest);Shot(camera,shoulderFrame,on*cell,0,cell);
                            Clear(shown);}
                        if(corrective){corrective.active=true;corrective.Solve();}
                        if(cloth)cloth.Step(1/30f);
                        // Whole body with feet: front three-quarter (left) and side (right), cell is 2:1 tall.
                        {var shown=Bake();camera.orthographicSize=2.75f;camera.aspect=.5f;var look=new Vector3(0,2.25f,.4f);
                            camera.transform.position=look+new Vector3(4.5f,1.2f,7.5f);camera.transform.LookAt(look);Shot(camera,bodyFrame,0,0,cell,cell*2);
                            camera.transform.position=look+new Vector3(8.6f,.9f,.3f);camera.transform.LookAt(look);Shot(camera,bodyFrame,cell,0,cell,cell*2);Clear(shown);}
                        File.WriteAllBytes(Out+id+"-shoulder/"+n.ToString("0000")+".jpg",shoulderFrame.EncodeToJPG(90));File.WriteAllBytes(Out+id+"-body/"+n.ToString("0000")+".jpg",bodyFrame.EncodeToJPG(90));}}
                Object.DestroyImmediate(shoulderFrame);Object.DestroyImmediate(bodyFrame);Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(root);}
            heroId="guanyu";Debug.Log("SANGO3_REELS_DONE");
        }
        static List<GameObject> Bake()
        {
            var shown=new List<GameObject>();
            foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>()){var baked=new Mesh();r.BakeMesh(baked);var go=new GameObject("pose",typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=baked;go.GetComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;go.transform.SetPositionAndRotation(r.transform.position,r.transform.rotation);shown.Add(go);r.enabled=false;}
            return shown;
        }
        static void Clear(List<GameObject> shown){foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>())r.enabled=true;foreach(var go in shown){Object.DestroyImmediate(go.GetComponent<MeshFilter>().sharedMesh);Object.DestroyImmediate(go);}}
        static void Shot(Camera camera,Texture2D into,int x,int y,int w,int h=-1)
        {
            if(h<0)h=w;var target=RenderTexture.GetTemporary(w,h,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;into.ReadPixels(new Rect(0,0,w,h),x,y);into.Apply(false);RenderTexture.active=null;camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);
        }
    }
}
