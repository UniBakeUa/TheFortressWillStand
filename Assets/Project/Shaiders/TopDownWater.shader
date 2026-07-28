Shader "Custom/TopDownWater"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.25, 0.65, 0.75, 0.55)
        _DeepColor ("Deep Color", Color) = (0.02, 0.12, 0.32, 0.95)
        _DepthColorDistance ("Depth Color Distance", Float) = 1.0

        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _FoamWidth ("Foam Width (world units)", Float) = 0.15
        _FoamNoiseScale ("Foam Noise Scale", Float) = 6
        _FoamNoiseSpeed ("Foam Noise Speed", Float) = 0.8
        _FoamThreshold ("Foam Threshold", Range(0,1)) = 0.55

        _FlowDirection ("Flow Direction", Vector) = (-1, 0, 0, 0)
        _FlowSpeed ("Flow Speed", Float) = 0.35
        _CrestNoiseScale ("Crest Noise Scale", Float) = 2.5
        _CrestThreshold ("Crest Threshold", Range(0,1)) = 0.72
        _CrestStrength ("Crest Strength", Range(0,1)) = 0.4

        [Header(Foam Detail)]
        _FoamSoftness ("Foam Softness", Range(0.01, 0.5)) = 0.18
        _FoamBubbleScale ("Foam Bubble Scale", Float) = 28
        _FoamBubbleStrength ("Foam Bubble Strength", Range(0,1)) = 0.45
        _FoamStretch ("Foam Stretch Along Shore", Float) = 3

        [Header(Sparkle)]
        _SparkleScale ("Sparkle Scale", Float) = 34
        _SparkleSpeed ("Sparkle Speed", Float) = 1.4
        _SparkleThreshold ("Sparkle Threshold", Range(0.5, 1)) = 0.86
        _SparkleStrength ("Sparkle Strength", Range(0,1)) = 0.5
        _SparkleColor ("Sparkle Color", Color) = (1, 1, 0.95, 1)

        [Header(Depth Shading)]
        _ShorelineTint ("Shoreline Wet Tint", Color) = (0.45, 0.42, 0.30, 1)
        _ShorelineFade ("Shoreline Blend Width", Range(0.001, 0.6)) = 0.18
        _CausticScale ("Caustic Scale", Float) = 11
        _CausticSpeed ("Caustic Speed", Float) = 0.5
        _CausticStrength ("Caustic Strength", Range(0,1)) = 0.35
        _EdgeAlphaFade ("Edge Alpha Fade", Range(0.001, 0.5)) = 0.12
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardUnlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_WaterHeightTex);
            SAMPLER(sampler_WaterHeightTex);

            // ці три пушаться глобально з WaterGrid.LateUpdate() - не властивості матеріалу
            float2 _WaterGridOrigin;
            float _WaterGridCellSize;
            float2 _WaterGridSize; // (width, height) в клітинках

            float4 _ShallowColor;
            float4 _DeepColor;
            float _DepthColorDistance;

            float4 _FoamColor;
            float _FoamWidth;
            float _FoamNoiseScale;
            float _FoamNoiseSpeed;
            float _FoamThreshold;

            float2 _FlowDirection;
            float _FlowSpeed;
            float _CrestNoiseScale;
            float _CrestThreshold;
            float _CrestStrength;

            float _FoamSoftness;
            float _FoamBubbleScale;
            float _FoamBubbleStrength;
            float _FoamStretch;

            float _SparkleScale;
            float _SparkleSpeed;
            float _SparkleThreshold;
            float _SparkleStrength;
            float4 _SparkleColor;

            float4 _ShorelineTint;
            float _ShorelineFade;
            float _CausticScale;
            float _CausticSpeed;
            float _CausticStrength;
            float _EdgeAlphaFade;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            // легкий value-noise, достатньо для піни/гребенів у прототипі
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

            // Кілька октав шуму - дає нерівномірну структуру замість
            // однорідної "каші" одного масштабу
            float fbm(float2 p)
            {
                float sum = 0;
                float amp = 0.5;
                [unroll]
                for (int i = 0; i < 3; i++)
                {
                    sum += noise2D(p) * amp;
                    p *= 2.07;
                    amp *= 0.5;
                }
                return sum;
            }


            // >0 = під водою (значення = глибина), <=0 = суходіл
 float SampleDepth(float2 worldPosXY)
{
    // --- ДИНАМІЧНИЙ ЖИВИЙ КРАЙ ---
    // Перша хвиля шуму: рухається по діагоналі в один бік
    float2 uvNoise1 = worldPosXY * 2.5 + float2(_Time.y * 0.4, _Time.y * 0.3);
    float noise1 = noise2D(uvNoise1);
    
    // Друга хвиля шуму (дрібніша): рухається в протилежний бік, щоб зламати симетрію
    float2 uvNoise2 = worldPosXY * 5.5 + float2(-_Time.y * 0.5, _Time.y * 0.4);
    float noise2 = noise2D(uvNoise2);
    
    // Змішуємо їх для отримання нелінійного результату
    float combinedNoise = (noise1 * 0.65 + noise2 * 0.35);

    // Викривляємо координати по обох осях:
    // X — відповідає за затікання вперед/назад по пляжу
    // Y — відповідає за зміщення хвиль вбік (ефект перетікання вздовж берега)
    float2 distortedWorldPos = worldPosXY;
    distortedWorldPos.x += (combinedNoise - 0.5) * 0.5; // Амплітуда нерівності по X
    distortedWorldPos.y += (noise1 - 0.5) * 0.3;        // Амплітуда зміщення по Y

    // Стандартний прорахунок UV для вибірки з текстури
    float2 uv = (distortedWorldPos - _WaterGridOrigin) / (_WaterGridSize * _WaterGridCellSize);
    
    if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return -1;
    return SAMPLE_TEXTURE2D_LOD(_WaterHeightTex, sampler_WaterHeightTex, uv, 0).r;
}

            half4 frag(Varyings IN) : SV_Target
            {
                float2 worldXY = IN.positionWS.xy; // top-down: XY - площина гри, Z - вгору/умовна глибина сцени

                float depth = SampleDepth(worldXY);

                if (depth <= 0.006f)
                    discard; // суходіл - тут нічого не малюємо, під ним видно пісок/острів

                float2 flowDir = normalize(_FlowDirection + 1e-5);
                float flowTime = _Time.y * _FlowSpeed;

                // --- базовий колір за глибиною ---
                float depthT = saturate(depth / _DepthColorDistance);
                // Крива замість лінійного lerp: мілководдя світлішає швидше,
                // глибина набирає колір повільніше - води виглядає "об'ємнішою"
                float depthCurve = 1.0 - pow(1.0 - depthT, 2.2);
                float4 baseColor = lerp(_ShallowColor, _DeepColor, depthCurve);

                // --- найтонший шар біля берега підбирає колір мокрого піску ---
                // Без цього кромка води обривається різким кольоровим стрибком.
                float shoreT = 1.0 - saturate(depth / _ShorelineFade);
                baseColor.rgb = lerp(baseColor.rgb, _ShorelineTint.rgb, shoreT * shoreT * 0.75);

                // --- каустика: світлові сітки на дні мілководдя ---
                // Дві сітки, що рухаються з різною швидкістю; їх перетин дає
                // характерні "комірки", які повзуть по піску.
                float2 cUV1 = worldXY * _CausticScale + flowDir * flowTime * _CausticSpeed;
                float2 cUV2 = worldXY * _CausticScale * 1.37 - flowDir * flowTime * _CausticSpeed * 0.75;
                float caustic1 = 1.0 - abs(noise2D(cUV1) * 2.0 - 1.0);
                float caustic2 = 1.0 - abs(noise2D(cUV2) * 2.0 - 1.0);
                float caustic = pow(saturate(caustic1 * caustic2), 3.0);
                // видно лише на мілкому - у глибині світло не дістає дна
                caustic *= (1.0 - depthT) * _CausticStrength;
                baseColor.rgb += caustic;

                // --- піна на кромці ---
                // Розтягуємо шум уздовж берега: піна лягає смугами паралельно
                // лінії води, а не рівномірними плямами.
                float2 foamUV = float2(worldXY.x, worldXY.y / max(_FoamStretch, 0.01))
                                * _FoamNoiseScale + flowDir * _Time.y * _FoamNoiseSpeed;
                float foamNoise = fbm(foamUV);

                float edgeT = 1 - saturate(depth / _FoamWidth);
                // smoothstep замість step: край піни м'який, без "лесенки"
                float foamEdge = smoothstep(_FoamThreshold - _FoamSoftness,
                                            _FoamThreshold + _FoamSoftness,
                                            foamNoise + edgeT * 0.45) * edgeT;

                // Бульбашки всередині піни - дрібна структура, щоб вона не
                // виглядала суцільною білою заливкою
                float bubbles = noise2D(worldXY * _FoamBubbleScale + flowDir * _Time.y * 0.6);
                foamEdge *= lerp(1.0, smoothstep(0.25, 0.75, bubbles), _FoamBubbleStrength);

                // --- гребені хвиль по глибшій воді ---
                float2 crestUV = worldXY * _CrestNoiseScale + flowDir * flowTime;
                float crestNoise = fbm(crestUV);
                float crest = smoothstep(_CrestThreshold - 0.12, _CrestThreshold + 0.12, crestNoise)
                              * depthT * _CrestStrength;

                float foamMask = saturate(foamEdge + crest);
                float4 finalColor = lerp(baseColor, _FoamColor, foamMask);

                // --- відблиски сонця на брижах ---
                // Мерехтять у часі, гасяться там, де вже є піна
                float2 spUV = worldXY * _SparkleScale + flowDir * _Time.y * _SparkleSpeed;
                float sparkleNoise = noise2D(spUV) * noise2D(spUV * 1.9 + 7.3);
                float sparkle = smoothstep(_SparkleThreshold, _SparkleThreshold + 0.06, sparkleNoise);
                sparkle *= _SparkleStrength * (1.0 - foamMask) * saturate(depthT * 2.0);
                finalColor.rgb += _SparkleColor.rgb * sparkle;

                // --- м'який альфа-край ---
                // Різкий discard лишав "пилку" по контуру води; тепер тонкий
                // шар плавно розчиняється в піску.
                finalColor.a *= smoothstep(0.0, _EdgeAlphaFade, depth);

                return finalColor;
            }
            ENDHLSL
        }
    }
}
