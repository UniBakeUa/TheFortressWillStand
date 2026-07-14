using Towers.Data;
using UnityEngine;

namespace Towers.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewTowerData", menuName = "Building/Tower Config")]
    public class TowerConfig : BuildingConfig, ITowerData
    {
        [field: Header("Інформація вежі")]
        [field: SerializeField] public float DamageReductionPerWall { get; private set; }
        [field: SerializeField] public WallConfig WallConfig { get; private set; }
    }
}
