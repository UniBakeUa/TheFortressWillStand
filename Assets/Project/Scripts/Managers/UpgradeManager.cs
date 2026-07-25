using Towers;
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

        [Header("Полагодити фортецю")]
        [SerializeField] private Fortress _fortress;

        [Header("Збільшити радіус ураження")]
        [SerializeField] private MouseBomber _mouseBomber;

        [SerializeField] private SecretScreenFiller _secretScreenFiller;

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

            if (_moneyManager.GetMoney() < upgradeButton.Cost)
            {
                _moneyManager.ShowNotEnoughPopup();
                return;
            }
            UseUpgrade(config, upgradeButton);
            upgradeButton.ChangeCost(CalculateCost(config, upgradeButton));
        }
        public float GetAmountOfInfluenceById(int id)
        {
            var config = _upgradeLibrary.GetConfigById(id) as UpgradeConfig;
            return (config != null) ? config.amountOfInfluence : 999999;
        }
        private void UseUpgrade(UpgradeConfig config, UpgradeButton upgradeButton)
        {
            // Ремонтуємо фортецю.
            switch (config.Id)
            {
                case 0:
                    {
                        if (_fortress.Model.CurrentHP == _fortress.Model.MaxHP)
                        {
                            Debug.Log("Ваша фортеця достатньо ціла для ремонту");
                            return;
                        }
                        _fortress.Repair(_fortress.Model.MaxHP / 100 * 15);
                        UpdateButtonValue(0, upgradeButton);
                    }
                    break;
                case 1:
                    _mouseBomber.ModifyRadius(config.amountOfInfluence);
                    UpdateButtonValue(1, upgradeButton);
                    break;
                case 2:
                    _secretScreenFiller.SpawnSecretSprite();
                    UpdateButtonValue(2, upgradeButton);
                    break;
            }
            upgradeButton.Use();
            _moneyManager.SpendMoney(upgradeButton.Cost);

        }
        private int CalculateCost(UpgradeConfig config, UpgradeButton upgradeButton)
        {
            if (config.priceChangePercentage == 0) return config.BaseCost;
            int newCost = Mathf.RoundToInt(config.BaseCost + config.BaseCost * (config.priceChangePercentage * upgradeButton.TimesUsed));

            return newCost;
        }
        public void UpdateButtonValue(int id, UpgradeButton upgradeButton)
        {
            string newValue = string.Empty;
            switch (id)
            {
                case 0:
                    if (_fortress.Model == null)
                    {
                        newValue = 100.ToString();
                        break;
                    }
                    newValue = (_fortress.Model.CurrentHP / _fortress.Model.MaxHP).ToString();
                    break;
                case 1:
                    newValue = $"+{_mouseBomber.GetRadiusFraction():F0}%";
                    break;
                case 2:
                    newValue = upgradeButton.TimesUsed.ToString();
                    break;
            }
            upgradeButton.ChangeCurrentValue(newValue);
        }
    }
}
