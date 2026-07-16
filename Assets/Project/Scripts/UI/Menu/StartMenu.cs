using UnityEngine;
using Managers;

public class StartMenu : MonoBehaviour
{
    [SerializeField] GameObject startMenu;
    public void Play()
    {
        startMenu.SetActive(false);
        GameStateManager.Instance.ChangeState(GameState.Building);
    }
}
