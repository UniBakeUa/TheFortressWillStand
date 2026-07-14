using System;
using UnityEngine;

namespace Items.Data
{
    [Serializable]
    public class ItemSpawnData
    {
        [field: Tooltip("Який саме предмет спавнити")]
        [field: SerializeField] public SpawnableItemType ItemType { get; private set; }

        [field: Tooltip("З якої хвилі цей предмет починає з'являтися")]
        [field: SerializeField] public int StartingWave { get; private set; }

        [field: Tooltip("Шанс спавну (від 0 до 100)")]
        [field: Range(0f, 100f)]
        [field: SerializeField] public float SpawnChance { get; private set; }

        [field: Tooltip("Частота спавну (в секундах)")]
        [field: SerializeField] public float SpawnInterval { get; private set; }

        [field: Tooltip("Префаб предмету")]
        [field: SerializeField] public ClickableItem Prefab { get; private set; }
    }
}
