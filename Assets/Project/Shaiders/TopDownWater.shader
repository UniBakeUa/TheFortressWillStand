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

                // --- базовий колір за глибиною ---
                float depthT = saturate(depth / _DepthColorDistance);
                float4 baseColor = lerp(_ShallowColor, _DeepColor, depthT);

                float2 flowDir = normalize(_FlowDirection + 1e-5);

                // --- піна на кромці (мілководдя біля берега) ---
                float edgeT = 1 - saturate(depth / _FoamWidth);
                float2 foamUV = worldXY * _FoamNoiseScale + flowDir * _Time.y * _FoamNoiseSpeed;
                float foamNoise = noise2D(foamUV);
                float foamEdge = edgeT * step(_FoamThreshold, foamNoise);

                // --- гребені хвиль, що біжать у напрямку течії по глибшій воді ---
                float2 crestUV = worldXY * _CrestNoiseScale + flowDir * _Time.y * _FlowSpeed;
                float crestNoise = noise2D(crestUV);
                float crest = step(_CrestThreshold, crestNoise) * depthT * _CrestStrength;

                float foamMask = saturate(foamEdge + crest);
                float4 finalColor = lerp(baseColor, _FoamColor, foamMask);

                return finalColor;
            }
            ENDHLSL
        }
    }
}
