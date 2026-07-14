using Towers.Data;
using Towers.ScriptableObjects;

namespace Towers.Models
{
    public class WallModel : BuildingModel, IWallData
    {
        public float WallHalfWidth { get; private set; }

        public WallModel(WallConfig buildingConfig) : base(buildingConfig)
        {
            WallHalfWidth = buildingConfig.WallHalfWidth;
        }
    }
}
