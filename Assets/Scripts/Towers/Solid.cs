using UnityEngine;
using Waves;

namespace Towers
{
    [RequireComponent(typeof(Collider2D))]
    public class Solid : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHP = 100f;
        [SerializeField] protected float baseErosionRate = 1f;
        [SerializeField] protected float damageReductionPerWall = 0.2f;
        public int StructureId;
        public float CurrentHP { get; set; }
        public float MaxHP => maxHP;
        public float ExposureFraction { get; private set; }

        protected Collider2D Collider { get; private set; }
        protected WaterGrid WaterGrid { get; private set; }

        // радіус, яким об'єкт реально зареєструвався в сітці - треба той самий при знятті реєстрації
        float _registeredRadius;

        // Tower готовий одразу після Awake; Wall стає готовим лише після явного Init() -
        // так FixedUpdate/RegisterFootprint ніколи не звернуться до незаповнених nodeA/nodeB
        protected bool IsReady { get; set; }

        // дає нащадкам (Wall, майбутні нові типи веж) підмінити формулу erosion
        // без дублювання FixedUpdate - саме тут "легко додати нову вежу":
        // достатньо перевизначити ErosionRate у нащадку зі своєю логікою
        protected virtual float ErosionRate => baseErosionRate;

        protected virtual void Awake()
        {
            Collider = GetComponent<Collider2D>();
            CurrentHP = maxHP;
            // резолвимо тут, а не в Start() - Awake гарантовано відпрацьовує для ВСІХ обʼєктів
            // сцени раніше за Start() будь-кого, тому Init(), викликаний з чужого Start(), завжди безпечний
            WaterGrid = FindFirstObjectByType<WaterGrid>();
        }

        protected virtual void Start()
        {
            IsReady = true;
            RegisterFootprint();
            // ФІКС: раніше тут ще додатково викликався WaterGrid.RegisterObstacle(transform.position, 0.5f)
            // з хардкодженим радіусом 0.5 - подвійна реєстрація тієї самої перешкоди різними радіусами.
            // RegisterFootprint() вже робить це коректно на основі реального Collider.bounds.
        }

        protected virtual void RegisterFootprint()
        {
            var bounds = Collider.bounds;
            _registeredRadius = Mathf.Max(bounds.extents.x, bounds.extents.y);
            WaterGrid.RegisterObstacle(bounds.center, _registeredRadius);
        }

        void FixedUpdate()
        {
            if (!IsReady || CurrentHP <= 0f) return;

            var bounds = Collider.bounds;

            // Семплимо зону навколо, щоб "бачити" прихід води (не всередині власного тіла -
            // там вода завжди 0, бо це Solid-клітинки)
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

        public void TakeDamage(float amount)
        {
            if (CurrentHP <= 0f) return;
            CurrentHP -= amount;
            if (CurrentHP <= 0f) Collapse();
        }

        public void Repair(float amount) => CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);

        public virtual void Collapse()
        {
            WaterGrid.UnregisterObstacle(transform.position, _registeredRadius);
            // партикли/звук у нащадка через override
            Destroy(gameObject);
        }
    }
}