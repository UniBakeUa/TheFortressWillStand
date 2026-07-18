using TMPro;
using UnityEngine;

public class ShowGameStateText : MonoBehaviour
{
    [SerializeField] private GameStateManager _gmManager;

    [SerializeField] private TextMeshProUGUI _text;

    private void OnEnable()
    {
        _gmManager.OnStateChange += UpdateText;
    }

    private void OnDisable()
    {
        _gmManager.OnStateChange -= UpdateText;
    }

    private void UpdateText(GameState state)
    {
        _text.SetText(state.ToString());
    }
}
