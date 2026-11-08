using System.IO;
using UnityEditor;
using UnityEngine;
namespace Sango3.Editor
{
    public static class RecoveredSurfaceMap
    {
        // First-pass material separation, not a replacement for authored PBR textures.
        public static Texture2D Build(Mesh mesh,Texture2D albedo,bool robe,Vector3 wrist,string path){
            const int size=512;var target=RenderTexture.GetTemporary(size,size,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);var previous=RenderTexture.active;var readable=new Texture2D(size,size,TextureFormat.RGBA32,false,true);
            try{Graphics.Blit(albedo,target);RenderTexture.active=target;readable.ReadPixels(new Rect(0,0,size,size),0,0);readable.Apply();}finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);}
            var colors=new Color[size*size];var covered=new bool[colors.Length];for(int i=0;i<colors.Length;i++)colors[i]=new Color(0,0,0,.20f);
            var uv=mesh.uv;var points=mesh.vertices;var triangles=mesh.triangles;
            float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
            for(int t=0;t<triangles.Length;t+=3){int ia=triangles[t],ib=triangles[t+1],ic=triangles[t+2];var a=uv[ia];var b=uv[ib];var c=uv[ic];float area=Cross(b-a,c-a);if(Mathf.Abs(area)<1e-10f)continue;
                int minX=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x,b.x,c.x)*size),0,size-1),maxX=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x,b.x,c.x)*size),0,size-1),minY=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y,b.y,c.y)*size),0,size-1),maxY=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y,b.y,c.y)*size),0,size-1);
                for(int y=minY;y<=maxY;y++)for(int x=minX;x<=maxX;x++){var q=new Vector2((x+.5f)/size,(y+.5f)/size);float wb=Cross(q-a,c-a)/area,wc=Cross(b-a,q-a)/area,wa=1-wb-wc;if(wa<0||wb<0||wc<0)continue;
                    var p=points[ia]*wa+points[ib]*wb+points[ic]*wc;var tint=readable.GetPixel(x,y);float bright=(tint.r+tint.g+tint.b)/3;
                    bool face=Mathf.Abs(p.x)<.38f&&p.y>3.32f&&p.y<4.02f;bool hand=Vector3.Distance(new Vector3(Mathf.Abs(p.x),p.y,p.z),wrist)<.44f;
                    bool gold=tint.r>tint.g*.98f&&tint.g>tint.b*1.22f&&bright>.025f;
                    bool steel=Mathf.Max(tint.r,tint.g,tint.b)-Mathf.Min(tint.r,tint.g,tint.b)<.035f&&bright>.04f&&bright<.40f;
                    float metal=0,smooth=robe?.24f:.18f;
                    if(face||hand)smooth=.30f;else if((!robe||p.y>4.02f)&&gold){metal=.62f;smooth=.46f;}else if(!robe&&steel){metal=.35f;smooth=.34f;}
                    int index=y*size+x;colors[index]=new Color(metal,0,0,smooth);covered[index]=true;
                }
            }
            // Pad UV island borders so mip filtering does not introduce black seams.
            for(int pass=0;pass<3;pass++){var next=(Color[])colors.Clone();var filled=(bool[])covered.Clone();for(int y=1;y<size-1;y++)for(int x=1;x<size-1;x++){int i=y*size+x;if(covered[i])continue;foreach(int n in new[]{i-1,i+1,i-size,i+size})if(covered[n]){next[i]=colors[n];filled[i]=true;break;}}colors=next;covered=filled;}
            readable.SetPixels(colors);readable.Apply();File.WriteAllBytes(path,readable.EncodeToPNG());Object.DestroyImmediate(readable);AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=false;importer.maxTextureSize=size;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
