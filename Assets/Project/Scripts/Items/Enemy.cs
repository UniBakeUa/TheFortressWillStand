using UnityEngine;
using Managers;
using System.Collections;
using Towers;
using System.Collections.Generic;
namespace Items
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Enemy : ClickableItem
    {
        [SerializeField] private float speed = 3f;
        [SerializeField] private float AttackDistance = 3f;
        [SerializeField] private float damage;
        [SerializeField] private float attackDelay = 1f;
        private float timeSinceLastAttack = 1f;

        [Header("Physics")]
        [SerializeField] private float crashGravity = 3f;
        [SerializeField] private float crashRotation = 200f;

        [Header("Audio")]
        [SerializeField] private AudioClip falling;
        [SerializeField] private AudioClip[] voiceLines;

        private Rigidbody2D rb;
        private Camera _camera;
        private Fortress _fortress;

        private Vector2 direction;

        private bool isDead;
        private bool isReachTower;
        private bool isBombDropped = false;

        [Header("Explosion")]
        [SerializeField] private GameObject _explosionPrefab;

        private List<Enemy> _activeEnemieslistReference;
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            _camera = Camera.main;
        }

        private void OnEnable()
        {
            isDead = false;
            isBombDropped = false;

            rb.gravityScale = 0;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0;

            rb.bodyType = RigidbodyType2D.Kinematic;
            StartCoroutine(ReturnInPoolIfNotVisible());
        }

        public void StartFlight(Vector2 _direction)
        {
            direction = _direction.normalized;

            transform.right = direction;
        }


        private void Update()
        {
            if (isDead)
                return;

            if (!isReachTower)
            {

                transform.position +=
                    (Vector3)(direction * speed * Time.deltaTime);
            }

            CheckForDropBomb();
        }

        protected override void OnMouseDown()
        {
            if (isDead)
                return;

            base.OnMouseDown();
        }

        private void CheckForDropBomb()
        {
            if (Fortress != null && !isBombDropped && Vector3.Distance(transform.position, Fortress.transform.position) <= AttackDistance)
            {
                isReachTower = true;
                if (timeSinceLastAttack >= attackDelay)
                {
                    CreateExplosionInTheFortress();
                    timeSinceLastAttack = 0f;
                }
                else
                {
                    timeSinceLastAttack += Time.deltaTime;
                }
            }
        }

        private void CreateExplosionInTheFortress()
        {
            isBombDropped = true;
            Instantiate(_explosionPrefab, GetRandomPositionAround(Fortress.transform.position, 0.4f), Quaternion.identity);
            _fortress.GetComponentInParent<IDamageable>().TakeDamage(damage);
        }
        private Vector3 GetRandomPositionAround(Vector3 center, float radius)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            if (offset.x > center.x)
            {
                offset.x = -offset.x;
            }
            return center + new Vector3(offset.x, offset.y, 0f);
        }

        protected override void Collect()
        {
            PlayFallingAudio();

            MoneyManager.Instance.AddMoney(moneyValue);
            StartFalling();
            _activeEnemieslistReference.Remove(this);
        }

        private void StartFalling()
        {
            isDead = true;

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = crashGravity;
            rb.linearVelocity = direction * speed;
            rb.angularVelocity = crashRotation;
        }

        private void OnBecameInvisible()
        {
            if (isDead)
            {
                Finish();
            }
        }

        public void WasStricken()
        {
            isDead = true;

            Collect();
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
            if (isDead)
            {
                Finish();
            }
        }

        private bool IsInCameraRange(Camera camera)
        {
            Vector3 viewportPos = camera.WorldToViewportPoint(transform.position);

            return viewportPos.x >= 0 && viewportPos.x <= 1 &&
                   viewportPos.y >= 0 && viewportPos.y <= 1 &&
                   viewportPos.z > 0;

        }

        public Fortress Fortress
        {
            get { return _fortress; }
            set { _fortress = value; }
        }

        public List<Enemy> ActiveEnemieslistReference
        {
            get { return _activeEnemieslistReference; }
            set { _activeEnemieslistReference = value; }

        }
    }

}
