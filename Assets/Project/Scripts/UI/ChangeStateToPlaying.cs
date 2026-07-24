using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ChangeStateToPlaying : MonoBehaviour
    {
        [SerializeField] private float _timeToStart;
        [SerializeField] private Slider _timerSlider;
        [SerializeField] private PekpekMessage _pekpekMessage;
        private bool _oncePekpek;

        private float _timer;

        private void Start()
        {
            _timer = _timeToStart;
            _timerSlider.maxValue = _timeToStart;
            _timerSlider.value = _timer;
        }

        public void StartPlaying()
        {
            GameStateManager.Instance.ChangeState(GameState.Playing);
            _timer = _timeToStart;
            _timerSlider.value = _timer;

            _oncePekpek = false;
            _pekpekMessage.FoldIn();
        }

        public void OnTimerEnd()
        {
            if (_timer > 0f) return;

            GameStateManager.Instance.ChangeState(GameState.Playing);
            _timer = _timeToStart;
            _timerSlider.value = _timer;

            _oncePekpek = false;
            _pekpekMessage.FoldIn();
        }

        private void Update()
        {
            if(GameStateManager.Instance.CurrentState == GameState.Playing) return;

            _timer -= Time.deltaTime;
            _timerSlider.value = _timer;
            if(!_oncePekpek && _timer <= _timeToStart / 2)
            {
                _oncePekpek = true;
                _pekpekMessage.Unfold();
            }
        }
    }
}