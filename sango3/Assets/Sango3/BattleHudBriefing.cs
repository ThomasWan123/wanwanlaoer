using UnityEngine;
using UnityEngine.UI;
namespace Sango3
{
    public partial class BattleHud
    {
        GameObject loading,briefing;Text loadText;Image loadFill;bool briefingPaused;Button viewToggle;
        void BuildBriefing()
        {
            Action("战报",new Vector2(.015f,.76f),new Vector2(.085f,.83f),Vector2.zero,Vector2.zero,()=>ShowBriefing());
            // Tactical overview / combat close-up following the selected hero.
            viewToggle=Action(BattleCamera.Mode==BattleCamera.View.Close?"战术全景":"战斗近景",new Vector2(.015f,.67f),new Vector2(.085f,.74f),Vector2.zero,Vector2.zero,()=>{BattleCamera.Toggle();viewToggle.GetComponentInChildren<Text>().text=BattleCamera.Mode==BattleCamera.View.Close?"战术全景":"战斗近景";});
            briefing=Rect("Battle briefing",new Vector2(.16f,.30f),new Vector2(.84f,.78f),Vector2.zero,Vector2.zero).gameObject;briefing.AddComponent<Image>().color=new Color(.035f,.055f,.075f,.98f);
            var s=battle.Stage.Scenario;
            var title=Label("Briefing title",new Vector2(.04f,.80f),new Vector2(.96f,.96f),Vector2.zero,Vector2.zero,30,TextAnchor.MiddleLeft);title.transform.SetParent(briefing.transform,false);title.text=s.Title+" · "+battle.Stage.Name;
            var body=Label("Briefing body",new Vector2(.04f,.20f),new Vector2(.96f,.80f),Vector2.zero,Vector2.zero,23,TextAnchor.UpperLeft);body.transform.SetParent(briefing.transform,false);body.text=s.Briefing+"\n\n目标："+s.ObjectiveText+"\n战场："+s.Rule+"\n军令："+s.TacticName+"；每波补充 1 次，最多 4 次，冷却 10 秒。\n剧情关卡为历史与演义题材改编。";
            var close=Action("返回布阵",new Vector2(.34f,.04f),new Vector2(.66f,.17f),Vector2.zero,Vector2.zero,()=>{briefing.SetActive(false);if(briefingPaused&&battle.Paused)battle.Pause();briefingPaused=false;});close.transform.SetParent(briefing.transform,false);briefing.SetActive(false);
            loading=Rect("Preparing battlefield",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).gameObject;loading.AddComponent<Image>().color=new Color(.035f,.055f,.075f,1);
            loadText=Label("Preparation",new Vector2(.1f,.48f),new Vector2(.9f,.64f),Vector2.zero,Vector2.zero,30,TextAnchor.MiddleCenter);loadText.transform.SetParent(loading.transform,false);
            var track=Rect("Progress track",new Vector2(.20f,.42f),new Vector2(.80f,.435f),Vector2.zero,Vector2.zero);track.SetParent(loading.transform,false);track.gameObject.AddComponent<Image>().color=new Color(.17f,.22f,.23f);
            var fill=Rect("Progress",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);fill.SetParent(track,false);loadFill=fill.gameObject.AddComponent<Image>();loadFill.color=gold;
        }
        void ShowBriefing(){if(!battle.Ready||battle.Finished)return;briefingPaused=battle.Started&&!battle.Paused;if(briefingPaused)battle.Pause();briefing.SetActive(true);briefing.transform.SetAsLastSibling();}
        void UpdateLoading(){if(!loading)return;loading.SetActive(!battle.Ready);if(!battle.Ready){loadText.text=battle.Stage.Scenario.Title+"\n"+battle.LoadLabel;loadFill.rectTransform.anchorMax=new Vector2(battle.LoadProgress,1);} }
    }
}
