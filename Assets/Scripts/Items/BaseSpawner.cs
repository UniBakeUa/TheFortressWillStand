using Core;
using UnityEngine;

namespace Items
{
    public abstract class BaseSpawner<T> : MonoBehaviour where T : ClickableItem
    {
        [Header("Базові налаштування")]
        [SerializeField] protected T prefab;
        [SerializeField] protected float spawnInterval = 3f;
        [SerializeField] protected Transform container;

        protected ObjectPool<T> pool;
        private float _timer;

        protected virtual void Start()
        {
            // Ініціалізуємо пул один раз для всіх майбутніх предметів
            pool = new ObjectPool<T>(prefab, container);
        }

        protected virtual void Update()
        {
            if (GameStateManager.Instance.CurrentState != GameState.Playing) return;

            _timer += Time.deltaTime;
            if (_timer >= spawnInterval)
            {
                if (TryGetSpawnPosition(out Vector3 spawnPos))
                {
                    SpawnItem(spawnPos);
                    _timer = 0;
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