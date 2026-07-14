using Core;
using Items.Data;
using UnityEngine;

namespace Items.Spawners
{
    public abstract class BaseSpawner<T>: ISpawner where T : ClickableItem
    {
        protected readonly Transform container;

        protected ObjectPool<T> pool;
        private float _timer;

        public BaseSpawner(T prefab, Transform container)
        {
            pool = new ObjectPool<T>(prefab, container);
        }

        public virtual void SpawnTimer(ItemSpawnData spawnData)
        {
            if (GameStateManager.Instance.CurrentState != GameState.Playing) return;

            _timer += Time.deltaTime;
            if (_timer >= spawnData.SpawnInterval)
            {
                _timer = 0;

                if (Random.Range(0f, 100f) <= spawnData.SpawnChance)
                {
                    if (TryGetSpawnPosition(out Vector3 spawnPos))
                    {
                        SpawnItem(spawnPos);
                    }
                }
            }
        }

        private void SpawnItem(Vector3 position)
        {
            T item = pool.Get();
            item.transform.position = position;

            item.Init(ReturnToPool);

            item.gameObject.SetActive(true);

            SetupSpawnedItem(item);
        }

        private void ReturnToPool(ClickableItem clickedItem)
        {
            pool.Return((T)clickedItem);
        }

        protected abstract bool TryGetSpawnPosition(out Vector3 position);

        protected virtual void SetupSpawnedItem(T item) { }
    }
}