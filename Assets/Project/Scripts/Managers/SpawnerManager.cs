using Items;
using Items.Data;
using Items.ScriptableObjects;
using Items.Spawners;
using System.Collections.Generic;
using UnityEngine;
using Waves;

namespace Managers
{
    public class SpawnerManager : MonoBehaviour
    {
        [Header("Налаштування")]
        [SerializeField] private SpawnConfig _spawnConfig;
        [SerializeField] private WaveManager _waveManager;

        [Header("Компоненти для спавну")]
        [SerializeField] private Transform _itemContainer;
        [SerializeField] private WaterGrid _waterGrid;
        [SerializeField] private Collider2D _shellSpawnZone;
        [SerializeField] private Collider2D _jellyfishSpawnZone;

        private Dictionary<SpawnableItemType, ISpawner> _spawners = new Dictionary<SpawnableItemType, ISpawner>();

        private void Awake()
        {
            _spawners.Add(SpawnableItemType.Airplane, new AirplaneSpawner(_spawnConfig.GetPrefab<Airplane>(SpawnableItemType.Airplane), _itemContainer));
            _spawners.Add(SpawnableItemType.PONCHIC, new ScreenBottomSpawner(_spawnConfig.GetPrefab<PONCHIC>(SpawnableItemType.PONCHIC), _itemContainer));
            _spawners.Add(SpawnableItemType.Shell, new WaterShellSpawner(_spawnConfig.GetPrefab<Shell>(SpawnableItemType.Shell), _itemContainer, _waterGrid, _shellSpawnZone));
            _spawners.Add(SpawnableItemType.Cheliks, new CheliksSpawner(_spawnConfig.GetPrefab<Cheliks>(SpawnableItemType.Cheliks), _itemContainer, _waterGrid, _shellSpawnZone));
            _spawners.Add(SpawnableItemType.Jellyfish, new JellyfishSpawner(_spawnConfig.GetPrefab<Jellyfish>(SpawnableItemType.Jellyfish), _itemContainer, _waterGrid, _jellyfishSpawnZone));
        }

        private void OnValidate()
        {
            var prefab = _spawnConfig.GetPrefab<Cheliks>(SpawnableItemType.Cheliks);

            if(prefab == null)
            {
                Debug.LogError("ПРИВІТ! Бачу ти побачив це повідомлення (чи то в консолі чи то в коді). Це префаб належить Челіксу. Нумо зробимо так, щоб він залишився до кінця розробки. З любов'ю, Меррфіс =) /n P.S. Якщо його видалять, то я через гіт дізнаюсь хто це зробив, й тоді до цієї людини прийде у ночі СТЕПАН З НОЖЕМ!!!");
            }
        }

        private void Update()
        {
            if (GameStateManager.Instance.CurrentState != GameState.Playing) return;

            foreach (var spawnerData in _spawners)
            {
                if (spawnerData.Value == null) continue;

                ItemSpawnData itemConfig = _spawnConfig.GetSpawnData(spawnerData.Key);

                if (itemConfig != null)
                {
                    if (_waveManager.CurrentLevel >= itemConfig.StartingWave)
                    {
                        spawnerData.Value.SpawnTimer(itemConfig);
                    }
                }
            }
        }
    }
}