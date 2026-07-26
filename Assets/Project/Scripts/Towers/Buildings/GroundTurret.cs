using Items;
using Managers;
using System.Collections.Generic;
using Towers.Models;
using Towers.ScriptableObjects;
using UnityEngine;

namespace Towers.Buildings
{
    public class GroundTurret : TurretBase
    {
        public TurretModel TurretModel;

        private List<Enemy> _activeEnemies = new();
        private Enemy _currentTargetEnemy;

        protected override float CoolDown => TurretModel.CoolDown;
        protected override float AttackRange => TurretModel.AttackRange;
        protected override float RotationSpeed => TurretModel.RotationSpeed;

        public override void Initialize(BuildingConfig config)
        {
            TurretModel = new TurretModel(config as TurretConfig);
            base.Initialize(config);

            _activeEnemies = BuildManager.Instance.SpawnerManager.ActiveEnemies;
        }

        protected override void FindTarget()
        {
            float closestDistance = float.MaxValue;
            Vector2 myPos = transform.position;
            Enemy closest = null;

            foreach (var enemy in _activeEnemies)
            {
                if (enemy == null || !enemy.isActiveAndEnabled) continue;

                Vector2 enemyPos = enemy.transform.position;
                float sqrDst = (enemyPos - myPos).sqrMagnitude;

                if (sqrDst < closestDistance)
                {
                    closestDistance = sqrDst;
                    closest = enemy;
                }
            }

            float sqrAttackRange = TurretModel.AttackRange * TurretModel.AttackRange;
            if (closest != null && closestDistance > sqrAttackRange)
            {
                closest = null;
            }

            _currentTargetEnemy = closest;
            HasTarget = _currentTargetEnemy != null;
        }

        protected override Vector3 GetTargetPosition() => _currentTargetEnemy.transform.position;

        protected override void Shoot()
        {
            GameObject expectedTarget = _currentTargetEnemy.gameObject;
            Vector3 impactPosition = _currentTargetEnemy.transform.position;

            FireRaycast(expectedTarget, () => SplashDamage(impactPosition));

            _currentTargetEnemy = null;
            HasTarget = false;
            StartPostShotFreeze();
        }

        private void SplashDamage(Vector3 center)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, TurretModel.SplashRadius, _targetLayerMask);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out Enemy enemy))
                {
                    enemy.WasStricken();
                }
            }
        }
    }
}
