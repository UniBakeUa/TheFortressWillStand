using System;
using UnityEngine;

namespace Money
{
    public class MoneyManager : MonoBehaviour
    {
        public static MoneyManager Instance;
        
        [SerializeField] private int StartMoney;
        private int _money;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _money = StartMoney;
        }

        public int GetMoney() => _money; 
        public void ShowNotEnoughPopup()
        {
            Debug.Log("Not enough money!");
        }
        
        public void AddMoney(int money)
        {
            _money += money;
        }

        public void SpendMoney(int money)
        {
            _money -= money;
        }
    }
}