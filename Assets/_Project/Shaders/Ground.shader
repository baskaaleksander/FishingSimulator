Shader "FishingSim/Ground"
{
    Properties
    {
        _GrassMap ("Grass", 2D) = "white" {}
        _SandMap ("Sand", 2D) = "white" {}
        _TileMeters ("Tile Meters", Float) = 5
        _SandHeight ("Sand Height", Float) = 0.05
        _SandBlend ("Sand Blend", Float) = 0.4
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_GrassMap);
            SAMPLER(sampler_GrassMap);
            TEXTURE2D(_SandMap);
            SAMPLER(sampler_SandMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _GrassMap_ST;
                float4 _SandMap_ST;
                float _TileMeters;
                float _SandHeight;
                float _SandBlend;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogCoord : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(v.normalOS);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = n.normalWS;
                o.fogCoord = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.positionWS.xz / _TileMeters;
                half3 grass = SAMPLE_TEXTURE2D(_GrassMap, sampler_GrassMap, uv).rgb;
                half3 grassLarge = SAMPLE_TEXTURE2D(_GrassMap, sampler_GrassMap, uv * 0.23 + 0.37).rgb;
                grass *= lerp(0.8, 1.15, grassLarge.g);
                half3 sand = SAMPLE_TEXTURE2D(_SandMap, sampler_SandMap, uv * 1.3).rgb;

                float height = i.positionWS.y + (grass.r - 0.3) * 0.25;
                float sandAmount = 1.0 - smoothstep(_SandHeight, _SandHeight + _SandBlend, height);
                half3 albedo = lerp(grass, sand, sandAmount);

                float3 normal = normalize(i.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half diffuse = saturate(dot(normal, mainLight.direction)) * mainLight.shadowAttenuation;

                half3 color = albedo * (mainLight.color * diffuse + SampleSH(normal));
                color = MixFog(color, i.fogCoord);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
