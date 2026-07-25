using Managers;
using TMPro;
using UnityEngine;
using Towers;

namespace UI
{
    public class CostById : MonoBehaviour
    {
        [SerializeField] private ProductType productType;
        [SerializeField] private int Id;
        [SerializeField] private TMP_Text text;

        private void Update()
        {
            text.text = BuildManager.Instance.GetCostById(Id, productType).ToString();
        }
    }
}