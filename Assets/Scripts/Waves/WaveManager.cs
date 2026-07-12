using System.Collections;
using UnityEngine;

namespace Waves
{
    public class WaveManager : MonoBehaviour
    {
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
                // 1. Кожен 5-й раз збільшуємо кількість хвиль додатково
                _totalSequencesPlayed++;
                if (_totalSequencesPlayed % extraWavesEveryXStages == 0)
                {
                    Debug.Log($"БОНУС: Додаткова хвиля через кожні {extraWavesEveryXStages} циклів!");
                    _currentStage += 1;
                }

                // 2. Програємо поточну стадію
                for (int i = 0; i < _currentStage; i++)
                {
                    bool isWaveDone = false;
                    System.Action onDone = () => isWaveDone = true;
                    waterGrid.OnWaveFinished += onDone;

                    waterGrid.TriggerWave();

                    // Запобіжник на 20 секунд, щоб гра не зависла
                    float timeout = Time.time + 20f;
                    yield return new WaitUntil(() => isWaveDone || Time.time > timeout);

                    waterGrid.OnWaveFinished -= onDone;

                    // Пауза між хвилями
                    yield return new WaitForSeconds(delayBetweenWaves);
                }

                // 3. Завершення стадії - перехід в Building
                GameStateManager.Instance.ChangeState(GameState.Building);
        
                yield return new WaitUntil(() => GameStateManager.Instance.CurrentState == GameState.Playing);

                // 4. Оновлюємо складність
                _currentStage = GetNextFibonacci(_currentStage);
                yield return new WaitForSeconds(delayBetweenStages);
            }
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