using UnityEngine;
using UnityEngine.UI;
namespace Sango3
{
    public partial class BattleHud
    {
        GameObject campaignPanel;Text campaignHeading;Button nextStage;bool resumeAfterCampaign;
        readonly Button[] stageButtons=new Button[Campaign.Count];
        void BuildCampaign()
        {
            Action("战役",new Vector2(.64f,1),new Vector2(.72f,1),new Vector2(0,-65),new Vector2(-8,-14),OpenCampaign);
            nextStage=Action("下一关",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,()=>battle.NextStage());nextStage.transform.SetParent(result.transform,false);
            var nr=nextStage.GetComponent<RectTransform>();nr.anchorMin=new Vector2(.34f,.04f);nr.anchorMax=new Vector2(.66f,.21f);nr.offsetMin=nr.offsetMax=Vector2.zero;
            resultText.rectTransform.anchorMin=new Vector2(0,.22f);
            campaignPanel=Rect("Thirty stage campaign",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).gameObject;
            campaignPanel.AddComponent<Image>().color=new Color(.025f,.055f,.07f,.99f);
            campaignHeading=Label("Campaign heading",new Vector2(.025f,.88f),new Vector2(.82f,.985f),Vector2.zero,Vector2.zero,27,TextAnchor.MiddleLeft);campaignHeading.transform.SetParent(campaignPanel.transform,false);
            var close=Action("返回",new Vector2(.86f,.90f),new Vector2(.98f,.98f),Vector2.zero,Vector2.zero,CloseCampaign);close.transform.SetParent(campaignPanel.transform,false);
            for(int i=0;i<Campaign.Count;i++){
                int index=i,row=i/5,col=i%5;float x=.025f+col*.194f,y=.735f-row*.132f;
                var button=Action("",new Vector2(x,y),new Vector2(x+.184f,y+.112f),Vector2.zero,Vector2.zero,()=>battle.OpenStage(index));button.transform.SetParent(campaignPanel.transform,false);button.GetComponentInChildren<Text>().fontSize=19;stageButtons[i]=button;
            }
            campaignPanel.SetActive(false);
        }
        void OpenCampaign()
        {
            growth.SetActive(false);resumeAfterCampaign=!battle.Paused&&!battle.Finished;if(resumeAfterCampaign)battle.Pause();
            campaignHeading.text="三国征途 · 30 关     星级 "+Campaign.TotalStars+" / 90";
            for(int i=0;i<Campaign.Count;i++){
                var stage=Campaign.Get(i);bool unlocked=i<Campaign.Unlocked;var button=stageButtons[i];button.interactable=unlocked;
                button.GetComponentInChildren<Text>().text=(i+1).ToString("00")+"  "+stage.Name+"\n"+(unlocked?(Campaign.Stars(i)>0?new string('★',Campaign.Stars(i)):stage.ChapterName):"通关前一关解锁");
                button.GetComponent<Image>().color=i==battle.Stage.Index?new Color(.38f,.31f,.14f):Color.Lerp(new Color(.075f,.13f,.16f),stage.GroundTint,.12f);
            }
            campaignPanel.SetActive(true);campaignPanel.transform.SetAsLastSibling();
        }
        void CloseCampaign(){campaignPanel.SetActive(false);if(resumeAfterCampaign&&battle.Paused)battle.Pause();resumeAfterCampaign=false;}
        void UpdateCampaign(){nextStage.gameObject.SetActive(battle.Finished&&battle.Won&&battle.Stage.Index<Campaign.Count-1);}
    }
}
