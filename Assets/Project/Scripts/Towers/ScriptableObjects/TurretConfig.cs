using Towers.Data;
using Towers.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "GroundTurret", menuName = "Building/Ground Turret Config")]
public class TurretConfig : BuildingConfig, IAttackRangeConfig
{
    public int Damege;
    public float CoolDown;
    [field: SerializeField] public float AttackRange { get; private set; }

    [Header("Splash")]
    [Tooltip("Радіус ураження навколо цілі. Усі вороги в радіусі гинуть миттєво (як MouseBomber).")]
    public float SplashRadius = 1f;

    [Header("Visuals")]
    public float RotationSpeed;
}
