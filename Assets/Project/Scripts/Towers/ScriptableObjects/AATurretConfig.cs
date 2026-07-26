using Towers.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "AATurret", menuName = "Building/AA Turret Config")]
public class AATurretConfig : BuildingConfig, IAttackRangeConfig
{
    public int Damege;
    public float CoolDown;
    [field: SerializeField] public float AttackRange { get; private set; }

    [Header("Visuals")]
    public float RotationSpeed;
}
