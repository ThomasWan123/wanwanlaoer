using System.Collections.Generic;
namespace Sango3.Editor
{
    // Key poses for the five other playable heroes' move sets, authored like Guan Yu's (GuanyuSample.Moves):
    // anticipation, a fast drive to the contact key, a short hold, then recovery to Idle. Hand positions use S(...),
    // relative to the right shoulder at rest (x toward the body's centre, y up, z forward), so they fit each body.
    // Weapon direction is where the point/blade/fan faces; edge is the cutting edge (or fan face) direction.
    public static partial class GuanyuSample
    {
        static List<Key> HeroKeys(string id,string state)
        {
            switch(id){
            case "zhaoyun":return Spear(state,false);
            case "zhangfei":return Spear(state,true);
            case "liubei":{
                // Liu Bei's wide robe sleeves (drape bones) balloon when the arm rises above the shoulder: keep his hand
                // below it and let the body carry the cut; the sword still points skyward for 仁德.
                var keys=state=="Attack2"?LiubeiSweep():state=="Attack3"?LiubeiThrust():Sword(state,false);foreach(var k in keys)if(k.hand.HasValue)k.hand=new UnityEngine.Vector3(k.hand.Value.x,UnityEngine.Mathf.Min(k.hand.Value.y,-.55f),UnityEngine.Mathf.Min(k.hand.Value.z,.85f));return keys;}
            case "caocao":return Sword(state,true);
            case "zhugeliang":{
                // Wide robe sleeves shear when the arm rises above the shoulder or reaches far forward: keep the fan hand
                // below shoulder height and closer in; the fan's direction still carries the gesture.
                var keys=Fan(state);foreach(var k in keys)if(k.hand.HasValue)k.hand=new UnityEngine.Vector3(k.hand.Value.x,UnityEngine.Mathf.Min(k.hand.Value.y,-.45f),UnityEngine.Mathf.Min(k.hand.Value.z,.75f));return keys;}
            }
            throw new System.ArgumentException("No move keys for "+id);
        }

        // 赵云 (quick) and 张飞 (heavy): thrust, sweep, finisher; the ultimate is a spinning sweep or a planted roar.
        static List<Key> Spear(string state,bool heavy)
        {
            float w=heavy?1.15f:1f; // heavier hero: deeper coil and lunge
            switch(state){
            case "Attack1":{float c=heavy?.32f:.24f,len=heavy?.70f:.50f;return new List<Key>{K(0),
                K(c*.62f).P(0,-.06f*w,-.10f*w).A(Spine,0,-10,0).A(Chest,-4,-18,0).A(Head,2,12,0).A(RThigh,8,0,6).A(LThigh,-10,0,-6).A(LShin,12,0,0)
                    .S(.20f,-1.0f,-.45f*w, .05f,-.10f,1, 0,1,0),
                K(c).P(0,-.10f*w,.22f*w).A(Spine,6,8,0).A(Chest,10,16,0).A(Head,-4,-10,0).A(RThigh,16,0,6).A(LThigh,-28,0,-6).A(LShin,26,0,0).A(Skirt,-6,0,0)
                    .S(.42f,-.82f,1.15f*w, .02f,-.06f,1, 0,1,0),
                K(c+.10f).P(0,-.10f*w,.22f*w).A(Spine,6,8,0).A(Chest,10,16,0).A(Head,-4,-10,0).A(RThigh,16,0,6).A(LThigh,-28,0,-6).A(LShin,26,0,0)
                    .S(.42f,-.84f,1.10f*w, .02f,-.08f,1, 0,1,0),
                K(len)};}
            case "Attack2":{float c=heavy?.36f:.27f,len=heavy?.75f:.55f;return new List<Key>{K(0),
                K(c*.65f).P(.04f,-.08f,-.04f).A(Spine,0,18,0).A(Chest,-4,34,-4).A(Head,0,-22,0).A(RThigh,-6,0,10).A(LThigh,8,0,-12)
                    .S(-.35f,-.62f,.25f, -.85f,0,.5f, 0,1,0),
                K(c).P(0,-.12f,.08f).A(Spine,4,-2,0).A(Chest,8,-6,2).A(Head,0,4,0).A(RThigh,-12,0,12).A(LThigh,12,0,-12).A(Skirt,0,0,6)
                    .S(.35f,-.72f,.95f, .25f,-.05f,1, 0,1,0),
                K(c+.10f).P(-.04f,-.11f,.06f).A(Spine,3,-16,0).A(Chest,6,-34,4).A(Head,0,24,0).A(RThigh,-12,0,10).A(LThigh,10,0,-10)
                    .S(.85f,-.72f,.55f, .95f,-.05f,.25f, 0,1,0),
                K(len)};}
            case "Attack3":
                if(heavy){return new List<Key>{K(0), // overhead smash
                    K(.30f).P(0,-.16f,-.12f).A(Spine,-6,-8,0).A(Chest,-14,-12,0).A(Head,10,8,0).A(RThigh,-8,0,6).A(LThigh,-6,0,-8).A(LShin,16,0,0)
                        .S(.25f,.30f,-.10f, 0,.85f,-.5f, 0,0,1),
                    K(.52f).P(0,-.26f,.28f).A(Spine,14,4,0).A(Chest,26,6,0).A(Head,-16,-4,0).A(RThigh,20,0,6).A(LThigh,-34,0,-6).A(LShin,34,0,0).A(Skirt,-12,0,0)
                        .S(.45f,-.95f,1.10f, 0,-.55f,.85f, 0,.85f,.55f),
                    K(.64f).P(0,-.25f,.28f).A(Spine,13,4,0).A(Chest,24,6,0).A(Head,-15,-4,0).A(RThigh,20,0,6).A(LThigh,-33,0,-6).A(LShin,33,0,0)
                        .S(.45f,-.98f,1.08f, 0,-.6f,.8f, 0,.8f,.6f),
                    K(1.0f)};}
                return new List<Key>{K(0), // lunging thrust
                    K(.24f).P(0,-.20f,-.16f).A(Spine,-2,-14,0).A(Chest,-6,-22,0).A(Head,6,14,0).A(RThigh,-18,0,6).A(RShin,30,0,0).A(LThigh,-4,0,-8).A(LShin,22,0,0)
                        .S(.10f,-.95f,-.55f, .05f,.05f,1, 0,1,0),
                    K(.36f).P(0,-.18f,.40f).A(Spine,8,10,0).A(Chest,14,18,0).A(Head,-6,-12,0).A(RThigh,22,0,6).A(LThigh,-38,0,-6).A(LShin,36,0,0).A(Skirt,-10,0,0)
                        .S(.45f,-.80f,1.40f, 0,-.02f,1, 0,1,0),
                    K(.48f).P(0,-.17f,.40f).A(Spine,8,10,0).A(Chest,14,18,0).A(Head,-6,-12,0).A(RThigh,22,0,6).A(LThigh,-38,0,-6).A(LShin,36,0,0)
                        .S(.45f,-.82f,1.36f, 0,-.05f,1, 0,1,0),
                    K(.70f)};
            case "Ultimate":
                if(heavy){return new List<Key>{K(0), // 当阳怒吼: plant the spear, chest out, roar
                    K(.40f).P(0,-.22f,-.06f).A(Spine,10,0,0).A(Chest,18,0,0).A(Head,12,0,0).A(RThigh,-24,0,10).A(RShin,36,0,0).A(LThigh,-24,0,-10).A(LShin,36,0,0)
                        .S(.30f,-.70f,.55f, 0,.95f,.2f, 0,0,1),
                    K(.70f).P(0,-.08f,.02f).A(Spine,-10,0,0).A(Chest,-22,0,0).A(Head,-26,0,0).A(RThigh,-12,0,14).A(RShin,16,0,0).A(LThigh,-12,0,-14).A(LShin,16,0,0).A(Skirt,-8,0,0)
                        .S(.30f,-1.05f,.75f, 0,1,.12f, 0,0,1),
                    K(.95f).P(0,-.08f,.02f).A(Spine,-9,0,0).A(Chest,-20,0,0).A(Head,-24,0,0).A(RThigh,-12,0,14).A(RShin,16,0,0).A(LThigh,-12,0,-14).A(LShin,16,0,0)
                        .S(.30f,-1.05f,.75f, 0,1,.12f, 0,0,1),
                    K(1.25f)};}
                return new List<Key>{K(0), // 七进七出: coil and spin the spear round the body
                    K(.36f).P(0,-.24f,-.04f).A(Hips,0,-40,0).A(Spine,4,-24,0).A(Chest,4,-30,0).A(Head,0,30,0).A(RThigh,-26,0,8).A(RShin,40,0,0).A(LThigh,-20,0,-8).A(LShin,34,0,0)
                        .S(-.45f,-.65f,-.25f, -.95f,-.05f,-.2f, 0,1,0),
                    K(.58f).P(0,-.18f,.10f).A(Hips,0,10,0).A(Spine,6,10,0).A(Chest,8,14,0).A(Head,0,-10,0).A(RThigh,-14,0,10).A(RShin,22,0,0).A(LThigh,-16,0,-10).A(LShin,22,0,0)
                        .S(.25f,-.72f,.90f, .2f,-.05f,1, 0,1,0),
                    K(.72f).P(0,-.16f,.12f).A(Hips,0,40,0).A(Spine,6,24,0).A(Chest,8,30,0).A(Head,0,-24,0).A(RThigh,-12,0,10).A(RShin,20,0,0).A(LThigh,-14,0,-10).A(LShin,20,0,0).A(Skirt,0,0,10)
                        .S(.95f,-.72f,.35f, .95f,-.05f,-.2f, 0,1,0),
                    K(.95f).P(0,-.14f,.10f).A(Hips,0,30,0).A(Spine,4,18,0).A(Chest,6,22,0).A(Head,0,-18,0).A(RThigh,-10,0,8).A(LThigh,-12,0,-8)
                        .S(.85f,-.78f,.10f, .8f,-.1f,-.55f, 0,1,0),
                    K(1.30f)};
            }
            return null;
        }

        // 刘备 and 曹操: one-handed sword; 仁德 raises it to the sky, 魏武号令 points it forward.
        static List<Key> Sword(string state,bool command)
        {
            switch(state){
            case "Attack1":return new List<Key>{K(0), // diagonal cut
                K(.17f).P(0,-.05f,-.05f).A(Spine,0,-10,0).A(Chest,-6,-24,4).A(Head,4,16,0).A(RThigh,6,0,6).A(LThigh,-8,0,-6).A(LShin,10,0,0)
                    .S(-.05f,.20f,.15f, -.2f,.85f,-.45f, .35f,.5f,.8f),
                K(.28f).P(0,-.12f,.14f).A(Spine,8,12,0).A(Chest,16,22,-8).A(Head,-6,-16,0).A(RThigh,16,0,6).A(LThigh,-26,0,-6).A(LShin,26,0,0).A(Skirt,-6,0,0)
                    .S(.55f,-.60f,.80f, .55f,-.25f,.8f, .5f,-.85f,.1f),
                K(.38f).P(0,-.13f,.15f).A(Spine,8,14,0).A(Chest,17,26,-8).A(Head,-6,-18,0).A(RThigh,16,0,6).A(LThigh,-26,0,-6).A(LShin,26,0,0)
                    .S(.70f,-.90f,.40f, .6f,-.65f,.3f, .3f,-.5f,-.8f),
                K(.60f)};
            case "Attack2":return new List<Key>{K(0), // backhand sweep
                K(.20f).P(.04f,-.06f,-.03f).A(Spine,0,16,0).A(Chest,-4,30,-4).A(Head,0,-20,0).A(RThigh,-6,0,10).A(LThigh,8,0,-12)
                    .S(.70f,-.40f,.40f, .85f,.1f,.45f, 0,0,-1),
                K(.31f).P(0,-.10f,.08f).A(Spine,4,-2,0).A(Chest,8,-6,2).A(Head,0,4,0).A(RThigh,-12,0,12).A(LThigh,12,0,-12).A(Skirt,0,0,6)
                    .S(.15f,-.45f,.95f, -.1f,.05f,1, -1,0,0),
                K(.42f).P(-.04f,-.10f,.06f).A(Spine,3,-14,0).A(Chest,6,-30,4).A(Head,0,22,0).A(RThigh,-12,0,10).A(LThigh,10,0,-10)
                    .S(-.45f,-.50f,.60f, -.9f,0,.3f, -.3f,0,-.9f),
                K(.65f)};
            case "Attack3":return new List<Key>{K(0), // thrust
                K(.26f).P(0,-.16f,-.12f).A(Spine,-2,-12,0).A(Chest,-6,-18,0).A(Head,6,12,0).A(RThigh,-14,0,6).A(RShin,24,0,0).A(LThigh,-4,0,-8).A(LShin,18,0,0)
                    .S(-.05f,-.65f,-.30f, 0,.1f,1, 0,1,0),
                K(.40f).P(0,-.15f,.32f).A(Spine,8,10,0).A(Chest,14,16,0).A(Head,-6,-10,0).A(RThigh,20,0,6).A(LThigh,-34,0,-6).A(LShin,32,0,0).A(Skirt,-8,0,0)
                    .S(.40f,-.55f,1.25f, 0,0,1, 0,1,0),
                K(.52f).P(0,-.14f,.32f).A(Spine,8,10,0).A(Chest,14,16,0).A(Head,-6,-10,0).A(RThigh,20,0,6).A(LThigh,-34,0,-6).A(LShin,32,0,0)
                    .S(.40f,-.57f,1.20f, 0,-.03f,1, 0,1,0),
                K(.80f)};
            case "Ultimate":
                if(command)return new List<Key>{K(0), // 魏武号令: draw back, then point the sword at the enemy
                    K(.35f).P(0,-.06f,-.08f).A(Spine,-4,-12,0).A(Chest,-8,-20,0).A(Head,4,10,0)
                        .S(-.05f,-.35f,-.15f, -.2f,.9f,-.3f, 1,0,0),
                    K(.70f).P(0,.02f,.12f).A(Spine,-6,6,0).A(Chest,-12,10,0).A(Head,-8,-6,0).A(RThigh,10,0,6).A(LThigh,-18,0,-6).A(LShin,14,0,0)
                        .S(.35f,-.15f,1.05f, 0,.18f,1, 0,1,0),
                    K(.95f).P(0,.02f,.12f).A(Spine,-6,6,0).A(Chest,-12,10,0).A(Head,-8,-6,0).A(RThigh,10,0,6).A(LThigh,-18,0,-6).A(LShin,14,0,0)
                        .S(.35f,-.15f,1.03f, 0,.15f,1, 0,1,0),
                    K(1.20f)};
                return new List<Key>{K(0), // 仁德: bow the head, then raise the sword to the sky
                    K(.35f).P(0,-.10f,0).A(Spine,8,0,0).A(Chest,12,0,0).A(Head,16,0,0)
                        .S(.30f,-.85f,.45f, 0,.6f,.8f, 1,0,0),
                    K(.70f).P(0,.04f,0).A(Spine,-6,0,0).A(Chest,-12,0,0).A(Head,-18,0,0)
                        .S(.20f,.55f,.30f, 0,1,.12f, 1,0,0),
                    K(.95f).P(0,.04f,0).A(Spine,-6,0,0).A(Chest,-11,0,0).A(Head,-16,0,0)
                        .S(.20f,.53f,.30f, 0,1,.12f, 1,0,0),
                    K(1.20f)};
            }
            return null;
        }

        // Liu Bei's sweep is a forehand cut that opens outward from his right side: the backhand version crossed the arm,
        // and the hanging sleeve with it, in front of his face.
        static List<Key> LiubeiSweep()=>new List<Key>{K(0),
            K(.20f).P(-.03f,-.05f,-.04f).A(Spine,0,-14,0).A(Chest,-4,-24,2).A(Head,0,6,0).A(RThigh,-6,0,8).A(LThigh,8,0,-10)
                .S(-.40f,-.62f,.15f, -.75f,.05f,.65f, 0,0,1),
            // 0.25: back to the 0.23 path (the 0.24 path held the arm in front longer and the sleeve stayed bunched longer);
            // the sleeve itself is handled by the sleeve chain and torso collision instead.
            K(.31f).P(0,-.10f,.08f).A(Spine,4,4,0).A(Chest,8,8,-2).A(Head,0,-2,0).A(RThigh,-12,0,12).A(LThigh,12,0,-12).A(Skirt,0,0,-6)
                .S(.15f,-.62f,.85f, .35f,0,.95f, 1,0,0),
            K(.42f).P(.02f,-.10f,.06f).A(Spine,3,14,0).A(Chest,6,22,-2).A(Head,0,-8,0).A(RThigh,-12,0,10).A(LThigh,10,0,-10)
                .S(.45f,-.65f,.55f, .85f,-.05f,.5f, .45f,0,-.9f),
            K(.65f)};

        // Liu Bei's thrust draws the sword back at the hip, not at the chest, so the forearm (and sleeve) stays below his face.
        static List<Key> LiubeiThrust()=>new List<Key>{K(0),
            K(.26f).P(0,-.16f,-.12f).A(Spine,-2,-14,0).A(Chest,-4,-20,0).A(Head,4,8,0).A(RThigh,-14,0,6).A(RShin,24,0,0).A(LThigh,-4,0,-8).A(LShin,18,0,0)
                .S(-.30f,-.95f,-.10f, .1f,.15f,1, 0,1,0),
            K(.40f).P(0,-.15f,.32f).A(Spine,8,10,0).A(Chest,12,14,0).A(Head,-4,-8,0).A(RThigh,20,0,6).A(LThigh,-34,0,-6).A(LShin,32,0,0).A(Skirt,-8,0,0)
                .S(.35f,-.75f,.85f, 0,.05f,1, 0,1,0),
            K(.52f).P(0,-.14f,.32f).A(Spine,8,10,0).A(Chest,12,14,0).A(Head,-4,-8,0).A(RThigh,20,0,6).A(LThigh,-34,0,-6).A(LShin,32,0,0)
                .S(.35f,-.77f,.83f, 0,.02f,1, 0,1,0),
            K(.80f)};

        // 诸葛亮 羽扇: flick, sweeping gesture, raise and point; 八阵图 raises the fan high and brings it down.
        static List<Key> Fan(string state)
        {
            switch(state){
            case "Attack1":return new List<Key>{K(0),
                K(.22f).A(Chest,-4,-14,0).A(Head,2,10,0).S(.10f,-.55f,.20f, -.3f,.85f,-.3f, 0,0,1),
                K(.34f).A(Chest,4,10,0).A(Head,-2,-6,0).S(.40f,-.55f,.75f, .25f,.75f,.6f, 0,-.6f,.8f),
                K(.46f).A(Chest,4,12,0).A(Head,-2,-8,0).S(.42f,-.58f,.72f, .25f,.7f,.65f, 0,-.6f,.8f),
                K(.75f)};
            case "Attack2":return new List<Key>{K(0),
                K(.25f).A(Spine,0,14,0).A(Chest,-2,24,0).A(Head,0,-14,0).S(-.25f,-.55f,.40f, -.6f,.6f,.45f, 0,0,1),
                K(.38f).A(Spine,0,-6,0).A(Chest,2,-12,0).A(Head,0,8,0).S(.65f,-.55f,.60f, .7f,.5f,.45f, 0,0,1),
                K(.50f).A(Spine,0,-8,0).A(Chest,2,-16,0).A(Head,0,10,0).S(.72f,-.58f,.52f, .75f,.45f,.4f, 0,0,1),
                K(.80f)};
            case "Attack3":return new List<Key>{K(0),
                K(.30f).A(Chest,-8,0,0).A(Head,8,0,0).S(.10f,.15f,.35f, 0,1,.1f, 0,0,1),
                K(.52f).P(0,-.04f,.10f).A(Spine,4,6,0).A(Chest,10,10,0).A(Head,-6,-6,0).A(LThigh,-14,0,-4).A(LShin,10,0,0).S(.32f,-.35f,1.0f, .1f,.4f,.9f, 0,1,0),
                K(.66f).P(0,-.04f,.10f).A(Spine,4,6,0).A(Chest,10,10,0).A(Head,-6,-6,0).A(LThigh,-14,0,-4).A(LShin,10,0,0).S(.32f,-.37f,.98f, .1f,.38f,.9f, 0,1,0),
                K(1.0f)};
            case "Ultimate":return new List<Key>{K(0),
                K(.40f).P(0,.02f,0).A(Spine,-6,0,0).A(Chest,-12,0,0).A(Head,-14,0,0).S(.12f,.45f,.30f, 0,1,.1f, 0,0,1),
                K(.70f).P(0,-.10f,.12f).A(Spine,8,0,0).A(Chest,16,0,0).A(Head,-4,0,0).A(LThigh,-16,0,-4).A(LShin,12,0,0).S(.35f,-.50f,.90f, .05f,.3f,.95f, 0,1,0),
                K(.95f).P(0,-.09f,.12f).A(Spine,7,0,0).A(Chest,14,0,0).A(Head,-4,0,0).A(LThigh,-16,0,-4).A(LShin,12,0,0).S(.35f,-.52f,.88f, .05f,.3f,.95f, 0,1,0),
                K(1.25f)};
            }
            return null;
        }
    }
}
