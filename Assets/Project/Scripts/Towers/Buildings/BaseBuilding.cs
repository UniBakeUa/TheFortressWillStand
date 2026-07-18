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
        [Header("Базові налаштування")]
        [SerializeField] private bool _isSlipable;

        [Header("Child Objects")]
        [SerializeField] protected BuildingView _buildingView;

        public bool IsSlipable => _isSlipable;
        public BuildingModel Model { get; protected set; }
        public float ExposureFraction { get; private set; }
        public bool IsReady { get; protected set; }
        protected virtual float ErosionRate => Model.BaseErosionRate;

        protected Collider2D Collider { get; private set; }
        protected WaterGrid WaterGrid { get; private set; }
        private float _registeredRadius;

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
            RegisterFootprint();
            PlaySpawnAnimation();
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

        public virtual void Collapse()
        {
            if (WaterGrid != null)
                WaterGrid.UnregisterObstacle(transform.position, _registeredRadius);

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
             Model.CurrentHP = Mathf.Min(Model.MaxHP, Model.CurrentHP + amount);
        }

        protected virtual void OnDestroy()
        {
            if (WaterGrid != null)
                WaterGrid.OnGridRebuilt -= OnGridRebuilt;
        }
    }
}