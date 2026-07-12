using UnityEngine;

namespace UI
{
    public class ChangeStateToPlaying : MonoBehaviour
    {
        public void StartPlaying()
        {
            GameStateManager.Instance.ChangeState(GameState.Playing);
        }
    }
}