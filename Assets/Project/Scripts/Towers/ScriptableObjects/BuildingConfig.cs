using UnityEngine;

namespace Towers.ScriptableObjects
{
    public class BuildingConfig : ScriptableObject
    {
        [Header("������ ����������")]
        [field: SerializeField] public string StructureName { get; private set; }
        [field: SerializeField] public int Id { get; private set; }
        [field: SerializeField] public int BaseCost { get; private set; }
        [field: SerializeField] public int BaseHP { get; private set; }
        [field: SerializeField] public float BaseErosionRate { get; private set; }
        [field: SerializeField] public GameObject Prefab { get; private set; }
        [field: SerializeField] public GameObject GhostPrefab { get; private set; }
    }
}