// Аутлайн для спрайтів.
//
// Контур будується з альфи самого спрайта: у кожному пікселі семплимо альфу по
// колу навколо і, якщо поруч є непрозорий піксель, а сам ми прозорі - малюємо
// обводку.
//
// Геометрія спрайта НЕ змінюється. Роздування квада (щоб контур не зрізало
// краєм меша) ламає збіг рендера з коллайдером - спрайт візуально їде вбік
// і по об'єкту стає неможливо влучити. Тому контур малюється всередині
// наявного меша, а його ширина обмежена прозорим відступом самого спрайта.
Shader "Custom/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Outline)]
        [Toggle] _OutlineEnabled ("Enabled", Float) = 1
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        // У пікселях текстури: не залежить від масштабу об'єкта.
        // Контур малюється всередині меша спрайта, тож ширина обмежена
        // прозорим відступом навколо малюнка. Якщо обводку зрізає по краю -
        // додайте спрайту прозорі поля (Sprite Editor або сам PNG)
        _OutlineWidth ("Outline Width (px)", Range(0, 32)) = 3
        // Щільність променів на одиницю радіуса, а не стала кількість:
        // інакше на великій ширині контур розпадається на "пелюстки".
        // 4 - дешево і достатньо для тонкої обводки, 8+ для товстої
        _OutlineQuality ("Quality (ray density)", Range(2, 12)) = 8
        _OutlineAlphaCutoff ("Alpha Cutoff", Range(0.01, 1)) = 0.35
        // Контур усередині силуету замість зовнішнього
        [Toggle] _OutlineInside ("Draw Inside", Float) = 0

        // Діагностика: підсвічує область, де контуру не вистачає місця
        // в спрайті (силует упритул до краю). Якщо тут спалахує - спрайту
        // бракує прозорого відступу, шейдер зробити нічого не може
        [Toggle] _DebugShowClipping ("DEBUG: show clipped edges", Float) = 0

        [Header(Glow Pulse)]
        _PulseSpeed ("Pulse Speed", Float) = 0
        _PulseMin ("Pulse Min Alpha", Range(0, 1)) = 0.5

        // Службові - Unity виставляє їх сам для спрайтів
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            Name "SpriteOutlineUnlit"
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
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_TexelSize; // (1/w, 1/h, w, h)
            float4 _MainTex_ST;

            float4 _Color;
            float4 _RendererColor;

            float _OutlineEnabled;
            float4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineQuality;
            float _OutlineAlphaCutoff;
            float _OutlineInside;

            float _PulseSpeed;
            float _PulseMin;
            float _DebugShowClipping;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Геометрію НЕ чіпаємо.
                //
                // Спокусливо роздути квад, щоб зовнішній контур не зрізало
                // межею меша, але це зміщує спрайт відносно його коллайдера:
                // масштабування йде від початку координат об'єкта, а рендер
                // перестає збігатися з фізикою - по ворогу неможливо влучити.
                //
                // Тому позиція лишається як є, а місце під контур береться
                // із прозорого відступу самого спрайта (див. коментар до
                // _OutlineWidth: ширина обмежена цим відступом).
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;

                OUT.color = IN.color * _Color * _RendererColor;
                return OUT;
            }

            // Межі спрайта в UV текстури.
            //
            // Порівнювати просто з 0..1 НЕ МОЖНА: SpriteRenderer підставляє
            // _MainTex_ST, і UV спрайта - це підобласть текстури (а при
            // flipX/flipY масштаб ще й відʼємний). Через це обрізання виходило
            // рівно з двох суміжних боків - саме те, що видно на скріншоті.
            float4 SpriteBounds()
            {
                float2 a = _MainTex_ST.zw;                 // offset
                float2 b = _MainTex_ST.zw + _MainTex_ST.xy; // offset + scale
                return float4(min(a, b), max(a, b));        // (minU, minV, maxU, maxV)
            }

            // Альфа сусіднього пікселя. Поза межами спрайта повертаємо 0:
            // там або сусідній спрайт атласа, або розтягнутий крайній піксель.
            float SampleAlpha(float2 uv, float4 bounds)
            {
                if (any(uv < bounds.xy) || any(uv > bounds.zw)) return 0;
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                if (_OutlineEnabled < 0.5)
                    return sprite;

                float ownAlpha = sprite.a;

                // Рахуємо ВІДСТАНЬ до силуету, а не просто "чи є сусід".
                //
                // Одне кільце семплів дає рвану товщину: при 12 семплах і
                // радіусі 3px дуга між сусідніми точками ~1.6px, тобто вони
                // перестрибують пікселі. Тому скануємо кількома кільцями
                // РІЗНОГО радіуса і запам'ятовуємо найближче влучання -
                // товщина виходить однакова під будь-яким кутом.
                float2 texelSize = _MainTex_TexelSize.xy;
                float maxRadius = _OutlineWidth;
                float4 bounds = SpriteBounds();

                // Кількість кутів мусить рости разом з радіусом: при
                // фіксованих 12 променях дуга між ними на радіусі 12px сягає
                // 6px, і контур розпадається на "пелюстки". _OutlineQuality
                // задає щільність (частку від довжини кола), а не сталу
                // кількість променів.
                //
                // Верхня межа 64 стримує вартість: разом із кроками по радіусу
                // це найдорожче місце шейдера.
                int angleSteps = clamp((int)(maxRadius * _OutlineQuality * 0.5), 8, 64);

                // Крок по радіусу ~1px, але не більше 8 кроків: далі приріст
                // якості вже непомітний, а вартість росте лінійно
                int radiusSteps = clamp((int)maxRadius, 2, 8);

                // Найближча відстань до непрозорого пікселя (у пікселях),
                // maxRadius + 1 = "не знайдено"
                float nearestOpaque = maxRadius + 1.0;
                float nearestClear  = maxRadius + 1.0;

                [loop]
                for (int a = 0; a < angleSteps; a++)
                {
                    float angle = (a + 0.5) * 6.2831853 / angleSteps;
                    float2 dir = float2(cos(angle), sin(angle));

                    [loop]
                    for (int r = 1; r <= radiusSteps; r++)
                    {
                        float dist = maxRadius * r / radiusSteps;
                        float alpha = SampleAlpha(IN.uv + dir * texelSize * dist, bounds);

                        bool opaque = alpha >= _OutlineAlphaCutoff;

                        // Перше влучання вздовж променя - найближче
                        if (opaque) nearestOpaque = min(nearestOpaque, dist);
                        else        nearestClear  = min(nearestClear, dist);
                    }
                }

                float pulse = 1.0;
                if (_PulseSpeed > 0.001)
                {
                    float t = sin(_Time.y * _PulseSpeed * 6.2831853) * 0.5 + 0.5;
                    pulse = lerp(_PulseMin, 1.0, t);
                }

                bool isSolid = ownAlpha >= _OutlineAlphaCutoff;

                // Діагностика: пурпуровим позначаємо силует, що стоїть
                // ближче за _OutlineWidth до краю спрайта - там контур
                // фізично нікуди малювати
                if (_DebugShowClipping > 0.5 && isSolid)
                {
                    float2 toEdge = min(IN.uv - bounds.xy, bounds.zw - IN.uv);
                    float edgePx = min(toEdge.x / texelSize.x, toEdge.y / texelSize.y);
                    if (edgePx < maxRadius)
                        return float4(1, 0, 1, 1);
                }

                // Згладжування рівно в один крок сканування: край контуру
                // виходить м'яким, але товщина лишається сталою
                float aa = maxRadius / max(radiusSteps, 1);

                if (_OutlineInside > 0.5)
                {
                    // Внутрішній контур: ми в силуеті, а прозорість поруч
                    float edge = isSolid
                        ? 1.0 - smoothstep(maxRadius - aa, maxRadius, nearestClear)
                        : 0.0;

                    float4 outline = _OutlineColor;
                    outline.a *= pulse;
                    return lerp(sprite, outline, edge * outline.a);
                }

                // Зовнішній контур: ми поза силуетом, а непрозорий піксель
                // ближче за maxRadius. Через відстань, а не через "є/немає" -
                // саме це прибирає рвану товщину.
                float outlineMask = isSolid
                    ? 0.0
                    : 1.0 - smoothstep(maxRadius - aa, maxRadius, nearestOpaque);

                float4 outlineColor = _OutlineColor;
                outlineColor.a *= pulse * IN.color.a * outlineMask;

                // Спрайт малюємо поверх контуру
                return lerp(outlineColor, sprite, ownAlpha);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
