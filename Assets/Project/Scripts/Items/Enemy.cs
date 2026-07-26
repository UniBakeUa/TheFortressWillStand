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
        [SerializeField] private float firstAttackDelay = 2f;
        private float timeSinceLastAttack;
        private bool hasAttackedOnce;

        [Header("Physics")]
        [SerializeField] private float crashGravity = 3f;
        [SerializeField] private float crashRotation = 200f;

        [Header("Audio")]
        [SerializeField] private AudioClip falling;
        [SerializeField] private AudioClip shotSound;
        [SerializeField] private AudioClip[] voiceLines;

        private Rigidbody2D rb;
        private Camera _camera;
        private Fortress _fortress;

        private Vector2 direction;

        private bool isDead;
        private bool isReachTower;
        private bool isGrenadeInFlight;

        [Header("Explosion")]
        [SerializeField] private GameObject _explosionPrefab;
        [SerializeField] private Grenade _grenadePrefab;

        private List<Enemy> _activeEnemieslistReference;
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            _camera = Camera.main;
        }

        private void OnEnable()
        {
            isDead = false;
            isGrenadeInFlight = false;
            isReachTower = false;
            hasAttackedOnce = false;
            timeSinceLastAttack = 0f;

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
            if (Fortress != null && !isGrenadeInFlight && Vector3.Distance(transform.position, Fortress.transform.position) <= AttackDistance)
            {
                isReachTower = true;
                float currentDelay = hasAttackedOnce ? attackDelay : firstAttackDelay;
                if (timeSinceLastAttack >= currentDelay)
                {
                    ThrowGrenadeAtFortress();
                    timeSinceLastAttack = 0f;
                }
                else
                {
                    timeSinceLastAttack += Time.deltaTime;
                }
            }
        }

        private void ThrowGrenadeAtFortress()
        {
            isGrenadeInFlight = true;
            hasAttackedOnce = true;
            AudioSource.PlayClipAtPoint(shotSound, transform.position, 0.5f);

            Vector3 impactPosition = GetRandomPositionAround(Fortress.transform.position, 0.4f);
            var grenade = Instantiate(_grenadePrefab, transform.position, Quaternion.identity);
            grenade.Launch(impactPosition, () => CreateExplosionInTheFortress(impactPosition));
        }

        private void CreateExplosionInTheFortress(Vector3 impactPosition)
        {
            isGrenadeInFlight = false;
            Instantiate(_explosionPrefab, impactPosition, Quaternion.identity);
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

            base.Collect();
            //MoneyManager.Instance.AddMoney(moneyValue);
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
            if (isDead) return;

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
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + direction);
        }
    }
}
