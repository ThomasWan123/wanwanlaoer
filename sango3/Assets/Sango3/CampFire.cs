using UnityEngine;
namespace Sango3
{
    // Campfire flicker (stage 16, 0.27): the flame cards change height and brightness out of step with each other and
    // the warm point light breathes with them, so the fire reads as fire and its light moves on the tents and ground.
    public sealed class CampFireFlicker:MonoBehaviour
    {
        public Light fireLight;public Transform[] flames;public float baseIntensity=2.2f;
        Vector3[] scales;float seed;
        void Start(){seed=Random.value*10;scales=new Vector3[flames.Length];for(int i=0;i<flames.Length;i++)scales[i]=flames[i].localScale;}
        void Update()
        {
            float t=Time.time+seed;
            float f=.85f+.1f*Mathf.Sin(t*11.3f)+.07f*Mathf.Sin(t*17.9f+1.3f)+.05f*Mathf.PerlinNoise(t*3.1f,.5f);
            if(fireLight)fireLight.intensity=baseIntensity*f;
            for(int i=0;i<flames.Length;i++){if(!flames[i])continue;float k=.85f+.18f*Mathf.Sin(t*(8.7f+i*2.3f)+i)+.08f*Mathf.Sin(t*(15.1f+i)+i*2);flames[i].localScale=new Vector3(scales[i].x*(.95f+.08f*Mathf.Sin(t*6f+i)),scales[i].y*k,scales[i].z);}
        }
        // Flame texture: a teardrop, hot white-yellow at the core fading to orange, with uneven tongues at the top.
        static Texture2D texture;
        public static Texture2D Texture()
        {
            if(texture)return texture;const int W=64,H=128;texture=new Texture2D(W,H,TextureFormat.RGBA32,false){name="Campfire flame",wrapMode=TextureWrapMode.Clamp};
            var px=new Color[W*H];
            for(int y=0;y<H;y++)for(int x=0;x<W;x++){float u=(x+.5f)/W*2-1,v=(y+.5f)/H;
                // Width of the flame at this height: wide low, narrowing to a point, with tongues from noise.
                float width=Mathf.Lerp(1f,0,Mathf.Pow(v,1.1f))*(.8f+.45f*Mathf.PerlinNoise(u*3+7,v*4));float edge=Mathf.Abs(u)/Mathf.Max(width,1e-3f);
                float a=Mathf.Clamp01(1-edge)*Mathf.SmoothStep(0,1,v/.12f)*(1-Mathf.SmoothStep(.75f,1,v));a=Mathf.Pow(a,.6f);
                float heat=Mathf.Clamp01((1-edge)*1.4f-v*.6f);var c=Color.Lerp(new Color(1,.38f,.08f),new Color(1,.92f,.65f),heat);c.a=a;px[y*W+x]=c;}
            texture.SetPixels(px);texture.Apply(false,true);return texture;
        }
    }
}
