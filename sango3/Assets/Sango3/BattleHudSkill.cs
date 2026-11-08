using UnityEngine;
using UnityEngine.UI;
namespace Sango3
{
    // Round ultimate dial for the selected hero: portrait, a sweep that fills with morale and empties
    // through the cooldown, and a pulsing ring when the ultimate is ready. Tapping it casts.
    public partial class BattleHud
    {
        readonly Sprite[] portraitSprites=new Sprite[BattleRules.Portraits.Length];RectTransform dial;Image dialPortrait,dialSweep,dialReady;Text dialName;int dialHero=-2;
        static Sprite Disc(string name){var t=Resources.Load<Texture2D>("Fx/"+name);return t?Sprite.Create(t,new Rect(0,0,t.width,t.height),Vector2.one*.5f):null;}
        void BuildSkillDial()
        {
            var disc=Disc("ui_disc");var ring=Disc("ui_ring");
            dial=Rect("Ultimate dial",new Vector2(.915f,0),new Vector2(.915f,0),new Vector2(-72,168),new Vector2(72,312));
            var back=dial.gameObject.AddComponent<Image>();back.sprite=disc;back.color=new Color(.06f,.08f,.07f,.92f);
            dial.gameObject.AddComponent<Button>().onClick.AddListener(()=>battle.Skill());
            var maskRect=Rect("Portrait mask",Vector2.zero,Vector2.one,new Vector2(8,8),new Vector2(-8,-8));maskRect.SetParent(dial,false);
            var mask=maskRect.gameObject.AddComponent<Image>();mask.sprite=disc;maskRect.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            var portrait=Rect("Portrait",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);portrait.SetParent(maskRect,false);dialPortrait=portrait.gameObject.AddComponent<Image>();dialPortrait.preserveAspect=false;
            var sweep=Rect("Charge sweep",Vector2.zero,Vector2.one,new Vector2(8,8),new Vector2(-8,-8));sweep.SetParent(dial,false);dialSweep=sweep.gameObject.AddComponent<Image>();dialSweep.sprite=disc;dialSweep.type=Image.Type.Filled;dialSweep.fillMethod=Image.FillMethod.Radial360;dialSweep.fillOrigin=(int)Image.Origin360.Top;dialSweep.fillClockwise=false;dialSweep.color=new Color(0,0,0,.62f);dialSweep.raycastTarget=false;
            var edge=Rect("Ready ring",Vector2.zero,Vector2.one,new Vector2(-6,-6),new Vector2(6,6));edge.SetParent(dial,false);dialReady=edge.gameObject.AddComponent<Image>();dialReady.sprite=ring;dialReady.raycastTarget=false;
            dialName=Label("Ultimate name",new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-110,4),new Vector2(110,34),22,TextAnchor.MiddleCenter);dialName.transform.SetParent(dial,false);MakeBold(dialName);dialName.raycastTarget=false;
            // Portrait sprites made up front, not when a hero is first selected.
            for(int i=0;i<portraitSprites.Length;i++){var tex=Resources.Load<Texture2D>("Portraits/"+BattleRules.Portraits[i]);portraitSprites[i]=tex?Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),Vector2.one*.5f):null;}
            dial.gameObject.SetActive(false);
        }
        void UpdateSkillDial()
        {
            if(!dial)return;int hero=battle.SelectedTowerHero;dial.gameObject.SetActive(hero>=0&&!battle.Finished);if(hero<0)return;
            if(hero!=dialHero){dialHero=hero;dialPortrait.sprite=portraitSprites[hero];dialName.text=BattleRules.Skills[hero];}
            bool cooling=battle.SkillCooldown>0;bool ready=!cooling&&battle.Morale>=100;
            // Covered fraction: during cooldown it drains as time passes; otherwise it uncovers with morale.
            dialSweep.fillAmount=cooling?battle.SkillCooldown/Mathf.Max(1,battle.SkillCooldownMax):1-battle.Morale/100f;
            dialPortrait.color=ready?Color.white:new Color(.6f,.6f,.6f);
            float pulse=.55f+.45f*Mathf.Sin(Time.unscaledTime*6);dialReady.color=ready?new Color(1,.8f,.3f,pulse):new Color(.5f,.45f,.3f,.5f);
            dial.localScale=Vector3.one*(ready?1+.04f*Mathf.Sin(Time.unscaledTime*6):1);
            dialName.color=ready?gold:new Color(.75f,.75f,.7f);
        }
    }
}
