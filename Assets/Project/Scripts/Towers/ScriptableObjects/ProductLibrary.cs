using System.Collections.Generic;
using UnityEngine;

namespace Towers.ScriptableObjects
{
    public abstract class ProductLibrary<T> : ScriptableObject where T : ProductConfig
    {
        [Tooltip("Додай сюди всі конфіги будівель (DrillConfig, TowerConfig, WallConfig тощо)")]
        [SerializeField] private List<T> _configs = new List<T>();

        private Dictionary<int, T> _configDictionary;

        /// <summary>
        /// Повертає конфіг будівлі за її ID.
        /// </summary>
        public ProductConfig GetConfigById(int id)
        {
            // Ініціалізуємо словник при першому зверненні або якщо в редакторі додали нові елементи
            if (_configDictionary == null || _configDictionary.Count != _configs.Count)
            {
                InitializeDictionary();
            }

            if (_configDictionary.TryGetValue(id, out T config))
            {
                return config;
            }

            Debug.LogError($"[BuildingLibrary] Конфіг з ID {id} не знайдено! Перевір, чи додано його в BuildingLibrary.");
            return null;
        }

        private void InitializeDictionary()
        {
            _configDictionary = new Dictionary<int, T>();

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
