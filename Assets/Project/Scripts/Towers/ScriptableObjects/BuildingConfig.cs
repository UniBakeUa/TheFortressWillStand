using UnityEngine;

namespace Towers.ScriptableObjects
{
    public class BuildingConfig : ProductConfig
    {
        [Header("������ ����������")]
        [field: SerializeField] public int BaseHP { get; private set; }
        [field: SerializeField] public float BaseErosionRate { get; private set; }
        [field: SerializeField] public GameObject Prefab { get; private set; }
        [field: SerializeField] public GameObject GhostPrefab { get; private set; }
    }
}