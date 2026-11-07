Shader "FishingSim/Sky"
{
    Properties
    {
        _ZenithColor ("Zenith Color", Color) = (0.18, 0.4, 0.78, 1)
        _HorizonColor ("Horizon Color", Color) = (0.72, 0.84, 0.94, 1)
        _GroundColor ("Ground Color", Color) = (0.55, 0.62, 0.6, 1)
        _CloudColor ("Cloud Color", Color) = (1, 1, 1, 1)
        _CloudShadeColor ("Cloud Shade Color", Color) = (0.62, 0.7, 0.82, 1)
        _CloudScale ("Cloud Scale", Float) = 1.4
        _CloudSpeed ("Cloud Speed", Float) = 0.008
        _CloudCoverage ("Cloud Coverage", Range(0, 1)) = 0.5
        _SunColor ("Sun Color", Color) = (1, 0.95, 0.8, 1)
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "Sky"
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _HorizonColor;
                half4 _GroundColor;
                half4 _CloudColor;
                half4 _CloudShadeColor;
                half4 _SunColor;
                float _CloudScale;
                float _CloudSpeed;
                float _CloudCoverage;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
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

            float Fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < 5; i++)
                {
                    value += Noise(p) * amplitude;
                    p = p * 2.03 + 17.1;
                    amplitude *= 0.5;
                }
                return value;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.direction = v.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 dir = normalize(i.direction);
                float3 sunDir = GetMainLight().direction;

                float up = saturate(dir.y);
                half3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(up, 0.55));
                half3 ground = lerp(_HorizonColor.rgb, _GroundColor.rgb, saturate(-dir.y * 4.0));
                half3 color = dir.y >= 0.0 ? sky : ground;

                float sunDot = saturate(dot(dir, sunDir));
                color += _SunColor.rgb * pow(sunDot, 10.0) * 0.25;
                color += _SunColor.rgb * pow(sunDot, 600.0) * 6.0;

                if (dir.y > 0.0)
                {
                    float2 uv = dir.xz / (dir.y + 0.2) * _CloudScale + _Time.y * _CloudSpeed;
                    float density = Fbm(uv);
                    float cloud = smoothstep(1.0 - _CloudCoverage, 1.0 - _CloudCoverage + 0.25, density);
                    cloud *= smoothstep(0.0, 0.2, dir.y);

                    float lit = saturate(Fbm(uv + sunDir.xz * 0.15) - density + 0.5);
                    half3 cloudColor = lerp(_CloudShadeColor.rgb, _CloudColor.rgb, lit);
                    cloudColor += _SunColor.rgb * pow(sunDot, 6.0) * 0.2;
                    color = lerp(color, cloudColor, cloud);
                }

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
