using UnityEngine;

namespace Towers.ScriptableObjects
{
    [CreateAssetMenu(fileName = "BuildingLibrary", menuName = "Building/Building Library")]
    public class BuildingLibrary : ProductLibrary<BuildingConfig>
    {
    }
}
