Shader "Sango3/RiverSurface"
{
    Properties { _BaseColor("Water", Color)=(0.055,0.23,0.28,1) _FresnelColor("Sky reflection", Color)=(0.23,0.44,0.47,1) _Silt("Silt swirls", Range(0,1))=0 _Width("River width (units across the strip)", Float)=11.75 }
    SubShader {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Input { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Output { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float2 uv:TEXCOORD1; };
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor; half4 _FresnelColor; half _Silt; float _Width;
            CBUFFER_END
            // Value noise and fbm: irregular patterns instead of repeating sine stripes.
            float Hash(float2 p){p=frac(p*float2(123.34,456.21));p+=dot(p,p+45.32);return frac(p.x*p.y);}
            float Noise(float2 p){float2 i=floor(p),f=frac(p);float2 u=f*f*(3-2*f);
                return lerp(lerp(Hash(i),Hash(i+float2(1,0)),u.x),lerp(Hash(i+float2(0,1)),Hash(i+float2(1,1)),u.x),u.y);}
            float Fbm(float2 p){float s=0,a=.5;for(int k=0;k<4;k++){s+=Noise(p)*a;p=p*2.03+float2(17.1,9.7);a*=.5;}return s;}
            Output Vert(Input v){Output o;o.world=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.uv=v.uv;return o;}
            half4 Frag(Output i):SV_Target {
                float2 p=i.world.xz;float time=_Time.y;
                // The Yellow River flows along +x; small waves drift with it, larger ones more slowly.
                float2 flow=float2(time*.35,0);
                float h0=Fbm(p*float2(.9,1.6)-flow);float h1=Fbm(p*float2(2.6,3.8)-flow*1.7+11);
                float2 slope=float2(Fbm(p*float2(.9,1.6)-flow+float2(.07,0))-h0,Fbm(p*float2(.9,1.6)-flow+float2(0,.07))-h0)*5.5
                           +float2(Fbm(p*float2(2.6,3.8)-flow*1.7+11+float2(.05,0))-h1,Fbm(p*float2(2.6,3.8)-flow*1.7+11+float2(0,.05))-h1)*3.2;
                float3 normal=normalize(float3(-slope.x*.35,1,-slope.y*.35));
                float3 view=GetWorldSpaceNormalizeViewDir(i.world);
                Light sun=GetMainLight();
                float fresnel=.04+.96*pow(1-saturate(dot(normal,view)),4);
                float glint=pow(saturate(dot(reflect(-sun.direction,normal),view)),90)*1.6+pow(saturate(dot(reflect(-sun.direction,normal),view)),14)*.12;
                // Distance from the near (south) bank across the strip: shallow silty water at the bank, deeper and darker out.
                float across=i.uv.y*_Width;float edgeNoise=(Fbm(float2(p.x*.6,3.1))-.5)*.5;
                float shallow=1-smoothstep(0,2.6+edgeNoise*2,across);float far=smoothstep(_Width-1.2,_Width,across);
                // Sediment: broad lighter and darker patches drifting downstream, stretched along the flow.
                float sediment=Fbm(p*float2(.08,.22)-float2(time*.05,0));float streaks=Fbm(p*float2(.18,1.4)-float2(time*.12,0));
                half3 deep=_BaseColor.rgb*.78;half3 silty=_BaseColor.rgb*half3(1.28,1.22,1.12);
                // A deeper channel runs mid-river, wandering a little; sediment patches are broad and clearly lighter or darker.
                float channel=exp(-pow((across-_Width*.55-(Fbm(float2(p.x*.05,1.3))-.5)*4)/(_Width*.18),2));
                half3 water=lerp(deep,_BaseColor.rgb*1.08,saturate(.2+1.3*(sediment-.25)));water=lerp(water,deep*.85,channel*.55);water*=lerp(1,lerp(.84,1.14,streaks),_Silt);
                water=lerp(water,silty,shallow*.8);
                half3 sky=lerp(_FresnelColor.rgb,_FresnelColor.rgb*1.15+.04,saturate(view.y));
                half3 color=lerp(water,sky,fresnel*(.55+.45*(1-shallow)))*(.82+.18*sun.color.r)+glint*sun.color*.55;
                // Broken foam along the bank, thicker where small waves pile up.
                float foamBand=smoothstep(.85,0,across-.05-edgeNoise*.4);float foam=foamBand*smoothstep(.45,.75,Fbm(p*float2(3.5,5)-flow*2+h1));
                color=lerp(color,half3(.93,.88,.78),foam*.55);
                float alpha=lerp(.78,.95,1-shallow)*(1-far)*smoothstep(0,.12,across);
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
}
