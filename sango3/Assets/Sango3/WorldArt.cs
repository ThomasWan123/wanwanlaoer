using System.Collections.Generic;
using UnityEngine;
namespace Sango3
{
    public static class WorldArt
    {
        static readonly Dictionary<Color,Material> Materials = new Dictionary<Color,Material>();
        public static void ResetMaterialCache(){Materials.Clear();}
        public static Material Material(Color color)
        {
            if (Materials.TryGetValue(color,out var material) && material) return material;
            var template=Resources.Load<Material>("BattleLit");material = template?new Material(template):new Material(Shader.Find("Universal Render Pipeline/Lit"));
            SceneArtResources.Own(material);material.color=color; material.SetFloat("_Smoothness",.15f); Materials[color]=material; return material;
        }
        public static GameObject Shape(string name,PrimitiveType type,Vector3 position,Vector3 scale,Color color,Transform parent=null,bool collider=false)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=Material(color);
            if(!collider) { var c=go.GetComponent<Collider>(); if(c){c.enabled=false; if(Application.isPlaying) Object.Destroy(c); else Object.DestroyImmediate(c);} }
            return go;
        }
        public static GameObject Warrior(string name,Vector3 position,Color color,bool hero) => HeroArt.Build(name,position,color,hero);
        public static void Landscape(Transform parent) => ScenicArt.Build(parent);
    }
}
