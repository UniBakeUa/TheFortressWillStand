// Процедурний пісок для top-down виду.
//
// Замість плоскої заливки: зернистість у кількох масштабах, окремі світлі
// піщинки-блищики, широкі "дюнні" плями і брижі вздовж берега.
// Мокрість читається з _SandWetTex (SandWetnessSystem): мокрий пісок темніший,
// насиченіший і трохи блищить; висихає він поступово, лишаючи темну смугу
// там, де щойно була хвиля.
Shader "Custom/TopDownSand"
{
    Properties
    {
        // SpriteRenderer підставляє сюди спрайт автоматично; беремо з нього
        // тільки альфу (форму), колір рахуємо процедурно
        [HideInInspector] _MainTex ("Sprite Texture", 2D) = "white" {}

        [Header(Base Colors)]
        _DryColor ("Dry Sand", Color) = (0.82, 0.76, 0.55, 1)
        _DryColorAlt ("Dry Sand Variation", Color) = (0.76, 0.69, 0.47, 1)
        _WetColor ("Wet Sand", Color) = (0.42, 0.34, 0.22, 1)

        [Header(Grain)]
        _GrainScale ("Grain Scale", Float) = 55
        _GrainStrength ("Grain Strength", Range(0, 1)) = 0.22
        _CoarseScale ("Coarse Patch Scale", Float) = 3.5
        _CoarseStrength ("Coarse Patch Strength", Range(0, 1)) = 0.35

        [Header(Sparkle)]
        _SparkleScale ("Sparkle Scale", Float) = 120
        _SparkleThreshold ("Sparkle Threshold", Range(0.5, 1)) = 0.93
        _SparkleStrength ("Sparkle Strength", Range(0, 1)) = 0.5

        [Header(Ripples)]
        _RippleScale ("Ripple Scale", Float) = 9
        _RippleStrength ("Ripple Strength", Range(0, 1)) = 0.18
        _RippleStretch ("Ripple Stretch (along shore)", Float) = 5

        [Header(Wetness)]
        _WetDarkening ("Wet Darkening", Range(0, 1)) = 0.75
        _WetSaturation ("Wet Saturation Boost", Range(0, 1)) = 0.3
        _WetEdgeNoise ("Wet Edge Noise", Range(0, 1)) = 0.35
        _WetSheen ("Wet Sheen", Range(0, 1)) = 0.25
        _WetGrainDamp ("Wet Grain Damping", Range(0, 1)) = 0.6

        [Header(Misc)]
        // 0 = ігнорувати тінт SpriteRenderer (типово, бо колір уже в _DryColor)
        _UseVertexColor ("Use Sprite Tint", Range(0, 1)) = 0
    }

    SubShader
    {
        // Хоч пісок і непрозорий за виглядом, рендеримо його як спрайт у
        // Transparent-черзі з ZWrite Off. Причина: Canvas у сцені стоїть у
        // режимі Screen Space - Camera, тож UI сортується разом зі сценою.
        // Непрозорий пас із ZWrite On писав би в z-buffer і відсікав UI по
        // глибині - інтерфейс зникав за піском. Решта 2D-спрайтів у проєкті
        // (вода, будівлі) з тієї ж причини живуть у Transparent.
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "SandUnlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float2 uv          : TEXCOORD1;
                float4 color       : COLOR;
            };

            // Спрайт потрібен лише заради форми/альфи - колір пісок малює сам
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _DryColor;
            float4 _DryColorAlt;
            float4 _WetColor;

            float _GrainScale;
            float _GrainStrength;
            float _CoarseScale;
            float _CoarseStrength;

            float _SparkleScale;
            float _SparkleThreshold;
            float _SparkleStrength;

            float _RippleScale;
            float _RippleStrength;
            float _RippleStretch;

            float _WetDarkening;
            float _WetSaturation;
            float _WetEdgeNoise;
            float _WetSheen;
            float _WetGrainDamp;
            float _UseVertexColor;

            // Мокрість, пушить SandWetnessSystem
            TEXTURE2D(_SandWetTex);
            SAMPLER(sampler_SandWetTex);
            float4 _SandAreaRect; // xy = origin, zw = size

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
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

            // Кілька октав - дає нерівномірність замість "телевізійного снігу"
            float fbm(float2 p)
            {
                float sum = 0;
                float amp = 0.5;
                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    sum += noise2D(p) * amp;
                    p *= 2.03;
                    amp *= 0.5;
                }
                return sum;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 worldXY = IN.positionWS.xy;

                // --- Мокрість ---
                float wet = 0;
                float2 wetUV = (worldXY - _SandAreaRect.xy) / _SandAreaRect.zw;
                if (wetUV.x >= 0 && wetUV.x <= 1 && wetUV.y >= 0 && wetUV.y <= 1)
                {
                    wet = SAMPLE_TEXTURE2D(_SandWetTex, sampler_SandWetTex, wetUV).r;
                }

                // Рваний край мокрої зони - інакше межа виглядає як рівна лінія
                float edgeNoise = (fbm(worldXY * 4.5) - 0.5) * _WetEdgeNoise;
                wet = saturate(wet * (1.0 + edgeNoise * 2.0) + edgeNoise * 0.35);

                // --- Базовий колір: широкі плями світлішого/темнішого піску ---
                float coarse = fbm(worldXY * _CoarseScale * 0.1);
                float3 baseColor = lerp(_DryColor.rgb, _DryColorAlt.rgb,
                                        saturate(coarse * _CoarseStrength * 2.5));

                // --- Дрібне зерно ---
                // На мокрому піску зерно майже не видно - вода згладжує рельєф
                float grainDamp = lerp(1.0, 1.0 - _WetGrainDamp, wet);
                float grain = noise2D(worldXY * _GrainScale) - 0.5;
                float grainFine = noise2D(worldXY * _GrainScale * 2.7) - 0.5;
                float grainMix = (grain * 0.65 + grainFine * 0.35) * _GrainStrength * grainDamp;
                baseColor += grainMix;

                // --- Брижі вздовж берега ---
                // Розтягнуті по Y (вздовж лінії води), тому читаються як сліди хвиль
                float2 rippleUV = float2(worldXY.x * _RippleScale, worldXY.y * _RippleScale / max(_RippleStretch, 0.01));
                float ripple = sin(rippleUV.x * 3.14159 + fbm(rippleUV * 0.5) * 6.0) * 0.5 + 0.5;
                // На мокрому піску брижі помітніші
                float rippleAmount = _RippleStrength * (0.45 + wet * 0.85);
                baseColor += (ripple - 0.5) * rippleAmount * 0.35;

                // --- Мокрий пісок: темніший і насиченіший ---
                float3 wetColor = _WetColor.rgb;
                // трохи зберігаємо варіації сухого піску, щоб мокре не було "плоским"
                wetColor += grainMix * 0.35 + (coarse - 0.5) * 0.06;

                float3 color = lerp(baseColor, wetColor, wet * _WetDarkening);

                // Насиченість: мокре виглядає "глибшим"
                float luma = dot(color, float3(0.299, 0.587, 0.114));
                color = lerp(color, luma + (color - luma) * 1.6, wet * _WetSaturation);

                // --- Блищики сухих піщинок ---
                // Мокрий пісок їх гасить (піщинки залиті водою)
                float sparkleNoise = hash21(floor(worldXY * _SparkleScale));
                float sparkle = step(_SparkleThreshold, sparkleNoise) * _SparkleStrength * (1.0 - wet);
                color += sparkle * 0.35;

                // --- Плівка води на мокрому: рівномірне підсвічування ---
                float sheen = wet * _WetSheen * (0.6 + ripple * 0.4);
                color += sheen * 0.12;

                // Тінт спрайта за замовчуванням вимкнено: у сцені на піску вже
                // стоїть піщаний m_Color, і множення на нього затемнило б
                // _DryColor удруге. Вмикайте, якщо тінт потрібен для керування.
                color *= lerp(1.0, IN.color.rgb, _UseVertexColor);

                // Форму задає спрайт: за його межами (альфа 0) не малюємо нічого
                float spriteAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a;

                return half4(color, spriteAlpha * IN.color.a);
            }
            ENDHLSL
        }
    }
}
