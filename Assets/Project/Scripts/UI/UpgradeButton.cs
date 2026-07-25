using UnityEngine;
using TMPro;
using Managers;
namespace UI
{
    public class UpgradeButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _currentValueText;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private int _id;
        private int timesUsed;

        public int Id { get => _id;}
        public int TimesUsed { get => timesUsed; }

        private void Start()
        {
            PutPercentageInAName(UpgradeManager.Instance.GetAmountOfInfluenceById(_id));
        }
        public void PutPercentageInAName(float percentage) => _nameText.text += $" +{percentage:P0}";
        public void ChangeCurrentValue(float number) => _currentValueText.text = number.ToString();
        public void ChangeCostText(int number) => _costText.text = number.ToString();
        public void Use()
        {
            timesUsed++;
        }
    }
}