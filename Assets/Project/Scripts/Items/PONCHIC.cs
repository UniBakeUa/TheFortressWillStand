using Managers;
using Managers.Audio;
using UI;
using UI.Factories;
using UnityEngine;

namespace Items
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PONCHIC : ClickableItem
    {

        [Header("Visual Settings")]
        [SerializeField] private FloatingTextView _floatingTextPrefab;

        private FloatingTextFactory _floatingTextFactory;
        [Header("Impulse Settings")]
        [SerializeField] private float minForce = 5f;
        [SerializeField] private float maxForce = 8f;
        [SerializeField] private float sideForceRange = 2f; // �������� ������ ���
        [SerializeField] private float rotationSpeed = 100f;

        private Rigidbody2D _rb;
        private float _timer;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _floatingTextFactory = new FloatingTextFactory(_floatingTextPrefab, transform.parent);
        }

        private void InitForce()
        {
            float forceUp = Random.Range(minForce, maxForce);
            float forceSide = Random.Range(-sideForceRange, sideForceRange);

            _rb.AddForce(new Vector2(forceSide, forceUp), ForceMode2D.Impulse);

            _rb.angularVelocity = Random.Range(-rotationSpeed, rotationSpeed);
        }

        [Header("Ponchic Resource")]
        [Tooltip("Скільки пончиків як ресурсу дає збір. Валюта для карток прокачки")]
        [SerializeField] private int _ponchicValue = 1;

        protected override void OnMouseDown()
        {
            base.OnMouseDown();
            if (GameStateManager.Instance.CurrentState != GameState.Playing)
                return;

            // Пончик дає і монети (base.Collect), і пончики - окремий ресурс,
            // за який беруться картки прокачки після першої.
            if (PonchicManager.Instance != null)
                PonchicManager.Instance.AddPonchics(_ponchicValue);

            _floatingTextFactory.SpawnText((int)moneyValue, transform.position + Vector3.up * 1.5f);
        }

        private void OnEnable()
        {
            SoundManager.Instance.Play(SoundId.PonchicSpawn, transform.position);
            InitForce();
            _timer = 0f;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= 5f)
            {
                _onItemFinished?.Invoke(this);
            }
        }
    }
}