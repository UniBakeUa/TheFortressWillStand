using Towers.ScriptableObjects;

namespace Towers.Data
{
    public interface ITowerData
    {
        public float DamageReductionPerWall { get; }
        public WallConfig WallConfig { get; }
    }
}
