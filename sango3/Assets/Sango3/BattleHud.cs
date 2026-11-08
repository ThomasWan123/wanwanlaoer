using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Sango3
{
    public partial class BattleHud : MonoBehaviour
    {
        BattleController battle;Font font;RectTransform safe;Text stats,message,selection,preview,resultText;GameObject result;Button startButton;Button pause,speed,upgrade,skill;int width,height;Rect lastSafe;
        readonly Color ink=new Color(.1f,.16f,.15f,.96f),gold=new Color(.9f,.77f,.48f);
        // Combat close-up: the hero cards give way to the battlefield; only the right-hand controls stay.
        readonly System.Collections.Generic.List<GameObject> heroCards=new System.Collections.Generic.List<GameObject>();RectTransform dock;bool compact;
        void UpdateCompact(){bool close=BattleCamera.Mode==BattleCamera.View.Close;if(close==compact)return;compact=close;foreach(var c in heroCards)c.SetActive(!close);dock.anchorMin=new Vector2(close?.695f:0,0);
            var t=ultimateTitle.rectTransform;t.anchorMin=close?new Vector2(.60f,.70f):new Vector2(.18f,.75f);t.anchorMax=close?new Vector2(.97f,.77f):new Vector2(.82f,.87f);ultimateTitle.fontSize=close?30:42;}
        GameObject growth;Text growthText,ultimateTitle;Button equipment,trainAttack,trainUltimate;float bannerTimer;Color bannerColor;
        readonly System.Collections.Generic.List<FloatingLabel> floating=new System.Collections.Generic.List<FloatingLabel>();
        class FloatingLabel {public Text text;public Vector3 world;public float age;public Color color;public float pop;public Vector2 drift;}
        static Font sharedFont;
        // The OS font lookup is the most expensive part of HUD setup; load it in its own frame and keep it across stage reloads.
        // A bundled subset of Noto Sans SC (QA/sample-130/font_subset.py) loads in a few ms; building a font from the
        // OS's full CJK font could take 20-140 ms on device. Glyphs missing from the subset fall back to OS fonts.
        // Rasterise, during loading, the glyphs the HUD will show later at the pixel sizes it will use: otherwise the first
        // new glyph (a skill name on the ultimate dial, a damage number size) grows the font atlas mid-battle and every Text
        // rebuilds. Warming every glyph at every size took ~0.9 s on device, so only the text that appears later is warmed,
        // one request per loading frame. Text rasterises at its size times the canvas scale factor.
        static int atlasRebuilds;static bool atlasListening;
        public System.Collections.IEnumerator PrewarmGlyphs()
        {
            // Diagnostics listener is registered once per process; stage reloads must not stack it.
            if(!atlasListening){atlasListening=true;Font.textureRebuilt+=f=>atlasRebuilds++;}
            var font=PreloadFont();var bold=BoldFont();var chars=Resources.Load<TextAsset>("Fonts/game-chars");if(!font||!chars)yield break;
            float scale=safe?safe.GetComponentInParent<Canvas>().scaleFactor:1;int Px(int size)=>Mathf.RoundToInt(size*scale);
            string names=string.Join("",BattleRules.Names)+string.Join("",BattleRules.Skills)+"·Lv.0123456789+−/ 级士气绝技冷却秒就绪";
            var requests=new (string text,int size,FontStyle style)[]{(chars.text,23,FontStyle.Normal),(chars.text,25,FontStyle.Bold),("0123456789",27,FontStyle.Bold),("0123456789",38,FontStyle.Bold),("0123456789",50,FontStyle.Bold),(names,22,FontStyle.Bold),(names,42,FontStyle.Bold),(names,30,FontStyle.Bold)};
            // Each request is cut into small chunks, one per frame, and timed: rasterising all characters at one size in a
            // single call could take over 100 ms on device.
            const int Chunk=60;
            foreach(var r in requests)for(int i=0;i<r.text.Length;i+=Chunk){
                var target=r.style==FontStyle.Bold?bold:font;var tex=target.material?target.material.mainTexture:null;int before=tex?tex.width*tex.height:0;int rebuilds=atlasRebuilds;int gc=System.GC.CollectionCount(0);
                float begin=Time.realtimeSinceStartup;target.RequestCharactersInTexture(r.text.Substring(i,Mathf.Min(Chunk,r.text.Length-i)),Px(r.size),FontStyle.Normal);
                if(Debug.isDebugBuild){float ms=(Time.realtimeSinceStartup-begin)*1000;tex=target.material?target.material.mainTexture:null;
                    if(ms>8)Debug.Log("SANGO3_GLYPH size="+Px(r.size)+" style="+r.style+" chars="+i+"+"+Chunk+" ms="+ms.ToString("F1")+" atlas="+(tex?tex.width+"x"+tex.height:"-")+" grew="+((tex?tex.width*tex.height:0)!=before)+" rebuilds="+(atlasRebuilds-rebuilds)+" gc="+(System.GC.CollectionCount(0)-gc));}
                yield return null;}
        }
        // Bold text uses a real bold subset (QA/sample-130/font_subset.py), not Unity's synthesized bold.
        static Font boldFont;
        public static Font BoldFont(){if(!boldFont)boldFont=Resources.Load<Font>("Fonts/NotoSansSC-GameBold");return boldFont?boldFont:PreloadFont();}
        static void MakeBold(Text t){t.font=BoldFont();t.fontStyle=FontStyle.Normal;}
        public static Font PreloadFont(){if(!sharedFont)sharedFont=Resources.Load<Font>("Fonts/NotoSansSC-Game");if(!sharedFont)sharedFont=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Noto Sans CJK SC","sans-serif"},24);return sharedFont;}
        public void Initialize(BattleController owner)
        {
            battle=owner;font=PreloadFont();
            var canvas=new GameObject("Battle HUD",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
            safe=new GameObject("Safe Area",typeof(RectTransform)).GetComponent<RectTransform>();safe.SetParent(canvas.transform,false);FitSafe();
            if(!FindObjectOfType<EventSystem>())new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            Panel("Top bar",new Vector2(0,1),new Vector2(1,1),new Vector2(0,-78),Vector2.zero,ink);
            stats=Label("Resources",new Vector2(0,1),new Vector2(.63f,1),new Vector2(20,-70),new Vector2(0,-12),23,TextAnchor.MiddleLeft);
            pause=Action("暂停",new Vector2(.82f,1),new Vector2(.90f,1),new Vector2(0,-65),new Vector2(-8,-14),()=>battle.Pause());
            speed=Action("×1",new Vector2(.73f,1),new Vector2(.81f,1),new Vector2(0,-65),new Vector2(-8,-14),()=>battle.Faster());
            Action("重开",new Vector2(.91f,1),new Vector2(1,1),new Vector2(0,-65),new Vector2(-16,-14),()=>battle.Restart());
            dock=Panel("Bottom dock",Vector2.zero,new Vector2(1,0),Vector2.zero,new Vector2(0,190),ink);
            message=Label("Message",Vector2.zero,new Vector2(1,0),new Vector2(24,195),new Vector2(-24,232),23,TextAnchor.MiddleCenter);
            for(int i=0;i<6;i++) {
                int index=i;float x=.015f+i*.112f;
                var button=Action(BattleRules.Names[i]+"  "+BattleRules.Costs[i]+"金",new Vector2(x,0),new Vector2(x+.103f,0),new Vector2(0,15),new Vector2(0,175),()=>battle.SelectHero(index));heroCards.Add(button.gameObject);
                var texture=Resources.Load<Texture2D>("Portraits/"+BattleRules.Portraits[i]);
                if(texture){var portrait=new GameObject("Portrait",typeof(RectTransform),typeof(RawImage));portrait.transform.SetParent(button.transform,false);var rect=portrait.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.06f,.3f);rect.anchorMax=new Vector2(.94f,.97f);rect.offsetMin=rect.offsetMax=Vector2.zero;portrait.GetComponent<RawImage>().texture=texture;portrait.GetComponent<RawImage>().raycastTarget=false;}
                var label=button.GetComponentInChildren<Text>();label.alignment=TextAnchor.LowerCenter;label.fontSize=21;
            }
            selection=Label("Selection",new Vector2(.70f,0),new Vector2(.98f,0),new Vector2(0,145),new Vector2(0,180),22,TextAnchor.MiddleCenter);
            upgrade=Action("成长 / 装备",new Vector2(.71f,0),new Vector2(.83f,0),new Vector2(0,80),new Vector2(-5,135),()=>growth.SetActive(!growth.activeSelf));
            skill=Action("绝技 · 100士气",new Vector2(.84f,0),new Vector2(.99f,0),new Vector2(0,80),new Vector2(-12,135),()=>battle.Skill());BuildSkillDial();
            startButton=Action("开始守城",new Vector2(.71f,0),new Vector2(.99f,0),new Vector2(0,15),new Vector2(-12,68),()=>{if(battle.Started)battle.UseTactic();else battle.StartBattle();});
            preview=Label("Wave preview",new Vector2(0,1),new Vector2(1,1),new Vector2(24,-120),new Vector2(-24,-82),22,TextAnchor.MiddleCenter);
            ultimateTitle=Label("Ultimate announcement",new Vector2(.18f,.75f),new Vector2(.82f,.87f),Vector2.zero,Vector2.zero,42,TextAnchor.MiddleCenter);MakeBold(ultimateTitle);ultimateTitle.gameObject.SetActive(false);
            growth=Rect("Hero growth",new Vector2(.58f,.26f),new Vector2(.98f,.73f),Vector2.zero,Vector2.zero).gameObject;growth.AddComponent<UnityEngine.UI.Image>().color=new Color(.035f,.06f,.085f,.97f);
            growthText=Label("Growth stats",new Vector2(0,.47f),Vector2.one,new Vector2(22,0),new Vector2(-22,-25),23,TextAnchor.UpperLeft);growthText.transform.SetParent(growth.transform,false);
            equipment=Action("购买兵刃",new Vector2(.04f,.32f),new Vector2(.96f,.46f),Vector2.zero,Vector2.zero,()=>battle.Upgrade());equipment.transform.SetParent(growth.transform,false);
            trainAttack=Action("武技 +",new Vector2(.04f,.15f),new Vector2(.48f,.29f),Vector2.zero,Vector2.zero,()=>battle.TrainSkill(false));trainAttack.transform.SetParent(growth.transform,false);
            trainUltimate=Action("绝技 +",new Vector2(.52f,.15f),new Vector2(.96f,.29f),Vector2.zero,Vector2.zero,()=>battle.TrainSkill(true));trainUltimate.transform.SetParent(growth.transform,false);
            var close=Action("收起",new Vector2(.35f,.02f),new Vector2(.65f,.12f),Vector2.zero,Vector2.zero,()=>growth.SetActive(false));close.transform.SetParent(growth.transform,false);close.GetComponentInChildren<Text>().fontSize=17;close.GetComponentInChildren<Text>().rectTransform.offsetMin=new Vector2(5,2);close.GetComponentInChildren<Text>().rectTransform.offsetMax=new Vector2(-5,-2);growth.SetActive(false);
            var panel=Rect("Result",new Vector2(.2f,.32f),new Vector2(.8f,.7f),Vector2.zero,Vector2.zero);panel.gameObject.AddComponent<Image>().color=ink;result=panel.gameObject;resultText=Label("Result text",new Vector2(.23f,.35f),new Vector2(.77f,.67f),Vector2.zero,Vector2.zero,32,TextAnchor.MiddleCenter);resultText.transform.SetParent(panel,false);resultText.rectTransform.anchorMin=Vector2.zero;resultText.rectTransform.anchorMax=Vector2.one;resultText.rectTransform.offsetMin=new Vector2(24,24);resultText.rectTransform.offsetMax=new Vector2(-24,-24);result.SetActive(false);BuildCampaign();BuildBriefing();
        }
        public void ShowUltimate(int hero){bannerTimer=2.7f;bannerColor=SkillVfx.Colors[hero];ultimateTitle.text=BattleRules.Names[hero]+" · "+BattleRules.Skills[hero];ultimateTitle.gameObject.SetActive(true);}
        public void ShowFloating(Vector3 world,string text,Color color){if(floating.Count>=24)return;var label=Label("Combat feedback",Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(280,40),25,TextAnchor.MiddleCenter);label.text=text;label.color=color;MakeBold(label);floating.Add(new FloatingLabel{text=label,world=world,color=color});}
        // Damage numbers in three tiers: normal, heavy (third combo hit) and ultimate. Heavier blows are larger and pop.
        static readonly Color[] DamageColors={new Color(1,.93f,.78f),new Color(1,.58f,.18f),new Color(1,.86f,.25f)};
        public void ShowDamage(Vector3 world,int amount,int tier){if(floating.Count>=24)return;var label=Label("Damage",Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(320,70),tier==2?50:tier==1?38:27,TextAnchor.MiddleCenter);label.text=amount.ToString();label.color=DamageColors[tier];MakeBold(label);var outline=label.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.18f,.05f,0,.9f);outline.effectDistance=new Vector2(2,-2);floating.Add(new FloatingLabel{text=label,world=world,color=DamageColors[tier],pop=tier==0?.25f:.6f,drift=new Vector2(Random.Range(-28f,28f),0)});}
        void UpdateFeedback(){float dt=battle.Paused?0:Time.deltaTime;bannerTimer-=dt;ultimateTitle.gameObject.SetActive(bannerTimer>0);ultimateTitle.color=bannerColor;ultimateTitle.rectTransform.localScale=Vector3.one*(1+Mathf.Max(0,bannerTimer-2.3f)*.2f);
            for(int i=floating.Count-1;i>=0;i--){var f=floating[i];f.age+=dt;if(f.age>1.3f){Destroy(f.text.gameObject);floating.RemoveAt(i);continue;}var screen=Camera.main.WorldToScreenPoint(f.world+Vector3.up*f.age*.8f);RectTransformUtility.ScreenPointToLocalPointInRectangle(safe,screen,null,out var p);f.text.rectTransform.localPosition=new Vector3(p.x+f.drift.x*f.age,p.y,0);if(f.pop>0)f.text.rectTransform.localScale=Vector3.one*(1+f.pop*Mathf.Clamp01(1-f.age/.18f));var c=f.color;c.a=Mathf.Clamp01((1.3f-f.age)*2);f.text.color=c;}
            if(battle.Finished)growth.SetActive(false);growthText.text=battle.GrowthSummary;equipment.GetComponentInChildren<Text>().text=battle.EquipmentLabel;equipment.interactable=!battle.Paused&&!battle.Finished&&battle.SelectedLevel>0&&battle.SelectedEquipment<2;
            trainAttack.interactable=!battle.Paused&&!battle.Finished&&battle.SkillPoints>0&&battle.AttackRank<4;trainUltimate.interactable=!battle.Paused&&!battle.Finished&&battle.SkillPoints>0&&battle.SkillRank<3;
            trainAttack.GetComponentInChildren<Text>().text="武技 "+battle.AttackRank+"级  ＋";trainUltimate.GetComponentInChildren<Text>().text="绝技 "+battle.SkillRank+"级  ＋";
        }
        void Update(){if(!battle)return;UpdateLoading();UpdateFeedback();UpdateCampaign();UpdateSkillDial();UpdateCompact();if(width!=Screen.width||height!=Screen.height||lastSafe!=Screen.safeArea)FitSafe();stats.text="第 "+(battle.Stage.Index+1)+"关 · "+battle.Stage.Name+"   金 "+battle.Gold+"   城 "+battle.Life+"   波 "+battle.Wave+"/"+battle.MaxWaves+"   士 "+battle.Morale;message.text=battle.Message;preview.text=battle.WavePreview;startButton.interactable=battle.Ready&&(battle.Started?battle.TacticReady:!battle.Finished&&!battle.Paused);startButton.GetComponentInChildren<Text>().text=battle.Started?battle.TacticLabel:"开始战役";upgrade.interactable=!battle.Paused&&!battle.Finished;skill.interactable=!battle.Paused&&!battle.Finished;upgrade.GetComponentInChildren<Text>().text="成长 / 装备"+(battle.SkillPoints>0?" ＋":"");skill.GetComponentInChildren<Text>().text=battle.SkillCooldown>0?"冷却 "+Mathf.CeilToInt(battle.SkillCooldown)+"秒":battle.Morale>=100?"绝技 · 就绪":"绝技 "+battle.Morale+"/100";skill.GetComponent<UnityEngine.UI.Image>().color=battle.Morale>=100&&battle.SkillCooldown<=0?new Color(.55f,.39f,.13f):new Color(.22f,.27f,.30f);result.SetActive(battle.Finished);if(battle.Finished)resultText.text=battle.Message+"\n击败敌军 "+battle.Kills+"    剩余城防 "+battle.Life+"\n"+(battle.Won?new string('★',battle.Life>=10?3:battle.Life>=5?2:1)+" 守城成功 · 星级已保存":"调整武将站位与升阶时机，再战一次");selection.text=battle.Selection;pause.GetComponentInChildren<Text>().text=battle.Paused?"继续":"暂停";speed.GetComponentInChildren<Text>().text="×"+battle.Speed;}
        void FitSafe(){width=Screen.width;height=Screen.height;lastSafe=Screen.safeArea;safe.anchorMin=new Vector2(lastSafe.xMin/width,lastSafe.yMin/height);safe.anchorMax=new Vector2(lastSafe.xMax/width,lastSafe.yMax/height);safe.offsetMin=safe.offsetMax=Vector2.zero;}
        RectTransform Rect(string name,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax){var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(safe,false);rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=offsetMin;rect.offsetMax=offsetMax;return rect;}
        RectTransform Panel(string name,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color){var rect=Rect(name,a,b,c,d);rect.gameObject.AddComponent<Image>().color=color;return rect;}
        Text Label(string name,Vector2 a,Vector2 b,Vector2 c,Vector2 d,int size,TextAnchor align){var t=Rect(name,a,b,c,d).gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.color=gold;t.alignment=align;t.raycastTarget=false;return t;}
        Button Action(string text,Vector2 a,Vector2 b,Vector2 c,Vector2 d,UnityEngine.Events.UnityAction action){var rect=Rect(text,a,b,c,d);rect.gameObject.AddComponent<Image>().color=new Color(.27f,.31f,.25f);var button=rect.gameObject.AddComponent<Button>();button.onClick.AddListener(action);var label=new GameObject("Label",typeof(RectTransform),typeof(Text));label.transform.SetParent(rect,false);var r=label.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(5,8);r.offsetMax=new Vector2(-5,-8);var t=label.GetComponent<Text>();t.font=font;t.text=text;t.fontSize=23;t.color=gold;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return button;}
    }
}



