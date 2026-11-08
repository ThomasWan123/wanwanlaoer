using UnityEngine;
using UnityEngine.Rendering;
namespace Sango3
{
    public static class HeroLighting
    {
        public static void Apply(bool night=false){var sky=Resources.Load<Cubemap>("Lighting/BattleSky");if(!sky)return;RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflection=sky;RenderSettings.reflectionIntensity=night?.25f:.85f;}
    }
}
