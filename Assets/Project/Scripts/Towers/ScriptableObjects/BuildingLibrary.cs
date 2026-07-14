using System.Collections.Generic;
using UnityEngine;

namespace Towers.ScriptableObjects
{
    [CreateAssetMenu(fileName = "BuildingLibrary", menuName = "Building/Building Library")]
    public class BuildingLibrary : ScriptableObject
    {
        [Tooltip("Додай сюди всі конфіги будівель (DrillConfig, TowerConfig, WallConfig тощо)")]
        [SerializeField] private List<BuildingConfig> _configs = new List<BuildingConfig>();

        private Dictionary<int, BuildingConfig> _configDictionary;

        /// <summary>
        /// Повертає конфіг будівлі за її ID.
        /// </summary>
        public BuildingConfig GetConfigById(int id)
        {
            // Ініціалізуємо словник при першому зверненні або якщо в редакторі додали нові елементи
            if (_configDictionary == null || _configDictionary.Count != _configs.Count)
            {
                InitializeDictionary();
            }

            if (_configDictionary.TryGetValue(id, out BuildingConfig config))
            {
                return config;
            }

            Debug.LogError($"[BuildingLibrary] Конфіг з ID {id} не знайдено! Перевір, чи додано його в BuildingLibrary.");
            return null;
        }

        private void InitializeDictionary()
        {
            _configDictionary = new Dictionary<int, BuildingConfig>();

            foreach (var config in _configs)
            {
                if (config == null) continue;

                // Захист від випадкових дублікатів ID в інспекторі
                if (!_configDictionary.ContainsKey(config.Id))
                {
                    _configDictionary.Add(config.Id, config);
                }
                else
                {
                    Debug.LogWarning($"[BuildingLibrary] Знайдено дублікат ID: {config.Id} у конфігах '{config.StructureName}' та '{_configDictionary[config.Id].StructureName}'. ID мають бути унікальними!");
                }
            }
        }
    }
}
