using System.IO;
using UnityEditor;
using UnityEngine;
namespace Sango3.Editor
{
    public static class HeroLightingBuilder
    {
        public static void Review(){Build();RecoveredHeroBuilder.RenderAccepted();HandStudy.Review();}
        public static void Build(){
            const string path="Assets/Sango3/Resources/Lighting/BattleSky.asset";Directory.CreateDirectory("Assets/Sango3/Resources/Lighting");var map=AssetDatabase.LoadAssetAtPath<Cubemap>(path);bool create=!map;if(create)map=new Cubemap(128,TextureFormat.RGBAHalf,true){name="Battle sky reflection"};
            var sun=new Vector3(-.4f,.75f,.45f).normalized;
            for(int face=0;face<6;face++){var pixels=new Color[128*128];for(int y=0;y<128;y++)for(int x=0;x<128;x++){float u=(x+.5f)/64-1,v=(y+.5f)/64-1;Vector3 d=face==0?new Vector3(1,-v,-u):face==1?new Vector3(-1,-v,u):face==2?new Vector3(u,1,v):face==3?new Vector3(u,-1,-v):face==4?new Vector3(u,-v,1):new Vector3(-u,-v,-1);d.Normalize();
                    Color color=d.y>0?Color.Lerp(new Color(.70f,.66f,.56f),new Color(.20f,.32f,.48f),Mathf.Sqrt(d.y)):Color.Lerp(new Color(.36f,.33f,.27f),new Color(.055f,.05f,.035f),Mathf.Sqrt(-d.y));
                    color+=new Color(1,.87f,.68f)*Mathf.Pow(Mathf.Max(0,Vector3.Dot(d,sun)),220)*3.5f;color.a=1;pixels[y*128+x]=color;
                }map.SetPixels(pixels,(CubemapFace)face);}
            map.Apply(true,false);map.wrapMode=TextureWrapMode.Clamp;map.filterMode=FilterMode.Trilinear;if(create)AssetDatabase.CreateAsset(map,path);else EditorUtility.SetDirty(map);AssetDatabase.SaveAssets();Debug.Log("HERO_REFLECTION_READY");
        }
    }
}
