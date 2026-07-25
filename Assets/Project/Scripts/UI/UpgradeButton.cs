using UnityEngine;
using TMPro;
using Managers;
using Towers;

namespace UI
{
    public class UpgradeButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _currentValueText;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private int _id;
        public int Cost { get; private set; }
        private int timesUsed;

        public int Id { get => _id;}
        public int TimesUsed { get => timesUsed; }

        private void Start()
        {
            PutPercentageInAName(UpgradeManager.Instance.GetAmountOfInfluenceById(_id));
            ChangeCost(UpgradeManager.Instance.GetCostById(Id));
            UpgradeManager.Instance.UpdateButtonValue(_id, this);
        }
        public void PutPercentageInAName(float percentage) => _nameText.text += $" +{percentage:P0}";
        public void ChangeCurrentValue(string value) => _currentValueText.text = value;
        public void ChangeCost(int number)
        {
            Cost = number;
            _costText.text = Cost.ToString();
        }

        public void Use() => timesUsed++;
    }
}