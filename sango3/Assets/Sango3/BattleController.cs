using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Sango3
{
    public partial class BattleController : MonoBehaviour
    {
        class Enemy { public GameObject view,source; public float hp,maxHp,speed,slow,stun,incoming; public bool removed; public int next=1; public bool boss,cargo;public Vector3[] route; public CombatPresentation presentation; }
        class Tower { public int combo;public float idle;public int hero,level=1,xp,points,equipment,attackRank=1,skillRank=1;public float cooldown,ultimateCooldown,buff,cast;public GameObject view;public CombatPresentation presentation; }
        class PendingSkill {public Tower tower;public Vector3 center,direction;public float delay=SkillImpactDelay;}
        // A swing deals damage at its contact frame, not when the attack starts.
        class PendingHit {public Tower tower;public Enemy target;public float delay,damage;public int move=-1;}
        // Battle-time seconds; the Attack clip strikes between 40% and 58% of its 0.65 s length.
        public const float AttackImpactDelay=.34f,SkillImpactDelay=.55f;
        // Stage 16 白马破阵: the 0.13.0 production sample stage.
        public const int BaimaStage=15;
        readonly List<PendingSkill> pendingSkills=new List<PendingSkill>();
        readonly List<PendingHit> pendingHits=new List<PendingHit>();
        readonly List<Enemy> enemies=new List<Enemy>();
        readonly Tower[] towers=new Tower[8];
        readonly List<GameObject> effects=new List<GameObject>();
        readonly List<float> effectTimes=new List<float>();
        GameObject rangeRing;
        Camera cam; BattleHud hud; GameObject actors;
        int gold=300,life=12,morale=0,wave=0,spawnLeft,selectedHero,selectedSlot=-1,kills;
        float spawnTimer,waveTimer=8,moraleTimer,loadStart;bool started,finished,paused;int speed=1;
        public string Message {get;private set;}="选择武将，再点击金色部署点。准备好后开始战斗。";
        public StageDefinition Stage=>Campaign.Current;
        public int MaxWaves=>Stage.Waves;
        public int Kills=>kills;public bool Started=>started;
        public string WavePreview=>finished?"战斗结束":!started?Stage.Scenario.Title+" · "+Stage.Scenario.ObjectiveText:ObjectiveStatus+" · "+(wave==MaxWaves?"终波："+Stage.Boss:"下波 "+Stage.Count(wave+1)+" 人");
        public int Gold=>gold;public int Life=>life;public int Wave=>wave;public int Morale=>morale;public int SelectedHero=>selectedHero;
        public int SelectedLevel=>selectedSlot>=0 && towers[selectedSlot]!=null?towers[selectedSlot].level:0;
        Tower Selected=>selectedSlot>=0?towers[selectedSlot]:null;
        public int SelectedEquipment=>Selected!=null?Selected.equipment:0;
        public int SkillPoints=>Selected!=null?Selected.points:0;
        public int SkillRank=>Selected!=null?Selected.skillRank:0;
        public int AttackRank=>Selected!=null?Selected.attackRank:0;
        public float SkillCooldown=>Selected!=null?Selected.ultimateCooldown:0;
        public float SkillCooldownMax=>Selected!=null?18-(Selected.skillRank-1)*3:18;
        // Close-up camera follows the selected hero, else the last hero that acted.
        Transform lastActor;
        // Where the close-up looks: the hero's body, following it up when it leaps (skinned bounds track the animation).
        public Vector3 CameraFocusPoint{get{var f=CameraFocus;if(!f)return Vector3.zero;var p=f.position+Vector3.up*1.2f;var skin=f.GetComponent<SkinnedMeshRenderer>();if(skin)p.y=Mathf.Max(p.y,skin.bounds.max.y-3.4f);return p;}}
        public Transform CameraFocus=>Selected!=null&&Selected.view?Selected.view.transform:lastActor?lastActor:null;
        public int SelectedTowerHero=>Selected!=null?Selected.hero:-1;
        public int CastsResolved {get;private set;}
        public string EquipmentLabel=>Selected==null?"先选择武将":Selected.equipment>=2?"装备已满":"兵刃 +25%伤害 · "+BattleRules.UpgradeCost(Selected.equipment+1)+"金";
        public string GrowthSummary=>Selected==null?"点击已部署武将查看成长":Selection+"\n"+(Selected.level>=6?"经验已满级":"经验 "+Selected.xp+" / "+XpRequired(Selected.level))+"    技能点 "+Selected.points+"\n普攻 "+Mathf.RoundToInt(AttackDamage(Selected))+"    武技 "+Selected.attackRank+"级    绝技 "+Selected.skillRank+"级\n击败敌军获得经验；技能点强化武技或绝技。";
        public bool Finished=>finished;public bool Paused=>paused;public int Speed=>speed;
        // One clock for combat, animation and effects so 1x/2x stay in step; frozen while paused.
        // Total battle-clock seconds since load, for timing checks.
        public float BattleTime {get;private set;}
        public float BattleDeltaTime=>paused?0:Time.deltaTime*TimeScale;
        // Hit-stop: a heavy blow briefly freezes combat (real seconds) so the impact registers.
        float hitStop;
        public float TimeScale=>speed*(hitStop>0?.06f:1);
        public void HitStop(float seconds){hitStop=Mathf.Max(hitStop,seconds);}
        public string Selection=>Selected!=null ? BattleRules.Names[Selected.hero]+" · Lv."+Selected.level : BattleRules.Names[selectedHero]+" · "+BattleRules.Costs[selectedHero]+"金";
        System.Collections.IEnumerator Start()
        {
            float stepStart=Time.realtimeSinceStartup;loadStart=stepStart;Debug.Log("SANGO3_BOOT controllerStart="+stepStart.ToString("F3"));gold=Stage.StartingGold;SceneArtResources.Begin(gameObject);Application.targetFrameRate=60;if(Debug.isDebugBuild)gameObject.AddComponent<FrameDiagnostics>();cam=Camera.main;ScenicArt.ConfigureCamera(cam);BattleCamera.Attach(cam,this);cam.enabled=false;actors=new GameObject("Battle actors");
            // The engine's first frame and the OS font load are each expensive; keep them in separate frames.
            yield return null;stepStart=Time.realtimeSinceStartup;
            BattleHud.PreloadFont();Debug.Log("SANGO3_BOOT fontMs="+((Time.realtimeSinceStartup-stepStart)*1000).ToString("F1"));yield return null;stepStart=Time.realtimeSinceStartup;
            hud=gameObject.AddComponent<BattleHud>();hud.Initialize(this);
            Debug.Log("SANGO3_BOOT hudMs="+((Time.realtimeSinceStartup-stepStart)*1000).ToString("F1"));yield return null;stepStart=Time.realtimeSinceStartup;yield return hud.PrewarmGlyphs();Debug.Log("SANGO3_BOOT glyphMs="+((Time.realtimeSinceStartup-stepStart)*1000).ToString("F1"));yield return null;stepStart=Time.realtimeSinceStartup;yield return WarmAssets();Debug.Log("SANGO3_BOOT preloadMs="+((Time.realtimeSinceStartup-stepStart)*1000).ToString("F1"));stepStart=Time.realtimeSinceStartup;LoadLabel="修筑战场";yield return ScenicArt.BuildAsync(new GameObject("Three Kingdoms battlefield").transform);Debug.Log("SANGO3_BOOT sceneryMs="+((Time.realtimeSinceStartup-stepStart)*1000).ToString("F1"));
            // The opaque loading panel hides the battlefield, so the 3D camera stays off while loading. Before showing it,
            // draw it once into a small target so shaders, meshes and shadow maps are ready for the first visible frame.
            yield return null;stepStart=Time.realtimeSinceStartup;{var warm=RenderTexture.GetTemporary(256,128,24);cam.targetTexture=warm;cam.Render();cam.targetTexture=null;RenderTexture.ReleaseTemporary(warm);}
            Debug.Log("SANGO3_BOOT cameraWarmMs="+((Time.realtimeSinceStartup-stepStart)*1000).ToString("F1"));yield return null;
            // The combat close-up sees the same objects nearer, so higher texture mips and more detail come in on its first
            // frame. Draw one close view at screen resolution here, in its own loading frame, so the first switch does not stall.
            stepStart=Time.realtimeSinceStartup;{var pose=cam.transform.position;var close=RenderTexture.GetTemporary(Screen.width,Screen.height,24);cam.transform.position=Stage.Route[1]+Vector3.up*1.2f-cam.transform.forward*15.5f;cam.targetTexture=close;cam.Render();cam.targetTexture=null;cam.transform.position=pose;RenderTexture.ReleaseTemporary(close);}
            Debug.Log("SANGO3_BOOT closeWarmMs="+((Time.realtimeSinceStartup-stepStart)*1000).ToString("F1"));yield return null;cam.enabled=true;
            for(int i=0;i<BattleRules.Slots.Length;i++) {
                var go=WorldArt.Shape("Deployment "+i,PrimitiveType.Cylinder,BattleRules.Slots[i]+Vector3.up*.12f,new Vector3(1.7f,.12f,1.7f),new Color(.38f,.35f,.25f),null,true);
                go.AddComponent<DeploymentSlot>().index=i;
            }
            if(Stage.Index==BaimaStage)SangoAudio.StartAmbience("amb_baima");
            Ready=true;LoadProgress=1;Message=Stage.Scenario.Rule;Debug.Log("SANGO3_READY seconds="+(Time.realtimeSinceStartup-loadStart).ToString("F2")+" processSeconds="+Time.realtimeSinceStartup.ToString("F2"));
        }
        public void SelectHero(int hero) {if(!Ready||finished||paused||hero<0||hero>=BattleRules.Names.Length)return;selectedHero=hero;selectedSlot=-1;if(rangeRing)Destroy(rangeRing);Message="点击金色部署点，部署"+BattleRules.Names[hero];}
        public bool Deploy(int slot){float begin=Time.realtimeSinceStartup;bool placed=DeployCore(slot);if(Debug.isDebugBuild)Debug.Log("SANGO3_DEPLOY slot="+slot+" placed="+placed+" ms="+((Time.realtimeSinceStartup-begin)*1000).ToString("F1"));return placed;}
        bool DeployCore(int slot)
        {
            if(!Ready||finished||paused||slot<0||slot>=towers.Length)return false;
            selectedSlot=slot;ShowRange(slot);
            if(towers[slot]!=null){Message="已选择"+Selection+"；可升级或释放绝技。";return false;}
            int cost=BattleRules.Costs[selectedHero];if(gold<cost){Message="金币不足，击败敌军可获得金币。";return false;}
            gold-=cost;var t=new Tower{hero=selectedHero};var position=BattleRules.Slots[slot]+Vector3.up*.25f;t.view=TakePreparedHero(t.hero,position)??WorldArt.Warrior(BattleRules.Names[t.hero],position,BattleRules.Colors[t.hero],true);t.view.transform.SetParent(actors.transform);t.presentation=t.view.GetComponent<CombatPresentation>();towers[slot]=t;
            Message=BattleRules.Names[t.hero]+"已部署";return true;
        }
        public void Upgrade()
        {
            if(paused||finished)return;
            if(finished||selectedSlot<0||towers[selectedSlot]==null){Message="先点击已部署的武将。";return;}
            var t=towers[selectedSlot];if(t.equipment>=2){Message="兵刃已满级。";return;}int cost=BattleRules.UpgradeCost(t.equipment+1);
            if(gold<cost){Message="购装备需要"+cost+"金币。";return;}gold-=cost;t.equipment++;Message=Selection+"：兵刃强化，基础伤害 +25%";SkillVfx.Create(5,t.view.transform.position,1.3f,this,true);
        }
        public void TrainSkill(bool ultimate)
        {
            if(paused||finished)return;var t=Selected;if(t==null){Message="先选择场上的武将。";return;}
            int rank=ultimate?t.skillRank:t.attackRank;if(rank>=(ultimate?3:4)){Message="此技能已满级。";return;}if(t.points<=0){Message="经验升级后获得技能点。";return;}
            t.points--;if(ultimate)t.skillRank++;else t.attackRank++;Message=BattleRules.Names[t.hero]+(ultimate?"：绝技强化，伤害/效果提升，冷却缩短":"：武技强化，普攻伤害 +20%");
        }
        static int XpRequired(int level)=>level>=6?0:20+level*10;
        static float AttackDamage(Tower t)=>BattleRules.Damage[t.hero]*(1+(t.level-1)*.12f+t.equipment*.25f+(t.attackRank-1)*.2f)*(t.buff>0?1.35f:1);
        void AwardExperience(int amount){foreach(var t in towers)if(t!=null&&t.level<6){t.xp+=amount;while(t.level<6&&t.xp>=XpRequired(t.level)){t.xp-=XpRequired(t.level);t.level++;t.points++;hud.ShowFloating(t.view.transform.position+Vector3.up*3,"Lv."+t.level+"  +1技能点",new Color(1,.85f,.3f));}if(t.level==6)t.xp=0;}}
        public void StartBattle(){if(Ready&&!started&&!finished&&!paused){started=true;SangoAudio.Play("war_drums",cam.transform.position);waveTimer=0;Message=Stage.Scenario.EnemyArmy+"来袭！"+Stage.Scenario.ObjectiveText;}}
        // Test and capture hook: fill morale so the ultimate can be shown on demand. Editor and development builds only.
        public void FillMorale(){if(Application.isEditor||Debug.isDebugBuild)morale=100;}
        public void Pause(){if(!finished){paused=!paused;SangoAudio.Pause(paused);}}
        public void Faster(){speed=speed==1?2:1;}
        public void Restart(){SangoAudio.Pause(false);UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);}
        public void Skill()
        {
            if(finished||paused)return;
            if(selectedSlot<0||towers[selectedSlot]==null){Message="先选择场上的武将。";return;}
            var t=towers[selectedSlot];if(t.ultimateCooldown>0){Message="绝技冷却中。";return;}
            if(morale<100){Message="绝技需要 100 士气。";return;}
            if(!started||((t.hero!=3&&t.hero!=5)&&enemies.Count==0)){Message="当前没有可攻击目标。";return;}
            morale=0;t.ultimateCooldown=18-(t.skillRank-1)*3;t.cast=.75f;CancelHits(t);
            Message=BattleRules.Names[t.hero]+" · "+BattleRules.Skills[t.hero];
            Vector3 center=t.view.transform.position;float best=float.MaxValue;if(t.hero!=3&&t.hero!=5)foreach(var e in enemies){float d=Vector3.Distance(e.view.transform.position,t.view.transform.position);if(d<best){best=d;center=e.view.transform.position;}}
            if(GuanyuMoves.Uses(t.hero)){
                var origin=t.view.transform.position;var direction=center-origin;direction.y=0;direction=direction.sqrMagnitude>.01f?direction.normalized:t.view.transform.forward;
                t.view.transform.rotation=Quaternion.LookRotation(direction);t.cast=GuanyuMoves.Ultimate.length;t.combo=0;
                t.presentation.Play(GuanyuMoves.Ultimate.state,GuanyuMoves.Ultimate.length);hud.ShowUltimate(t.hero);GuanyuVfx.Telegraph(origin,direction,this);GuanyuVfx.Trail(t.view.transform).Open(.48f,.92f,1.6f,GuanyuVfx.Gold);BattleCamera.Push(cam,origin+direction*2.5f,.42f,-.3f,GuanyuMoves.Ultimate.length+.4f);
                SangoAudio.Play("ult_charge",origin);
                pendingSkills.Add(new PendingSkill{tower=t,center=origin,direction=direction,delay=GuanyuMoves.Ultimate.contact});return;
            }
            if(HeroMoves.Has(t.hero)){
                // Every other hero: its own ultimate clip; the telegraph closes and the effect lands on the clip's contact frame.
                var set=HeroMoves.For(t.hero);var face=center-t.view.transform.position;face.y=0;if(face.sqrMagnitude>.01f)t.view.transform.rotation=Quaternion.LookRotation(face);
                t.cast=set.Ultimate.length;t.combo=0;t.presentation.Play(set.Ultimate.state,set.Ultimate.length);hud.ShowUltimate(t.hero);
                // Heal and command are not area strikes: no ground telegraph for them.
                if(t.hero!=3&&t.hero!=5){var tele=SkillVfx.Create(t.hero,center,4.5f,this);tele.telegraph=set.Ultimate.contact;}SangoAudio.Play("ult_charge",t.view.transform.position,.8f);
                GuanyuVfx.Trail(t.view.transform,set).Open(set.Ultimate.contact-.25f,set.Ultimate.contact+.08f,1.2f,set.Hot);
                pendingSkills.Add(new PendingSkill{tower=t,center=center,delay=set.Ultimate.contact});return;
            }
            t.presentation.Cast();hud.ShowUltimate(t.hero);SkillVfx.Create(t.hero,center,4.5f,this);pendingSkills.Add(new PendingSkill{tower=t,center=center});
        }
        // Every hero receiving a heal or command gets the same effect family as the caster's main burst (the caster already
        // has the full one); heroes without a move set keep the old ring.
        void AllyBuffEffect(int hero,Tower caster,Tower ally){if(!HeroMoves.Has(hero)){SkillVfx.Create(hero,ally.view.transform.position,1.1f,this,true);return;}if(ally!=caster)HeroUltimateVfx.Buff(hero,ally.view.transform.position);}
        void Resolve(PendingSkill cast)
        {
            var t=cast.tower;CastsResolved++;
            if(GuanyuMoves.Uses(t.hero)){ResolveCleave(cast);return;}
            // Heroes with their own move set get the layered impact (HeroUltimateVfx) instead of the old line pillars.
            if(!HeroMoves.Has(t.hero))SkillVfx.Create(t.hero,cast.center,4.5f,this,true);else{HeroUltimateVfx.Impact(t.hero,cast.center,this);BattleCamera.Shake(cam,t.hero==2?.32f:.22f,.3f);HitStop(.07f);SangoAudio.Play("ult_slam",cast.center,.75f);}
            if(t.hero==3){int heal=3+t.skillRank;life=Mathf.Min(12,life+heal);foreach(var ally in towers)if(ally!=null){AllyBuffEffect(3,t,ally);hud.ShowFloating(ally.view.transform.position+Vector3.up*3,"仁德 · 城防恢复",SkillVfx.Colors[3]);}return;}
            if(t.hero==5){foreach(var ally in towers)if(ally!=null){ally.buff=6+t.skillRank*2;AllyBuffEffect(5,t,ally);hud.ShowFloating(ally.view.transform.position+Vector3.up*3,"伤害 +35%",SkillVfx.Colors[5]);}return;}
            float damage=(t.hero==0?150:t.hero==4?180:105)*(1+(t.skillRank-1)*.4f);
            foreach(var e in enemies)if(Vector3.Distance(e.view.transform.position,cast.center)<=4.5f){e.hp-=damage;if(t.hero==1)e.slow=3+t.skillRank;if(t.hero==2)e.stun=1+t.skillRank*.5f;e.presentation.Hit();hud.ShowFloating(e.view.transform.position+Vector3.up*2.8f,"−"+Mathf.RoundToInt(damage),SkillVfx.Colors[t.hero]);}
        }
        void Update()
        {
            if(!Ready)return;
            if(Input.GetMouseButtonDown(0)&&!paused&&!finished && (EventSystem.current==null||!EventSystem.current.IsPointerOverGameObject()) && (Input.touchCount==0||!EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))) {
                if(Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition),out var hit,150)) {var slot=hit.collider.GetComponent<DeploymentSlot>();if(slot)Deploy(slot.index);}
            }
            hitStop=Mathf.Max(0,hitStop-Time.unscaledDeltaTime);
            if(paused||finished)return;float dt=BattleDeltaTime;BattleTime+=dt;
            TrackTargets(dt);
            for(int i=pendingHits.Count-1;i>=0;i--){var h=pendingHits[i];h.delay-=dt;if(h.delay>0)continue;pendingHits.RemoveAt(i);ResolveHit(h);}
            for(int i=pendingSkills.Count-1;i>=0;i--){pendingSkills[i].delay-=dt;if(pendingSkills[i].delay<=0){Resolve(pendingSkills[i]);pendingSkills.RemoveAt(i);}}
            for(int i=effects.Count-1;i>=0;i--){effectTimes[i]-=dt;if(effectTimes[i]<=0){Destroy(effects[i]);effects.RemoveAt(i);effectTimes.RemoveAt(i);}}
            if(!started)return;
            TickScenario(dt);if(finished)return;
            moraleTimer+=dt;if(moraleTimer>=1){morale=Mathf.Min(100,morale+2);moraleTimer-=1;}
            if(spawnLeft==0&&enemies.Count==0) {
                if(wave==MaxWaves){if(ObjectiveSatisfied)Finish(true);else if(!ObjectiveCanWait)Finish(false);else Message=ObjectiveStatus+" · 完成目标后结束战斗";return;}waveTimer-=dt;
                if(waveTimer<=0){wave++;spawnLeft=Stage.Count(wave);spawnTimer=0;waveTimer=Stage.Index==19?1:3;BeginScenarioWave();Message="第 "+wave+" / "+MaxWaves+" 波 · "+Stage.Scenario.Rule;}
            }
            if(spawnLeft>0){spawnTimer-=dt;if(spawnTimer<=0){bool commander=Stage.Scenario.Objective==BattleObjective.Commanders&&wave%Mathf.Max(1,MaxWaves/Stage.Scenario.Target)==0;Spawn((wave==MaxWaves||commander)&&spawnLeft==1);spawnLeft--;spawnTimer=Stage.SpawnInterval;}}
            foreach(var t in towers)if(t!=null) {
                t.ultimateCooldown=Mathf.Max(0,t.ultimateCooldown-dt);t.buff=Mathf.Max(0,t.buff-dt);t.cast=Mathf.Max(0,t.cast-dt);if(t.cast>0)continue;
                t.cooldown-=dt;t.idle+=dt;if(t.cooldown>0)continue;Enemy target=null;float best=float.MaxValue;
                foreach(var e in enemies){float d=Vector3.Distance(e.view.transform.position,t.view.transform.position);if(e.hp-e.incoming>0&&d<(BattleRules.Range[t.hero]+t.level*.3f)*VisionScale&&d<best){target=e;best=d;}}
                if(target!=null){
                    var direction=target.view.transform.position-t.view.transform.position;direction.y=0;if(direction.sqrMagnitude>.01f)t.view.transform.rotation=Quaternion.LookRotation(direction);
                    lastActor=t.view.transform;
                    if(HeroMoves.Has(t.hero)){
                        // The combo restarts after a pause in fighting.
                        if(t.idle>2.5f)t.combo=0;var set=HeroMoves.For(t.hero);var move=set.Combo[t.combo];float damage=AttackDamage(t)*move.damage;
                        target.incoming+=damage;t.cooldown=move.length+.12f;t.idle=0;
                        t.presentation.Play(move.state,move.length);GuanyuVfx.Swing(t.view.transform,t.hero,t.combo,this);SangoAudio.Play(t.combo==2?"swing_heavy":"swing",t.view.transform.position,1,move.contact-.12f,t);
                        pendingHits.Add(new PendingHit{tower=t,target=target,damage=damage,delay=move.contact,move=t.combo});t.combo=(t.combo+1)%set.Combo.Length;
                    }else{
                        float damage=AttackDamage(t);target.incoming+=damage;t.cooldown=t.hero==4?.65f:1.05f;
                        t.presentation.Attack();pendingHits.Add(new PendingHit{tower=t,target=target,damage=damage,delay=AttackImpactDelay});
                    }
                }
            }
            for(int i=enemies.Count-1;i>=0;i--) {
                var e=enemies[i];e.presentation.Health(e.hp/e.maxHp);if(e.hp<=0){gold+=e.boss?80:e.cargo?45:Stage.Index==12||Stage.Index==18?9:12;OnScenarioKill(e);morale=Mathf.Min(100,morale+4);kills++;AwardExperience(e.boss?25:10);e.removed=true;ReturnEnemy(e.source,e.view);enemies.RemoveAt(i);continue;}
                e.stun=Mathf.Max(0,e.stun-dt);if(e.stun>0)continue;
                e.slow=Mathf.Max(0,e.slow-dt);var destination=e.route[e.next];
                var direction=destination-e.view.transform.position;if(direction.sqrMagnitude>.001f)e.view.transform.rotation=Quaternion.LookRotation(direction);
                e.view.transform.position=Vector3.MoveTowards(e.view.transform.position,destination,e.speed*(e.slow>0?.4f:1)*dt);
                if(Vector3.Distance(e.view.transform.position,destination)<.02f) e.next++;
                if(e.next>=e.route.Length){life-=e.boss?3:1;e.removed=true;ReturnEnemy(e.source,e.view);enemies.RemoveAt(i);if(life<=0){life=0;Finish(false);return;}}
            }

        }

        void ResolveHit(PendingHit h)
        {
            var e=h.target;e.incoming=Mathf.Max(0,e.incoming-h.damage);
            // The target died to another source or left the field during the swing: the blow lands on nothing.
            if(h.move>=0){ResolveGuanyuHit(h);return;}
            if(e.removed||e.hp<=0||!e.view)return;
            e.hp-=h.damage;e.presentation.Hit();if(h.tower.hero==1)e.slow=1.3f;
            Bolt(h.tower.view.transform.position+Vector3.up,e.view.transform.position+Vector3.up*.7f,BattleRules.Colors[h.tower.hero]);
        }
        void CancelHits(Tower t){SangoAudio.Cancel(t);for(int i=pendingHits.Count-1;i>=0;i--)if(pendingHits[i].tower==t){var e=pendingHits[i].target;e.incoming=Mathf.Max(0,e.incoming-pendingHits[i].damage);pendingHits.RemoveAt(i);}}
        void Spawn(bool boss)
        {
            float begin=Time.realtimeSinceStartup;
            int kind=boss?3:Stage.EnemyKind(wave,Stage.Count(wave)-spawnLeft);
            bool cargo=!boss&&Stage.Scenario.Objective==BattleObjective.Supplies&&spawnLeft==Stage.Count(wave)/2;var route=Stage.Scenario.Flank!=null&&wave%2==0&&spawnLeft%3==0?Stage.Scenario.Flank:Stage.Route;
            string name=boss?(wave==MaxWaves?Stage.Boss:Stage.Scenario.EnemyArmy+"将"):cargo?"护粮车":kind==1?"疾行斥候":kind==2?"重甲卫士":"敌军步卒";
            var source=cargo?null:HeroArt.Prefab(name,false);var view=TakePooledEnemy(source);bool prepared=view;
            if(view){view.name=name;view.transform.SetPositionAndRotation(route[0],Quaternion.identity);view.transform.localScale=Vector3.one*.65f;view.SetActive(true);view.GetComponent<CombatPresentation>().ResetForReuse();}
            else view=WorldArt.Warrior(name,route[0],kind==1?new Color(.35f,.48f,.36f):kind==2?new Color(.34f,.37f,.42f):new Color(.67f,.52f,.16f),false);
            view.transform.SetParent(actors.transform);if(cargo)MakeCargo(view.transform);
            if(boss)view.transform.localScale=Vector3.one*(Stage.Index==BaimaStage?.86f:1.15f);else if(kind==2)view.transform.localScale*=1.15f;
            float hp=(boss?650:65+wave*18)*Stage.HealthScale*(kind==1?.72f:kind==2?1.55f:1);
            enemies.Add(new Enemy{view=view,source=source,presentation=view.GetComponent<CombatPresentation>(),hp=hp,maxHp=hp,speed=(boss?1.1f:1.6f+wave*.06f)*(kind==1?1.45f:kind==2?.78f:1)*Stage.Scenario.EnemySpeed,boss=boss,cargo=cargo,route=route});
            if(Debug.isDebugBuild&&(prepared||wave==1&&spawnLeft>=Stage.Count(wave)-2))Debug.Log("SANGO3_SPAWN prepared="+prepared+" cpuMs="+((Time.realtimeSinceStartup-begin)*1000).ToString("F2"));
        }
        void ShowRange(int slot){if(rangeRing)Destroy(rangeRing);var t=towers[slot];rangeRing=VisualGeometry.Ring(BattleRules.Slots[slot],BattleRules.Range[t!=null?t.hero:selectedHero]+(t!=null?t.level:1)*.3f,new Color(.85f,.7f,.24f));}
        public void OpenStage(int index){if(!Campaign.Select(index))return;Restart();}
        public void NextStage(){if(finished&&Won&&Stage.Index<Campaign.Count-1)OpenStage(Stage.Index+1);}
        void Finish(bool won){Won=won;finished=true;Message=won?"大捷！"+Stage.Name+" · "+Stage.Scenario.ObjectiveText+"完成":life<=0?"城门失守 · 调整部署，再战一次":"战役目标未完成 · "+ObjectiveStatus;if(won)Campaign.Complete(Stage.Index,life);}
        void Flash(Vector3 point,Color color,float size){var go=WorldArt.Shape("Skill",PrimitiveType.Sphere,point,Vector3.one*size,color);effects.Add(go);effectTimes.Add(.25f);}
        void Bolt(Vector3 a,Vector3 b,Color color){var go=WorldArt.Shape("Attack",PrimitiveType.Cube,(a+b)*.5f,new Vector3(.045f,.045f,Vector3.Distance(a,b)),color);go.transform.rotation=Quaternion.LookRotation(b-a);effects.Add(go);effectTimes.Add(.12f);}
    }
    public class DeploymentSlot : MonoBehaviour { public int index; }
}



