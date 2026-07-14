using Towers.Data;
using UnityEngine;

namespace Towers.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewMinerData", menuName = "Building/Drill Config")]
    public class DrillConfig : BuildingConfig, IMinerData
    {
        [field: Header("Інформація видобутку")]
        [field: SerializeField] public int IncomeAmount { get; private set; }
        [field: SerializeField] public float IncomeInterval { get; private set; }
    }
}
