using Items;
using Managers;
using Managers.Audio;
using System.Collections.Generic;
using Towers.Models;
using Towers.ScriptableObjects;
using UnityEngine;

namespace Towers.Buildings
{
    public class AATurret : TurretBase
    {
        public AATurretModel TurretModel;

        private List<Airplane> _activeAirplanes = new();
        private Airplane _currentTarget;

        protected override float CoolDown => TurretModel.CoolDown;
        protected override float AttackRange => TurretModel.AttackRange;
        protected override float RotationSpeed => TurretModel.RotationSpeed;

        public override void Initialize(BuildingConfig config)
        {
            TurretModel = new AATurretModel(config as AATurretConfig);
            base.Initialize(config);

            _activeAirplanes = BuildManager.Instance.SpawnerManager.ActiveAirplanes;
        }

        protected override void FindTarget()
        {
            float closestDistance = float.MaxValue;
            Vector2 myPos = transform.position;
            Airplane closest = null;

            foreach (var plane in _activeAirplanes)
            {
                if (plane == null || !plane.isActiveAndEnabled) continue;

                Vector2 planePos = plane.transform.position;
                float sqrDst = (planePos - myPos).sqrMagnitude;

                if (sqrDst < closestDistance)
                {
                    closestDistance = sqrDst;
                    closest = plane;
                }
            }

            float sqrAttackRange = TurretModel.AttackRange * TurretModel.AttackRange;
            if (closest != null && closestDistance > sqrAttackRange)
            {
                closest = null;
            }

            _currentTarget = closest;
            HasTarget = _currentTarget != null;
        }

        protected override Vector3 GetTargetPosition() => _currentTarget.transform.position;

        protected override void Shoot()
        {
            Airplane target = _currentTarget;
            FireRaycast(target.gameObject, () =>
            {
                target.TakeHit(TurretModel.Damage);
                SoundManager.Instance.Play(SoundId.AATurretHit, target.transform.position);
            });

            _currentTarget = null;
            HasTarget = false;
            StartPostShotFreeze();
        }
    }
}
