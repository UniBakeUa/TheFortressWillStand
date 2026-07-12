using System;
using Money;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class MoneyView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        private MoneyManager _moneyManager;
        

        private void Start()
        {
            _moneyManager = MoneyManager.Instance;
        }

        private void Update()
        {
            _text.text = _moneyManager.GetMoney().ToString();
        }
    }
}