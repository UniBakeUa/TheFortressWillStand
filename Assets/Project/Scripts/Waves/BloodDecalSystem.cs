using UnityEngine;

namespace Waves
{
    /// <summary>
    /// Персистентний world-space шар крові.
    ///
    /// Кров живе в одній RenderTexture (R8), яка накриває ігрову зону і мапиться
    /// у світ так само, як сітка води. Смерть гуманоїда домальовує пляму
    /// (Pass "Stamp"), а хвиля щокадру стирає кров там, де вода накрила пісок
    /// (Pass "Wash", читає ту саму глобальну _WaterHeightTex, що й TopDownWater).
    ///
    /// Малювання - це один blit на пляму, змивання - один blit на кадр, тож
    /// кількість трупів не впливає на кількість об'єктів у сцені.
    /// </summary>
    public class BloodDecalSystem : MonoBehaviour
    {
        public static BloodDecalSystem Instance { get; private set; }

        [Header("Область крові у світових координатах")]
        [Tooltip("Якщо заданий - область береться з цього трансформа (зазвичай Sand), а не з полів нижче")]
        [SerializeField] private Transform _areaSource;
        [SerializeField] private Vector2 _areaOrigin = new Vector2(-5f, -5f);
        [SerializeField] private Vector2 _areaSize = new Vector2(50f, 32f);

        [Header("Роздільна здатність")]
        [Tooltip("Пікселів текстури на одну світову одиницю. 32 - достатньо для плям, 64 - різкіше і вчетверо дорожче")]
        [SerializeField] private float _pixelsPerUnit = 32f;
        [SerializeField] private int _maxTextureSize = 2048;

        [Header("Форма плями")]
        [Tooltip("Радіус калюжі під тілом, у світових одиницях")]
        [SerializeField] private float _splatRadius = 0.35f;
        [Tooltip("Довжина розльоту крові за напрямком удару")]
        [SerializeField] private float _splatLength = 1.4f;
        [Tooltip("Випадковий розкид радіуса/довжини (частка від базового значення)")]
        [SerializeField, Range(0f, 1f)] private float _splatVariance = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _splatStrength = 1f;

        [Header("Тонкі лінії-патьоки з крапками")]
        [Tooltip("Скільки з 6 можливих ліній лишається. 0 = без ліній, 1 = всі")]
        [SerializeField, Range(0f, 1f)] private float _streakDensity = 0.65f;
        [Tooltip("Множник довжини ліній")]
        [SerializeField, Range(0.1f, 3f)] private float _streakLength = 1f;
        [Tooltip("Множник товщини ліній і крапок на них")]
        [SerializeField, Range(0.1f, 3f)] private float _streakWidth = 1f;

        [Header("Змивання хвилею")]
        [Tooltip("Глибина води, з якої вона починає змивати кров")]
        [SerializeField] private float _washDepthThreshold = 0.05f;
        [Tooltip("Частка крові, що змивається за секунду під водою")]
        [SerializeField] private float _washSpeed = 2.5f;
        [Tooltip("Повільне висихання/вбирання в пісок без води, частка за секунду. 0 = кров лишається до хвилі")]
        [SerializeField] private float _dryFadeSpeed = 0f;

        [Header("Матеріали")]
        [SerializeField] private Shader _stampShader;
        [SerializeField] private Renderer _decalRenderer;

        private RenderTexture _bloodRT;
        private Material _stampMaterial;
        private MaterialPropertyBlock _decalBlock;

        private static readonly int BloodTexID = Shader.PropertyToID("_BloodTex");
        private static readonly int BloodAreaRectID = Shader.PropertyToID("_BloodAreaRect");

        private static readonly int SplatCenterID = Shader.PropertyToID("_SplatCenter");
        private static readonly int SplatDirID = Shader.PropertyToID("_SplatDir");
        private static readonly int SplatRadiusID = Shader.PropertyToID("_SplatRadius");
        private static readonly int SplatLengthID = Shader.PropertyToID("_SplatLength");
        private static readonly int SplatSeedID = Shader.PropertyToID("_SplatSeed");
        private static readonly int SplatStrengthID = Shader.PropertyToID("_SplatStrength");
        private static readonly int SplatRadialID = Shader.PropertyToID("_SplatRadial");
        private static readonly int StreakDensityID = Shader.PropertyToID("_StreakDensity");
        private static readonly int StreakLengthID = Shader.PropertyToID("_StreakLength");
        private static readonly int StreakWidthID = Shader.PropertyToID("_StreakWidth");

        private static readonly int WashDepthThresholdID = Shader.PropertyToID("_WashDepthThreshold");
        private static readonly int WashAmountID = Shader.PropertyToID("_WashAmount");
        private static readonly int DryFadeID = Shader.PropertyToID("_DryFade");
        private static readonly int WashTimeID = Shader.PropertyToID("_WashTime");

        private const int StampPass = 0;
        private const int WashPass = 1;

        private Vector2 _currentOrigin;
        private Vector2 _currentSize;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (_stampShader == null)
                _stampShader = Shader.Find("Hidden/BloodStamp");

            if (_stampShader == null)
            {
                Debug.LogError("[BloodDecalSystem] Не знайдено шейдер Hidden/BloodStamp - кров не працюватиме.");
                enabled = false;
                return;
            }

            _stampMaterial = new Material(_stampShader) { hideFlags = HideFlags.HideAndDontSave };
            _decalBlock = new MaterialPropertyBlock();

            RebuildArea();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (_bloodRT != null)
            {
                _bloodRT.Release();
                Destroy(_bloodRT);
                _bloodRT = null;
            }

            if (_stampMaterial != null) Destroy(_stampMaterial);
        }

        /// <summary>
        /// Перебудовує область крові під поточний розмір/позицію піску.
        /// Викликати після зміни BuildingAreaConfig (як RebuildWaterGrid для води).
        /// </summary>
        public void RebuildArea()
        {
            ResolveArea(out _currentOrigin, out _currentSize);

            int w = Mathf.Clamp(Mathf.RoundToInt(_currentSize.x * _pixelsPerUnit), 64, _maxTextureSize);
            int h = Mathf.Clamp(Mathf.RoundToInt(_currentSize.y * _pixelsPerUnit), 64, _maxTextureSize);

            bool needsNewTexture = _bloodRT == null || _bloodRT.width != w || _bloodRT.height != h;

            if (needsNewTexture)
            {
                if (_bloodRT != null)
                {
                    _bloodRT.Release();
                    Destroy(_bloodRT);
                }

                _bloodRT = new RenderTexture(w, h, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear)
                {
                    name = "BloodDecalRT",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    useMipMap = false
                };
                _bloodRT.Create();
            }

            // Нова зона - чистий пісок: стара кров все одно мапилась би не туди.
            Clear();
            PushGlobals();
        }

        /// <summary>Стирає всю кров миттєво.</summary>
        public void Clear()
        {
            if (_bloodRT == null) return;

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _bloodRT;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = previous;
        }

        private void ResolveArea(out Vector2 origin, out Vector2 size)
        {
            if (_areaSource != null)
            {
                // Sand - квадратний спрайт 1x1, масштабований; його трансформ і
                // задає покриту зону.
                Vector3 scale = _areaSource.lossyScale;
                Vector3 center = _areaSource.position;

                size = new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                origin = (Vector2)center - size * 0.5f;
                return;
            }

            origin = _areaOrigin;
            size = _areaSize;
        }

        private void PushGlobals()
        {
            Shader.SetGlobalVector(BloodAreaRectID,
                new Vector4(_currentOrigin.x, _currentOrigin.y, _currentSize.x, _currentSize.y));

            if (_decalRenderer != null && _bloodRT != null)
            {
                _decalRenderer.GetPropertyBlock(_decalBlock);
                _decalBlock.SetTexture(BloodTexID, _bloodRT);
                _decalRenderer.SetPropertyBlock(_decalBlock);
            }

            Shader.SetGlobalTexture(BloodTexID, _bloodRT);
        }

        /// <summary>
        /// Домальовує пляму крові.
        /// </summary>
        /// <param name="worldPosition">де загинув ворог</param>
        /// <param name="impactDirection">
        /// куди розлітається кров - тобто напрямок ВІД джерела пострілу ДО ворога.
        /// Нульовий вектор = випадковий напрямок.
        /// </param>
        /// <param name="scale">множник розміру плями</param>
        public void PaintSplat(Vector2 worldPosition, Vector2 impactDirection, float scale = 1f)
        {
            Vector2 dir = impactDirection.sqrMagnitude > 0.0001f
                ? impactDirection.normalized
                : Random.insideUnitCircle.normalized;

            Stamp(worldPosition, dir, scale, radial: false);
        }

        /// <summary>
        /// Домальовує кляксу без напрямку - для ударів "згори" (постріл мишкою),
        /// де кров розбризкується рівномірно навколо, а не летить убік.
        /// </summary>
        /// <param name="worldPosition">де загинув ворог</param>
        /// <param name="scale">множник розміру плями</param>
        public void PaintBlob(Vector2 worldPosition, float scale = 1f)
        {
            Stamp(worldPosition, Vector2.right, scale, radial: true);
        }

        private void Stamp(Vector2 worldPosition, Vector2 direction, float scale, bool radial)
        {
            if (_bloodRT == null || _stampMaterial == null) return;

            // Пляма поза покритою зоною - малювати нема куди
            if (worldPosition.x < _currentOrigin.x - _splatLength ||
                worldPosition.y < _currentOrigin.y - _splatLength ||
                worldPosition.x > _currentOrigin.x + _currentSize.x + _splatLength ||
                worldPosition.y > _currentOrigin.y + _currentSize.y + _splatLength)
            {
                return;
            }

            float variance = 1f + Random.Range(-_splatVariance, _splatVariance);

            _stampMaterial.SetVector(SplatCenterID, worldPosition);
            _stampMaterial.SetVector(SplatDirID, direction);
            _stampMaterial.SetFloat(SplatRadiusID, _splatRadius * scale * variance);
            _stampMaterial.SetFloat(SplatLengthID, _splatLength * scale * variance);
            _stampMaterial.SetFloat(SplatSeedID, Random.Range(0f, 100f));
            _stampMaterial.SetFloat(SplatStrengthID, _splatStrength);
            _stampMaterial.SetFloat(SplatRadialID, radial ? 1f : 0f);
            _stampMaterial.SetFloat(StreakDensityID, _streakDensity);
            _stampMaterial.SetFloat(StreakLengthID, _streakLength);
            _stampMaterial.SetFloat(StreakWidthID, _streakWidth);
            _stampMaterial.SetVector(BloodAreaRectID,
                new Vector4(_currentOrigin.x, _currentOrigin.y, _currentSize.x, _currentSize.y));

            BlitInPlace(StampPass);
        }

        private void LateUpdate()
        {
            if (_bloodRT == null || _stampMaterial == null) return;

            // Область прив'язана до піску, а той рухається при зміні етапу -
            // тримаємо мапінг актуальним.
            ResolveArea(out Vector2 origin, out Vector2 size);
            if ((origin - _currentOrigin).sqrMagnitude > 0.0001f ||
                (size - _currentSize).sqrMagnitude > 0.0001f)
            {
                RebuildArea();
                return;
            }

            PushGlobals();
            WashStep();
        }

        private void WashStep()
        {
            float washAmount = _washSpeed * Time.deltaTime;
            float dryFade = _dryFadeSpeed * Time.deltaTime;

            if (washAmount <= 0f && dryFade <= 0f) return;

            _stampMaterial.SetFloat(WashDepthThresholdID, _washDepthThreshold);
            _stampMaterial.SetFloat(WashAmountID, Mathf.Clamp01(washAmount));
            _stampMaterial.SetFloat(DryFadeID, Mathf.Clamp01(dryFade));
            _stampMaterial.SetFloat(WashTimeID, Time.time);
            _stampMaterial.SetVector(BloodAreaRectID,
                new Vector4(_currentOrigin.x, _currentOrigin.y, _currentSize.x, _currentSize.y));

            BlitInPlace(WashPass);
        }

        // Обидва паси читають ТІЛЬКИ uv (не саму RT), тож можна рендерити
        // в ту саму текстуру без проміжного буфера - blend-режим паса робить
        // усю роботу (Max для плями, множення для змивання).
        private void BlitInPlace(int pass)
        {
            RenderTexture previous = RenderTexture.active;

            RenderTexture.active = _bloodRT;
            GL.PushMatrix();
            GL.LoadOrtho();

            _stampMaterial.SetPass(pass);

            GL.Begin(GL.QUADS);
            GL.TexCoord2(0f, 0f); GL.Vertex3(0f, 0f, 0f);
            GL.TexCoord2(1f, 0f); GL.Vertex3(1f, 0f, 0f);
            GL.TexCoord2(1f, 1f); GL.Vertex3(1f, 1f, 0f);
            GL.TexCoord2(0f, 1f); GL.Vertex3(0f, 1f, 0f);
            GL.End();

            GL.PopMatrix();
            RenderTexture.active = previous;
        }

        private void OnDrawGizmosSelected()
        {
            ResolveArea(out Vector2 origin, out Vector2 size);

            Gizmos.color = new Color(0.8f, 0.1f, 0.1f, 0.6f);
            Gizmos.DrawWireCube(origin + size * 0.5f, new Vector3(size.x, size.y, 0.01f));
        }
    }
}
