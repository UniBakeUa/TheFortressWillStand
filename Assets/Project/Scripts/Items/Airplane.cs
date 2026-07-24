using UnityEngine;
using Managers;
using System.Collections;
using Towers;
using System.Collections.Generic;
using static UnityEditor.Progress;
using Items.Spawners;
using Items.Data;

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
        private Fortress _fortress;

        private Vector2 direction;

        private bool isCrashing;
        private bool isBombDropped = false;

        [Header("Explosion")]
        [SerializeField] private GameObject _explosionPrefab;
        [SerializeField] private float _epxlosionDamage;

        private List<Airplane> _activeAirplaneslistReference;
        [field: SerializeField] public SpawnableItemType ItemType { get; private set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            _camera = Camera.main;
        }

        private void OnEnable()
        {
            isCrashing = false;
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

            transform.up = direction;
        }


        private void Update()
        {
            if (isCrashing)
                return;

            transform.position +=
                (Vector3)(direction * speed * Time.deltaTime);

            CheckForDropBomb();
        }

        protected override void OnMouseDown()
        {
            if (isCrashing)
                return;

            base.OnMouseDown();
        }

        private void CheckForDropBomb()
        {
            if (Fortress != null && !isBombDropped && Vector3.Distance(transform.position, Fortress.transform.position) <= 0.05f)
            {
                CreateExplosionInTheFortress();
            }
        }

        private void CreateExplosionInTheFortress()
        {
            isBombDropped = true;
            Instantiate(_explosionPrefab, GetRandomPositionAround(Fortress.transform.position, 0.3f), Quaternion.identity);
            _fortress.GetComponentInParent<IDamageable>().TakeDamage(_epxlosionDamage);
        }
        private Vector3 GetRandomPositionAround(Vector3 center, float radius)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            return center + new Vector3(offset.x, offset.y, 0f);
        }

        protected override void Collect()
        {
            PlayFallingAudio();

            MoneyManager.Instance.AddMoney(moneyValue);
            StartFalling();

            // Літак видаляється зі списку активних літаків.
            _activeAirplaneslistReference.Remove(this);
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

        public void WasStricken()
        {
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

            // Літак видаляється зі списку активних літаків.
            //_activeAirplaneslistReference.Remove(this);
            Respawn();
        }
        private void Respawn()
        {
            if (isCrashing) return;

            if (TryGetSpawnPosition(out Vector3 spawnPos))
            {
                transform.position = spawnPos;
            }
            Vector3 fortressPos = _fortress.transform.position;
            Vector2 direction =
                (fortressPos - transform.position)
                .normalized;

            StartFlight(direction);
            StartCoroutine(ReturnInPoolIfNotVisible());
            isBombDropped = false;
        }

        // Скопіював зі спавнера, спагеті-код стає все смачнішим :)
        private bool TryGetSpawnPosition(out Vector3 position)
        {
            Vector3 min =
                _camera.ViewportToWorldPoint(
                    new Vector3(0, 0));

            Vector3 max =
                _camera.ViewportToWorldPoint(
                    new Vector3(1, 1));

            int side = Random.Range(0, 4);

            switch (side)
            {
                case 0:
                    position = new Vector3(
                        Random.Range(min.x, max.x),
                        max.y + 2f);

                    break;

                case 1:
                    position = new Vector3(
                        Random.Range(min.x, max.x),
                        min.y - 2f);

                    break;

                case 2:
                    position = new Vector3(
                        min.x - 2f,
                        Random.Range(min.y, max.y));

                    break;

                default:
                    position = new Vector3(
                        max.x + 2f,
                        Random.Range(min.y, max.y));

                    break;
            }

            return true;
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

        public List<Airplane> ActiveAirplaneslistReference
        {
            get { return _activeAirplaneslistReference; }
            set { _activeAirplaneslistReference = value; }

        }
    }
}