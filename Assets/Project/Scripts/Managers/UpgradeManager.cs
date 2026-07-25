using Towers.ScriptableObjects;
using UI;
using UnityEngine;

namespace Managers
{
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [Header("База даних покращень")]
        [SerializeField] private UpgradeLibrary _upgradeLibrary;
        [SerializeField] private MoneyManager _moneyManager;

        private void Awake()
        {
            Instance = this;
        }
        public int GetCostById(int id)
        {
            var config = _upgradeLibrary.GetConfigById(id);
            return (config != null) ? config.BaseCost : 999999;
        }
        public void SelectUpgrade(UpgradeButton upgradeButton)
        {
            UpgradeConfig config = _upgradeLibrary.GetConfigById(upgradeButton.Id) as UpgradeConfig;
            if (config == null) return;

            //if (_moneyManager.GetMoney() < config.BaseCost)
            //{
            //    _moneyManager.ShowNotEnoughPopup();
            //    return;
            //}
            UseUpgrade(config);
            CalculateCost(config, upgradeButton);
        }
        public float GetAmountOfInfluenceById(int id)
        {
            var config = _upgradeLibrary.GetConfigById(id) as UpgradeConfig;
            return (config != null) ? config.amountOfInfluence : 999999;
        }
        private void UseUpgrade(UpgradeConfig config)
        {
            //_moneyManager.SpendMoney(config.BaseCost);
        }
        private int CalculateCost(UpgradeConfig config, UpgradeButton upgradeButton)
        {
            if (config.priceChangePercentage == 0) return config.BaseCost;
            int newCost = (int)(config.BaseCost + config.BaseCost * (config.priceChangePercentage * upgradeButton.TimesUsed));

            upgradeButton.ChangeCostText(newCost);

            return newCost;
        }
    }
}
