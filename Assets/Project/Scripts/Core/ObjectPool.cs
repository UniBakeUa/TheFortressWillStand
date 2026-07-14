using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class ObjectPool<T> where T : Component
    {
        private T _prefab;
        private Queue<T> _pool = new Queue<T>();
        private Transform _container;

        public ObjectPool(T prefab, Transform container = null)
        {
            _prefab = prefab;
            _container = container;
        }

        public T Get()
        {
            if (_pool.Count > 0)
            {
                T obj = _pool.Dequeue();
                return obj;
            }

            T newObj = Object.Instantiate(_prefab, _container);
            return newObj;
        }

        public void Return(T obj)
        {
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
        }
    }
}