using UnityEngine;
using Managers;
using System.Collections;

namespace Items
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Airplane : ClickableItem
    {
        [SerializeField] private float speed = 3f;

        [Header("Physics")]
        [SerializeField] private float crashGravity = 3f;
        [SerializeField] private float crashRotation = 200f;

        [Header("Audio")]
        [SerializeField] private AudioClip falling;
        [SerializeField] private AudioClip[] voiceLines;

        private Rigidbody2D rb;
        private Camera _camera;

        private Vector2 direction;

        private bool isCrashing;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            _camera = Camera.main;
        }

        private void OnEnable()
        {
            isCrashing = false;

            rb.gravityScale = 0;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0;

            rb.bodyType = RigidbodyType2D.Kinematic;
            StartCoroutine(ReturnInPoolIfNotVisible());
        }

        public void StartFlight(Vector2 _direction)
        {
            direction = _direction.normalized;
            
            transform.up = direction;
        }


        private void Update()
        {
            if (isCrashing)
                return;

            transform.position +=
                (Vector3)(direction * speed * Time.deltaTime);
        }

        protected override void OnMouseDown()
        {
            if (isCrashing)
                return;

            base.OnMouseDown();
        }

        protected override void Collect()
        {
            PlayFallingAudio();

            MoneyManager.Instance.AddMoney(moneyValue);
            StartFalling();
        }

        private void StartFalling()
        {
            isCrashing = true;

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = crashGravity;
            rb.linearVelocity = direction * speed;
            rb.angularVelocity = crashRotation;
        }

        private void OnBecameInvisible()
        {
            if (isCrashing)
            {
                Finish();
            }
        }

        private void PlayFallingAudio()
        {
            AudioSource.PlayClipAtPoint(falling, transform.position);


            if (Random.Range(0, 3) != 0) return;

            int x = Random.Range(0, voiceLines.Length);
            AudioSource.PlayClipAtPoint(voiceLines[x], transform.position);
        }

        private IEnumerator ReturnInPoolIfNotVisible()
        {
            yield return new WaitForSeconds(1f);
            yield return new WaitUntil(() => !IsInCameraRange(_camera));
            yield return new WaitForSeconds(1f);
            Finish();
        }

        private bool IsInCameraRange(Camera camera)
        {
            Vector3 viewportPos = camera.WorldToViewportPoint(transform.position);

            return viewportPos.x >= 0 && viewportPos.x <= 1 &&
                   viewportPos.y >= 0 && viewportPos.y <= 1 &&
                   viewportPos.z > 0;

        }
    }
}