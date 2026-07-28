using System;
using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Пончики - окрема валюта, за яку беруться картки прокачки після першої
    /// безкоштовної. Живуть окремо від монет: монети витрачаються на будівлі,
    /// пончики - тільки на перки.
    /// </summary>
    public class PonchicManager : MonoBehaviour
    {
        public static PonchicManager Instance { get; private set; }

        [SerializeField] private int _startPonchics;

        private int _ponchics;

        /// <summary>Скільки пончиків зараз. UI підписується сюди, а не полить в Update.</summary>
        public event Action<int> OnPonchicsChanged;

        public int Ponchics => _ponchics;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            SetPonchics(_startPonchics);
        }

        public void AddPonchics(int amount)
        {
            if (amount <= 0) return;
            SetPonchics(_ponchics + amount);
        }

        public bool HasPonchics(int amount) => _ponchics >= amount;

        /// <summary>
        /// Знімає пончики, якщо їх вистачає. Повертає false і нічого не змінює,
        /// якщо не вистачає - викликач сам вирішує, що показати гравцю.
        /// </summary>
        public bool TrySpendPonchics(int amount)
        {
            if (amount <= 0) return true;
            if (_ponchics < amount) return false;

            SetPonchics(_ponchics - amount);
            return true;
        }

        private void SetPonchics(int value)
        {
            _ponchics = Mathf.Max(0, value);
            OnPonchicsChanged?.Invoke(_ponchics);
        }
    }
}
