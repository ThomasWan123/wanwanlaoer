using UnityEngine;
namespace Sango3
{
    public static class HistoricalArchitecture
    {
        public static GameObject Place(string kind,Vector3 position,Transform parent,float scale=1,float yaw=0)
        {
            var prefab=Resources.Load<GameObject>("Architecture/"+kind);
            if(!prefab)throw new System.InvalidOperationException("Missing baked architecture: "+kind);
            var go=Object.Instantiate(prefab,parent,false);go.name=kind;go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(0,yaw,0);go.transform.localScale=Vector3.one*scale;return go;
        }
    }
}
