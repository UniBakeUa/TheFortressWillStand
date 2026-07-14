using Towers.Data;
using UnityEngine;

namespace Towers.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewWallData", menuName = "Building/Wall Config")]
    public class WallConfig : BuildingConfig, IWallData
    {
        [field: Header("Інформація стіни")]
        [field: SerializeField] public float WallHalfWidth { get; private set; }
    }
}
