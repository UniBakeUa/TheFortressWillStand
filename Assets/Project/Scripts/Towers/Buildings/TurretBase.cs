using System.Collections;
using Towers.ScriptableObjects;
using Towers.UI;
using UnityEngine;

namespace Towers.Buildings
{
    public abstract class TurretBase : BaseBuilding
    {
        protected TurretView TurretView;

        protected float Timer;
        protected Vector3 LastTarget;
        protected bool HasTarget;

        [SerializeField] protected Transform _turretVisual;
        [SerializeField] protected Transform _muzzle;
        [SerializeField] protected BulletTrail _bulletTrail;
        [SerializeField] protected TimedVisualEffect _muzzleFlash;
        [SerializeField] protected Explosion _explosionPrefab;
        [SerializeField] protected LayerMask _targetLayerMask;
        [SerializeField] protected float _recoilOffset = 0.15f;
        [SerializeField] protected float _recoilDuration = 0.08f;
        [SerializeField] protected float _aimBeforeShootDuration = 0.3f;
        [SerializeField] protected float _postShotFreezeDuration = 0.5f;
        [SerializeField] protected float _aimingRotationSpeedMultiplier = 3f;
        [SerializeField] protected RangeCircle _rangeCircle;
        [SerializeField] protected Color _hoverRangeColor = Color.green;

        protected const float AimToleranceDegrees = 5f;

        protected enum TurretState
        {
            Tracking,
            Aiming,
            PostShotFreeze
        }

        protected TurretState State = TurretState.Tracking;
        private Coroutine _recoilRoutine;
        protected float AimTimer;
        protected float PostShotTimer;
        private Vector3 _lastRecoilDirection = Vector3.up;

        protected abstract float CoolDown { get; }
        protected abstract float AttackRange { get; }
        protected abstract float RotationSpeed { get; }

        public override void Initialize(BuildingConfig config)
        {
            base.Initialize(config);
            TurretView = _buildingView as TurretView;
            if (TurretView != null)
            {
                TurretView.SetupTimer(CoolDown);
            }
        }

        protected virtual void Update()
        {
            if (!IsReady) return;

            if (State == TurretState.PostShotFreeze)
            {
                PostShotTimer += Time.deltaTime;
                if (PostShotTimer >= _postShotFreezeDuration)
                {
                    State = TurretState.Tracking;
                }
                return;
            }

            FindTarget();

            if (HasTarget)
            {
                LastTarget = GetTargetPosition();
            }

            Timer += Time.deltaTime;

            if (State == TurretState.Tracking)
            {
                if (Timer >= CoolDown)
                {
                    State = TurretState.Aiming;
                    AimTimer = 0f;
                }
                else if (TurretView != null)
                {
                    TurretView.UpdateMoneyTimer(Timer);
                }
                return;
            }

            // State == TurretState.Aiming
            if (IsAimedAtTarget())
            {
                AimTimer += Time.deltaTime;
            }
            else
            {
                AimTimer = 0f;
            }

            if (AimTimer >= _aimBeforeShootDuration && HasTarget)
            {
                Shoot();
            }
        }

        protected abstract void FindTarget();
        protected abstract Vector3 GetTargetPosition();
        protected abstract void Shoot();

        private bool IsAimedAtTarget()
        {
            if (!HasTarget) return false;

            Vector3 direction = LastTarget - _turretVisual.position;
            if (direction.sqrMagnitude < 0.001f) return true;

            float targetAngle = (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) - 90f;
            float currentAngle = _turretVisual.eulerAngles.z;

            return Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle)) <= AimToleranceDegrees;
        }

        protected void StartPostShotFreeze()
        {
            Timer = 0f;
            AimTimer = 0f;
            PostShotTimer = 0f;
            State = TurretState.PostShotFreeze;
        }

        protected void FireRaycast(GameObject expectedTarget, System.Action onHit)
        {
            Vector3 origin = _muzzle != null ? _muzzle.position : transform.position;
            Vector3 direction = (LastTarget - origin);
            float distance = Mathf.Max(direction.magnitude, AttackRange);
            direction.Normalize();
            _lastRecoilDirection = direction;

            Vector3 endPoint = origin + direction * distance;

            RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, _targetLayerMask);
            if (hit.collider != null)
            {
                endPoint = hit.point;
                if (hit.collider.gameObject == expectedTarget)
                {
                    onHit?.Invoke();
                }
            }

            if (_explosionPrefab != null)
            {
                Instantiate(_explosionPrefab, endPoint, Quaternion.identity);
            }

            if (_bulletTrail != null)
            {
                _bulletTrail.Show(origin, endPoint);
            }

            if (_muzzleFlash != null)
            {
                _muzzleFlash.Show();
            }

            if (_recoilRoutine != null)
            {
                StopCoroutine(_recoilRoutine);
            }
            _recoilRoutine = StartCoroutine(RecoilRoutine());
        }

        private IEnumerator RecoilRoutine()
        {
            Vector3 startWorldPosition = _turretVisual.position;
            Vector3 recoiledWorldPosition = startWorldPosition - _lastRecoilDirection * _recoilOffset;

            float halfDuration = _recoilDuration * 0.5f;

            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                _turretVisual.position = Vector3.Lerp(startWorldPosition, recoiledWorldPosition, elapsed / halfDuration);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                _turretVisual.position = Vector3.Lerp(recoiledWorldPosition, startWorldPosition, elapsed / halfDuration);
                yield return null;
            }

            _turretVisual.position = startWorldPosition;
            _recoilRoutine = null;
        }

        protected virtual void LateUpdate()
        {
            RotateTurret();
        }

        private void RotateTurret()
        {
            if (State == TurretState.PostShotFreeze) return;
            if (!HasTarget || LastTarget == Vector3.zero)
                return;

            Vector3 direction = LastTarget - _turretVisual.position;
            if (direction.sqrMagnitude < 0.001f) return;

            float angle = (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) - 90f;
            Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);

            float speedMultiplier = State == TurretState.Aiming ? _aimingRotationSpeedMultiplier : 1f;

            _turretVisual.rotation = Quaternion.RotateTowards(
                _turretVisual.rotation,
                targetRotation,
                RotationSpeed * speedMultiplier * Time.deltaTime * 100
            );
        }

        private int _rangeVisibilityCount;

        public void ShowRange(Color color)
        {
            _rangeVisibilityCount++;
            RefreshRangeVisual(color);
        }

        public void HideRange()
        {
            _rangeVisibilityCount = Mathf.Max(0, _rangeVisibilityCount - 1);
            RefreshRangeVisual(_hoverRangeColor);
        }

        private void RefreshRangeVisual(Color color)
        {
            if (_rangeCircle == null) return;

            if (_rangeVisibilityCount > 0)
            {
                _rangeCircle.Show(AttackRange, color);
            }
            else
            {
                _rangeCircle.Hide();
            }
        }
    }
}
