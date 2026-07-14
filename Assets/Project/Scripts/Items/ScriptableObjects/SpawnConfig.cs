using Items.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Items.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewSpawnConfig", menuName = "Spawning/Spawn Config")]
    public class SpawnConfig : ScriptableObject
    {
        [field: Header("Налаштування предметів")]
        [field: Tooltip("Список всіх предметів, що можуть заспавнитись, та їхні умови")]
        [field: SerializeField] public List<ItemSpawnData> SpawnableItems { get; private set; } = new List<ItemSpawnData>();

        public ItemSpawnData GetSpawnData(SpawnableItemType itemType)
        {
            return SpawnableItems.Find(x => x.ItemType == itemType);
        }

        public T GetPrefab<T>(SpawnableItemType itemType) where T : ClickableItem
        {
            ItemSpawnData spawnData = GetSpawnData(itemType);
            ClickableItem prefab = spawnData != null ? spawnData.Prefab : null;

            return prefab as T;
        }
    }
}
