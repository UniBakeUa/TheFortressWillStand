using UnityEngine;

namespace Towers.ScriptableObjects
{
    [CreateAssetMenu(fileName = "UpgradeLibrary", menuName = "Building/Upgrade Library")]
    public class UpgradeLibrary : ProductLibrary<UpgradeConfig>
    {
    }
}