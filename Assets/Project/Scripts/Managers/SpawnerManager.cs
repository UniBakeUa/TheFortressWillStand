using Items;
using Items.Data;
using Items.ScriptableObjects;
using Items.Spawners;
using System.Collections.Generic;
using Towers;
using UnityEngine;
using Waves;

namespace Managers
{
    public class SpawnerManager : MonoBehaviour
    {
        [Header("������������")]
        [SerializeField] private SpawnConfig _spawnConfig;
        [SerializeField] private WaveManager _waveManager;

        [Header("���������� ��� ������")]
        [SerializeField] private Transform _itemContainer;
        [SerializeField] private WaterGrid _waterGrid;
        [SerializeField] private Collider2D _shellSpawnZone;
        [SerializeField] private Collider2D _jellyfishSpawnZone;
        [SerializeField] private Fortress _fortress;

        private List<Airplane> _activeAirplanes = new();
        private List<Enemy> _activeEnemies = new();

        private Dictionary<SpawnableItemType, ISpawner> _spawners = new Dictionary<SpawnableItemType, ISpawner>();

        private void Awake()
        {
            _spawners.Add(SpawnableItemType.Airplane, new AirplaneSpawner(_spawnConfig.GetPrefab<Airplane>(SpawnableItemType.Airplane), _itemContainer, _fortress, _activeAirplanes));
            _spawners.Add(SpawnableItemType.Airplane1, new AirplaneSpawner(_spawnConfig.GetPrefab<Airplane>(SpawnableItemType.Airplane1), _itemContainer, _fortress, _activeAirplanes));
            _spawners.Add(SpawnableItemType.Airplane2, new AirplaneSpawner(_spawnConfig.GetPrefab<Airplane>(SpawnableItemType.Airplane2), _itemContainer, _fortress, _activeAirplanes));
            _spawners.Add(SpawnableItemType.Enemy, new EnemySpawner(_spawnConfig.GetPrefab<Enemy>(SpawnableItemType.Enemy), _itemContainer, _fortress, _activeEnemies));
            _spawners.Add(SpawnableItemType.Enemy1, new EnemySpawner(_spawnConfig.GetPrefab<Enemy>(SpawnableItemType.Enemy1), _itemContainer, _fortress, _activeEnemies));
            _spawners.Add(SpawnableItemType.Enemy2, new EnemySpawner(_spawnConfig.GetPrefab<Enemy>(SpawnableItemType.Enemy2), _itemContainer, _fortress, _activeEnemies));
            _spawners.Add(SpawnableItemType.Enemy3, new EnemySpawner(_spawnConfig.GetPrefab<Enemy>(SpawnableItemType.Enemy3), _itemContainer, _fortress, _activeEnemies));
            _spawners.Add(SpawnableItemType.Enemy4, new EnemySpawner(_spawnConfig.GetPrefab<Enemy>(SpawnableItemType.Enemy4), _itemContainer, _fortress, _activeEnemies));
            _spawners.Add(SpawnableItemType.Enemy5, new EnemySpawner(_spawnConfig.GetPrefab<Enemy>(SpawnableItemType.Enemy5), _itemContainer, _fortress, _activeEnemies));
            _spawners.Add(SpawnableItemType.PONCHIC, new ScreenBottomSpawner(_spawnConfig.GetPrefab<PONCHIC>(SpawnableItemType.PONCHIC), _itemContainer));
            _spawners.Add(SpawnableItemType.Shell, new WaterShellSpawner(_spawnConfig.GetPrefab<Shell>(SpawnableItemType.Shell), _itemContainer, _waterGrid, _shellSpawnZone));
            _spawners.Add(SpawnableItemType.Cheliks, new CheliksSpawner(_spawnConfig.GetPrefab<Cheliks>(SpawnableItemType.Cheliks), _itemContainer, _waterGrid, _shellSpawnZone));
            _spawners.Add(SpawnableItemType.Jellyfish, new JellyfishSpawner(_spawnConfig.GetPrefab<Jellyfish>(SpawnableItemType.Jellyfish), _itemContainer, _waterGrid, _jellyfishSpawnZone));
        }

        private void OnValidate()
        {
            var prefab = _spawnConfig.GetPrefab<Cheliks>(SpawnableItemType.Cheliks);

            if (prefab == null)
            {
                Debug.LogError("���²�! ���� �� ������� �� ����������� (�� �� � ������ �� �� � ���). �� ������ �������� ������. ���� ������� ���, ��� �� ��������� �� ���� ��������. � �����'�, ������ =) /n P.S. ���� ���� ��������, �� � ����� �� ������� ��� �� ������, � ��� �� ���� ������ ������ � ���� ������ � �����!!!");
            }
        }
        public bool CheckIsAllDead()
        {
            if (_activeAirplanes.Count == 0 && _activeEnemies.Count == 0)
            {
                return true;
            }
            return false;

        }
        private void Update()
        {
            if (GameStateManager.Instance.CurrentState == GameState.Playing && !_waveManager.IsStageFinished())
            {
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

        public List<Airplane> ActiveAirplanes => _activeAirplanes;
        public List<Enemy> ActiveEnemies => _activeEnemies;
    }
}