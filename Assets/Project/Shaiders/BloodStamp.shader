// Малює/змиває кров у персистентній world-space RT (див. BloodDecalSystem).
// RT формат R8: один канал = "кількість крові" в клітинці, 0 = чистий пісок.
//
// Pass 0 (Stamp)  - додає одну пляму: ядро + напрямлений "хвіст" + бризки.
// Pass 1 (Wash)   - стирає кров там, де хвиля накрила пісок (читає _WaterHeightTex,
//                   ту саму глобальну текстуру, що й TopDownWater).
Shader "Hidden/BloodStamp"
{
    Properties
    {
        _MainTex ("Source", 2D) = "black" {}
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        HLSLINCLUDE
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

        // Рендеримо через GL.LoadOrtho() + GL.Vertex3 у діапазоні 0..1,
        // тож потрібна саме MVP-трансформація (UNITY_MATRIX_MVP), а не
        // object->world->clip: "світ" тут - це нормалізований квад RT.
        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            OUT.positionHCS = mul(UNITY_MATRIX_MVP, float4(IN.positionOS.xyz, 1.0));
            OUT.uv = IN.uv;
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
        ENDHLSL

        // ============================================================
        // Pass 0: STAMP - домальовуємо пляму крові
        // ============================================================
        Pass
        {
            Name "Stamp"
            // Кров накопичується: нова пляма поверх старої не стирає її.
            Blend One One
            BlendOp Max

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragStamp

            // Область RT у світових координатах: xy = origin, zw = розмір
            float4 _BloodAreaRect;

            float2 _SplatCenter;    // світова позиція смерті
            float2 _SplatDir;       // нормалізований напрямок розльоту (звідки прилетіло -> куди летить кров)
            float  _SplatRadius;    // радіус ядра плями у світових одиницях
            float  _SplatLength;    // довжина "хвоста" по напрямку, у світових одиницях
            float  _SplatSeed;      // рандомізація форми, щоб плями не були однакові
            float  _SplatStrength;  // 0..1 насиченість
            float  _SplatRadial;    // 1 = клякса без напрямку (удар згори), 0 = напрямлений розліт
            float  _StreakDensity;  // 0..1 скільки з 6 тонких ліній лишається (0 = жодної)
            float  _StreakLength;   // множник довжини ліній-патьоків
            float  _StreakWidth;    // множник товщини ліній

            float fragStamp(Varyings IN) : SV_Target
            {
                // uv RT -> світові координати
                float2 worldPos = _BloodAreaRect.xy + IN.uv * _BloodAreaRect.zw;
                float2 delta = worldPos - _SplatCenter;

                // Локальний базис: x - уздовж напрямку розльоту, y - поперек
                float2 dir = normalize(_SplatDir + 1e-5);
                float2 perp = float2(-dir.y, dir.x);
                float along  = dot(delta, dir);
                float across = dot(delta, perp);

                bool isRadial = _SplatRadial > 0.5;

                float blood = 0;

                // --- 1. Ядро ---
                // Напрямлений удар зміщує калюжу за напрямком; удар згори
                // лишає її рівно під тілом.
                float2 coreDelta = isRadial ? delta : (delta - dir * _SplatRadius * 0.35);
                float coreDist = length(coreDelta);

                float coreWobble;
                if (isRadial)
                {
                    // Клякса: край "рветься" за кутом, тому пляма виходить
                    // неправильної форми, а не рівним кружком. Кутовий шум
                    // семплимо по колу навколо центра, щоб він не зривався
                    // на пікселях, а йшов суцільним контуром.
                    float angle = atan2(coreDelta.y, coreDelta.x);
                    float2 ringUV = float2(cos(angle), sin(angle)) * 2.3 + _SplatSeed;
                    float lobes = noise2D(ringUV) * 0.75 + noise2D(ringUV * 2.7 + 11.3) * 0.25;
                    coreWobble = (lobes - 0.45) * _SplatRadius * 0.85;
                }
                else
                {
                    coreWobble = (noise2D(worldPos * 9.0 + _SplatSeed) - 0.5) * _SplatRadius * 0.55;
                }

                // Клякса від пострілу згори ширша за калюжу від бічного удару
                float coreRadius = _SplatRadius * (isRadial ? 1.35 : 1.0);
                float core = 1.0 - smoothstep(0.0, coreRadius + coreWobble, coreDist);
                blood = max(blood, core);

                // --- 2. Конус розльоту (тільки для напрямленого удару) ---
                // t = 0 біля тіла, 1 у кінці хвоста
                float t = saturate(along / max(_SplatLength, 1e-4));
                if (!isRadial && along > 0)
                {
                    // ширина конуса росте, але щільність падає
                    float coneHalfWidth = _SplatRadius * (0.35 + t * 1.5);
                    float widthFalloff = 1.0 - smoothstep(0.0, coneHalfWidth, abs(across));
                    float lengthFalloff = 1.0 - smoothstep(0.0, 1.0, t);

                    // рваність струменя вздовж напрямку
                    float streak = noise2D(float2(along * 7.0, across * 16.0) + _SplatSeed * 3.7);
                    float cone = widthFalloff * lengthFalloff * (0.45 + streak * 0.75);
                    blood = max(blood, cone);
                }

                // --- 3. Окремі бризки-краплі ---
                // Напрямлений удар: краплі летять по конусу вперед.
                // Удар згори: краплі розкидані рівномірно навколо центра.
                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    float fi = (float)i;
                    float r1 = hash21(float2(_SplatSeed + fi * 1.37, fi * 7.13));
                    float r2 = hash21(float2(fi * 3.71, _SplatSeed + fi * 2.19));
                    float r3 = hash21(float2(_SplatSeed * 2.3 + fi, fi * 5.11));

                    float2 dropPos;
                    if (isRadial)
                    {
                        // рівномірний кут + випадкова відстань від центра
                        float dropAngle = (fi + r1) * 6.28318 / 8.0;
                        float dropDist = coreRadius * (0.85 + r2 * 1.25);
                        dropPos = _SplatCenter + float2(cos(dropAngle), sin(dropAngle)) * dropDist;
                    }
                    else
                    {
                        // позиція краплі: вздовж напрямку + бічний розкид
                        float dAlong  = _SplatRadius * 0.8 + r1 * _SplatLength * 1.15;
                        float dAcross = (r2 - 0.5) * _SplatRadius * (1.2 + r1 * 3.0);
                        dropPos = _SplatCenter + dir * dAlong + perp * dAcross;
                    }

                    // дрібніші краплі далі від тіла
                    float dropR = _SplatRadius * (0.30 - 0.18 * r1) * (0.5 + r3);
                    float drop = 1.0 - smoothstep(0.0, max(dropR, 1e-4), length(worldPos - dropPos));

                    blood = max(blood, drop * (0.75 + r3 * 0.25));
                }

                // --- 4. Тонкі лінії-патьоки з крапками на кінці ---
                // Вузькі струмені, що вистрілюють від ядра назовні; уздовж
                // кожного сидять дрібні краплі, які до кінця лінії рідшають.
                [unroll]
                for (int s = 0; s < 6; s++)
                {
                    float fs = (float)s;
                    float s1 = hash21(float2(_SplatSeed * 1.9 + fs * 2.53, fs * 4.27));
                    float s2 = hash21(float2(fs * 6.11, _SplatSeed * 3.1 + fs * 1.77));
                    float s3 = hash21(float2(_SplatSeed + fs * 8.39, fs * 2.91));

                    // Не з кожної плями стирчить 6 ліній - частину глушимо.
                    // Множник, а не continue: у розгорнутому циклі з вкладеним
                    // циклом всередині continue компілиться нестабільно.
                    float streakOn = step(1.0 - _StreakDensity, s3);

                    // Клякса: лінії розходяться навсібіч.
                    // Напрямлений удар: тримаються конуса розльоту.
                    float streakAngle;
                    if (isRadial)
                    {
                        streakAngle = (fs + s1 * 0.85) * 6.28318 / 6.0;
                    }
                    else
                    {
                        float baseAngle = atan2(dir.y, dir.x);
                        streakAngle = baseAngle + (s1 - 0.5) * 1.1;
                    }

                    float2 sDir = float2(cos(streakAngle), sin(streakAngle));
                    float2 sPerp = float2(-sDir.y, sDir.x);

                    // Довжина: у кляксі коротші, у напрямленому ударі тягнуться далі
                    float sLen = (isRadial ? coreRadius * 2.6 : _SplatLength * 1.25)
                                 * (0.55 + s2 * 0.9) * _StreakLength;
                    float sStart = coreRadius * 0.55;

                    float sAlong  = dot(delta, sDir);
                    float sAcross = dot(delta, sPerp);

                    if (sAlong > sStart && sAlong < sStart + sLen)
                    {
                        // t = 0 біля ядра, 1 на кінці лінії
                        float st = (sAlong - sStart) / max(sLen, 1e-4);

                        // Лінія звужується до кінця і трохи звивається
                        float wiggle = (noise2D(float2(sAlong * 5.5, fs * 13.7) + _SplatSeed) - 0.5)
                                       * _SplatRadius * 0.35 * st;
                        float halfWidth = _SplatRadius * (0.11 - 0.075 * st) * (0.7 + s2 * 0.6) * _StreakWidth;

                        // "line" - зарезервоване слово HLSL (#line), тому streakMask
                        float streakMask = 1.0 - smoothstep(0.0, max(halfWidth, 1e-4), abs(sAcross - wiggle));

                        // Струмінь рветься на окремі сегменти ближче до кінця
                        float breakUp = noise2D(float2(sAlong * 9.0, fs * 21.3) + _SplatSeed * 2.1);
                        streakMask *= step(st * 0.55, breakUp);

                        blood = max(blood, streakMask * (0.85 - st * 0.25) * streakOn);
                    }

                    // Крапки вздовж лінії - по 3 на струмінь, дрібнішають до кінця
                    [unroll]
                    for (int d = 0; d < 3; d++)
                    {
                        float fd = (float)d;
                        float d1 = hash21(float2(_SplatSeed + fs * 3.3 + fd * 7.7, fd * 1.93));
                        float d2 = hash21(float2(fd * 5.29, _SplatSeed * 1.4 + fs * 2.11 + fd));

                        // Розкидані по довжині, останні - вже за кінцем лінії
                        float dt = (fd + 0.35 + d1 * 0.8) / 3.0;
                        float dotAlong = sStart + sLen * dt * 1.25;
                        float dotAcross = (d2 - 0.5) * _SplatRadius * 0.5 * dt;

                        float2 dotPos = _SplatCenter + sDir * dotAlong + sPerp * dotAcross;

                        float dotR = _SplatRadius * (0.13 - 0.075 * dt) * (0.6 + d1 * 0.8) * _StreakWidth;
                        float dotMask = 1.0 - smoothstep(0.0, max(dotR, 1e-4), length(worldPos - dotPos));

                        blood = max(blood, dotMask * (0.9 - dt * 0.2) * streakOn);
                    }
                }

                return saturate(blood) * _SplatStrength;
            }
            ENDHLSL
        }

        // ============================================================
        // Pass 1: WASH - хвиля змиває кров
        // ============================================================
        Pass
        {
            Name "Wash"
            // Множимо наявну кров на коефіцієнт < 1 там, де є вода.
            Blend DstColor Zero

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragWash

            float4 _BloodAreaRect;

            // Глобальні, пушить WaterGrid.LateUpdate()
            TEXTURE2D(_WaterHeightTex);
            SAMPLER(sampler_WaterHeightTex);
            float2 _WaterGridOrigin;
            float  _WaterGridCellSize;
            float2 _WaterGridSize;

            float _WashDepthThreshold; // глибина, з якої вода вважається такою, що змиває
            float _WashAmount;         // скільки змивається за кадр (0..1), масштабується deltaTime
            float _DryFade;            // повільне висихання/вбирання в пісок навіть без води
            float _WashTime;           // Time.time; _Time не гарантований при ручному SetPass-блиті

            float fragWash(Varyings IN) : SV_Target
            {
                float2 worldPos = _BloodAreaRect.xy + IN.uv * _BloodAreaRect.zw;

                float2 wuv = (worldPos - _WaterGridOrigin) / (_WaterGridSize * _WaterGridCellSize);

                float wash = 0;
                if (wuv.x >= 0 && wuv.x <= 1 && wuv.y >= 0 && wuv.y <= 1)
                {
                    float depth = SAMPLE_TEXTURE2D_LOD(_WaterHeightTex, sampler_WaterHeightTex, wuv, 0).r;

                    // Чим глибше накрило - тим швидше змиває. Рвані краї, щоб
                    // кров зникала плямами, а не рівною лінією.
                    float edgeNoise = noise2D(worldPos * 6.0 + _WashTime * 0.7) * 0.35;
                    float submerged = smoothstep(_WashDepthThreshold, _WashDepthThreshold + 0.25 + edgeNoise, depth);
                    wash = submerged * _WashAmount;
                }

                // Результат = стара кров * keep
                float keep = saturate(1.0 - wash - _DryFade);
                return keep;
            }
            ENDHLSL
        }
    }
}
