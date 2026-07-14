using System.Collections;
using UnityEngine;

namespace Waves
{
    public class WaterGrid : MonoBehaviour
    {
        [SerializeField] int width = 128, height = 128;
        [SerializeField] float cellSize = 0.25f;
        [SerializeField] Vector2 origin;
        public event System.Action OnWaveFinished;
        public float CellSize => cellSize; // потрібен Solid для розрахунку "запасу" при семплінгу зовні власного тіла

        [Header("ДІАГНОСТИКА: пряме читання власних масивів (обходить Solid/дебагер повністю)")]
        [SerializeField] bool logDebugProbe = false;
        [SerializeField] Vector2 debugProbeWorldPos = new Vector2(15.28f, 9.06f);
        float _debugTimer;

        [Header("Потік")]
        [SerializeField] float flowX = 0.05f;

        [Header("Налаштування хвилі")]
        [SerializeField] float defaultWaterLevel = 0.4f;
        [SerializeField] float waveTargetLevel = 0.6f;
        [Header("Динамічний Damping")]
        [SerializeField] float dampNormal = 0.95f;
        [SerializeField] float dampRising = 0.86f;
        [SerializeField] float dampFalling = 0.5f;

        [Header("Таймінги")]
        [SerializeField] private float normalTime = 2.0f;
        [SerializeField] private float torisingTime = 1.0f;
        [SerializeField] private float risingTime = 1.0f;
        [SerializeField] private float fallingTime = 2.0f;
        [SerializeField] private float recoverTime = 2.0f;

        private float _currentDamping;
        Texture2D _renderTex;
        static readonly int WaterHeightTexID = Shader.PropertyToID("_WaterHeightTex");

        float[] _bufA, _bufB;
        bool[] _solid;
        float[] _terrainHeight;

        float _dynamicInjectedLevel;
        Coroutine _waveCoroutine;
        int Idx(int x, int y) => y * width + x;

        [Header("Берег - лінія проходить точно через ці дві world-точки")]
        [SerializeField] float restLineWorldX = 5f;
        [SerializeField] float tsunamiLineWorldX = 3f;
// Метод для динамічної зміни сили хвилі
        public void SetWaveIntensity(float intensity)
        {
            // intensity від 0 до 1
            waveTargetLevel = Mathf.Lerp(0.5f, 1.5f, intensity); 
        }
        void Awake()
        {
            _currentDamping = 0f;
            StartCoroutine(InitialDampingRoutine());

            EnsureBuffersAllocated();
            GenerateNaturalTerrain();
        }

        private IEnumerator InitialDampingRoutine()
        {
            float timer = 0f;
            while (timer < normalTime)
            {
                timer += Time.deltaTime;
                _currentDamping = Mathf.Lerp(0f, dampNormal, timer / normalTime);
                yield return null;
            }
        }

        void EnsureBuffersAllocated()
        {
            int n = width * height;
            if (_terrainHeight != null && _terrainHeight.Length == n) return;

            _bufA = new float[n];
            _bufB = new float[n];
            _solid = new bool[n];
            _terrainHeight = new float[n];
        }

        [ContextMenu("Regenerate Terrain From Lines")]
        public void RegenerateTerrainFromLines()
        {
            EnsureBuffersAllocated();
            GenerateNaturalTerrain();
        }

        void GenerateNaturalTerrain()
        {
            float dx = tsunamiLineWorldX - restLineWorldX;
            float slope = Mathf.Abs(dx) > 0.0001f ? (waveTargetLevel - defaultWaterLevel) / dx : 0f;

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int i = Idx(x, y);
                float worldX = origin.x + x * cellSize;
                _terrainHeight[i] = defaultWaterLevel + slope * (worldX - restLineWorldX);
            }
        }

        void OnDrawGizmos()
        {
            if (_terrainHeight == null || _terrainHeight.Length == 0) return;

            Gizmos.matrix = transform.localToWorldMatrix;

            for (int y = 0; y < height; y += 4)
            {
                for (int x = 0; x < width; x += 4)
                {
                    float h = _terrainHeight[Idx(x, y)];
                    Gizmos.color = new Color(1f, 0.9f, 0.6f, h);
                    Vector3 pos = new Vector3(x * cellSize, y * cellSize, 0);
                    Gizmos.DrawCube(pos, new Vector3(cellSize * 3, cellSize * 3, 0.1f));
                }
            }
        }

        [ContextMenu("Trigger Tsunami Wave")]
        public void TriggerWave()
        {
            if (_waveCoroutine != null) StopCoroutine(_waveCoroutine);
            _waveCoroutine = StartCoroutine(TsunamiSequence());
        }

        public bool TryGetRandomFloodedPosition(out Vector3 result)
        {
            result = Vector2.zero;

            for (int attempts = 0; attempts < 20; attempts++)
            {
                int x = UnityEngine.Random.Range(0, width);
                int y = UnityEngine.Random.Range(0, height);
                int i = Idx(x, y);

                // Перевіряємо, чи немає тут стіни/вежі і чи рівень води вище мінімального
                if (!_solid[i] && _bufA[i] > 0.1f)
                {
                    result = GridToWorld(x, y);
                    return true;
                }
            }

            return false;
        }

        public bool IsFlooded(Vector2 worldPos)
        {
            WorldToGrid(worldPos, out int gx, out int gy);

            // Якщо точка за межами сітки — води там точно немає
            if (!InBounds(gx, gy)) return false;

            int i = Idx(gx, gy);
            // Перевіряємо, чи немає там перешкоди і чи рівень води достатній
            return !_solid[i] && _bufA[i] > 0.1f;
        }

        private IEnumerator TsunamiSequence()
        {
            // 1. Прилив (Rising)
            float timer = 0f;
            float startDamp = _currentDamping;
            while (timer < torisingTime)
            {
                timer += Time.deltaTime;
                _currentDamping = Mathf.Lerp(startDamp, dampRising, timer / torisingTime);
                _dynamicInjectedLevel = Mathf.Lerp(defaultWaterLevel, waveTargetLevel, timer / torisingTime);
                yield return null;
            }

            yield return new WaitForSeconds(risingTime);

            // 2. Відлив (Falling)
            timer = 0f;
            while (timer < fallingTime)
            {
                timer += Time.deltaTime;
                _currentDamping = Mathf.Lerp(dampRising, dampFalling, timer / fallingTime);
                _dynamicInjectedLevel = Mathf.Lerp(waveTargetLevel, defaultWaterLevel, timer / fallingTime);
                yield return null;
            }

            // 3. Відновлення (Recover)
            // ФІКС: _waveCoroutine = null тепер ПІСЛЯ while, а не всередині нього -
            // раніше інжекція зупинялась вже на другому кадрі фази відновлення
            timer = 0f;
            while (timer < recoverTime)
            {
                timer += Time.deltaTime;
                _currentDamping = Mathf.Lerp(dampFalling, dampNormal, timer / recoverTime);

                float recoveryFactor = 1f - (timer / recoverTime);
                for (int i = 0; i < _bufB.Length; i++)
                {
                    if (_bufB[i] > defaultWaterLevel)
                        _bufB[i] = Mathf.Lerp(_bufB[i], defaultWaterLevel, 0.05f * recoveryFactor);
                }

                yield return null;
            }

            _currentDamping = dampNormal;
            _waveCoroutine = null;
            OnWaveFinished?.Invoke();
        }

        void InjectWaterAtRightEdge(float targetLevel)
        {
            int startX = Mathf.FloorToInt(width * 0.7f);
            float pressure = (targetLevel - defaultWaterLevel) * 0.5f;

            for (int y = 0; y < height; y++)
            {
                for (int x = startX; x < width; x++)
                {
                    int i = Idx(x, y);
                    if (_solid[i]) continue;

                    _bufB[i] += pressure;
                    _bufB[i] = Mathf.Clamp(_bufB[i], 0f, 2.5f);
                }
            }
        }

        void FixedUpdate()
        {
            int oceanStartX = Mathf.FloorToInt((restLineWorldX + 2f - origin.x) / cellSize);
            for (int y = 0; y < height; y++)
            {
                for (int x = Mathf.Max(oceanStartX, 0); x < width; x++)
                {
                    if (_bufB[Idx(x, y)] < defaultWaterLevel)
                        _bufB[Idx(x, y)] = defaultWaterLevel;
                }
            }

            if (_waveCoroutine != null)
            {
                InjectWaterAtRightEdge(_dynamicInjectedLevel);
            }

            Step();
        }

        [SerializeField, Range(0.05f, 0.49f)] float waveSpeedSq = 0.4f;

        void Step()
        {
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int i = Idx(x, y);
                if (_solid[i]) { _bufA[i] = 0f; _bufB[i] = 0f; continue; }

                int shift = Mathf.RoundToInt(_bufB[i] * 2.0f);
                int xL = Mathf.Clamp(x - 1 - shift, 0, width - 1);
                int xR = Mathf.Clamp(x + 1 - shift, 0, width - 1);
                int yU = Mathf.Max(y - 1, 0), yD = Mathf.Min(y + 1, height - 1);

                float lap = _bufB[Idx(xL, y)] + _bufB[Idx(xR, y)] + _bufB[Idx(x, yU)] + _bufB[Idx(x, yD)] - 4f * _bufB[i];

                float newH = 2f * _bufB[i] - _bufA[i] + waveSpeedSq * lap;

                if (_bufB[i] > 0.05f && _bufB[i] < _terrainHeight[i] + 0.1f)
                {
                    newH += flowX * 0.5f;
                }

                _bufA[i] = Mathf.Clamp(newH * _currentDamping, 0, 10f);
            }
            (_bufA, _bufB) = (_bufB, _bufA);
        }
        
        float[] _smoothingScratch;
        
        public void RegisterObstacle(Vector2 w, float r) => SetCircle(w, r, true);
        public void UnregisterObstacle(Vector2 w, float r = -1f) => SetCircle(w, r > 0 ? r : cellSize, false);

        void SetCircle(Vector2 worldPos, float radius, bool value)
        {
            WorldToGrid(worldPos, out int cx, out int cy);
            int r = Mathf.CeilToInt(radius / cellSize);
            for (int y = -r; y <= r; y++)
            for (int x = -r; x <= r; x++)
            {
                int gx = cx + x, gy = cy + y;
                if (InBounds(gx, gy) && x * x + y * y <= r * r) _solid[Idx(gx, gy)] = value;
            }
        }

        public void RegisterObstacleCapsule(Vector2 a, Vector2 b, float hw) => SetCapsule(a, b, hw, true);
        public void UnregisterObstacleCapsule(Vector2 a, Vector2 b, float hw) => SetCapsule(a, b, hw, false);

        void SetCapsule(Vector2 a, Vector2 b, float hw, bool value)
        {
            WorldToGrid(a, out int ax, out int ay);
            WorldToGrid(b, out int bx, out int by);
            int minX = Mathf.Min(ax, bx) - Mathf.CeilToInt(hw / cellSize) - 1;
            int maxX = Mathf.Max(ax, bx) + Mathf.CeilToInt(hw / cellSize) + 1;
            int minY = Mathf.Min(ay, by) - Mathf.CeilToInt(hw / cellSize) - 1;
            int maxY = Mathf.Max(ay, by) + Mathf.CeilToInt(hw / cellSize) + 1;

            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                if (!InBounds(x, y)) continue;
                if (DistancePointToSegment(GridToWorld(x, y), a, b) <= hw) _solid[Idx(x, y)] = value;
            }
        }

        static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f));
            return Vector2.Distance(p, a + ab * t);
        }

        Vector2 GridToWorld(int x, int y) => origin + new Vector2(x, y) * cellSize;
        void WorldToGrid(Vector2 w, out int x, out int y)
        {
            x = Mathf.FloorToInt((w.x - origin.x) / cellSize);
            y = Mathf.FloorToInt((w.y - origin.y) / cellSize);
        }
        bool InBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;

        public (float exposure, float avgDepth) SampleCoverage(Vector2 min, Vector2 max)
        {
            WorldToGrid(min, out int x0, out int y0);
            WorldToGrid(max, out int x1, out int y1);

            int total = 0, submerged = 0;
            float depthSum = 0f;

            for (int y = Mathf.Clamp(y0, 0, height - 1); y <= Mathf.Clamp(y1, 0, height - 1); y++)
            for (int x = Mathf.Clamp(x0, 0, width - 1); x <= Mathf.Clamp(x1, 0, width - 1); x++)
            {
                int i = Idx(x, y);
                total++;
                if (_solid[i]) continue;
        
                // РАДИКАЛЬНА ЗМІНА:
                // Ми беремо просто рівень води (_bufA[i]), не порівнюючи з ландшафтом
                float currentLevel = _bufA[i];
        
                if (currentLevel > 0.01f) 
                { 
                    submerged++; 
                    depthSum += currentLevel; 
                }
            }
            return total == 0 ? (0f, 0f) : ((float)submerged / total, submerged > 0 ? depthSum / submerged : 0f);
        }
        static readonly int WaterGridOriginID = Shader.PropertyToID("_WaterGridOrigin");
        static readonly int WaterGridCellSizeID = Shader.PropertyToID("_WaterGridCellSize");
        static readonly int WaterGridSizeID = Shader.PropertyToID("_WaterGridSize");

        // ФІКС: раніше перший if логував помилку і виходив з методу ДО того, як текстура
        // встигала створитись - _renderTex залишався null назавжди, помилка спамилась щокадру
        void LateUpdate()
        {
            if (_renderTex == null)
                _renderTex = new Texture2D(width, height, TextureFormat.RFloat, false);

            var raw = _renderTex.GetRawTextureData<float>();
            for (int i = 0; i < _bufA.Length; i++) raw[i] = _solid[i] ? 0f : _bufA[i];
            _renderTex.Apply();

            Shader.SetGlobalTexture(WaterHeightTexID, _renderTex);
            Shader.SetGlobalVector(WaterGridOriginID, origin);
            Shader.SetGlobalFloat(WaterGridCellSizeID, cellSize);
            Shader.SetGlobalVector(WaterGridSizeID, new Vector2(width, height));

            if (logDebugProbe) LogDebugProbe();
        }

        // Пряме читання _bufA/_terrainHeight/_solid ЦЬОГО САМОГО екземпляра WaterGrid -
        // якщо ці числа відрізняються від того, що показує WaterDamageDebugger/Solid,
        // значить десь посилання йде на ІНШИЙ екземпляр WaterGrid на сцені.
        [SerializeField] float probeRingRadius = 0.8f; // трохи більше за registeredRadius вежі (0.5)

        void LogDebugProbe()
        {
            _debugTimer += Time.deltaTime;
            if (_debugTimer < 1f) return;
            _debugTimer = 0f;

            // центр (очікувано _solid=True, це нормально)
            LogPointAt(debugProbeWorldPos, "center");

            // кільце з 8 точок навколо, за межами Solid-зони вежі - ось де мала б бути вода
            for (int k = 0; k < 8; k++)
            {
                float angle = k * Mathf.PI * 2f / 8f;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * probeRingRadius;
                LogPointAt(debugProbeWorldPos + offset, $"ring[{k}]");
            }
        }

        void LogPointAt(Vector2 worldPos, string label)
        {
            WorldToGrid(worldPos, out int gx, out int gy);
            if (!InBounds(gx, gy))
            {
                Debug.LogError($"[probe {label}] worldPos={worldPos} -> grid({gx},{gy}) ПОЗА МЕЖАМИ сітки");
                return;
            }

            int i = Idx(gx, gy);
            Debug.LogError($"[probe {label}] worldPos={worldPos} -> grid({gx},{gy}) | " +
                      $"_bufA={_bufA[i]:F3}, _terrainHeight={_terrainHeight[i]:F3}, _solid={_solid[i]}, " +
                      $"depthOnLand={_bufA[i] - _terrainHeight[i]:F3}");
        }
    }
}