Shader "FishingSim/Water"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.25, 0.68, 0.72, 0.18)
        _DeepColor ("Deep Color", Color) = (0.02, 0.2, 0.35, 0.8)
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _DepthDistance ("Depth Distance", Float) = 5
        _FoamDistance ("Foam Distance", Float) = 0.3
        _WaveScale ("Wave Scale", Float) = 0.45
        _WaveSpeed ("Wave Speed", Float) = 0.12
        _NormalStrength ("Normal Strength", Float) = 0.35
        _SpecularPower ("Specular Power", Float) = 180
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "Water"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _FoamColor;
                float _DepthDistance;
                float _FoamDistance;
                float _WaveScale;
                float _WaveSpeed;
                float _NormalStrength;
                float _SpecularPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float fogCoord : TEXCOORD1;
            };

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float WaveHeight(float2 p)
            {
                float t = _Time.y * _WaveSpeed;
                float h = Noise(p * _WaveScale + float2(t, t * 0.7)) * 0.6;
                h += Noise(p * _WaveScale * 2.3 - float2(t * 0.8, t)) * 0.4;
                return h;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.fogCoord = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 xz = i.positionWS.xz;
                const float e = 0.05;
                float slopeX = (WaveHeight(xz + float2(e, 0)) - WaveHeight(xz - float2(e, 0))) / (2.0 * e);
                float slopeZ = (WaveHeight(xz + float2(0, e)) - WaveHeight(xz - float2(0, e))) / (2.0 * e);
                float3 normal = normalize(float3(-slopeX * _NormalStrength, 1.0, -slopeZ * _NormalStrength));

                float2 screenUV = i.positionCS.xy / _ScaledScreenParams.xy;
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float surfaceDepth = i.positionCS.w;
                float depthDiff = max(0.0, sceneDepth - surfaceDepth);

                float depth01 = saturate(depthDiff / _DepthDistance);
                half4 color = lerp(_ShallowColor, _DeepColor, depth01);

                float foam = 1.0 - saturate(depthDiff / _FoamDistance);
                foam *= saturate(0.4 + Noise(xz * 3.0 + _Time.y * 0.2));
                color.rgb = lerp(color.rgb, _FoamColor.rgb, foam * 0.85);
                color.a = max(color.a, foam);

                float3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), 4.0);
                color.rgb += fresnel * 0.4 * half3(0.6, 0.75, 0.9);
                color.a = lerp(color.a, 1.0, fresnel * 0.25);

                Light mainLight = GetMainLight();
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float spec = pow(saturate(dot(normal, halfDir)), _SpecularPower);
                color.rgb += spec * mainLight.color * 1.5;

                color.rgb = MixFog(color.rgb, i.fogCoord);
                return color;
            }
            ENDHLSL
        }
    }
}
