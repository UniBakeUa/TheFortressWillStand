using UnityEngine;
using Waves;

namespace Items.Spawners
{
    public class WaterShellSpawner : BaseSpawner<Shell>
    {
        private readonly WaterGrid _waterGrid;
        private readonly Collider2D _spawnZone;
        private readonly Shell _prefab;

        public WaterShellSpawner(Shell prefab, Transform container, WaterGrid waterGrid, Collider2D spawnZone) : base(prefab, container)
        {
            _waterGrid = waterGrid;
            _spawnZone = spawnZone;
            _prefab = prefab;
        }

        protected override bool TryGetSpawnPosition(out Vector3 position)
        {
            position = Vector3.zero;

            if (_spawnZone == null) return false;

            Bounds bounds = _spawnZone.bounds;

            for (int i = 0; i < 20; i++)
            {
                float randomX = Random.Range(bounds.min.x, bounds.max.x);
                float randomY = Random.Range(bounds.min.y, bounds.max.y);
                Vector2 randomPoint = new Vector2(randomX, randomY);

                if (_spawnZone.OverlapPoint(randomPoint) && _waterGrid.IsFlooded(randomPoint))
                {
                    position = randomPoint;
                    position.z = _prefab.transform.position.z;
                    return true; // Точку знайдено успішно
                }
            }


            return false;
        }
    }
}