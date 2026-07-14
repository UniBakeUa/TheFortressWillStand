using System.Collections;
using UI;
using UnityEngine;

namespace Waves
{
    public class WaveManager : MonoBehaviour
    {
        [SerializeField] private WaveTimerView _waveTimerView;
        [SerializeField] WaterGrid waterGrid;
        [SerializeField] float delayBetweenWaves = 2.0f;
        [SerializeField] float delayBetweenStages = 10.0f;

        private int _currentStage = 1;
        private Coroutine _waveCoroutine;
        private int _totalSequencesPlayed = 0; // Лічильник загальної кількості циклів
        [SerializeField] private int extraWavesEveryXStages = 5;
        void Start()
        {
            // Підписуємося на зміни станів
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChange += HandleStateChange;
        }

        void OnDestroy()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChange -= HandleStateChange;
        }

        private void HandleStateChange(GameState newState)
        {
            if (newState == GameState.Playing)
            {
                // Якщо ми в Playing - запускаємо хвилі
                if (_waveCoroutine == null)
                    _waveCoroutine = StartCoroutine(WaveProgressionRoutine());
            }
            else
            {
                // Якщо ми в Building або Paused - зупиняємо хвилі
                if (_waveCoroutine != null)
                {
                    StopCoroutine(_waveCoroutine);
                    _waveCoroutine = null;
                }
            }
        }

        IEnumerator WaveProgressionRoutine()
        {
            while (true)
            {
                // 1. Оновлення стадії
                _totalSequencesPlayed++;
                if (_totalSequencesPlayed % extraWavesEveryXStages == 0)
                {
                    Debug.Log($"БОНУС: Додаткова хвиля через кожні {extraWavesEveryXStages} циклів!");
                    _currentStage += 1;
                }

                // 2. Розраховуємо загальний час стадії для UI
                // Оскільки час хвилі залежить від isWaveDone, ми беремо орієнтовний або максимальний час
                float expectedWaveTime = 7f; // Задай тут середній або максимальний час проходження однієї хвилі
                float totalStageTime = (expectedWaveTime + delayBetweenWaves) * _currentStage;

                float timeLeft = totalStageTime;
                _waveTimerView.ShowTimer(totalStageTime); // Встановлюємо maxValue

                // 3. Запускаємо логіку хвиль паралельно
                bool isStageFinished = false;
                StartCoroutine(PlayWavesSequence(_currentStage, () => isStageFinished = true));

                // 4. Плавно оновлюємо таймер зворотного відліку
                while (!isStageFinished)
                {
                    timeLeft -= Time.deltaTime;
                    // Використовуємо Mathf.Max, щоб слайдер не пішов у мінус, 
                    // якщо хвиля триває довше очікуваного часу
                    _waveTimerView.UpdateTimer(Mathf.Max(0, timeLeft));

                    yield return null;
                }

                // 5. Завершення стадії - перехід в Building
                GameStateManager.Instance.ChangeState(GameState.Building);
                _waveTimerView.HideTimer();

                // Чекаємо, поки гравець завершить будівництво
                yield return new WaitUntil(() => GameStateManager.Instance.CurrentState == GameState.Playing);

                // 6. Оновлюємо складність
                _currentStage = GetNextFibonacci(_currentStage);
                yield return new WaitForSeconds(delayBetweenStages);
            }
        }

        // Логіка спавну залишається такою ж, як в попередньому прикладі
        IEnumerator PlayWavesSequence(int stagesToPlay, System.Action onComplete)
        {
            for (int i = 0; i < stagesToPlay; i++)
            {
                bool isWaveDone = false;
                System.Action onDone = () => isWaveDone = true;
                waterGrid.OnWaveFinished += onDone;

                waterGrid.TriggerWave();

                // Таймаут на випадок, якщо OnWaveFinished не спрацює
                float timeout = Time.time + 20f;
                yield return new WaitUntil(() => isWaveDone || Time.time > timeout);

                waterGrid.OnWaveFinished -= onDone;

                yield return new WaitForSeconds(delayBetweenWaves);
            }

            onComplete?.Invoke();
        }

        int GetNextFibonacci(int current)
        {
            if (current == 1) return 2;
            if (current == 2) return 3;
            if (current == 3) return 5;
            return current + 3;
        }
    }
}