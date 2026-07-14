using Towers.Data;
using Towers.ScriptableObjects;

namespace Towers.Models
{
    public class TowerModel : BuildingModel, ITowerData
    {
        public float DamageReductionPerWall { get; private set; }

        public WallConfig WallConfig { get; private set; }

        public TowerModel(TowerConfig buildingConfig) : base(buildingConfig)
        {
            DamageReductionPerWall = buildingConfig.DamageReductionPerWall;
            WallConfig = buildingConfig.WallConfig;
        }
    }
}
