using UnityEditor;
using UnityEngine;
namespace Sango3.Editor
{
    public static class NatureBuilder
    {
        const string Folder="Assets/Sango3/Resources/Nature/";
        public static void Prepare()
        {
            AssetDatabase.Refresh();
            foreach(var name in new[]{"Ground.jpg","Rock.jpg","Needles.png","NeedlesAlpha.png"}){var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+name);importer.textureType=TextureImporterType.Default;importer.wrapMode=TextureWrapMode.Repeat;importer.maxTextureSize=1024;importer.SaveAndReimport();}
            var modelImporter=(ModelImporter)AssetImporter.GetAtPath(Folder+"Boulder.fbx");modelImporter.isReadable=true;modelImporter.importCameras=false;modelImporter.importLights=false;modelImporter.SaveAndReimport();
            var normal=(TextureImporter)AssetImporter.GetAtPath(Folder+"GroundNormal.jpg");normal.textureType=TextureImporterType.NormalMap;normal.wrapMode=TextureWrapMode.Repeat;normal.maxTextureSize=1024;normal.SaveAndReimport();
            foreach(var name in new[]{"Needles.png","NeedlesAlpha.png"}){var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+name);importer.isReadable=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
            var diffuse=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"Needles.png");var alpha=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"NeedlesAlpha.png");
            var rgba=new Texture2D(diffuse.width,diffuse.height,TextureFormat.RGBA32,true);var pixels=diffuse.GetPixels();
            for(int y=0;y<rgba.height;y++)for(int x=0;x<rgba.width;x++)pixels[y*rgba.width+x].a=alpha.GetPixelBilinear(x/(float)rgba.width,y/(float)rgba.height).r;
            rgba.SetPixels(pixels);rgba.Apply();System.IO.File.WriteAllBytes(Folder+"Foliage.png",rgba.EncodeToPNG());Object.DestroyImmediate(rgba);AssetDatabase.Refresh();
            var foliageImporter=(TextureImporter)AssetImporter.GetAtPath(Folder+"Foliage.png");foliageImporter.textureType=TextureImporterType.Default;foliageImporter.alphaIsTransparency=true;foliageImporter.SaveAndReimport();
            var foliage=Material("FoliageMat",Color.white,"Foliage.png");foliage.mainTextureScale=new Vector2(.33f,.49f);foliage.mainTextureOffset=new Vector2(.67f,.51f);foliage.SetFloat("_AlphaClip",1);foliage.SetFloat("_Cutoff",.42f);foliage.EnableKeyword("_ALPHATEST_ON");foliage.SetFloat("_Cull",0);foliage.renderQueue=2450;
            var ground=Material("GroundMat",new Color(.65f,.72f,.52f),"Ground.jpg");ground.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"GroundNormal.jpg"));ground.SetFloat("_BumpScale",.8f);ground.EnableKeyword("_NORMALMAP");
            Material("RockMat",new Color(.67f,.73f,.69f),"Rock.jpg");
            var boulder=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Boulder.fbx");var filters=boulder.GetComponentsInChildren<MeshFilter>();
            var source=filters[0];foreach(var candidate in filters)if(candidate.sharedMesh.vertexCount<source.sharedMesh.vertexCount)source=candidate;var mesh=Object.Instantiate(source.sharedMesh);mesh.name="Normalized scanned boulder";var vertices=mesh.vertices;var bounds=mesh.bounds;
            for(int i=0;i<vertices.Length;i++){var v=vertices[i]-bounds.center;vertices[i]=new Vector3(v.x/bounds.size.x,v.y/bounds.size.y,v.z/bounds.size.z);}
            mesh.vertices=vertices;mesh.RecalculateBounds();
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"RockMesh.asset");if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);}else AssetDatabase.CreateAsset(mesh,Folder+"RockMesh.asset");
            Debug.Log("SANGO3_NATURE rock vertices="+source.sharedMesh.vertexCount+" meshes="+filters.Length);
            AssetDatabase.SaveAssets();
        }
        static Material Material(string name,Color color,string texture)
        {
            var path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+texture);m.SetFloat("_Smoothness",.16f);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
        }
    }
}
