using Towers.ScriptableObjects;
using UnityEngine;

namespace Towers.Buildings
{
    public interface ITower
    {
        public TowerNode Node { get; }
        public Transform Transform { get; }
        public WallConfig WallConfig { get; }

        public void EnsureNodeInitialized();
        public void AddWall(IWall wall);
        public void RemoveWall(IWall wall);
    }
}
