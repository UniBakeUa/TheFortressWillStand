using Managers;
using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>Лічильник пончиків у HUD.</summary>
    public class PonchicView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;

        private void Start()
        {
            if (PonchicManager.Instance == null) return;

            PonchicManager.Instance.OnPonchicsChanged += UpdateText;
            UpdateText(PonchicManager.Instance.Ponchics);
        }

        private void UpdateText(int amount)
        {
            if (_text != null)
                _text.text = amount.ToString();
        }

        private void OnDestroy()
        {
            if (PonchicManager.Instance != null)
                PonchicManager.Instance.OnPonchicsChanged -= UpdateText;
        }
    }
}
