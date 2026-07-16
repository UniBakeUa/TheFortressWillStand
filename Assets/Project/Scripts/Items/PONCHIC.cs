using UnityEngine;

namespace Items
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PONCHIC : ClickableItem
    {
        [Header("Impulse Settings")]
        [SerializeField] private float minForce = 5f;
        [SerializeField] private float maxForce = 8f;
        [SerializeField] private float sideForceRange = 2f; // Наскільки сильно вбік
        [SerializeField] private float rotationSpeed = 100f;

        private Rigidbody2D _rb;
        private float _timer;

        private void Start()
        {
            _rb = GetComponent<Rigidbody2D>();

            float forceUp = Random.Range(minForce, maxForce);
            float forceSide = Random.Range(-sideForceRange, sideForceRange);

            _rb.AddForce(new Vector2(forceSide, forceUp), ForceMode2D.Impulse);

            _rb.angularVelocity = Random.Range(-rotationSpeed, rotationSpeed);
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