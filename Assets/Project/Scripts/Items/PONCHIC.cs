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

        [SerializeField] private AudioClip _audioClip;

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

        protected override void OnMouseDown()
        {
            base.OnMouseDown();
            if (GameStateManager.Instance.CurrentState != GameState.Playing)
                return;

            _floatingTextFactory.SpawnText((int)moneyValue, transform.position + Vector3.up * 1.5f);
        }

        private void OnEnable()
        {
            AudioSource.PlayClipAtPoint(_audioClip, transform.position);
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