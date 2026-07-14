using Core;
using UnityEngine;

namespace UI.Factories
{
    public class FloatingTextFactory
    {
        private ObjectPool<FloatingTextView> _pool;

        public FloatingTextFactory(FloatingTextView prefab, Transform container)
        {
            _pool = new ObjectPool<FloatingTextView>(prefab, container);
        }

        public void SpawnText(int coinAmount, Vector3 spawnPosition)
        {
            FloatingTextView textObj = _pool.Get();

            textObj.gameObject.SetActive(true);
            textObj.transform.position = spawnPosition;

            textObj.Setup(coinAmount, ReturnToPool);
        }

        private void ReturnToPool(FloatingTextView textObj)
        {
            _pool.Return(textObj);
        }
    }
}