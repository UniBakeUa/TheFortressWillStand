using Managers;
using TMPro;
using UnityEngine;

namespace UI
{
    public class CostById : MonoBehaviour
    {
        [SerializeField] private int Id;
        [SerializeField] private TMP_Text text;

        private void Update()
        {
            text.text = BuildManager.Instance.GetCostById(Id).ToString();
        }
    }
}