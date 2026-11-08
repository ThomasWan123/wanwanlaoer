using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Sango3
{
    public partial class BattleController
    {
        public bool Ready {get;private set;}
        public float LoadProgress {get;private set;}
        public string LoadLabel {get;private set;}="整备军械";
        static readonly Dictionary<string,Object> warmAssets=new Dictionary<string,Object>();
        // Enemy views are reused: a soldier that falls or reaches the gate returns here instead of being destroyed,
        // so detailed models are instantiated during loading, not mid-wave.
        readonly Dictionary<GameObject,Stack<GameObject>> enemyPool=new Dictionary<GameObject,Stack<GameObject>>();
        GameObject TakePooledEnemy(GameObject source){if(source&&enemyPool.TryGetValue(source,out var stack))while(stack.Count>0){var v=stack.Pop();if(v)return v;}return null;}
        void ReturnEnemy(GameObject source,GameObject view){if(!source){Destroy(view);return;}view.SetActive(false);if(!enemyPool.TryGetValue(source,out var stack))enemyPool[source]=stack=new Stack<GameObject>();stack.Push(view);}
        IEnumerator WarmAssets()
        {
            QualitySettings.asyncUploadTimeSlice=2;QualitySettings.asyncUploadBufferSize=16;
            var paths=new List<string>{"BattleLit","Nature/GroundMat","Nature/RockMat","Nature/RockMesh","Nature/Rock","Nature/FoliageMat","Heroes/enemy"};
            paths.Add("Architecture/Gate");paths.Add("Architecture/Watchtower");
            if(Stage.Index==BaimaStage){paths.Add("Heroes/Baima/yuan-soldier-0");paths.Add("Heroes/Baima/yuan-soldier-1");paths.Add("Heroes/Baima/yuan-soldier-2");paths.Add("Heroes/Baima/yanliang");paths.Add("Architecture/Riverboat");paths.Add("Architecture/Tent");}
            switch(Stage.Scenario.Theme){
                case BattleTheme.Village:paths.Add("Architecture/House");paths.Add("Architecture/Granary");break;
                case BattleTheme.Camp:paths.Add("Architecture/Tent");break;
                case BattleTheme.River:paths.Add("Architecture/Riverboat");break;
                case BattleTheme.Night:case BattleTheme.Pass:paths.Add("Architecture/Palisade");break;
            }
            foreach(var id in BattleRules.Portraits)paths.Add("Heroes/Final/"+id);
            for(int i=0;i<paths.Count;i++){
                LoadLabel=i<7?"勘察地势":"武将整装";LoadProgress=(i+1f)/(paths.Count+2);
                if(!warmAssets.TryGetValue(paths[i],out var asset)||!asset){var request=Resources.LoadAsync(paths[i]);yield return request;if(request.asset)warmAssets[paths[i]]=request.asset;}
                else yield return null;
            }
            // Build the first soldier and every playable hero while the loading UI is up, one step per frame,
            // so neither the first spawn nor the first deployment pays for instantiation or first-draw work.
            LoadLabel="整备先锋";
            int soldiers=Stage.Index==BaimaStage?9:1;
            for(int k=0;k<soldiers;k++){LoadLabel="整备先锋 "+(k+1)+"/"+soldiers;var source=HeroArt.Prefab("Prepared soldier",false);yield return Prepare("Prepared soldier",false,v=>ReturnEnemy(source,v),source);}
            if(Stage.Index==BaimaStage){var general=HeroArt.Prefab(Stage.Boss,false);yield return Prepare(Stage.Boss,false,v=>ReturnEnemy(general,v),general);}
            for(int i=0;i<BattleRules.Names.Length;i++){
                LoadLabel="武将整装 "+(i+1)+"/"+BattleRules.Names.Length;LoadProgress=(paths.Count+(i+1f)/BattleRules.Names.Length)/(paths.Count+2);int hero=i;
                yield return Prepare(BattleRules.Names[i],true,v=>preparedHeroes[hero]=v);
            }
        }
        readonly GameObject[] preparedHeroes=new GameObject[BattleRules.Names.Length];
        // Hands out the instance prepared during loading once; later deployments of the same hero build a new one.
        GameObject TakePreparedHero(int hero,Vector3 position)
        {
            var view=preparedHeroes[hero];if(!view)return null;preparedHeroes[hero]=null;
            view.transform.position=position;view.GetComponent<CombatPresentation>().ResetPosition();view.SetActive(true);return view;
        }
        IEnumerator Prepare(string name,bool hero,System.Action<GameObject> store,GameObject source=null)
        {
            float begin=Time.realtimeSinceStartup;var prefab=source?source:HeroArt.Prefab(name,hero);GameObject view;
            if(prefab){
                var request=Object.InstantiateAsync(prefab,Vector3.zero,Quaternion.identity);while(!request.isDone)yield return null;
                view=HeroArt.Configure((GameObject)request.Result[0],name,hero);
            }else view=WorldArt.Warrior(name,Vector3.zero,Color.white,hero);
            view.transform.SetParent(actors.transform);
            if(Debug.isDebugBuild)Debug.Log("SANGO3_PREPARE name="+name+" instantiateMs="+((Time.realtimeSinceStartup-begin)*1000).ToString("F2"));
            yield return null;begin=Time.realtimeSinceStartup;
            WarmRendering(view,hero?"Attack":"Walk",false);
            if(Debug.isDebugBuild)Debug.Log("SANGO3_PREPARE name="+name+" drawMs="+((Time.realtimeSinceStartup-begin)*1000).ToString("F2"));
            yield return null;begin=Time.realtimeSinceStartup;
            WarmRendering(view,null,true);view.SetActive(false);store(view);
            if(Debug.isDebugBuild)Debug.Log("SANGO3_PREPARE name="+name+" flashMs="+((Time.realtimeSinceStartup-begin)*1000).ToString("F2"));
            yield return null;
        }
        // Draws the instance once off-screen so mesh upload, skinning buffers and shader variants are ready before it appears.
        void WarmRendering(GameObject view,string state,bool flashVariant){
            var nodes=view.GetComponentsInChildren<Transform>();var layers=new int[nodes.Length];
            for(int i=0;i<nodes.Length;i++){layers[i]=nodes[i].gameObject.layer;nodes[i].gameObject.layer=31;}
            var cameraObject=new GameObject("Loading render warmup");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.nearClipPlane=.1f;camera.farClipPlane=30;camera.transform.position=view.transform.position+new Vector3(0,3,9);camera.transform.LookAt(view.transform.position+Vector3.up*2);
            var rt=RenderTexture.GetTemporary(64,64,24);var previous=RenderTexture.active;
            try{
                var animator=view.GetComponentInChildren<Animator>();if(animator&&state!=null){animator.Play(state,0,0);animator.Update(1f/60);}
                camera.targetTexture=rt;
                if(flashVariant){var flash=new MaterialPropertyBlock();flash.SetColor("_BaseColor",new Color(1.7f,1.5f,1.2f));var renderers=view.GetComponentsInChildren<Renderer>();foreach(var r in renderers)r.SetPropertyBlock(flash);camera.Render();foreach(var r in renderers)r.SetPropertyBlock(null);}
                else camera.Render();
            }finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);for(int i=0;i<nodes.Length;i++)nodes[i].gameObject.layer=layers[i];Destroy(cameraObject);}
        }
    }
}
