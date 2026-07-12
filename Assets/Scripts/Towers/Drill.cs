using System.Collections;
using UnityEngine;
using Money;

namespace Towers
{
    public class Drill : Solid, IIncomeProducer
    {
        [SerializeField] float moneyPerTick = 1f;
        [SerializeField] float tickInterval = 5f;

        public float IncomePerTick => moneyPerTick;
        public float TickInterval => tickInterval;

        float _incomeTimer;
        MoneyManager _moneyManager;

        // ФІКС: раніше було "private void Start()" без override - це ХОВАЛО Solid.Start()
        // замість перевизначення (спрацьовувало лише тому, що всередині явно викликали base.Start()).
        // Тепер це правильний override, як і має бути для узгодженості з рештою ієрархії.
        protected override void Start()
        {
            base.Start(); // критично: тут IsReady=true і RegisterFootprint()

            _moneyManager = MoneyManager.Instance;
            PlaySpawnAnimation();
        }

        void Update()
        {
            if (GameStateManager.Instance.CurrentState != GameState.Playing) return;

            _incomeTimer += Time.deltaTime;
            if (_incomeTimer >= tickInterval)
            {
                ProduceMoney();
                _incomeTimer = 0f;
            }
        }

        void ProduceMoney()
        {
            _moneyManager.AddMoney((int)moneyPerTick);
        }

        public void PlaySpawnAnimation(float duration = 0.5f)
        {
            StartCoroutine(SpawnRoutine(duration));
        }

        IEnumerator SpawnRoutine(float duration)
        {
            transform.localScale = Vector3.zero;
            float timer = 0;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float s = Mathf.SmoothStep(0f, 1f, timer / duration);
                transform.localScale = new Vector3(s, s, 1);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }

        public override void Collapse()
        {
            Debug.Log("Drill зруйновано водою!");
            base.Collapse();
        }
    }
}