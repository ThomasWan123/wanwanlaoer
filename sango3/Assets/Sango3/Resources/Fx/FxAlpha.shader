// Effect shader for particles and ribbons: texture x vertex colour x tint. Loaded from Resources so it ships.
Shader "Sango3/FxAlpha"
{
    Properties { _MainTex("Texture",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1) _Boost("Boost",Float)=1 }
    SubShader {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS:POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST; half4 _BaseColor; half _Boost;
            CBUFFER_END
            V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.color=i.color*_BaseColor;o.uv=TRANSFORM_TEX(i.uv,_MainTex);return o;}
            half4 frag(V i):SV_Target{half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color;c.rgb*=_Boost;return c;}
            ENDHLSL
        }
    }
}
