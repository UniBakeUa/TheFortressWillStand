using Towers.Data;
using Towers.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "Turret", menuName = "Building/Turret Config")]
public class TurretConfig : BuildingConfig
{ 
    public int Damege;
    public float CoolDown;
    public float AttackRange;

    [Header("Visuals")]
    public float RotationSpeed;
}
