using UnityEngine;
using Managers;
using Managers.Audio;
using System.Collections;
using Towers;
using System.Collections.Generic;
using DG.Tweening;
using Items.Spawners;
using Items.Data;

namespace Items
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Airplane : ClickableItem
    {
        [SerializeField] private float speed = 3f;

        [Header("Health")]
        [SerializeField] private int maxHP = 10;
        [Tooltip("Скільки HP знімає клік пальцем. Стільки ж, скільки по наземних ворогах")]
        [SerializeField] private int _clickDamage = 5;
        [SerializeField] private Color _hitFlashColor = new Color(1f, 0.5f, 0.5f, 1f);
        [SerializeField] private float _hitFlashDuration = 0.06f;
        [Tooltip("Іскра/вибух у місці влучання. Можна взяти той самий Explosion")]
        [SerializeField] private Explosion _hitSparkPrefab;
        [Tooltip("Розмір іскри. Менше за вибух від збиття")]
        [SerializeField] private float _hitSparkScale = 0.15f;
        [Tooltip("Наскільки смикається спрайт. 0 = без трясіння")]
        [SerializeField] private float _hitShakeStrength = 0.12f;

        [Header("Crash")]
        [Tooltip("Мінімум часу, який збитий літак лишається в грі, навіть якщо " +
                 "одразу вилетів за кадр - інакше анімація падіння не видно")]
        [SerializeField] private float _minCrashVisibleTime = 0.6f;
        [Tooltip("Через скільки секунд уламок прибирається в будь-якому разі")]
        [SerializeField] private float _maxCrashDuration = 4f;
        private int currentHP;

        [Header("Physics")]
        [SerializeField] private float crashGravity = 3f;
        [SerializeField] private float crashRotation = 200f;

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
            _isFinished = false;
            currentHP = maxHP;

            // Корутини попереднього життя могли лишитись висіти - інакше стара
            // FinishAfterCrash поверне в пул уже цей, щойно заспавнений літак.
            StopAllCoroutines();
            _returnRoutine = null;

            // Літак міг піти в пул посеред спалаху - інакше відродиться червоним.
            if (_hitFlashRoutine != null)
            {
                StopCoroutine(_hitFlashRoutine);
                _hitFlashRoutine = null;
            }
            if (_spriteRenderers != null) ResetSpriteColors();

            // Скидаємо трясіння, інакше спрайт лишиться зміщеним відносно літака.
            if (_hasVisualLocalPosition && _spriteRenderers != null && _spriteRenderers.Length > 0)
            {
                Transform visual = _spriteRenderers[0].transform;
                visual.DOKill();
                visual.localPosition = _visualLocalPosition;
            }

            rb.gravityScale = 0;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0;

            rb.bodyType = RigidbodyType2D.Kinematic;

            if (_returnRoutine != null) StopCoroutine(_returnRoutine);
            _returnRoutine = StartCoroutine(ReturnInPoolIfNotVisible());
        }

        private Coroutine _returnRoutine;

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

            if (GameStateManager.Instance.CurrentState != GameState.Playing)
                return;

            // НЕ кличемо base.OnMouseDown(): він одразу робить Collect(), тобто
            // збиває літак з одного кліка повз HP. Клік має знімати стільки ж,
            // скільки й по наземних ворогах.
            TakeHit(_clickDamage);
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
            SoundManager.Instance.Play(SoundId.PlaneBombExplosion, Fortress.transform.position);
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

            // Літак падає - корутина респавну більше не потрібна. Інакше вона
            // могла б смикнути Respawn() посеред анімації падіння.
            if (_returnRoutine != null)
            {
                StopCoroutine(_returnRoutine);
                _returnRoutine = null;
            }

            _crashStartTime = Time.time;

            // Страховка: якщо літак упав за межами екрана, OnBecameInvisible уже
            // не спрацює вдруге, і без цього уламок висів би вічно.
            StartCoroutine(FinishAfterCrash());

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = crashGravity;
            rb.linearVelocity = direction * speed;
            rb.angularVelocity = crashRotation;
        }

        private float _crashStartTime;

        /// <summary>
        /// Прибирає уламок, коли анімація падіння відпрацювала, а OnBecameInvisible
        /// уже не спрацює (літак упав за кадром).
        /// </summary>
        private IEnumerator FinishAfterCrash()
        {
            yield return new WaitForSeconds(_maxCrashDuration);

            FinishOnce();
        }

        private void OnBecameInvisible()
        {
            // Літак, збитий біля краю екрана, вилітає з кадру за лічені кадри, і
            // Finish() з'їдав би всю анімацію падіння. Даємо їй мінімальний час.
            if (isCrashing && Time.time - _crashStartTime >= _minCrashVisibleTime)
            {
                FinishOnce();
            }
        }

        /// <summary>
        /// Повертає в пул рівно один раз за життя. Finish() можуть смикнути і
        /// OnBecameInvisible, і страхувальна корутина - другий виклик повернув би
        /// у пул літак, який уже переспавнили, і той зник би посеред польоту.
        /// </summary>
        private void FinishOnce()
        {
            if (!isCrashing || _isFinished) return;

            _isFinished = true;
            Finish();
        }

        private bool _isFinished;

        public void TakeHit(int damage)
        {
            if (isCrashing) return;

            currentHP -= damage;
            if (currentHP <= 0)
            {
                Collect();
                return;
            }

            // Влучив, але не збив - без цього незрозуміло, чи клік зарахувався.
            PlayHitFlash();
            PlayHitShake();
            SpawnHitSpark();
        }

        /// <summary>Маленький вибух у місці влучання - найпомітніша частина ефекту.</summary>
        private void SpawnHitSpark()
        {
            if (_hitSparkPrefab == null) return;

            var spark = Instantiate(_hitSparkPrefab, transform.position, Quaternion.identity);
            spark.enabled = false;
            spark.ChangeScaleModifier(_hitSparkScale);
            spark.enabled = true;
        }

        /// <summary>Спрайт смикається від влучання.</summary>
        private void PlayHitShake()
        {
            if (_hitShakeStrength <= 0f) return;
            if (_spriteRenderers == null || _spriteRenderers.Length == 0) return;

            // Трясемо ДОЧІРНІЙ спрайт, а не корінь: сам літак рухається через
            // transform.position += щокадру, і твін на корені бився б із цим рухом.
            Transform visual = _spriteRenderers[0].transform;
            if (visual == transform) return;

            visual.DOKill();
            visual.localPosition = _visualLocalPosition;
            visual.DOShakePosition(_hitFlashDuration * 2f, _hitShakeStrength, vibrato: 12, randomness: 90f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => visual.localPosition = _visualLocalPosition);
        }

        private Vector3 _visualLocalPosition;
        private bool _hasVisualLocalPosition;

        private SpriteRenderer[] _spriteRenderers;
        private Color[] _originalSpriteColors;
        private Coroutine _hitFlashRoutine;

        private void PlayHitFlash()
        {
            if (_spriteRenderers == null)
            {
                _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
                _originalSpriteColors = new Color[_spriteRenderers.Length];
                for (int i = 0; i < _spriteRenderers.Length; i++)
                {
                    _originalSpriteColors[i] = _spriteRenderers[i].color;
                }

                if (!_hasVisualLocalPosition && _spriteRenderers.Length > 0)
                {
                    _visualLocalPosition = _spriteRenderers[0].transform.localPosition;
                    _hasVisualLocalPosition = true;
                }
            }

            if (_spriteRenderers.Length == 0) return;

            if (_hitFlashRoutine != null)
            {
                StopCoroutine(_hitFlashRoutine);
                ResetSpriteColors();
            }

            _hitFlashRoutine = StartCoroutine(HitFlashRoutine());
        }

        private IEnumerator HitFlashRoutine()
        {
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] != null) _spriteRenderers[i].color = _hitFlashColor;
            }

            yield return new WaitForSeconds(_hitFlashDuration);

            ResetSpriteColors();
            _hitFlashRoutine = null;
        }

        private void ResetSpriteColors()
        {
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] != null) _spriteRenderers[i].color = _originalSpriteColors[i];
            }
        }

        private void PlayFallingAudio()
        {
            SoundManager.Instance.Play(SoundId.PlaneFalling, transform.position);

            if (Random.Range(0, 3) != 0) return;

            SoundManager.Instance.Play(SoundId.PlaneVoice, transform.position);
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
        /// <summary>
        /// Літак пролетів повз і вийшов з кадру - розвертаємо його назад на
        /// фортецю. Раніше тут був телепорт на випадковий край екрана, і збоку це
        /// читалось як "літак пропав без жодного дамагу".
        /// </summary>
        private void Respawn()
        {
            if (isCrashing) return;
            if (_fortress == null) return;

            Vector2 direction = ((Vector2)_fortress.transform.position - (Vector2)transform.position).normalized;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector2.down;

            // HP не чіпаємо: це той самий літак, підбитий лишається підбитим.
            StartFlight(direction);

            // Заходить на ціль наново, тож може скинути бомбу ще раз.
            isBombDropped = false;

            // Стару корутину перезапускаємо, а не додаємо ще одну: інакше з кожним
            // колом їх ставало б більше, і Respawn викликався б по кілька разів.
            if (_returnRoutine != null) StopCoroutine(_returnRoutine);
            _returnRoutine = StartCoroutine(ReturnInPoolIfNotVisible());
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