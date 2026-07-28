using System.Collections;
using System.Collections.Generic;
using Towers.Models;
using Towers.ScriptableObjects;
using Towers.UI;
using UnityEngine;
using Waves;

namespace Towers.Buildings
{
    public class BaseBuilding : MonoBehaviour, IDamageable
    {
        [Header("����� ������������")]
        [SerializeField] private bool _isSlipable;

        [Header("Child Objects")]
        [SerializeField] protected BuildingView _buildingView;

        public bool IsSlipable => _isSlipable;
        public BuildingModel Model { get; protected set; }

        // Усі живі будівлі. Перки авторемонту і бафи турелей ходять сюди замість
        // FindObjectsOfType щокадру. Реєстрація в Initialize, зняття - в Collapse
        // і OnDestroy (будівля може зникнути і не через Collapse).
        private static readonly List<BaseBuilding> _allBuildings = new();

        /// <summary>Усі збудовані будівлі, включно з фортецею.</summary>
        public static IReadOnlyList<BaseBuilding> AllBuildings => _allBuildings;

        /// <summary>
        /// Чи це фортеця. Перки авторемонту розділені: один лагодить фортецю,
        /// інший - усе решта, тож їм треба відрізняти одне від одного.
        /// </summary>
        public virtual bool IsFortress => false;

        /// <summary>Будівля пошкоджена і її є сенс лагодити.</summary>
        public bool NeedsRepair => Model != null && Model.CurrentHP > 0f && Model.CurrentHP < Model.MaxHP;
        public float ExposureFraction { get; private set; }
        public bool IsReady { get; protected set; }
        protected virtual float ErosionRate => Model.BaseErosionRate;

        protected Collider2D Collider { get; private set; }
        protected WaterGrid WaterGrid { get; private set; }
        private float _registeredRadius;

        /// <summary>Радіус колайдера будівлі у world-одиницях (для обходу ворогами).</summary>
        public float ObstacleRadius
        {
            get
            {
                if (Collider == null) return 0f;
                var bounds = Collider.bounds;
                return Mathf.Max(bounds.extents.x, bounds.extents.y);
            }
        }

        protected virtual void Awake()
        {
            Collider = GetComponent<Collider2D>();
            WaterGrid = FindFirstObjectByType<WaterGrid>();
            WaterGrid.OnGridRebuilt += OnGridRebuilt;
        }

        public virtual void Initialize(BuildingConfig config)
        {
            Model = new BuildingModel(config);

            if (_buildingView != null)
            {
                _buildingView.SetupHealth(Model.MaxHP);
                Model.OnHealthChanged += _buildingView.UpdateHealth;
            }

            IsReady = true;
            RegisterInRegistry();
            RegisterFootprint();
            PlaySpawnAnimation();

            // Нова будівля могла з'явитись на шляху ворогів (вежа/турель - точкова
            // перешкода для pathfinder-графа) - інвалідуємо кеш, щоб наступний
            // перерахунок шляху враховував її. Wall.Collapse() інвалідує окремо
            // при руйнуванні, тут - тільки поява нової перешкоди.
            Items.EnemyPathfinder.InvalidateCache();
        }

        protected virtual void RegisterFootprint()
        {
            var bounds = Collider.bounds;
            _registeredRadius = Mathf.Max(bounds.extents.x, bounds.extents.y);
            _registeredRadius = 0.5f;
            WaterGrid.RegisterObstacle(bounds.center, _registeredRadius);
        }

        protected virtual void FixedUpdate()
        {
            if (!IsReady || Model.CurrentHP <= 0f) return;

            var bounds = Collider.bounds;
            float margin = WaterGrid.CellSize * 2f;
            Vector3 min = bounds.min - new Vector3(margin, margin, 0f);
            Vector3 max = bounds.max + new Vector3(margin, margin, 0f);

            var (exposure, _) = WaterGrid.SampleCoverage(min, max);
            ExposureFraction = exposure;

            if (exposure > 0f)
            {
                float damage = exposure * ErosionRate * Time.fixedDeltaTime;
                TakeDamage(damage);
            }
        }

        protected virtual void OnGridRebuilt()
        {
            if (!IsReady) return;
            var bounds = Collider.bounds;
            WaterGrid.RegisterObstacle(bounds.center, _registeredRadius);
        }

        public virtual void TakeDamage(float amount)
        {
            if (Model == null || Model.CurrentHP <= 0f) return;
            Model.CurrentHP -= amount;
            if (Model.CurrentHP <= 0f) Collapse();
        }

        /// <summary>
        /// Ставить будівлю в глобальний реєстр. Fortress перевизначає Initialize
        /// без виклику base, тож кличе цей метод сам.
        /// </summary>
        protected void RegisterInRegistry()
        {
            if (!_allBuildings.Contains(this))
                _allBuildings.Add(this);
        }

        public virtual void Collapse()
        {
            _allBuildings.Remove(this);

            if (WaterGrid != null)
                WaterGrid.UnregisterObstacle(transform.position, _registeredRadius);

            Items.EnemyPathfinder.InvalidateCache();

            Destroy(gameObject);
        }

        protected void PlaySpawnAnimation(float duration = 0.5f)
        {
            StartCoroutine(SpawnRoutine(duration));
        }

        private IEnumerator SpawnRoutine(float duration)
        {
            transform.localScale = Vector3.zero;
            float timer = 0;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float s = Mathf.SmoothStep(0f, 1f, timer / duration);
                transform.localScale = new Vector3(s, s, 1);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }

        public void Repair(float amount)
        {
            if (Model == null || amount <= 0f) return;

            float before = Model.CurrentHP;
            Model.CurrentHP = Mathf.Min(Model.MaxHP, Model.CurrentHP + amount);

            // Спалах лише коли HP реально зросло: авторемонт цокає щосекунди і
            // б'є в тому числі по цілих будівлях, а блимати ними не треба.
            if (Model.CurrentHP > before)
                PlayRepairFlash();
        }

        /// <summary>Зелений спалах спрайтів. Компонент необов'язковий.</summary>
        protected void PlayRepairFlash()
        {
            if (!_repairFlashCached)
            {
                _repairFlash = GetComponent<RepairFlash>();
                _repairFlashCached = true;
            }

            if (_repairFlash != null)
                _repairFlash.Play();
        }

        // Кешуємо і сам компонент, і факт пошуку: без прапорця GetComponent
        // викликався б щоразу на будівлях, де RepairFlash не висить.
        private RepairFlash _repairFlash;
        private bool _repairFlashCached;

        protected virtual void OnDestroy()
        {
            // Не тільки для Collapse: будівля може зникнути разом зі сценою або
            // через Destroy ззовні, а мертвий запис у статичному списку лишиться.
            _allBuildings.Remove(this);

            if (WaterGrid != null)
                WaterGrid.OnGridRebuilt -= OnGridRebuilt;
        }
    }
}