using System.Collections.Generic;
using UnityEngine;
namespace Sango3
{
    // Runtime-created meshes/materials are not project assets; release them on restart.
    public class SceneArtResources : MonoBehaviour
    {
        static SceneArtResources current;
        readonly List<Object> owned=new List<Object>();
        public static void Begin(GameObject owner){WorldArt.ResetMaterialCache();HeroArt.ResetMaterialCache();current=owner.AddComponent<SceneArtResources>();}
        public static T Own<T>(T value) where T:Object {if(current)current.owned.Add(value);return value;}
        void OnDestroy(){foreach(var asset in owned)if(asset)Destroy(asset);if(current==this)current=null;}
    }
}
