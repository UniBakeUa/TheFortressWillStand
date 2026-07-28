using UnityEngine;

namespace Waves
{
    /// <summary>
    /// Тримає карту "мокрості" піску.
    ///
    /// Вода (_WaterHeightTex) показує лише ПОТОЧНИЙ стан: коли хвиля відкотилась,
    /// вона зникає з текстури миттєво. Пісок так поводитись не повинен - тому
    /// мокрість накопичується в окремій RT: під водою підскакує до 1, а далі
    /// повільно спадає, і за хвилею лишається темна смуга, що поступово сохне.
    ///
    /// Шейдер читає попередній стан, тож потрібні два буфери (ping-pong):
    /// писати в ту саму текстуру, яку читаєш, не можна.
    /// </summary>
    public class SandWetnessSystem : MonoBehaviour
    {
        public static SandWetnessSystem Instance { get; private set; }

        [Header("Область піску у світових координатах")]
        [Tooltip("Якщо заданий - область береться з цього трансформа (Sand), а не з полів нижче")]
        [SerializeField] private Transform _areaSource;
        [SerializeField] private Vector2 _areaOrigin = new Vector2(-5f, -5f);
        [SerializeField] private Vector2 _areaSize = new Vector2(50f, 32f);

        [Header("Роздільна здатність")]
        [Tooltip("Пікселів на світову одиницю. Мокрість - плавна маска, тут вистачає менше, ніж для крові")]
        [SerializeField] private float _pixelsPerUnit = 16f;
        [SerializeField] private int _maxTextureSize = 1024;

        [Header("Намокання / висихання")]
        [Tooltip("Глибина води, з якої пісок вважається залитим")]
        [SerializeField] private float _waterDepthThreshold = 0.02f;
        [Tooltip("Скільки часу (сек) мокрий пісок сохне до сухого")]
        [SerializeField] private float _dryDuration = 6f;

        [Header("Шейдер")]
        [SerializeField] private Shader _wetnessShader;

        private RenderTexture _wetRT;
        private RenderTexture _prevRT;
        private Material _wetnessMaterial;

        private static readonly int SandWetTexID = Shader.PropertyToID("_SandWetTex");
        private static readonly int SandAreaRectID = Shader.PropertyToID("_SandAreaRect");
        private static readonly int PrevWetTexID = Shader.PropertyToID("_PrevWetTex");
        private static readonly int WaterDepthThresholdID = Shader.PropertyToID("_WaterDepthThreshold");
        private static readonly int DryAmountID = Shader.PropertyToID("_DryAmount");

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

            if (_wetnessShader == null)
                _wetnessShader = Shader.Find("Hidden/SandWetness");

            if (_wetnessShader == null)
            {
                Debug.LogError("[SandWetnessSystem] Не знайдено шейдер Hidden/SandWetness - пісок не намокатиме.");
                enabled = false;
                return;
            }

            _wetnessMaterial = new Material(_wetnessShader) { hideFlags = HideFlags.HideAndDontSave };

            RebuildArea();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            ReleaseTexture(ref _wetRT);
            ReleaseTexture(ref _prevRT);

            if (_wetnessMaterial != null) Destroy(_wetnessMaterial);
        }

        private void ReleaseTexture(ref RenderTexture rt)
        {
            if (rt == null) return;
            rt.Release();
            Destroy(rt);
            rt = null;
        }

        /// <summary>
        /// Перебудовує область під поточний розмір/позицію піску.
        /// Викликати після зміни BuildingAreaConfig.
        /// </summary>
        public void RebuildArea()
        {
            ResolveArea(out _currentOrigin, out _currentSize);

            int w = Mathf.Clamp(Mathf.RoundToInt(_currentSize.x * _pixelsPerUnit), 32, _maxTextureSize);
            int h = Mathf.Clamp(Mathf.RoundToInt(_currentSize.y * _pixelsPerUnit), 32, _maxTextureSize);

            if (_wetRT == null || _wetRT.width != w || _wetRT.height != h)
            {
                ReleaseTexture(ref _wetRT);
                ReleaseTexture(ref _prevRT);

                _wetRT = CreateTexture(w, h, "SandWetnessRT");
                _prevRT = CreateTexture(w, h, "SandWetnessPrevRT");
            }

            Clear();
            PushGlobals();
        }

        private RenderTexture CreateTexture(int w, int h, string name)
        {
            var rt = new RenderTexture(w, h, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false
            };
            rt.Create();
            return rt;
        }

        /// <summary>Робить увесь пісок сухим миттєво.</summary>
        public void Clear()
        {
            ClearTexture(_wetRT);
            ClearTexture(_prevRT);
        }

        private void ClearTexture(RenderTexture rt)
        {
            if (rt == null) return;

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = previous;
        }

        private void ResolveArea(out Vector2 origin, out Vector2 size)
        {
            if (_areaSource != null)
            {
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
            Shader.SetGlobalVector(SandAreaRectID,
                new Vector4(_currentOrigin.x, _currentOrigin.y, _currentSize.x, _currentSize.y));
            Shader.SetGlobalTexture(SandWetTexID, _wetRT);
        }

        private void LateUpdate()
        {
            if (_wetRT == null || _wetnessMaterial == null) return;

            // Пісок рухається при зміні етапу - тримаємо мапінг актуальним
            ResolveArea(out Vector2 origin, out Vector2 size);
            if ((origin - _currentOrigin).sqrMagnitude > 0.0001f ||
                (size - _currentSize).sqrMagnitude > 0.0001f)
            {
                RebuildArea();
                return;
            }

            StepWetness();
            PushGlobals();
        }

        private void StepWetness()
        {
            // Шейдер читає попередній стан, тож спершу копіюємо його вбік
            Graphics.Blit(_wetRT, _prevRT);

            float dryAmount = _dryDuration > 0.001f ? Time.deltaTime / _dryDuration : 1f;

            _wetnessMaterial.SetTexture(PrevWetTexID, _prevRT);
            _wetnessMaterial.SetFloat(WaterDepthThresholdID, _waterDepthThreshold);
            _wetnessMaterial.SetFloat(DryAmountID, Mathf.Clamp01(dryAmount));
            _wetnessMaterial.SetVector(SandAreaRectID,
                new Vector4(_currentOrigin.x, _currentOrigin.y, _currentSize.x, _currentSize.y));

            Graphics.Blit(null, _wetRT, _wetnessMaterial, 0);
        }

        private void OnDrawGizmosSelected()
        {
            ResolveArea(out Vector2 origin, out Vector2 size);

            Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.6f);
            Gizmos.DrawWireCube(origin + size * 0.5f, new Vector3(size.x, size.y, 0.01f));
        }
    }
}
