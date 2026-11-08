using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Sango3.Editor
{
    // Bakes Guan Yu's sample move set. Each move is authored as key poses: body bone angles, the right
    // hand's position and the guandao's direction and edge. The right arm is solved to place the weapon,
    // the left hand follows the shaft through RecoveredPolearmGrip, and every frame is baked to curves.
    public static partial class GuanyuSample
    {
        const string Prefab="Assets/Sango3/Resources/Heroes/Final/guanyu.prefab";
        const string Processed="Assets/Sango3/Characters/Recovered/Processed/";
        // Which hero the move tools work on; Guan Yu's assets sit at the top of Processed, the others in subfolders.
        static string heroId="guanyu";static int HeroIndex=>System.Array.IndexOf(BattleRules.Portraits,heroId);
        static string trialPrefab; // set by the corrective self-test to work on a temporary copy
        static string HeroPrefab=>trialPrefab??"Assets/Sango3/Resources/Heroes/Final/"+heroId+".prefab";
        static string HeroFolder=>Processed+(heroId=="guanyu"?"":heroId+"/");
        const string Evidence="QA/sample-130/";
        // Bone indices from RecoveredHeroBuilder.
        const int Hips=0,Spine=1,Chest=2,Neck=3,Head=4,RUpper=5,RFore=6,RHand=7,LUpper=8,RThigh=11,RShin=12,RFoot=13,LThigh=14,LShin=15,LFoot=16,Skirt=17,RClavicle=18,LClavicle=19;

        sealed class Key
        {
            public float t;public Vector3 hips;public Vector3? hand,dir,edge;
            public readonly Dictionary<int,Vector3> angles=new Dictionary<int,Vector3>();
            public Key A(int bone,float x,float y,float z){angles[bone]=new Vector3(x,y,z);return this;}
            public Key W(float hx,float hy,float hz,float dx,float dy,float dz,float ex,float ey,float ez){hand=new Vector3(hx,hy,hz);dir=new Vector3(dx,dy,dz).normalized;edge=new Vector3(ex,ey,ez).normalized;return this;}
            public Key P(float x,float y,float z){hips=new Vector3(x,y,z);return this;}
            // Hand position relative to the right shoulder at rest, so one set of keys fits every hero's proportions.
            public bool relative;public Key S(float hx,float hy,float hz,float dx,float dy,float dz,float ex,float ey,float ez){W(hx,hy,hz,dx,dy,dz,ex,ey,ez);relative=true;return this;}
        }
        static Key K(float t)=>new Key{t=t};

        // Rest is filled from the Idle pose so each move starts and ends exactly where Idle is.
        static List<Key> Moves(string state){switch(state){
            case "Attack1":return new List<Key>{ // 斜劈
                K(0),
                K(.18f).P(0,-.06f,-.06f).A(Spine,0,-12,0).A(Chest,-6,-30,4).A(Head,4,22,0).A(RThigh,6,0,6).A(LThigh,-8,0,-6).A(LShin,10,0,0)
                    .W(-.65f,3.60f,-.10f, -.35f,.62f,-.70f, .30f,.80f,.50f),
                K(.27f).P(0,-.10f,.08f).A(Spine,4,6,0).A(Chest,10,10,-6).A(Head,-2,-8,0).A(RThigh,14,0,6).A(LThigh,-22,0,-6).A(LShin,22,0,0)
                    .W(-.30f,3.40f,.75f, .20f,.55f,.80f, .25f,-.60f,.75f),
                K(.32f).P(0,-.14f,.16f).A(Spine,8,14,0).A(Chest,18,26,-8).A(Head,-6,-20,0).A(RThigh,18,0,6).A(LThigh,-30,0,-6).A(LShin,30,0,0).A(Skirt,-8,0,0)
                    .W(.10f,2.35f,.95f, .60f,-.30f,.74f, .45f,-.85f,.10f),
                K(.42f).P(0,-.15f,.17f).A(Spine,9,16,0).A(Chest,20,30,-8).A(Head,-6,-22,0).A(RThigh,18,0,6).A(LThigh,-30,0,-6).A(LShin,30,0,0).A(Skirt,-4,0,0)
                    .W(.40f,2.15f,.70f, .82f,-.48f,.30f, .30f,-.70f,-.60f),
                K(.70f)};
            case "Attack2":return new List<Key>{ // 横扫
                K(0),
                K(.22f).P(.04f,-.08f,-.04f).A(Spine,0,16,0).A(Chest,-4,38,-4).A(Head,0,-26,0).A(RThigh,-6,0,10).A(LThigh,8,0,-12)
                    .W(.55f,3.05f,.15f, .90f,.25f,-.35f, -.20f,0,1),
                K(.30f).P(.02f,-.11f,.04f).A(Spine,2,8,0).A(Chest,4,18,-2).A(Head,0,-12,0).A(RThigh,-10,0,12).A(LThigh,10,0,-12)
                    .W(.25f,2.60f,.80f, .50f,-.12f,.85f, -.85f,0,.50f),
                K(.36f).P(0,-.13f,.10f).A(Spine,4,-2,0).A(Chest,8,-4,2).A(Head,0,4,0).A(RThigh,-14,0,12).A(LThigh,12,0,-12).A(Skirt,0,0,6)
                    .W(-.20f,2.45f,.90f, -.10f,-.18f,1, -1,0,0),
                K(.44f).P(-.04f,-.12f,.08f).A(Spine,4,-14,0).A(Chest,8,-34,4).A(Head,0,24,0).A(RThigh,-14,0,12).A(LThigh,12,0,-12).A(Skirt,0,0,-8)
                    .W(-.72f,2.50f,.58f, -.85f,-.12f,.50f, -.50f,0,-.85f),
                K(.54f).P(-.04f,-.11f,.06f).A(Spine,3,-16,0).A(Chest,6,-38,4).A(Head,0,26,0).A(RThigh,-12,0,10).A(LThigh,10,0,-10)
                    .W(-.85f,2.70f,.30f, -.95f,.05f,-.10f, -.10f,0,-1),
                K(.75f)};
            case "Attack3":return new List<Key>{ // 拖刀上挑
                K(0),
                K(.24f).P(0,-.22f,-.12f).A(Spine,-4,-14,0).A(Chest,-8,-26,0).A(Head,8,20,0).A(RThigh,-20,0,6).A(RShin,34,0,0).A(LThigh,-6,0,-8).A(LShin,26,0,0)
                    .W(-.85f,2.30f,-.40f, -.30f,-.45f,-.85f, 0,-.30f,1),
                K(.40f).P(0,-.18f,.24f).A(Spine,6,-4,0).A(Chest,12,-6,0).A(Head,-4,6,0).A(RThigh,16,0,6).A(RShin,10,0,0).A(LThigh,-34,0,-6).A(LShin,34,0,0)
                    .W(-.50f,2.40f,.55f, -.15f,-.60f,.80f, 0,1,.30f),
                K(.50f).P(0,-.04f,.30f).A(Spine,-6,0,0).A(Chest,-14,4,0).A(Head,6,-4,0).A(RThigh,20,0,6).A(RShin,4,0,0).A(LThigh,-30,0,-6).A(LShin,20,0,0).A(Skirt,-10,0,0)
                    .W(-.20f,3.45f,.75f, 0,.75f,.65f, 0,.60f,-.80f),
                K(.62f).P(0,.04f,.28f).A(Spine,-8,0,0).A(Chest,-18,2,0).A(Head,8,-2,0).A(RThigh,18,0,6).A(LThigh,-26,0,-6).A(LShin,14,0,0)
                    .W(-.30f,3.85f,.55f, 0,.92f,.30f, 0,.20f,-1),
                K(.95f)};
            case "Ultimate":return new List<Key>{ // 青龙偃月·斩
                K(0),
                K(.26f).P(0,-.40f,-.10f).A(Spine,14,-10,0).A(Chest,22,-20,0).A(Head,-14,14,0).A(RThigh,-46,0,8).A(RShin,72,0,0).A(RFoot,-24,0,0).A(LThigh,-40,0,-8).A(LShin,66,0,0).A(LFoot,-24,0,0)
                    .W(-.80f,2.10f,-.50f, -.30f,.10f,-.95f, 0,1,0),
                K(.50f).P(0,1.35f,.55f).A(Spine,-8,0,0).A(Chest,-16,0,0).A(Head,8,0,0).A(RThigh,-55,0,8).A(RShin,80,0,0).A(LThigh,-40,0,-8).A(LShin,70,0,0).A(Skirt,-14,0,0)
                    .W(-.25f,4.15f,-.20f, 0,.45f,-.90f, 0,1,.2f),
                K(.64f).P(0,1.15f,1.0f).A(Spine,-4,0,0).A(Chest,-8,0,0).A(Head,4,0,0).A(RThigh,-30,0,8).A(RShin,40,0,0).A(LThigh,-50,0,-8).A(LShin,50,0,0)
                    .W(.10f,4.25f,.65f, 0,.90f,.42f, 0,.30f,1),
                K(.85f).P(0,-.36f,1.20f).A(Spine,16,0,0).A(Chest,34,0,0).A(Head,-22,0,0).A(RThigh,20,0,8).A(RShin,20,0,0).A(LThigh,-56,0,-8).A(LShin,64,0,0).A(LFoot,-10,0,0).A(Skirt,-16,0,0)
                    .W(.30f,2.05f,1.25f, -.12f,-.55f,.83f, 0,-.84f,-.55f),
                K(1.08f).P(0,-.34f,1.20f).A(Spine,15,0,0).A(Chest,32,0,0).A(Head,-20,0,0).A(RThigh,20,0,8).A(RShin,20,0,0).A(LThigh,-54,0,-8).A(LShin,62,0,0).A(LFoot,-10,0,0).A(Skirt,-10,0,0)
                    .W(.30f,2.10f,1.20f, -.12f,-.52f,.85f, 0,-.85f,-.52f),
                K(1.5f)};
        }return null;}

        static GameObject root;static Transform[] bones;static Transform shaft;static RecoveredPolearmGrip grip;static Animator animator;
        static Quaternion handToShaft;static Vector3 handToElbow;static Vector3[] restAngles;static Vector3 restShoulder;static Vector3 restHips,restHand,restDir,restEdge;

        static void Load()
        {
            root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefab));root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            bones=root.GetComponent<SkinnedMeshRenderer>().bones;shaft=root.GetComponentsInChildren<Transform>().First(t=>t.name=="Spear");grip=root.GetComponent<RecoveredPolearmGrip>();animator=root.GetComponent<Animator>();
            var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>(HeroFolder+"Idle.anim");idle.SampleAnimation(root,0);restShoulder=bones[RUpper].position;
            // Rest is Idle as it plays at runtime: the grip turns the right hand so both hands hold the shaft. The moves start
            // and end there; during a move the right hand is authored and the support hand may let go (keepRightHand).
            if(grip){grip.keepRightHand=false;grip.Solve();grip.keepRightHand=true;}
            restAngles=bones.Select(b=>Signed(b.localEulerAngles)).ToArray();restHips=bones[Hips].localPosition;
            handToShaft=Quaternion.Inverse(bones[RHand].rotation)*shaft.rotation;handToElbow=Quaternion.Inverse(bones[RHand].rotation)*(bones[RFore].position-bones[RHand].position).normalized;restHand=bones[RHand].position;restDir=shaft.up;restEdge=shaft.right;if(grip)grip.ResetAuthoring();
        }
        static void SolveDrape(){var drape=root.GetComponent<RecoveredSleeveDrape>();if(drape)drape.Solve();var helper=root.GetComponent<ShoulderHelper>();if(helper)helper.Solve();var corrective=root.GetComponent<ShoulderCorrective>();if(corrective)corrective.Solve();}
        static Vector3 Signed(Vector3 e)=>new Vector3(Mathf.DeltaAngle(0,e.x),Mathf.DeltaAngle(0,e.y),Mathf.DeltaAngle(0,e.z));

        // Fill unspecified values: body bones fall back to rest, weapon targets to the rest grip.
        static void Complete(List<Key> keys)
        {
            foreach(var k in keys){if(k.relative&&k.hand.HasValue)k.hand=restShoulder+k.hand.Value;k.hand??=restHand;k.dir??=restDir;k.edge??=restEdge;}
            if(heroId!="guanyu")BodyDrive(keys);
        }
        // Drive from the ground up (other heroes; Guan Yu's keys are already authored this way): the hips take part of the
        // chest's turn, so the torso twists against the pelvis instead of rotating as a block, and weight shift, knee bend
        // and stride grow by a third. The hand stays authored, so the weapon path is unchanged.
        static void BodyDrive(List<Key> keys)
        {
            const float HipShare=.4f,Legs=1.33f;int[] legs={RThigh,RShin,RFoot,LThigh,LShin,LFoot,Skirt};
            for(int i=1;i<keys.Count-1;i++){var k=keys[i];
                if(!k.angles.ContainsKey(Hips)&&k.angles.TryGetValue(Chest,out var chest)){float yaw=chest.y*HipShare;k.angles[Hips]=new Vector3(0,yaw,0);k.angles[Chest]=new Vector3(chest.x,chest.y-yaw,chest.z);if(k.angles.TryGetValue(Spine,out var spine))k.angles[Spine]=new Vector3(spine.x,spine.y-yaw*.5f,spine.z);}
                k.hips*=Legs;foreach(int b in legs)if(k.angles.TryGetValue(b,out var a))k.angles[b]=a*Legs;}
        }
        // Cardinal spline through key values: smooth, no overshoot beyond what neighbours imply.
        static Vector3 Interpolate(List<Key> keys,float t,System.Func<Key,Vector3> value)
        {
            int i=0;while(i<keys.Count-2&&t>keys[i+1].t)i++;
            var k1=keys[i];var k2=keys[i+1];var k0=keys[Mathf.Max(0,i-1)];var k3=keys[Mathf.Min(keys.Count-1,i+2)];
            float u=Mathf.Clamp01((t-k1.t)/Mathf.Max(.0001f,k2.t-k1.t));
            Vector3 p0=value(k0),p1=value(k1),p2=value(k2),p3=value(k3);
            Vector3 m1=(p2-p0)/Mathf.Max(.0001f,k2.t-k0.t)*(k2.t-k1.t)*.5f,m2=(p3-p1)/Mathf.Max(.0001f,k3.t-k1.t)*(k2.t-k1.t)*.5f;
            if(i==0)m1=Vector3.zero;if(i+2>=keys.Count)m2=Vector3.zero;
            float u2=u*u,u3=u2*u;return (2*u3-3*u2+1)*p1+(u3-2*u2+u)*m1+(-2*u3+3*u2)*p2+(u3-u2)*m2;
        }

        // Pose the rig for one sample. Returns the wrist bend so extreme poses are visible in the report.
        static bool HasClavicles=>bones.Length>LClavicle&&bones[RClavicle].name.Contains("Clavicle");
        const float MaxWrist=42,MaxTwist=85;static float gripCorrection,supportGap;
        static float Pose(List<Key> keys,float t)
        {
            for(int b=0;b<bones.Length;b++){if(b==RUpper||b==RFore||b==RHand||b==LUpper||b==LUpper+1||b==LUpper+2)continue;
                var angle=Interpolate(keys,t,k=>k.angles.TryGetValue(b,out var v)?restAngles[b]+v:restAngles[b]);bones[b].localRotation=Quaternion.Euler(angle);}
            bones[Hips].localPosition=restHips+Interpolate(keys,t,k=>k.hips);
            for(int b=RUpper;b<=LUpper+2;b++)bones[b].localRotation=Quaternion.Euler(restAngles[b]);
            var hand=Interpolate(keys,t,k=>k.hand.Value);var dir=Interpolate(keys,t,k=>k.dir.Value).normalized;var edge=Vector3.ProjectOnPlane(Interpolate(keys,t,k=>k.edge.Value),dir).normalized;
            // Body motion carries the shoulder; hand targets are authored relative to the rest chest.
            hand+=bones[Hips].localPosition-restHips;
            var shaftRotation=Quaternion.LookRotation(Vector3.Cross(edge,dir),dir);
            var handRotation=shaftRotation*Quaternion.Inverse(handToShaft);
            // The elbow goes where this hand orientation naturally puts it, so the wrist stays near its rest bend.
            var naturalElbow=hand+handRotation*handToElbow*Vector3.Distance(bones[RFore].position,bones[RHand].position);
            // Shoulder girdle: when the hand goes above the shoulder, the clavicle takes part of the lift.
            if(HasClavicles){var lift=Mathf.Max(0,Vector3.Angle(Vector3.down,hand-bones[RUpper].position)-90);bones[RClavicle].localRotation=Quaternion.Euler(restAngles[RClavicle]+new Vector3(0,0,-Mathf.Min(lift*.4f,26)));}
            SolveArm(hand,naturalElbow);
            // Forearm twist first: rolling about the forearm's own axis is done by the forearm (pronation/supination, up to
            // MaxTwist), which leaves the hand where it is. Only the remaining bend is limited at the wrist.
            var restLocal=Quaternion.Euler(restAngles[RHand]);
            {var axis=(bones[RHand].position-bones[RFore].position).normalized;var correction=handRotation*Quaternion.Inverse(bones[RFore].rotation*restLocal);
                Twist(correction,axis).ToAngleAxis(out float twistAngle,out var twistAxis);if(twistAngle>180)twistAngle-=360;if(Vector3.Dot(twistAxis,axis)<0)twistAngle=-twistAngle;
                bones[RFore].rotation=Quaternion.AngleAxis(Mathf.Clamp(twistAngle,-MaxTwist,MaxTwist),axis)*bones[RFore].rotation;}
            // Wrist limit: the hand may bend at most MaxWrist degrees from its rest relation to the forearm;
            // beyond that the weapon gives way a little instead of the wrist folding.
            var delta=Quaternion.Inverse(restLocal)*(Quaternion.Inverse(bones[RFore].rotation)*handRotation);float bend=Quaternion.Angle(Quaternion.identity,delta);
            if(bend>MaxWrist)delta=Quaternion.Slerp(Quaternion.identity,delta,MaxWrist/bend);
            bones[RHand].rotation=bones[RFore].rotation*restLocal*delta;
            var authored=bones[RHand].rotation;if(grip)grip.Solve();SolveDrape();
            if(grip&&HasClavicles){var leftLift=Mathf.Max(0,Vector3.Angle(Vector3.down,grip.CurrentGripTarget-bones[LUpper].position)-90);if(leftLift>0){for(int b=LUpper;b<=LUpper+2;b++)bones[b].localRotation=Quaternion.Euler(restAngles[b]);bones[LClavicle].localRotation=Quaternion.Euler(restAngles[LClavicle]+new Vector3(0,0,Mathf.Min(leftLift*.4f,26)));grip.Solve();}}
            gripCorrection=Quaternion.Angle(authored,bones[RHand].rotation);supportGap=grip?grip.SupportGap:0;
            return Quaternion.Angle(Quaternion.identity,Quaternion.Inverse(restLocal)*(Quaternion.Inverse(bones[RFore].rotation)*bones[RHand].rotation));
        }
        static Quaternion Twist(Quaternion q,Vector3 axis){var r=new Vector3(q.x,q.y,q.z);var p=Vector3.Project(r,axis);var t=new Quaternion(p.x,p.y,p.z,q.w);float m=Mathf.Sqrt(t.x*t.x+t.y*t.y+t.z*t.z+t.w*t.w);return m<1e-5f?Quaternion.identity:new Quaternion(t.x/m,t.y/m,t.z/m,t.w/m);}
        static void SolveArm(Vector3 target,Vector3 elbowHint)
        {
            var upper=bones[RUpper];var lower=bones[RFore];var hand=bones[RHand];
            Vector3 a=upper.position,b=lower.position,c=hand.position;float l1=Vector3.Distance(a,b),l2=Vector3.Distance(b,c);
            var delta=target-a;float distance=Mathf.Clamp(delta.magnitude,.05f,(l1+l2)*.999f);var direction=delta.normalized;
            var pole=elbowHint-(a+direction*distance*.5f);var perpendicular=Vector3.ProjectOnPlane(pole,direction);if(perpendicular.sqrMagnitude<1e-6f)perpendicular=Vector3.ProjectOnPlane(new Vector3(-1,-.55f,-.35f),direction);perpendicular.Normalize();
            float along=(l1*l1-l2*l2+distance*distance)/(2*distance);float height=Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));var elbow=a+direction*along+perpendicular*height;
            upper.rotation=Quaternion.FromToRotation(b-a,elbow-a)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,a+direction*distance-lower.position)*lower.rotation;
        }

        public static void Build()
        {
            Directory.CreateDirectory(Evidence);var report=new StringBuilder();
            Load();
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(HeroFolder+"Guanyu.controller");var machine=controller.layers[0].stateMachine;
            var moves=HeroMoves.For(HeroIndex).All.ToArray();
            foreach(var move in moves){
                var keys=heroId=="guanyu"?Moves(move.state):HeroKeys(heroId,move.state);Complete(keys);RecoveredPolearmGrip.StepSeconds=1/60f;if(grip)grip.ResetContinuity();
                var clip=new AnimationClip{name=move.state,frameRate=60};int frames=Mathf.RoundToInt(move.length*60);
                var curves=new AnimationCurve[bones.Length*3];for(int i=0;i<curves.Length;i++)curves[i]=new AnimationCurve();var hipCurves=new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};
                var previous=(Vector3[])restAngles.Clone();float worstWrist=0;float worstAt=0;float worstGrip=0;float worstGap=0;int released=0;
                for(int f=0;f<=frames;f++){float t=f/60f;float wrist=Pose(keys,t);if(wrist>worstWrist){worstWrist=wrist;worstAt=t;}worstGrip=Mathf.Max(worstGrip,gripCorrection);if(supportGap>.06f)released++;worstGap=Mathf.Max(worstGap,supportGap);
                    for(int b=0;b<bones.Length;b++){var e=bones[b].localEulerAngles;for(int axis=0;axis<3;axis++){float v=previous[b][axis]+Mathf.DeltaAngle(previous[b][axis],e[axis]);previous[b][axis]=v;curves[b*3+axis].AddKey(new Keyframe(t,v));}}
                    var hp=bones[Hips].localPosition;for(int axis=0;axis<3;axis++)hipCurves[axis].AddKey(new Keyframe(t,hp[axis]));
                }
                for(int b=0;b<bones.Length;b++)for(int axis=0;axis<3;axis++){var c=curves[b*3+axis];for(int i=0;i<c.length;i++){AnimationUtility.SetKeyLeftTangentMode(c,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(c,i,AnimationUtility.TangentMode.Linear);}
                    clip.SetCurve(AnimationUtility.CalculateTransformPath(bones[b],root.transform),typeof(Transform),"localEulerAnglesRaw."+"xyz"[axis],c);}
                for(int axis=0;axis<3;axis++)clip.SetCurve(AnimationUtility.CalculateTransformPath(bones[Hips],root.transform),typeof(Transform),"m_LocalPosition."+"xyz"[axis],hipCurves[axis]);
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);
                string path=HeroFolder+move.state+".anim";RecoveredHeroBuilder.SaveAsset(clip,path);
                var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==move.state)??machine.AddState(move.state);state.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                report.AppendLine(move.state+" length="+move.length+" contact="+move.contact+" frames="+frames+" maxWristBend="+worstWrist.ToString("F1")+"deg at t="+worstAt.ToString("F2")+" (limit "+MaxWrist+", measured from the rest wrist) maxGripFallback="+worstGrip.ToString("F1")+"deg leftHandOffShaft="+released+"/"+(frames+1)+" frames maxGap="+worstGap.ToString("F2"));
            }
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
            Object.DestroyImmediate(root);
            File.WriteAllText(Evidence+(heroId=="guanyu"?"":heroId+"-")+"moves.txt",report.ToString());Debug.Log("GUANYU_MOVES_BUILT\n"+report);
            Render();
        }

        // Contact sheets: each move sampled across its length from front three-quarter, side, back and the game camera.
        public static void Render()
        {
            Directory.CreateDirectory(Evidence);Load();HeroLighting.Apply();
            var light=new GameObject("key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(40,-35,0);RenderSettings.ambientLight=new Color(.55f,.56f,.6f);
            var camera=new GameObject("camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.10f,.12f,.15f);
            var skin=root.GetComponent<SkinnedMeshRenderer>();
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.transform.localScale=Vector3.one*3;floor.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.28f,.27f,.24f)};
            foreach(var move in HeroMoves.For(HeroIndex).All){
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(HeroFolder+move.state+".anim");const int columns=8;const int cell=256;
                var views=new[]{new Vector3(6,3.2f,9),new Vector3(11,3,0),new Vector3(-5,3.2f,-9),new Vector3(1,25,-27)*.42f};
                var sheet=new Texture2D(columns*cell,views.Length*cell,TextureFormat.RGB24,false);
                for(int c=0;c<columns;c++){float t=c==columns-1?move.length:Mathf.Min(move.length,c*move.length/(columns-1));if(c==3)t=move.contact;
                    clip.SampleAnimation(root,t);if(grip)grip.Solve();SolveDrape();
                    for(int v=0;v<views.Length;v++){
                        bool game=v==3;camera.orthographic=!game;camera.orthographicSize=3.4f;camera.fieldOfView=34;
                        // Game view: the hero turned to face the camera as on a deployment slot.
                        root.transform.rotation=game?Quaternion.Euler(0,175,0):Quaternion.identity;if(grip)grip.Solve();SolveDrape();
                        camera.transform.position=views[v];camera.transform.LookAt(new Vector3(0,game?1.8f:2.4f,game?0:.6f));
                        var shown=new List<GameObject>();foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>()){var baked=new Mesh();r.BakeMesh(baked);var go=new GameObject("pose",typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=baked;go.GetComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;go.transform.SetPositionAndRotation(r.transform.position,r.transform.rotation);shown.Add(go);r.enabled=false;}
                        var target=RenderTexture.GetTemporary(cell,cell,24);camera.targetTexture=target;camera.Render();foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>())r.enabled=true;foreach(var go in shown){Object.DestroyImmediate(go.GetComponent<MeshFilter>().sharedMesh);Object.DestroyImmediate(go);}RenderTexture.active=target;sheet.ReadPixels(new Rect(0,0,cell,cell),c*cell,(views.Length-1-v)*cell);RenderTexture.active=null;camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);
                    }
                    root.transform.rotation=Quaternion.identity;
                }
                sheet.Apply();File.WriteAllBytes(Evidence+heroId+"-"+move.state+".png",sheet.EncodeToPNG());Object.DestroyImmediate(sheet);
            }
            // Close-up of face and both hands at rest and at the first strike's contact, for skin and grip review.
            {var idleClip=AssetDatabase.LoadAssetAtPath<AnimationClip>(HeroFolder+"Idle.anim");var strike=AssetDatabase.LoadAssetAtPath<AnimationClip>(HeroFolder+"Attack1.anim");const int cell=512;var sheet=new Texture2D(cell*3,cell,TextureFormat.RGB24,false);
                for(int c=0;c<3;c++){if(c<2)idleClip.SampleAnimation(root,.2f);else strike.SampleAnimation(root,HeroMoves.For(HeroIndex).Combo[0].contact);if(grip)grip.Solve();SolveDrape();
                    var shown=new List<GameObject>();foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>()){var baked=new Mesh();r.BakeMesh(baked);var go=new GameObject("pose",typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=baked;go.GetComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;go.transform.SetPositionAndRotation(r.transform.position,r.transform.rotation);shown.Add(go);r.enabled=false;}
                    camera.orthographic=true;camera.orthographicSize=c==2?1.6f:1.05f;var focus=c==2?new Vector3(0,2.9f,.5f):new Vector3(-.45f,3.25f,.2f);camera.transform.position=focus+(c==1?new Vector3(-4,.6f,5):new Vector3(2.5f,.8f,7));camera.transform.LookAt(focus);
                    var target=RenderTexture.GetTemporary(cell,cell,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;sheet.ReadPixels(new Rect(0,0,cell,cell),c*cell,0);RenderTexture.active=null;camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);
                    foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>())r.enabled=true;foreach(var go in shown){Object.DestroyImmediate(go.GetComponent<MeshFilter>().sharedMesh);Object.DestroyImmediate(go);}}
                sheet.Apply();File.WriteAllBytes(Evidence+heroId+"-closeup.png",sheet.EncodeToPNG());Object.DestroyImmediate(sheet);}
            Object.DestroyImmediate(floor);Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(root);
            Debug.Log("GUANYU_MOVES_RENDERED");
        }

        // Stage 16 from the game camera, without characters or HUD, for scene review.
        public static void CaptureStage()
        {
            Directory.CreateDirectory(Evidence);Campaign.BeginTest("sango3.qa.sample130.");
            try{
                UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
                Campaign.SelectTest(BattleController.BaimaStage);WorldArt.ResetMaterialCache();HeroArt.ResetMaterialCache();
                var sun=new GameObject("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.shadows=LightShadows.Soft;
                var camera=new GameObject("Camera").AddComponent<Camera>();camera.tag="MainCamera";ScenicArt.ConfigureCamera(camera);
                WorldArt.Landscape(new GameObject("Battlefield").transform);
                ScenarioValidation.Capture(Evidence+"stage16.png");
                // Close views of the river bank (boats, jetty, shore) and Yan Liang's camp (tents, parasol), at the
                // distance the battle close-up camera uses, for the scene review.
                {var start=Campaign.Current.Route[0];var keep=(camera.transform.position,camera.transform.rotation,camera.fieldOfView);camera.fieldOfView=40;
                    camera.transform.position=new Vector3(-8.5f,3.2f,6.2f);camera.transform.LookAt(new Vector3(-13.5f,.3f,12.2f));ScenarioValidation.Capture(Evidence+"stage16-river.png");
                    camera.transform.position=new Vector3(4.5f,1.9f,8.0f);camera.transform.LookAt(new Vector3(3.2f,.1f,11.3f));ScenarioValidation.Capture(Evidence+"stage16-shore.png");
                    camera.transform.position=new Vector3(-2f,2.6f,3.5f);camera.transform.LookAt(new Vector3(-4f,2.2f,30f));ScenarioValidation.Capture(Evidence+"stage16-horizon.png");
                    camera.transform.position=start+new Vector3(3.5f,3f,-5.5f);camera.transform.LookAt(start+new Vector3(-2.6f,.6f,.6f));ScenarioValidation.Capture(Evidence+"stage16-camp.png");
                    camera.transform.SetPositionAndRotation(keep.Item1,keep.Item2);camera.fieldOfView=keep.Item3;}
                // Static batching on device rejects meshes with non-finite vertices; checked before batching in ScenicArt.
                File.WriteAllText(Evidence+"stage16-nonfinite.txt",ScenicArt.NonFinite.Length==0?"none":ScenicArt.NonFinite);if(ScenicArt.NonFinite.Length>0)throw new System.Exception("Non-finite scenery vertices: "+ScenicArt.NonFinite);
            }finally{Campaign.EndTest();}
            Debug.Log("SAMPLE130_STAGE_CAPTURED");
        }

        // Hand skin uses a generic texture; tint it so its average albedo matches Guan Yu's face
        // (the red-dark complexion of the original model). Reports CIE76 colour difference before and after.
        public static void MatchHands()
        {
            Directory.CreateDirectory(Evidence);
            var body=AssetDatabase.LoadAssetAtPath<Mesh>(HeroFolder+"guanyu-skin.asset");var bodyMat=AssetDatabase.LoadAssetAtPath<Material>(HeroFolder+"guanyu.mat");var handMat=AssetDatabase.LoadAssetAtPath<Material>(HeroFolder+"hand-skin.mat");
            var albedo=Readable((Texture2D)bodyMat.mainTexture);var handTex=Readable((Texture2D)handMat.mainTexture);
            var v=body.vertices;var uv=body.uv;float front=v.Where(p=>p.y>3.82f&&p.y<4.05f&&Mathf.Abs(p.x)<.16f).Max(p=>p.z);
            Color face=Average(Enumerable.Range(0,v.Length).Where(i=>v[i].y>3.82f&&v[i].y<4.05f&&Mathf.Abs(v[i].x)<.16f&&v[i].z>front-.09f).Select(i=>albedo.GetPixelBilinear(uv[i].x,uv[i].y)));
            var handUv=new List<Vector2>();foreach(var r in AssetDatabase.LoadAllAssetsAtPath(HeroFolder+"right-hand.asset").OfType<Mesh>().Concat(AssetDatabase.LoadAllAssetsAtPath(HeroFolder+"left-hand.asset").OfType<Mesh>()))handUv.AddRange(r.uv);
            Color handRaw=Average(handUv.Select(p=>handTex.GetPixelBilinear(p.x,p.y)));
            var before=handRaw*handMat.color;var tint=new Color(Mathf.Clamp(face.r/Mathf.Max(.01f,handRaw.r),0,2),Mathf.Clamp(face.g/Mathf.Max(.01f,handRaw.g),0,2),Mathf.Clamp(face.b/Mathf.Max(.01f,handRaw.b),0,2),1);
            handMat.color=tint;EditorUtility.SetDirty(handMat);AssetDatabase.SaveAssets();var after=handRaw*tint;
            string report="face="+face+" hand(before)="+before+" dE76 before="+DeltaE(face,before).ToString("F1")+" | hand tint="+tint+" hand(after)="+after+" dE76 after="+DeltaE(face,after).ToString("F1")+" (albedo averages; lit appearance still needs visual review)";
            File.WriteAllText(Evidence+heroId+"-hand-skin.txt",report);Debug.Log("SAMPLE130_HANDS "+report);
        }
        static Color Average(IEnumerable<Color> colors){var list=colors.Where(c=>{float l=c.r*.3f+c.g*.59f+c.b*.11f;return l>.1f&&c.r>=c.g;}).ToList();if(list.Count==0)throw new System.Exception("No skin samples");var sum=new Color(0,0,0,0);foreach(var c in list)sum+=c;return sum/list.Count;}
        static Texture2D Readable(Texture2D source){var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);Graphics.Blit(source,rt);var previous=RenderTexture.active;RenderTexture.active=rt;var copy=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);copy.ReadPixels(new Rect(0,0,source.width,source.height),0,0);copy.Apply();RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);return copy;}
        static float DeltaE(Color a,Color b){var x=Lab(a);var y=Lab(b);return Vector3.Distance(x,y);}
        static Vector3 Lab(Color c){float L(float u)=>u<=.04045f?u/12.92f:Mathf.Pow((u+.055f)/1.055f,2.4f);float r=L(c.r),g=L(c.g),b=L(c.b);
            float X=(r*.4124f+g*.3576f+b*.1805f)/.95047f,Y=r*.2126f+g*.7152f+b*.0722f,Z=(r*.0193f+g*.1192f+b*.9505f)/1.08883f;float F(float t)=>t>.008856f?Mathf.Pow(t,1/3f):7.787f*t+16/116f;
            return new Vector3(116*F(Y)-16,500*(F(X)-F(Y)),200*(F(Y)-F(Z)));}

        // 青龙偃月刀: replaces the flat placeholder blade in the baked prefab with a bevelled crescent
        // (polished edge band, ridged flats, spine notch), a gilt dragon inlay on both faces and a dragon-mouth socket.
        public static void BuildWeapon()
        {
            var contents=PrefabUtility.LoadPrefabContents(Prefab);
            try{
                var spear=contents.GetComponentsInChildren<Transform>().First(t=>t.name=="Spear");
                foreach(var old in spear.Cast<Transform>().Where(t=>t.name=="Bevelled crescent blade"||t.name.StartsWith("Guandao")).ToArray())Object.DestroyImmediate(old.gameObject);
                var outline=new[]{new Vector2(-.04f,.97f),new Vector2(-.05f,1.45f),new Vector2(-.12f,1.51f),new Vector2(-.06f,1.58f),new Vector2(-.03f,1.86f),new Vector2(.07f,2.02f),new Vector2(.22f,1.88f),new Vector2(.34f,1.67f),new Vector2(.41f,1.44f),new Vector2(.39f,1.23f),new Vector2(.30f,1.07f),new Vector2(.15f,.98f)};
                var steel=Metal("Guandao steel",new Color(.82f,.85f,.88f),.85f,.58f);AssetDatabase.ImportAsset(Processed+"guandao-steel.png");steel.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Processed+"guandao-steel.png");var edge=Metal("Guandao polished edge",new Color(.92f,.95f,.97f),1,.92f);var gilt=Metal("Guandao gilt",new Color(.78f,.55f,.18f),.85f,.6f);
                RecoveredHeroBuilder.SaveAsset(steel,Processed+"guandao-steel.mat");RecoveredHeroBuilder.SaveAsset(edge,Processed+"guandao-edge.mat");RecoveredHeroBuilder.SaveAsset(gilt,Processed+"guandao-gilt.mat");
                steel=AssetDatabase.LoadAssetAtPath<Material>(Processed+"guandao-steel.mat");edge=AssetDatabase.LoadAssetAtPath<Material>(Processed+"guandao-edge.mat");gilt=AssetDatabase.LoadAssetAtPath<Material>(Processed+"guandao-gilt.mat");
                RecoveredHeroBuilder.SaveAsset(BladeMesh(outline,.052f,.024f),Processed+"guandao-blade.asset");Part(spear,"Guandao blade",AssetDatabase.LoadAssetAtPath<Mesh>(Processed+"guandao-blade.asset"),new[]{steel,edge});
                // Dragon inlay: a sinuous gilt ribbon on each flat, following the blade's sweep.
                RecoveredHeroBuilder.SaveAsset(Inlay(),Processed+"guandao-inlay.asset");Part(spear,"Guandao dragon inlay",AssetDatabase.LoadAssetAtPath<Mesh>(Processed+"guandao-inlay.asset"),new[]{gilt});
                RecoveredHeroBuilder.SaveAsset(Socket(),Processed+"guandao-socket.asset");Part(spear,"Guandao dragon socket",AssetDatabase.LoadAssetAtPath<Mesh>(Processed+"guandao-socket.asset"),new[]{gilt});
                PrefabUtility.SaveAsPrefabAsset(contents,Prefab);
            }finally{PrefabUtility.UnloadPrefabContents(contents);}
            AssetDatabase.SaveAssets();Debug.Log("SAMPLE130_WEAPON_BUILT");
        }
        static Material Metal(string name,Color color,float metallic,float smooth){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);return m;}
        static void Part(Transform parent,string name,Mesh mesh,Material[] materials){var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterials=materials;}
        // Outline at the edge (zero thickness), an inset ring raised to the flats, and a centre ridge.
        static Mesh BladeMesh(Vector2[] outline,float inset,float thickness)
        {
            int n=outline.Length;var c=Vector2.zero;foreach(var p in outline)c+=p;c/=n;
            var inner=new Vector2[n];for(int i=0;i<n;i++){var prev=outline[(i+n-1)%n];var next=outline[(i+1)%n];var tangent=(next-prev).normalized;var normal=new Vector2(tangent.y,-tangent.x);if(Vector2.Dot(normal,c-outline[i])<0)normal=-normal;inner[i]=outline[i]+normal*inset;}
            var v=new List<Vector3>();var flats=new List<int>();var edges=new List<int>();
            for(int side=-1;side<=1;side+=2){
                float z=thickness*side;
                for(int i=0;i<n;i++){int j=(i+1)%n;int b=v.Count;v.Add(new Vector3(outline[i].x,outline[i].y,0));v.Add(new Vector3(outline[j].x,outline[j].y,0));v.Add(new Vector3(inner[j].x,inner[j].y,z));v.Add(new Vector3(inner[i].x,inner[i].y,z));
                    if(side>0)edges.AddRange(new[]{b,b+2,b+1,b,b+3,b+2});else edges.AddRange(new[]{b,b+1,b+2,b,b+2,b+3});}
                for(int i=0;i<n;i++){int j=(i+1)%n;int b=v.Count;v.Add(new Vector3(inner[i].x,inner[i].y,z));v.Add(new Vector3(inner[j].x,inner[j].y,z));v.Add(new Vector3(c.x,c.y,z*1.35f));
                    if(side>0)flats.AddRange(new[]{b,b+2,b+1});else flats.AddRange(new[]{b,b+1,b+2});}
            }
            // Planar UVs across the blade face: u from spine to edge, v along the blade (steel texture: art_textures.py).
            var uvs=v.Select(p=>new Vector2((p.x+.12f)/.55f,(p.y-.95f)/1.1f)).ToList();
            var mesh=new Mesh{name="Guandao blade"};mesh.SetVertices(v);mesh.SetUVs(0,uvs);mesh.subMeshCount=2;mesh.SetTriangles(flats,0);mesh.SetTriangles(edges,1);mesh.RecalculateNormals();
            // Faces must point outward on each side; flip both submeshes if the flats face inward.
            var normals=mesh.normals;float facing=0;for(int k=0;k<flats.Count;k+=3)facing+=normals[flats[k]].z*Mathf.Sign(v[flats[k]].z);
            if(facing<0){for(int k=0;k<flats.Count;k+=3){int t=flats[k];flats[k]=flats[k+1];flats[k+1]=t;}for(int k=0;k<edges.Count;k+=3){int t=edges[k];edges[k]=edges[k+1];edges[k+1]=t;}mesh.SetTriangles(flats,0);mesh.SetTriangles(edges,1);mesh.RecalculateNormals();}
            // Smooth shading: average normals of coincident vertices within each part (flats, edge band), so the blade reads
            // as one forged surface with a bright edge instead of separate flat facets.
            {var nn=mesh.normals;var groups=new Dictionary<(Vector3Int,bool),Vector3>();var flatSet=new HashSet<int>(flats);
                for(int i=0;i<v.Count;i++){var key=(Vector3Int.RoundToInt(v[i]*10000),flatSet.Contains(i));groups[key]=(groups.TryGetValue(key,out var acc)?acc:Vector3.zero)+nn[i];}
                for(int i=0;i<v.Count;i++)nn[i]=groups[(Vector3Int.RoundToInt(v[i]*10000),flatSet.Contains(i))].normalized;mesh.normals=nn;mesh.RecalculateTangents();}
            mesh.RecalculateBounds();return mesh;
        }
        static Mesh Inlay()
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int side=-1;side<=1;side+=2){const int k=28;int b=v.Count;
                for(int i=0;i<=k;i++){float u=i/(float)k;var p=new Vector2(.04f+.22f*Mathf.Sin(u*Mathf.PI*.95f)+.035f*Mathf.Sin(u*Mathf.PI*5),1.02f+u*.82f);float w=.018f*(1-u*.6f)+.006f;
                    v.Add(new Vector3(p.x-w,p.y,side*.033f));v.Add(new Vector3(p.x+w,p.y,side*.033f));
                    if(i<k){int a=b+i*2;if(side>0)t.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});else t.AddRange(new[]{a,a+1,a+2,a+1,a+3,a+2});}}
                // Dragon head: a small gilt boss where the inlay starts.
                int h=v.Count;for(int i=0;i<10;i++){float a=i*Mathf.PI/5;v.Add(new Vector3(.06f+Mathf.Cos(a)*.045f,1.06f+Mathf.Sin(a)*.035f,side*.034f));}v.Add(new Vector3(.06f,1.06f,side*.04f));
                for(int i=0;i<10;i++){int j=(i+1)%10;if(side>0)t.AddRange(new[]{h+10,h+i,h+j});else t.AddRange(new[]{h+10,h+j,h+i});}
            }
            var mesh=new Mesh{name="Guandao inlay"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        // Lathe-turned dragon-mouth socket joining blade and shaft.
        static Mesh Socket()
        {
            var profile=new[]{new Vector2(.05f,.84f),new Vector2(.07f,.87f),new Vector2(.062f,.90f),new Vector2(.085f,.93f),new Vector2(.10f,.96f),new Vector2(.07f,.985f),new Vector2(.035f,1.0f)};
            const int seg=16;var v=new List<Vector3>();var t=new List<int>();
            foreach(var p in profile)for(int s=0;s<=seg;s++){float a=s*Mathf.PI*2/seg;v.Add(new Vector3(Mathf.Cos(a)*p.x*1.15f,p.y,Mathf.Sin(a)*p.x*.8f));}
            for(int r=0;r<profile.Length-1;r++)for(int s=0;s<seg;s++){int a=r*(seg+1)+s,b=a+seg+1;t.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            var mesh=new Mesh{name="Guandao socket"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }


        // The baked riverboat timber mesh carries a few NaN vertices from its generator; collapse each onto a
        // finite corner of its triangle (degenerate, invisible) so static batching keeps the batch.
        public static void RepairArchitecture()
        {
            var report=new StringBuilder();
            foreach(var path in Directory.GetFiles("Assets/Sango3/Resources/Architecture","*.asset")){
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh)continue;var v=mesh.vertices;bool Bad(Vector3 p)=>float.IsNaN(p.x+p.y+p.z)||float.IsInfinity(p.x+p.y+p.z);
                int bad=v.Count(Bad);if(bad==0)continue;var t=mesh.triangles;
                for(int pass=0;pass<3;pass++)for(int k=0;k<t.Length;k+=3)for(int e=0;e<3;e++){int a=t[k+e];if(!Bad(v[a]))continue;for(int o=1;o<3;o++){int b=t[k+(e+o)%3];if(!Bad(v[b])){v[a]=v[b];break;}}}
                for(int a=0;a<v.Length;a++)if(Bad(v[a]))v[a]=Vector3.zero;
                var n=mesh.normals;for(int a=0;a<n.Length;a++)if(Bad(n[a]))n[a]=Vector3.up;
                mesh.vertices=v;if(n.Length==v.Length)mesh.normals=n;mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);report.AppendLine(path+" repaired "+bad+" vertices");
            }
            AssetDatabase.SaveAssets();File.WriteAllText(Evidence+"architecture-repair.txt",report.Length==0?"none":report.ToString());Debug.Log("SAMPLE130_ARCH "+report);
        }

        // Upper-body detail at the frames where wrist and shoulder deform most, front / side / back.
        public static void RenderDetail()
        {
            Directory.CreateDirectory(Evidence);Load();HeroLighting.Apply();
            var light=new GameObject("key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(40,-35,0);RenderSettings.ambientLight=new Color(.55f,.56f,.6f);
            var camera=new GameObject("camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.10f,.12f,.15f);camera.orthographic=true;camera.orthographicSize=1.55f;
            var samples=new[]{("Attack1",.20f),("Attack1",.32f),("Attack2",.28f),("Attack2",.36f),("Attack2",.47f),("Attack3",.25f),("Attack3",.50f),("Ultimate",.18f),("Ultimate",.60f),("Ultimate",.85f)};
            var views=new[]{new Vector3(1.5f,.4f,8),new Vector3(-8,.4f,0),new Vector3(8,.4f,0),new Vector3(-1.5f,.4f,-8)};const int cell=320;
            var sheet=new Texture2D(samples.Length*cell,views.Length*cell,TextureFormat.RGB24,false);
            for(int c=0;c<samples.Length;c++){var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(HeroFolder+samples[c].Item1+".anim");clip.SampleAnimation(root,samples[c].Item2);if(grip)grip.Solve();SolveDrape();
                var shown=new List<GameObject>();foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>()){var baked=new Mesh();r.BakeMesh(baked);var go=new GameObject("pose",typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=baked;go.GetComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;go.transform.SetPositionAndRotation(r.transform.position,r.transform.rotation);shown.Add(go);r.enabled=false;}
                var chest=bones[Chest].position+Vector3.up*.05f;
                for(int v=0;v<views.Length;v++){camera.transform.position=chest+views[v];camera.transform.LookAt(chest);var target=RenderTexture.GetTemporary(cell,cell,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;sheet.ReadPixels(new Rect(0,0,cell,cell),c*cell,(views.Length-1-v)*cell);RenderTexture.active=null;camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);}
                foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>())r.enabled=true;foreach(var go in shown){Object.DestroyImmediate(go.GetComponent<MeshFilter>().sharedMesh);Object.DestroyImmediate(go);}}
            sheet.Apply();File.WriteAllBytes(Evidence+heroId+"-detail.png",sheet.EncodeToPNG());Object.DestroyImmediate(sheet);
            Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(root);Debug.Log("GUANYU_DETAIL_RENDERED");
        }

        // Stage 16 Yuan army. Spearmen come from the yuansoldier build (xiahoudun body, recoloured, eyepatch painted out,
        // two-handed spear grip) in three helmet variants so a column does not read as one general copied; Yan Liang
        // uses the zhangliao model recoloured crimson. Textures: QA/sample-130/recolor.py, eyepatch.py.
        public const int SoldierVariants=3;
        public static void BuildBaimaArmy()
        {
            Directory.CreateDirectory("Assets/Sango3/Resources/Heroes/Baima");AssetDatabase.Refresh();
            foreach(var n in new[]{"helmet-iron","helmet-leather"})AssetDatabase.ImportAsset(Processed+"baima/"+n+".png");
            var iron=Metal("Yuan helmet iron",Color.white,.6f,.38f);iron.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Processed+"baima/helmet-iron.png");var leather=Metal("Yuan cap leather",Color.white,.05f,.24f);leather.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Processed+"baima/helmet-leather.png");var silk=Metal("Yuan tassel silk",new Color(.62f,.10f,.06f),0,.22f);var bronze=Metal("Yuan helmet bronze",new Color(.55f,.40f,.20f),.75f,.5f);
            foreach(var (m,n) in new[]{(iron,"helmet-iron"),(leather,"helmet-leather"),(silk,"helmet-silk"),(bronze,"helmet-bronze")})RecoveredHeroBuilder.SaveAsset(m,Processed+"baima/"+n+".mat");
            Material Load(string n)=>AssetDatabase.LoadAssetAtPath<Material>(Processed+"baima/"+n+".mat");
            for(int v=0;v<SoldierVariants;v++){
                var contents=PrefabUtility.LoadPrefabContents("Assets/Sango3/Resources/Heroes/Final/yuansoldier.prefab");
                try{
                    var skin=contents.GetComponent<SkinnedMeshRenderer>();var head=skin.bones[4];
                    // The grip turns the shaft into the support hand's reach; for this body that put the point at the ground.
                    contents.GetComponent<RecoveredPolearmGrip>().pointUp=true;
                    // Fit the helmet to the scalp: vertices above the brow line.
                    var baked=new Mesh();skin.BakeMesh(baked);var top=baked.vertices.Select(p=>contents.transform.TransformPoint(p)).Where(p=>p.y>head.position.y+.12f&&Mathf.Abs(p.x-head.position.x)<.32f).ToArray();Object.DestroyImmediate(baked);
                    var min=top.Aggregate(Vector3.Min);var max=top.Aggregate(Vector3.Max);var center=new Vector3((min.x+max.x)*.5f,0,(min.z+max.z)*.5f);float radius=Mathf.Max(max.x-min.x,max.z-min.z)*.5f*.9f;float brow=head.position.y+.12f;
                    var helmet=new GameObject("Helmet").transform;helmet.SetParent(head,false);helmet.position=new Vector3(center.x,brow,center.z);
                    float crown=Mathf.Max(max.y-brow+.05f,radius*.9f);
                    if(v==0){Lathe(helmet,"Iron bowl helmet",new[]{new Vector2(1.02f,0),new Vector2(1f,.25f),new Vector2(.92f,.6f),new Vector2(.68f,.88f),new Vector2(.3f,1.02f),new Vector2(0,1.06f)},radius,crown,Load("helmet-iron"));
                        Lathe(helmet,"Neck guard",new[]{new Vector2(1.04f,.02f),new Vector2(1.12f,-.28f),new Vector2(1.18f,-.48f)},radius,crown,Load("helmet-leather"),.55f,1.45f);
                        Lathe(helmet,"Brow band",new[]{new Vector2(1.06f,-.02f),new Vector2(1.06f,.1f)},radius,crown,Load("helmet-bronze"));}
                    else if(v==1){Lathe(helmet,"Leather cap",new[]{new Vector2(1.04f,0),new Vector2(1f,.35f),new Vector2(.8f,.72f),new Vector2(.42f,.95f),new Vector2(0,1f)},radius,crown*.92f,Load("helmet-leather"));
                        var tassel=WorldArt.Shape("Red tassel",PrimitiveType.Sphere,Vector3.zero,Vector3.one*radius*.55f,Color.white,helmet);tassel.transform.localPosition=Vector3.up*crown*.98f;tassel.GetComponent<Renderer>().sharedMaterial=Load("helmet-silk");Object.DestroyImmediate(tassel.GetComponent<Collider>());}
                    else{Lathe(helmet,"Pointed iron helmet",new[]{new Vector2(1.03f,0),new Vector2(1f,.3f),new Vector2(.84f,.66f),new Vector2(.5f,.95f),new Vector2(.14f,1.22f),new Vector2(0,1.42f)},radius,crown,Load("helmet-iron"));
                        Lathe(helmet,"Cheek flaps",new[]{new Vector2(1.04f,.0f),new Vector2(1.08f,-.42f)},radius,crown,Load("helmet-bronze"),.5f,.9f);
                        var plume=WorldArt.Shape("Plume",PrimitiveType.Capsule,Vector3.zero,new Vector3(radius*.28f,radius*.7f,radius*.28f),Color.white,helmet);plume.transform.localPosition=new Vector3(0,crown*1.25f,-radius*.25f);plume.transform.localRotation=Quaternion.Euler(-35,0,0);plume.GetComponent<Renderer>().sharedMaterial=Load("helmet-silk");Object.DestroyImmediate(plume.GetComponent<Collider>());}
                    // Rivets round the brow band, and the spear point in the same forged steel as Guan Yu's blade (not mirror-white).
                    for(int k=0;k<10;k++){float ang=k*Mathf.PI*2/10;var rivet=WorldArt.Shape("Rivet",PrimitiveType.Sphere,Vector3.zero,Vector3.one*radius*.09f,Color.white,helmet);rivet.transform.localPosition=new Vector3(Mathf.Cos(ang)*radius*1.05f,crown*.05f,Mathf.Sin(ang)*radius*1.1f);rivet.GetComponent<Renderer>().sharedMaterial=Load("helmet-bronze");Object.DestroyImmediate(rivet.GetComponent<Collider>());}
                    var forged=AssetDatabase.LoadAssetAtPath<Material>(Processed+"guandao-steel.mat");foreach(var r in contents.GetComponentsInChildren<MeshRenderer>())if(r.name.Contains("spear point"))r.sharedMaterial=forged;
                    string target="yuan-soldier-"+v;contents.name=target;PrefabUtility.SaveAsPrefabAsset(contents,"Assets/Sango3/Resources/Heroes/Baima/"+target+".prefab");
                }finally{PrefabUtility.UnloadPrefabContents(contents);}
            }
            {
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Processed+"baima/yanliang-albedo.png");var contents=PrefabUtility.LoadPrefabContents("Assets/Sango3/Resources/Heroes/Final/zhangliao.prefab");
                try{var skin=contents.GetComponent<SkinnedMeshRenderer>();var material=new Material(skin.sharedMaterial){name="yanliang"};material.mainTexture=texture;
                    RecoveredHeroBuilder.SaveAsset(material,Processed+"baima/yanliang.mat");skin.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Processed+"baima/yanliang.mat");
                    contents.name="yanliang";PrefabUtility.SaveAsPrefabAsset(contents,"Assets/Sango3/Resources/Heroes/Baima/yanliang.prefab");}
                finally{PrefabUtility.UnloadPrefabContents(contents);}
            }
            AssetDatabase.DeleteAsset("Assets/Sango3/Resources/Heroes/Baima/yuan-soldier.prefab");
            AssetDatabase.SaveAssets();Debug.Log("SAMPLE130_ARMY_BUILT");
        }
        // Lathe-turned helmet piece: profile x is a fraction of the scalp radius, y a fraction of the crown height.
        // An arc below 360° (start/end in turns) makes neck guards and cheek flaps that leave the face open.
        static void Lathe(Transform parent,string name,Vector2[] profile,float radius,float height,Material material,float startTurn=0,float endTurn=1)
        {
            const int segments=28;var v=new List<Vector3>();var t=new List<int>();int ring=segments+1;
            var uv=new List<Vector2>();for(int r=0;r<profile.Length;r++){var p=profile[r];for(int s=0;s<=segments;s++){float a=Mathf.Lerp(startTurn,endTurn,s/(float)segments)*Mathf.PI*2+Mathf.PI*.5f;v.Add(new Vector3(Mathf.Cos(a)*p.x*radius,p.y*height,-Mathf.Sin(a)*p.x*radius*1.06f));uv.Add(new Vector2(s/(float)segments*2,r/(float)(profile.Length-1)));}}
            for(int r=0;r<profile.Length-1;r++)for(int s=0;s<segments;s++){int a=r*ring+s,b=a+ring;t.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Processed+"baima/"+name.Replace(' ','-').ToLowerInvariant()+"-"+Mathf.RoundToInt(radius*1000)+".asset";RecoveredHeroBuilder.SaveAsset(mesh,path);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }

        // Share of frames per clip in which the support hand holds the shaft (gap within 2 cm of reach).
        public static void GripReport()
        {
            Load();var sb=new StringBuilder();
            foreach(var state in new[]{"Idle","Walk","Attack1","Attack2","Attack3","Ultimate"}){var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(HeroFolder+state+".anim");int held=0,n=0;float worst=0;grip.keepRightHand=state!="Idle"&&state!="Walk";
                Vector3 last=Vector3.zero;Vector3 lastRight=Vector3.zero;float jump=0,rightJump=0;RecoveredPolearmGrip.StepSeconds=1/60f;grip.ResetContinuity();
                // 60 fps sampling: how far the support hand moves in one frame compared with the sword hand, to catch pops.
                for(float t=0;t<=clip.length;t+=1/60f){clip.SampleAnimation(root,t);grip.Solve();n++;if(grip.SupportGap<=.02f)held++;worst=Mathf.Max(worst,grip.SupportGap);
                    var lp=grip.hand.position;var rp=bones[RHand].position;if(n>1){jump=Mathf.Max(jump,Vector3.Distance(lp,last));rightJump=Mathf.Max(rightJump,Vector3.Distance(rp,lastRight));}last=lp;lastRight=rp;}
                sb.AppendLine(state+" held="+held+"/"+n+" worstGap="+worst.ToString("F2")+" maxSupportHandStep="+jump.ToString("F3")+" maxSwordHandStep="+rightJump.ToString("F3")+" (units per 1/60 s)");}
            RecoveredPolearmGrip.StepSeconds=0;File.WriteAllText(Evidence+(heroId=="guanyu"?"":heroId+"-")+"grip-report.txt",sb.ToString());Object.DestroyImmediate(root);
        }

        // Import settings for the bundled UI font: dynamic, with OS CJK fonts as fallback for any glyph outside the subset.
        public static void ConfigureFont()
        {
            foreach(var path in new[]{"Assets/Sango3/Resources/Fonts/NotoSansSC-Game.ttf","Assets/Sango3/Resources/Fonts/NotoSansSC-GameBold.ttf"}){            AssetDatabase.ImportAsset(path);
            var importer=(TrueTypeFontImporter)AssetImporter.GetAtPath(path);importer.fontTextureCase=FontTextureCase.Dynamic;importer.includeFontData=true;
            importer.fontNames=new[]{"Noto Sans SC","Noto Sans CJK SC","Microsoft YaHei","sans-serif"};importer.SaveAndReimport();}Debug.Log("SAMPLE130_FONT_CONFIGURED");
        }

        // Writes UV coordinates of the face's eye band of a processed hero mesh (for texture touch-up tools).
        public static void ExportFaceUv()
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Processed+"yuansoldier/guanyu-skin.asset");var v=mesh.vertices;var uv=mesh.uv;float front=v.Where(p=>p.y>3.85f&&p.y<4.15f&&Mathf.Abs(p.x)<.2f).Max(p=>p.z);
            var sb=new StringBuilder();for(int i=0;i<v.Length;i++)if(v[i].y>3.8f&&v[i].y<4.25f&&Mathf.Abs(v[i].x)<.22f&&v[i].z>front-.12f)sb.AppendLine(v[i].x.ToString("F3")+" "+v[i].y.ToString("F3")+" "+v[i].z.ToString("F3")+" "+uv[i].x.ToString("F4")+" "+uv[i].y.ToString("F4"));
            File.WriteAllText(Evidence+"face-uv.txt",sb.ToString());
        }

        // Side-by-side render of the stage 16 army prefabs in their walk pose, with Guan Yu for scale.
        public static void RenderArmy()
        {
            Directory.CreateDirectory(Evidence);HeroLighting.Apply();var made=new List<GameObject>();
            var paths=new[]{"Heroes/Final/guanyu","Heroes/Baima/yuan-soldier-0","Heroes/Baima/yuan-soldier-1","Heroes/Baima/yuan-soldier-2","Heroes/Baima/yanliang"};
            for(int i=0;i<paths.Length;i++){var go=(GameObject)Object.Instantiate(Resources.Load<GameObject>(paths[i]),new Vector3(i*2.6f,0,0),Quaternion.Euler(0,200,0));if(i>0)go.transform.localScale=Vector3.one*(i==4?.86f:.65f)/.65f*.8f;
                var anim=go.GetComponent<Animator>();var clip=anim.runtimeAnimatorController.animationClips.First(c=>c.name==(i==0?"Idle":"Walk"));clip.SampleAnimation(go,.3f);var g=go.GetComponent<RecoveredPolearmGrip>();if(g)g.Solve();made.Add(go);}
            var shown=new List<GameObject>();foreach(var go in made)foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>()){var baked=new Mesh();r.BakeMesh(baked);var b=new GameObject("pose",typeof(MeshFilter),typeof(MeshRenderer));b.GetComponent<MeshFilter>().sharedMesh=baked;b.GetComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;b.transform.SetPositionAndRotation(r.transform.position,r.transform.rotation);shown.Add(b);r.enabled=false;}
            var light=new GameObject("key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(40,-35,0);RenderSettings.ambientLight=new Color(.55f,.56f,.6f);
            var camera=new GameObject("camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.10f,.12f,.15f);camera.orthographic=true;camera.orthographicSize=2.8f;camera.transform.position=new Vector3(5.2f,4.2f,-12);camera.transform.LookAt(new Vector3(5.2f,2.2f,0));
            var rt=new RenderTexture(1600,640,24);camera.targetTexture=rt;camera.aspect=2.5f;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1600,640,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,640),0,0);RenderTexture.active=null;File.WriteAllBytes(Evidence+"army.png",tex.EncodeToPNG());
            foreach(var b in shown)Object.DestroyImmediate(b);foreach(var go in made)Object.DestroyImmediate(go);Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(camera.gameObject);
        }

        // Xiahou Dun's eyepatch is painted into the texture over the right eye. For the Yuan spearman, every texel of the
        // patched eye's triangles is replaced by the texel at the mirrored point of the face (the open eye).
        public static void MirrorEye()
        {
            const string texPath="Assets/Sango3/Characters/Recovered/yuansoldier-albedo.jpg";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Processed+"yuansoldier/guanyu-skin.asset");var v=mesh.vertices;var uv=mesh.uv;var t=mesh.triangles;
            var tex=Readable(AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));int W=tex.width,H=tex.height;var src=tex.GetPixels();var dst=(Color[])src.Clone();
            bool Region(Vector3 p)=>p.x<-.012f&&p.x>-.19f&&p.y>3.76f&&p.y<3.97f&&p.z>.12f;
            // Source triangles: the other half of the face (mesh +x), mirrored onto the patched eye (mesh -x).
            var source=new List<int>();for(int k=0;k<t.Length;k+=3){var c=(v[t[k]]+v[t[k+1]]+v[t[k+2]])/3;if(c.x>0f&&c.x<.23f&&c.y>3.70f&&c.y<4.02f&&c.z>.1f)source.Add(k);}
            int changed=0;
            for(int k=0;k<t.Length;k+=3){
                Vector3 a=v[t[k]],b=v[t[k+1]],c=v[t[k+2]];if(!Region((a+b+c)/3))continue;Vector2 ua=uv[t[k]],ub=uv[t[k+1]],uc=uv[t[k+2]];
                int x0=Mathf.FloorToInt(Mathf.Min(ua.x,Mathf.Min(ub.x,uc.x))*W)-1,x1=Mathf.CeilToInt(Mathf.Max(ua.x,Mathf.Max(ub.x,uc.x))*W)+1,y0=Mathf.FloorToInt(Mathf.Min(ua.y,Mathf.Min(ub.y,uc.y))*H)-1,y1=Mathf.CeilToInt(Mathf.Max(ua.y,Mathf.Max(ub.y,uc.y))*H)+1;
                for(int y=Mathf.Max(0,y0);y<=Mathf.Min(H-1,y1);y++)for(int x=Mathf.Max(0,x0);x<=Mathf.Min(W-1,x1);x++){
                    var p=new Vector2((x+.5f)/W,(y+.5f)/H);var w=Bary(p,ua,ub,uc);if(w.x<-.02f||w.y<-.02f||w.z<-.02f)continue;
                    var point=a*w.x+b*w.y+c*w.z;var mirror=new Vector3(-point.x,point.y,point.z);
                    // Closest source triangle to the mirrored point; take its interpolated UV.
                    float best=float.MaxValue;Vector2 bestUv=p;
                    foreach(int s0 in source){Vector3 sa=v[t[s0]],sb=v[t[s0+1]],sc=v[t[s0+2]];var q=ClosestOnTriangle(mirror,sa,sb,sc,out var bw);float d=(q-mirror).sqrMagnitude;if(d<best){best=d;bestUv=uv[t[s0]]*bw.x+uv[t[s0+1]]*bw.y+uv[t[s0+2]]*bw.z;}}
                    dst[y*W+x]=tex.GetPixelBilinear(bestUv.x,bestUv.y);changed++;
                }
            }
            var output=new Texture2D(W,H,TextureFormat.RGB24,false);output.SetPixels(dst);output.Apply();File.WriteAllBytes(texPath,output.EncodeToJPG(92));AssetDatabase.ImportAsset(texPath);
            Debug.Log("SAMPLE130_EYE texels="+changed+" sourceTriangles="+source.Count);
        }
        static Vector3 Bary(Vector2 p,Vector2 a,Vector2 b,Vector2 c){var v0=b-a;var v1=c-a;var v2=p-a;float d00=Vector2.Dot(v0,v0),d01=Vector2.Dot(v0,v1),d11=Vector2.Dot(v1,v1),d20=Vector2.Dot(v2,v0),d21=Vector2.Dot(v2,v1);float den=d00*d11-d01*d01;if(Mathf.Abs(den)<1e-12f)return new Vector3(-1,-1,-1);float y=(d11*d20-d01*d21)/den,z=(d00*d21-d01*d20)/den;return new Vector3(1-y-z,y,z);}
        static Vector3 ClosestOnTriangle(Vector3 p,Vector3 a,Vector3 b,Vector3 c,out Vector3 w)
        {
            var n=Vector3.Cross(b-a,c-a);var q=p-n*Vector3.Dot(p-a,n)/Mathf.Max(1e-12f,n.sqrMagnitude);
            var v0=b-a;var v1=c-a;var v2=q-a;float d00=Vector3.Dot(v0,v0),d01=Vector3.Dot(v0,v1),d11=Vector3.Dot(v1,v1),d20=Vector3.Dot(v2,v0),d21=Vector3.Dot(v2,v1);float den=Mathf.Max(1e-12f,d00*d11-d01*d01);
            float y=(d11*d20-d01*d21)/den,z=(d00*d21-d01*d20)/den;float x=1-y-z;x=Mathf.Clamp01(x);y=Mathf.Clamp01(y);z=Mathf.Clamp01(z);float sum=x+y+z;w=new Vector3(x/sum,y/sum,z/sum);return a*w.x+b*w.y+c*w.z;
        }

        public static void RenderFace()
        {
            HeroLighting.Apply();var go=(GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sango3/Resources/Heroes/Final/yuansoldier.prefab"));var skin=go.GetComponent<SkinnedMeshRenderer>();
            var light=new GameObject("key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(20,180,0);RenderSettings.ambientLight=new Color(.6f,.6f,.6f);
            var camera=new GameObject("camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.gray;camera.orthographic=true;camera.orthographicSize=.28f;camera.transform.position=new Vector3(0,4.0f,3);camera.transform.LookAt(new Vector3(0,4.0f,0));
            var rt=new RenderTexture(600,600,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(600,600,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,600,600),0,0);RenderTexture.active=null;File.WriteAllBytes(Evidence+"soldier-face.png",tex.EncodeToPNG());
            Object.DestroyImmediate(go);Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(camera.gameObject);
        }

        // Bake, render and check the move sets of every playable hero (Guan Yu included).
        public static void BuildLiubei(){heroId="liubei";Build();RenderDetail();heroId="guanyu";}
        public static void BuildRobes(){foreach(var id in new[]{"liubei","zhugeliang"}){heroId=id;try{MatchHands();}catch(System.Exception ex){Debug.LogWarning(ex.Message);}Build();RenderDetail();}heroId="guanyu";}
        public static void BuildAllHeroes()
        {
            var summary=new StringBuilder();
            foreach(var id in BattleRules.Portraits){heroId=id;if(id!="guanyu")try{MatchHands();}catch(System.Exception ex){Debug.LogWarning("Hand match skipped for "+id+": "+ex.Message);}Build();GripReportFor();RenderDetail();
                summary.AppendLine("== "+id);summary.Append(File.ReadAllText(Evidence+(id=="guanyu"?"":id+"-")+"moves.txt"));}
            heroId="guanyu";File.WriteAllText(Evidence+"all-heroes-moves.txt",summary.ToString());Debug.Log("SAMPLE130_ALL_HEROES_BUILT");
        }
        static void GripReportFor(){if(AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefab).GetComponent<RecoveredPolearmGrip>())GripReport();}

        // 0.17.0 experiment (see Review-0.17.0): a boot scene did not remove the long first frame, so it was taken out.
        public static void RemoveBootScene(){EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Sango3/Generated/Yingchuan.unity",true)};AssetDatabase.DeleteAsset("Assets/Sango3/Generated/Boot.unity");}

        // Slow-motion film strips: each move sampled every 1/30 s (whole body, front three-quarter view), one row per move,
        // so the transitions between keys (anticipation, contact, recovery, feet, sleeves, weapon) can be reviewed in order.
        public static void RenderFilmstrips()
        {
            Directory.CreateDirectory(Evidence+"filmstrips/");
            foreach(var id in BattleRules.Portraits){heroId=id;Load();HeroLighting.Apply();
                var light=new GameObject("key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(40,-35,0);RenderSettings.ambientLight=new Color(.55f,.56f,.6f);
                var camera=new GameObject("camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.10f,.12f,.15f);camera.orthographic=true;camera.orthographicSize=2.9f;
                var moves=HeroMoves.For(HeroIndex).All.ToArray();const int cell=200;int cols=46;
                var sheet=new Texture2D(cols*cell/2,moves.Length*cell,TextureFormat.RGB24,false);
                for(int m=0;m<moves.Length;m++){var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(HeroFolder+moves[m].state+".anim");int frames=Mathf.Min(cols,Mathf.CeilToInt(moves[m].length*30)+1);
                    for(int f=0;f<frames;f++){float t=Mathf.Min(moves[m].length,f/30f);clip.SampleAnimation(root,t);if(grip)grip.Solve();SolveDrape();
                        var shown=new List<GameObject>();foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>()){var baked=new Mesh();r.BakeMesh(baked);var go=new GameObject("pose",typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=baked;go.GetComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;go.transform.SetPositionAndRotation(r.transform.position,r.transform.rotation);shown.Add(go);r.enabled=false;}
                        camera.transform.position=new Vector3(5,3.4f,8.5f);camera.transform.LookAt(new Vector3(0,2.3f,.5f));
                        // Contact frames get a lighter background so they stand out in the strip.
                        bool contact=Mathf.Abs(t-moves[m].contact)<1/60f;camera.backgroundColor=contact?new Color(.22f,.20f,.12f):new Color(.10f,.12f,.15f);
                        var target=RenderTexture.GetTemporary(cell/2,cell,24);camera.aspect=.5f;camera.targetTexture=target;camera.Render();RenderTexture.active=target;sheet.ReadPixels(new Rect(0,0,cell/2,cell),f*cell/2,(moves.Length-1-m)*cell);RenderTexture.active=null;camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);
                        foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>())r.enabled=true;foreach(var go in shown){Object.DestroyImmediate(go.GetComponent<MeshFilter>().sharedMesh);Object.DestroyImmediate(go);}}}
                sheet.Apply();File.WriteAllBytes(Evidence+"filmstrips/"+id+".png",sheet.EncodeToPNG());Object.DestroyImmediate(sheet);
                Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(root);}
            heroId="guanyu";Debug.Log("SAMPLE180_FILMSTRIPS");
        }
    }
}
