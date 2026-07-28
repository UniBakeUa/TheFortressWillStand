// Накопичує "мокрість" піску в персистентній world-space RT (див. SandWetnessSystem).
// R8: 1 = щойно залитий водою, 0 = сухий.
//
// Потрібен окремий буфер, а не пряме читання _WaterHeightTex, бо вода показує
// ЛИШЕ поточний стан: щойно хвиля відкотилась, вона зникає миттєво, а мокрий
// пісок має лишатись мокрим і сохнути поступово.
Shader "Hidden/SandWetness"
{
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "Soak"
            // Результат = max(старе * keep, нова вода).
            // Один пас робить і висихання, і намокання: спершу множимо
            // наявну мокрість на keep (висихання), потім беремо max з
            // поточною водою (намокання) - тому Blend не годиться, рахуємо
            // все у фрагменті, читаючи попередній стан з копії.
            Blend Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            // Рендеримо через Graphics.Blit, який малює повноекранний квад у
            // clip-space; UNITY_MATRIX_MVP тут - ортопроєкція самого блиту.
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = mul(UNITY_MATRIX_MVP, float4(IN.positionOS.xyz, 1.0));
                OUT.uv = IN.uv;
                return OUT;
            }

            // Попередній стан мокрості (копія RT)
            TEXTURE2D(_PrevWetTex);
            SAMPLER(sampler_PrevWetTex);

            // Глобальні, пушить WaterGrid.LateUpdate()
            TEXTURE2D(_WaterHeightTex);
            SAMPLER(sampler_WaterHeightTex);
            float2 _WaterGridOrigin;
            float  _WaterGridCellSize;
            float2 _WaterGridSize;

            // Область RT у світових координатах: xy = origin, zw = розмір
            float4 _SandAreaRect;

            float _WaterDepthThreshold; // з якої глибини вважаємо пісок залитим
            float _DryAmount;           // скільки висихає за цей кадр (0..1)

            float frag(Varyings IN) : SV_Target
            {
                float prev = SAMPLE_TEXTURE2D_LOD(_PrevWetTex, sampler_PrevWetTex, IN.uv, 0).r;

                // Висихання
                float wet = saturate(prev - _DryAmount);

                // Намокання: де зараз є вода - мокрість підскакує до 1
                float2 worldPos = _SandAreaRect.xy + IN.uv * _SandAreaRect.zw;
                float2 wuv = (worldPos - _WaterGridOrigin) / (_WaterGridSize * _WaterGridCellSize);

                if (wuv.x >= 0 && wuv.x <= 1 && wuv.y >= 0 && wuv.y <= 1)
                {
                    float depth = SAMPLE_TEXTURE2D_LOD(_WaterHeightTex, sampler_WaterHeightTex, wuv, 0).r;
                    float submerged = smoothstep(_WaterDepthThreshold, _WaterDepthThreshold + 0.12, depth);
                    wet = max(wet, submerged);
                }

                return wet;
            }
            ENDHLSL
        }
    }
}
