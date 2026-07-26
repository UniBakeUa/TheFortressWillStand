using Items;
using Managers;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Towers.Buildings;
using Towers.Models;
using Towers.ScriptableObjects;
using Towers.UI;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;


public class Turret : BaseBuilding
{
    public TurretModel TurretModel;
    private TurretView _turretView;

    private float _timer;
    private List<Airplane> _activeAirplanes = new();
    private List<Enemy> _activeEnemies = new();

    private Airplane _currentTarget;
    private Enemy _currentTargetEnemy;
    private Vector3 _lastTarget;

    [SerializeField] private Transform _turretVisual;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private BulletTrail _bulletTrail;
    [SerializeField] private TimedVisualEffect _muzzleFlash;
    [SerializeField] private Explosion _explosionPrefab;
    [SerializeField] private LayerMask _targetLayerMask;
    [SerializeField] private float _recoilOffset = 0.15f;
    [SerializeField] private float _recoilDuration = 0.08f;
    [SerializeField] private float _aimBeforeShootDuration = 0.3f;
    [SerializeField] private float _postShotFreezeDuration = 0.5f;
    [SerializeField] private float _aimingRotationSpeedMultiplier = 3f;

    private const float AimToleranceDegrees = 5f;

    private enum TurretState
    {
        Tracking,
        Aiming,
        PostShotFreeze
    }

    private TurretState _state = TurretState.Tracking;
    private Coroutine _recoilRoutine;
    private float _aimTimer;
    private float _postShotTimer;
    private Vector3 _lastRecoilDirection = Vector3.up;

    public override void Initialize(BuildingConfig config)
    {
        base.Initialize(config);
        TurretModel = new TurretModel(config as TurretConfig);

        _activeAirplanes = BuildManager.Instance.SpawnerManager.ActiveAirplanes;
        _activeEnemies = BuildManager.Instance.SpawnerManager.ActiveEnemies;
        _turretView = _buildingView as TurretView;
        if (_turretView != null)
        {
            _turretView.SetupTimer(TurretModel.CoolDown);
        }
    }

    private void Update()
    {
        if (!IsReady) return;

        if (_state == TurretState.PostShotFreeze)
        {
            _postShotTimer += Time.deltaTime;
            if (_postShotTimer >= _postShotFreezeDuration)
            {
                _state = TurretState.Tracking;
            }
            return;
        }

        FindClosestTarget();

        if (_currentTarget != null)
        {
            _lastTarget = _currentTarget.transform.position;
        }
        else if (_currentTargetEnemy != null)
        {
            _lastTarget = _currentTargetEnemy.transform.position;
        }

        _timer += Time.deltaTime;

        if (_state == TurretState.Tracking)
        {
            if (_timer >= TurretModel.CoolDown)
            {
                _state = TurretState.Aiming;
                _aimTimer = 0f;
            }
            else if (_turretView != null)
            {
                _turretView.UpdateMoneyTimer(_timer);
            }
            return;
        }

        // _state == TurretState.Aiming
        if (IsAimedAtTarget())
        {
            _aimTimer += Time.deltaTime;
        }
        else
        {
            _aimTimer = 0f;
        }

        if (_aimTimer >= _aimBeforeShootDuration)
        {
            if (_currentTarget != null)
            {
                ShootPlane();
            }
            else if (_currentTargetEnemy != null)
            {
                ShootEnemy();
            }
        }
    }

    private bool IsAimedAtTarget()
    {
        if (_currentTarget == null && _currentTargetEnemy == null) return false;

        Vector3 direction = _lastTarget - _turretVisual.position;
        if (direction.sqrMagnitude < 0.001f) return true;

        float targetAngle = (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) - 90f;
        float currentAngle = _turretVisual.eulerAngles.z;

        return Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle)) <= AimToleranceDegrees;
    }

    private void FindClosestTarget()
    {
        float closestDistance = float.MaxValue;
        Vector2 myPos = transform.position;
        foreach (var enemy in _activeEnemies)
        {
            if (enemy == null || !enemy.isActiveAndEnabled) continue;

            Vector2 enemyPos = enemy.transform.position;
            float sqrDst = (enemyPos - myPos).sqrMagnitude;

            if (sqrDst < closestDistance)
            {
                closestDistance = sqrDst;
                _currentTargetEnemy = enemy;
            }
        }

        foreach (var plane in _activeAirplanes)
        {
            if (plane == null || !plane.isActiveAndEnabled) continue;

            Vector2 planePos = plane.transform.position;
            float sqrDst = (planePos - myPos).sqrMagnitude;

            if (sqrDst < closestDistance)
            {
                closestDistance = sqrDst;
                _currentTarget = plane;
            }
        }


        float sqrAttackRange = TurretModel.AttackRange * TurretModel.AttackRange;
        if (_currentTargetEnemy != null && closestDistance > sqrAttackRange)
        {
            _currentTargetEnemy = null;
        }
        if (_currentTarget != null && closestDistance > sqrAttackRange)
        {
            _currentTarget = null;
        }
    }

    private void ShootEnemy()
    {
        FireRaycast(_currentTargetEnemy.gameObject, () => _currentTargetEnemy.WasStricken());

        _currentTargetEnemy = null;
        StartPostShotFreeze();
    }
    private void ShootPlane()
    {
        FireRaycast(_currentTarget.gameObject, () => _currentTarget.WasStricken());

        _currentTarget = null;
        StartPostShotFreeze();
    }

    private void StartPostShotFreeze()
    {
        _timer = 0f;
        _aimTimer = 0f;
        _postShotTimer = 0f;
        _state = TurretState.PostShotFreeze;
    }

    private void FireRaycast(GameObject expectedTarget, System.Action onHit)
    {
        Vector3 origin = _muzzle != null ? _muzzle.position : transform.position;
        Vector3 direction = (_lastTarget - origin);
        float distance = Mathf.Max(direction.magnitude, TurretModel.AttackRange);
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
        Vector3 startLocalPosition = _turretVisual.localPosition;
        Vector3 recoilDirection = _turretVisual.InverseTransformDirection(-_lastRecoilDirection).normalized;
        Vector3 recoiledLocalPosition = startLocalPosition + recoilDirection * _recoilOffset;

        float halfDuration = _recoilDuration * 0.5f;

        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            _turretVisual.localPosition = Vector3.Lerp(startLocalPosition, recoiledLocalPosition, elapsed / halfDuration);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            _turretVisual.localPosition = Vector3.Lerp(recoiledLocalPosition, startLocalPosition, elapsed / halfDuration);
            yield return null;
        }

        _turretVisual.localPosition = startLocalPosition;
        _recoilRoutine = null;
    }

    private void LateUpdate()
    {
        RotateTurret();
    }

    private void RotateTurret()
    {
        if (_state == TurretState.PostShotFreeze) return;
        if (_lastTarget == null || _lastTarget == Vector3.zero)
            return;

        Vector3 direction = _lastTarget - _turretVisual.position;
        if (direction.sqrMagnitude < 0.001f) return;

        float angle = (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) - 90f;
        Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);

        float speedMultiplier = _state == TurretState.Aiming ? _aimingRotationSpeedMultiplier : 1f;

        _turretVisual.rotation = Quaternion.RotateTowards(
            _turretVisual.rotation,
            targetRotation,
            TurretModel.RotationSpeed * speedMultiplier * Time.deltaTime * 100
        );

    }

}
