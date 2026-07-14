using Towers.ScriptableObjects;

namespace Towers.Models
{
    public class DrillModel : BuildingModel
    {
        public int IncomeAmount { get; private set; }
        public float IncomeInterval { get; private set; }

        public DrillModel(DrillConfig buildingConfig) : base(buildingConfig)
        {
            IncomeAmount = buildingConfig.IncomeAmount;
            IncomeInterval = buildingConfig.IncomeInterval;
        }
    }
}
