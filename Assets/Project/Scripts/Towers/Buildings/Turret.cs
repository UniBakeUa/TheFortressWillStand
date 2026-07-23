using Items;
using Managers;
using NUnit.Framework;
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

        FindClosestTarget();
        _timer += Time.deltaTime;
        if (_timer >= TurretModel.CoolDown)
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
        else if (_turretView != null)
        {
            _turretView.UpdateMoneyTimer(_timer);
        }
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
        _lastTarget = _currentTargetEnemy.transform.position;
        _currentTargetEnemy.WasStricken();

        _currentTargetEnemy = null;
        _timer = 0f;
    }
    private void ShootPlane()
    {
        _lastTarget = _currentTarget.transform.position;
        _currentTarget.WasStricken();

        _currentTarget = null;
        _timer = 0f;
    }

    private void LateUpdate()
    {
        RotateTurret();
    }

    private void RotateTurret()
    {
        if (_lastTarget == null || _lastTarget == Vector3.zero)
            return;

        Vector3 direction = _lastTarget - _turretVisual.position;
        if (direction.sqrMagnitude < 0.001f) return;

        float angle = (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) - 90f;
        Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);

        _turretVisual.rotation = Quaternion.RotateTowards(
            _turretVisual.rotation,
            targetRotation,
            TurretModel.RotationSpeed * Time.deltaTime * 100
        );

    }

}
