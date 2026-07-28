// Показує накопичену кров (RT з BloodStamp) поверх піску.
// Квад із цим матеріалом лежить над піском і ПІД водою за sorting order,
// тож хвиля природно накриває кров зверху, поки Wash-пас її стирає.
Shader "Custom/BloodDecal"
{
    Properties
    {
        _BloodTex ("Blood Mask (R)", 2D) = "black" {}

        _FreshColor ("Fresh Blood", Color) = (0.55, 0.02, 0.02, 1)
        _DriedColor ("Dried / Thin Blood", Color) = (0.30, 0.06, 0.03, 1)

        _Threshold ("Alpha Threshold", Range(0, 0.5)) = 0.04
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.5)) = 0.12
        _MaxAlpha ("Max Alpha", Range(0, 1)) = 0.92
        _NoiseScale ("Soak Noise Scale", Float) = 14
        _NoiseStrength ("Soak Noise Strength", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "BloodDecalUnlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BloodTex);
            SAMPLER(sampler_BloodTex);

            float4 _FreshColor;
            float4 _DriedColor;
            float _Threshold;
            float _EdgeSoftness;
            float _MaxAlpha;
            float _NoiseScale;
            float _NoiseStrength;

            // Область RT у світових координатах: xy = origin, zw = розмір.
            // Пушиться глобально з BloodDecalSystem.
            float4 _BloodAreaRect;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 worldXY = IN.positionWS.xy;
                float2 uv = (worldXY - _BloodAreaRect.xy) / _BloodAreaRect.zw;

                if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
                    discard;

                float blood = SAMPLE_TEXTURE2D(_BloodTex, sampler_BloodTex, uv).r;

                // Шум "вбирання в пісок": робить край плями зернистим, а не гладким
                float soak = noise2D(worldXY * _NoiseScale) * _NoiseStrength;
                blood = saturate(blood - soak * (1.0 - blood));

                if (blood <= _Threshold)
                    discard;

                float alpha = smoothstep(_Threshold, _Threshold + _EdgeSoftness, blood) * _MaxAlpha;

                // Густа кров - насичена, тонкий шар - вбирається в пісок і темніє
                float3 color = lerp(_DriedColor.rgb, _FreshColor.rgb, saturate(blood * 1.3));

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
