using UnityEngine;
using Managers;
using Managers.Audio;
using System.Collections;
using System.Collections.Generic;
using Towers;
using Towers.Buildings;
using Waves;

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


        private Rigidbody2D rb;
        private Camera _camera;
        private Fortress _fortress;

        private Vector2 direction;

        private bool isDead;
        private bool isReachTower;
        private bool isGrenadeInFlight;

        [Header("Health")]
        [Tooltip("Скільки влучань витримує. 10 = гине з десятого пострілу або кліка")]
        [SerializeField] private int _maxHP = 10;
        [Tooltip("Скільки HP знімає прямий клік по ворогу. Стільки ж, скільки MouseBomber")]
        [SerializeField] private int _clickDamage = 5;
        [Tooltip("Колір спалаху при влучанні, що не вбило")]
        [SerializeField] private Color _hitFlashColor = new Color(1f, 0.5f, 0.5f, 1f);
        [SerializeField] private float _hitFlashDuration = 0.06f;

        private int _currentHP;

        [Header("Turret (танки)")]
        [Tooltip("Башта, що відлітає при смерті. Порожньо - ворог гине без цього ефекту")]
        [SerializeField] private Transform _turret;
        [Tooltip("Швидкість наведення башти на ціль, градусів/с")]
        [SerializeField] private float _turretRotationSpeed = 120f;
        [Tooltip("Поправка кута, якщо спрайт башти намальований не вздовж осі X. " +
                 "Дуло вгору - постав -90")]
        [SerializeField] private float _turretAngleOffset;
        [Tooltip("З якою силою башту підкидає вгору")]
        [SerializeField] private float _turretLaunchForce = 8f;
        [Tooltip("Розкид вбік: 0 - строго вгору, 1 - до 45 градусів")]
        [SerializeField, Range(0f, 1f)] private float _turretSideSpread = 0.4f;
        [SerializeField] private float _turretGravity = 3f;
        [Tooltip("Швидкість обертання башти в польоті, градусів/с")]
        [SerializeField] private float _turretSpin = 400f;
        [Tooltip("Через скільки секунд башта повертається на корпус (у пулі)")]
        [SerializeField] private float _turretLifetime = 4f;

        [Header("Смерть")]
        [Tooltip("Instant - зникає одразу (гуманоїди); Wreck - зупиняється остовом " +
                 "і згасає (танки); Fall - відлітає за екран (літаки)")]
        [SerializeField] private DeathStyle _deathStyle = DeathStyle.Instant;

        [Tooltip("Скільки секунд остов лежить, перш ніж почне згасати")]
        [SerializeField] private float _wreckLifetime = 0.5f;
        [SerializeField] private float _wreckFadeDuration = 0.5f;
        [Tooltip("Множник кольору остова - темніший за живий танк")]
        [SerializeField] private Color _wreckTint = new Color(0.45f, 0.45f, 0.45f, 1f);

        [Header("Blood")]
        [Tooltip("Чи лишає цей ворог кровавий слід після смерті. Вимкнути для не-гуманоїдів (техніка, літаки тощо)")]
        [SerializeField] private bool _leavesBloodTrail = true;
        [Tooltip("Множник розміру плями - більші вороги лишають більше крові")]
        [SerializeField] private float _bloodSplatScale = 1f;

        [Header("Explosion")]
        [SerializeField] private GameObject _explosionPrefab;
        [SerializeField] private Grenade _grenadePrefab;

        [Header("Animation")]
        [SerializeField] private Animator _animator;
        [SerializeField] private string _runParameter = "run";
        [SerializeField] private string _fireParameter = "fire";
        [SerializeField] private float _attackAnimationDuration = 0.5f;

        [Header("Navigation")]
        [SerializeField] private LayerMask _wallMask;
        [Tooltip("У скільки разів обхідний шлях може бути довшим за пряму відстань до цілі, перш ніж вигідніше зламати стіну")]
        [SerializeField] private float _maxDetourMultiplier = 1.5f;
        [SerializeField] private float _waypointReachDistance = 0.3f;
        [Tooltip("Наскільки випадково ворог може підійти ближче за AttackDistance (0..1 частка від AttackDistance), щоб не всі ставали в один ряд")]
        [SerializeField, Range(0f, 0.9f)] private float _approachDistanceJitter = 0.3f;
        [Tooltip("Шанс, що ворог обере ламати найближчу стіну навіть коли є вільний обхідний шлях до фортеці")]
        [SerializeField, Range(0f, 1f)] private float _preferWallBreakChance = 0.5f;

        private bool _prefersWallBreak;
        private float _effectiveAttackDistance;
        private List<Vector2> _currentPath;
        private int _currentWaypointIndex;
        private Wall _targetWall;
        private Vector2 _attackTargetPosition;
        private int _pathExhaustedCount;
        private float _stuckAtFinalWaypointTimer;

        private List<Enemy> _activeEnemieslistReference;
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            _camera = Camera.main;

            // Вихідний стан башти знімаємо ОДИН раз, поки вона точно на місці.
            // Робити це в LaunchTurret ненадійно: якщо попередній цикл смерті не
            // встиг відновити башту, ми б запам'ятали вже спотворені значення.
            if (_turret != null)
            {
                _turretParent = _turret.parent;
                _turretLocalPosition = _turret.localPosition;
                _turretLocalRotation = _turret.localRotation;
                _turretLocalScale = _turret.localScale;
            }
        }

        private void OnEnable()
        {
            isDead = false;
            _currentHP = Mathf.Max(1, _maxHP);
            isGrenadeInFlight = false;
            _isReturnedToPool = false;

            // Корутини попереднього життя (WreckRoutine, ReturnInPoolIfNotVisible)
            // інакше добігли б уже на цьому, щойно заспавненому танку і відправили
            // б його в пул посеред бою.
            StopAllCoroutines();
            _hitFlashRoutine = null;

            // Ворог міг піти в пул раніше, ніж башта встигла повернутись, -
            // тоді танк виліз би без неї, а стара лишилась би лежати в сцені.
            RestoreTurret();

            // Скидаємо доворот від наведення: RestoreTurret виходить одразу, якщо
            // башта не відлітала, і танк вилазив би з пула з кутом від минулого бою.
            if (_turret != null) _turret.localRotation = _turretLocalRotation;

            // Після остова лишаються вимкнений колайдер і майже нульова прозорість -
            // без цього наступний ворог із пула буде невидимим привидом без колізії.
            if (_deathStyle == DeathStyle.Wreck)
            {
                var col = GetComponent<Collider2D>();
                if (col != null) col.enabled = true;
            }

            // Скидає і колір спалаху, і прозорість, що лишилась від згасання остова.
            if (_spriteRenderers != null) ResetSpriteColors();
            isReachTower = false;
            hasAttackedOnce = false;
            timeSinceLastAttack = 0f;
            _lastHitSource = null;

            _currentPath = null;
            _currentWaypointIndex = 0;
            _targetWall = null;
            _pathExhaustedCount = 0;
            _stuckAtFinalWaypointTimer = 0f;
            _effectiveAttackDistance = AttackDistance * (1f - Random.Range(0f, _approachDistanceJitter));
            _prefersWallBreak = Random.value < _preferWallBreakChance;

            rb.gravityScale = 0;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0;

            rb.bodyType = RigidbodyType2D.Kinematic;

            SetFiring(false);
            UpdateAnimator();

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
                if (IsFortressInAttackRange())
                {
                    isReachTower = true;
                    _currentPath = null;
                    _targetWall = null;
                }
                else
                {
                    UpdateNavigation();
                    MoveTowardsCurrentTarget();
                }
            }

            UpdateAnimator();
            CheckForDropBomb();
            AimTurret();
        }

        /// <summary>
        /// Наводить башту на ціль, поки танк живий. Ціллю є те саме, у що він
        /// стріляє: стіна на шляху або фортеця.
        /// </summary>
        private void AimTurret()
        {
            if (_turret == null || _turretLaunched) return;
            if (Fortress == null) return;

            Vector3 target = _targetWall != null
                ? (Vector3)Vector2.Lerp(_targetWall.NodeAPosition, _targetWall.NodeBPosition, 0.5f)
                : Fortress.transform.position;

            Vector2 toTarget = target - _turret.position;
            if (toTarget.sqrMagnitude < 0.0001f) return;

            // Башта - дочірній об'єкт, тож крутимо її у світових координатах:
            // корпус теж обертається, і локальний кут "поїхав би" разом з ним.
            float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg + _turretAngleOffset;
            Quaternion targetRotation = Quaternion.AngleAxis(targetAngle, Vector3.forward);

            _turret.rotation = Quaternion.RotateTowards(
                _turret.rotation,
                targetRotation,
                _turretRotationSpeed * Time.deltaTime);
        }

        private bool IsFortressInAttackRange()
        {
            if (Fortress == null) return false;
            if (!IsInCameraRange(_camera, _attackVisibilityMargin)) return false;

            Vector2 myPos = transform.position;
            Vector2 fortressPos = Fortress.transform.position;

            return Vector2.Distance(myPos, fortressPos) <= _effectiveAttackDistance
                && !EnemyPathfinder.IsBlocked(myPos, fortressPos, _wallMask);
        }

        private void UpdateNavigation()
        {
            if (Fortress == null) return;

            // Поки поточний шлях/ціль-стіна ще дійсні - тримаємось за них.
            // Throw-точка фіксована при розрахунку шляху; перерахунок щокадру
            // змушував би її "тікати" разом з рухом ворога і викликав би розвороти.
            if (_currentPath != null || _targetWall != null) return;

            RecalculatePath();
        }

        private const int MaxPathExhaustedRetries = 2;

        private void RecalculatePath()
        {
            Vector2 myPos = transform.position;
            Vector2 fortressPos = Fortress.transform.position;
            float directDistance = Vector2.Distance(myPos, fortressPos);

            bool forceWallBreak = _pathExhaustedCount >= MaxPathExhaustedRetries;
            bool isDirectPathBlocked = EnemyPathfinder.IsBlocked(myPos, fortressPos, _wallMask);

            if (!forceWallBreak && EnemyPathfinder.TryFindPath(myPos, fortressPos, _effectiveAttackDistance, _wallMask, out List<Vector2> path, out float pathLength))
            {
                // Ворог, що "за характером" волить обходити стіни, приймає знайдений
                // обхід незалежно від його довжини - інакше ліміт _maxDetourMultiplier
                // майже завжди відкидав би обхід як "занадто довгий" ще до того, як
                // дійде до вибору між обходом і ламанням стіни.
                bool detourAcceptable = !isDirectPathBlocked
                    || !_prefersWallBreak
                    || pathLength <= directDistance * _maxDetourMultiplier;

                if (detourAcceptable)
                {
                    _currentPath = path;
                    _currentWaypointIndex = 0;
                    _targetWall = null;
                    _pathExhaustedCount = 0;
                    return;
                }
            }

            // Немає прийнятного шляху (або попередні спроби вже вичерпувались без атаки) - йдемо ламати найближчу стіну, що заважає
            Wall blockingWall = EnemyPathfinder.FindNearestBlockingWall(myPos, fortressPos, _wallMask);
            if (blockingWall != null)
            {
                _targetWall = blockingWall;
                _currentPath = null;
                _pathExhaustedCount = 0;
                return;
            }

            // Заважаючої стіни не знайдено (малоймовірно, якщо шлях заблокований) - йдемо напряму
            Vector2 fallbackThrowPosition = fortressPos + (myPos - fortressPos).normalized * _effectiveAttackDistance;
            _currentPath = new List<Vector2> { myPos, fallbackThrowPosition };
            _currentWaypointIndex = 0;
            _targetWall = null;
        }

        private void MoveTowardsCurrentTarget()
        {
            Vector2 myPos = transform.position;

            if (_targetWall != null)
            {
                Vector2 wallTarget = Vector2.Lerp(_targetWall.NodeAPosition, _targetWall.NodeBPosition, 0.5f);
                bool inAttackDistance = Vector2.Distance(myPos, wallTarget) <= _effectiveAttackDistance;

                if (inAttackDistance && !IsInCameraRange(_camera, _attackVisibilityMargin))
                {
                    // Дійшов на AttackDistance до стіни, але поза кадром - йдемо
                    // ближче, повз точку атаки, у бік самої стіни, поки не
                    // потрапимо у видиму зону. wallTarget - це вже edge-точка на
                    // самій стіні, тож лінія до неї природно "блокується" стіною-
                    // ціллю; перевіряти тут IsBlocked нема сенсу (завжди true).
                    MoveTowards(wallTarget);
                    return;
                }

                MoveTowards(wallTarget);

                if (inAttackDistance && IsInCameraRange(_camera, _attackVisibilityMargin))
                {
                    isReachTower = true;
                    _attackTargetPosition = wallTarget;
                }
                return;
            }

            if (_currentPath == null || _currentWaypointIndex >= _currentPath.Count)
            {
                _currentPath = null;
                _pathExhaustedCount++;
                return;
            }

            bool isFinalWaypoint = _currentWaypointIndex == _currentPath.Count - 1;
            Vector2 waypoint = _currentPath[_currentWaypointIndex];

            bool reachedThrowPoint = isFinalWaypoint && Vector2.Distance(myPos, waypoint) <= _waypointReachDistance;

            // Дійшов до throw-точки, але поза кадром - атакувати поза камерою
            // заборонено, тож йдемо ще ближче (за AttackDistance) до фортеці,
            // поки не потрапимо у видиму зону. Рухаємось тільки якщо лінія до
            // фортеці вільна - інакше лишаємось на waypoint і чекаємо на
            // звичний stuck-таймер нижче, щоб не застрягти назавжди.
            if (reachedThrowPoint && !IsInCameraRange(_camera, _attackVisibilityMargin)
                && !EnemyPathfinder.IsBlocked(myPos, Fortress.transform.position, _wallMask))
            {
                MoveTowards(Fortress.transform.position);
            }
            else
            {
                MoveTowards(waypoint);
            }

            // Остання точка шляху - це throw-точка на AttackDistance від фортеці.
            // Її "досягнутість" перевіряємо через реальну відстань до фортеці
            // (IsFortressInAttackRange у Update), а не через _waypointReachDistance,
            // інакше ворог зупиняється за 0.3 од. до цілі, шлях перераховується
            // з трохи іншим кутом підходу щокадру, і ворога хаотично розвертає.
            if (isFinalWaypoint)
            {
                if (reachedThrowPoint)
                {
                    // Дійшов до throw-точки, але Update ще не підтвердив атаку -
                    // ймовірно лінія до фортеці звідси заблокована, або ворог
                    // ще не потрапив у видиму зону камери. Форсуємо перерахунок
                    // шляху після короткого таймауту, щоб не застрягти.
                    _stuckAtFinalWaypointTimer += Time.deltaTime;
                    if (_stuckAtFinalWaypointTimer >= 0.3f)
                    {
                        _currentPath = null;
                        _pathExhaustedCount++;
                        _stuckAtFinalWaypointTimer = 0f;
                    }
                }
                else
                {
                    _stuckAtFinalWaypointTimer = 0f;
                }
                return;
            }

            if (Vector2.Distance(myPos, waypoint) <= _waypointReachDistance)
            {
                _currentWaypointIndex++;
            }
        }

        private const float MinTurnDistance = 0.05f;

        private void MoveTowards(Vector2 target)
        {
            Vector2 myPos = transform.position;
            Vector2 toTarget = target - myPos;

            float distanceToTarget = toTarget.magnitude;
            if (distanceToTarget < 0.0001f) return;

            Vector2 moveDir = toTarget / distanceToTarget;

            // Не перевертаємо спрайт на мікроскопічних залишкових дистанціях
            // (уникаємо дрижання напрямку), але сам рух до цілі не зупиняємо -
            // інакше ворог застряг би за MinTurnDistance до throw-точки і
            // ніколи не потрапив би у справжній радіус атаки.
            if (distanceToTarget >= MinTurnDistance)
            {
                direction = moveDir;
                transform.right = moveDir;
            }

            float step = speed * Time.deltaTime;
            if (step > distanceToTarget) step = distanceToTarget;

            transform.position += (Vector3)(moveDir * step);
        }

        private void UpdateAnimator()
        {
            if (_animator == null) return;
            _animator.SetBool(_runParameter, !isReachTower);
        }

        private void SetFiring(bool isFiring)
        {
            if (_animator == null) return;
            _animator.SetBool(_fireParameter, isFiring);
        }

        protected override void OnMouseDown()
        {
            if (isDead)
                return;

            if (GameStateManager.Instance.CurrentState != GameState.Playing)
                return;

            // НЕ кличемо base.OnMouseDown(): він одразу робить Collect(), тобто
            // вбиває повз HP. Клік має знімати стільки ж, скільки MouseBomber.
            WasStricken(null, _clickDamage);
        }

        private void CheckForDropBomb()
        {
            if (!isReachTower || isGrenadeInFlight) return;

            Vector3 targetPosition = _targetWall != null
                ? (Vector3)Vector2.Lerp(_targetWall.NodeAPosition, _targetWall.NodeBPosition, 0.5f)
                : Fortress.transform.position;

            float distToTarget = Vector3.Distance(transform.position, targetPosition);
            if (distToTarget > AttackDistance)
            {
                isReachTower = false;
                return;
            }

            _attackTargetPosition = targetPosition;

            if (!IsInCameraRange(_camera, _attackVisibilityMargin)) return;

            float currentDelay = hasAttackedOnce ? attackDelay : firstAttackDelay;
            if (timeSinceLastAttack >= currentDelay)
            {
                ThrowGrenadeAtTarget();
                timeSinceLastAttack = 0f;
            }
            else
            {
                timeSinceLastAttack += Time.deltaTime;
            }
        }

        private void ThrowGrenadeAtTarget()
        {
            isGrenadeInFlight = true;
            hasAttackedOnce = true;
            SetFiring(true);

            StartCoroutine(AttackRoutine());
        }

        private IEnumerator AttackRoutine()
        {
            yield return new WaitForSeconds(_attackAnimationDuration);

            if (isDead) yield break;

            SetFiring(false);
            SoundManager.Instance.Play(SoundId.EnemyGrenadeThrow, transform.position);

            bool isWallTarget = _targetWall != null;
            Wall wallTarget = _targetWall;
            Vector3 impactPosition = GetRandomPositionAround(_attackTargetPosition, 0.4f);
            var grenade = Instantiate(_grenadePrefab, transform.position, Quaternion.identity);
            grenade.Launch(impactPosition, () => CreateExplosionAt(impactPosition, wallTarget, isWallTarget));
        }

        private void CreateExplosionAt(Vector3 impactPosition, Wall wallTarget, bool isWallTarget)
        {
            isGrenadeInFlight = false;
            Instantiate(_explosionPrefab, impactPosition, Quaternion.identity);
            SoundManager.Instance.Play(SoundId.EnemyGrenadeExplosion, impactPosition);

            if (isWallTarget)
            {
                if (wallTarget != null)
                {
                    wallTarget.TakeDamage(damage);
                }
                _targetWall = null;
                isReachTower = false;
            }
            else
            {
                _fortress.GetComponentInParent<IDamageable>().TakeDamage(damage);
            }
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
            SpawnBloodSplat();

            _activeEnemieslistReference.Remove(this);

            // Нагороду видаємо тут, а не через base.Collect(): той одразу кличе
            // Finish() і вимикає об'єкт, після чого корутини смерті вже не
            // стартують ("game object is inactive"). Коли саме тіло піде в пул,
            // вирішує StartFalling залежно від DeathStyle.
            MoneyManager.Instance.AddMoney(moneyValue);

            StartFalling();
        }

        /// <summary>Як ворог поводиться після смерті.</summary>
        public enum DeathStyle
        {
            /// <summary>Зникає одразу - гуманоїди.</summary>
            Instant,

            /// <summary>Зупиняється на місці, лежить остовом і згасає - танки.</summary>
            Wreck,

            /// <summary>Відлітає за екран із обертанням - літаки.</summary>
            Fall,
        }

        private void StartFalling()
        {
            isDead = true;

            switch (_deathStyle)
            {
                case DeathStyle.Wreck:
                    StartWreck();
                    LaunchTurret();
                    break;

                case DeathStyle.Fall:
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.gravityScale = crashGravity;
                    rb.linearVelocity = direction * speed;
                    rb.angularVelocity = crashRotation;
                    LaunchTurret();
                    break;

                default:
                    // Instant: тіло зникає цього ж кадру, анімації немає.
                    // Башту збирати не треба - вона й не відлітала.
                    FinishAndRestoreTurret();
                    break;
            }
        }

        /// <summary>
        /// Корпус зупиняється там, де його підбили, лежить _wreckLifetime секунд,
        /// потім згасає і йде в пул.
        /// </summary>
        private void StartWreck()
        {
            // Кінематичний, щоб не з'їжджав і не штовхався з живими ворогами.
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;

            // Остов не має перекривати шлях і ловити постріли.
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            StartCoroutine(WreckRoutine());
        }

        private IEnumerator WreckRoutine()
        {
            SetWreckTint();

            yield return new WaitForSeconds(_wreckLifetime);

            // Плавно згасає, щоб не блимнув і зник.
            float elapsed = 0f;
            while (elapsed < _wreckFadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / _wreckFadeDuration);
                SetSpritesAlpha(alpha);
                yield return null;
            }

            FinishAndRestoreTurret();
        }

        /// <summary>Пригашує остов, щоб його не плутали з живим танком.</summary>
        private void SetWreckTint()
        {
            EnsureSpriteCacheBuilt();

            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] == null) continue;

                Color c = _originalSpriteColors[i] * _wreckTint;
                c.a = _originalSpriteColors[i].a;
                _spriteRenderers[i].color = c;
            }
        }

        private void SetSpritesAlpha(float alpha)
        {
            if (_spriteRenderers == null) return;

            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] == null) continue;

                Color c = _spriteRenderers[i].color;
                c.a = alpha;
                _spriteRenderers[i].color = c;
            }
        }

        /// <summary>
        /// Відриває башту від корпуса і підкидає її вгору. Працює лише якщо в
        /// префабі призначено _turret (танки Enemy 4/5), решта ворогів гине як є.
        /// </summary>
        private void LaunchTurret()
        {
            if (_turret == null) return;
            if (_turretLaunched) return;

            _turretLaunched = true;

            // Куди повертати башту - уже знято в Awake, поки вона була на місці.

            // Від'єднуємо: інакше башта літала б разом із корпусом.
            _turret.SetParent(null, worldPositionStays: true);

            var turretRb = _turret.GetComponent<Rigidbody2D>();
            if (turretRb == null) turretRb = _turret.gameObject.AddComponent<Rigidbody2D>();

            turretRb.bodyType = RigidbodyType2D.Dynamic;
            turretRb.gravityScale = _turretGravity;

            // Підкидаємо вгору з невеликим випадковим нахилом, щоб два танки
            // поруч не викидали башти однаково.
            Vector2 launchDirection = new Vector2(
                Random.Range(-_turretSideSpread, _turretSideSpread),
                1f).normalized;

            turretRb.linearVelocity = launchDirection * _turretLaunchForce;
            turretRb.angularVelocity = Random.Range(-_turretSpin, _turretSpin);

            StartCoroutine(ReturnTurretAfter(_turretLifetime));
        }

        private IEnumerator ReturnTurretAfter(float delay)
        {
            yield return new WaitForSeconds(delay);

            RestoreTurret();
        }

        /// <summary>
        /// Повертає башту на місце. Обов'язково перед поверненням у пул - інакше
        /// наступний танк із цього пула вилізе без башти, а стара лишиться лежати
        /// посеред сцени.
        /// </summary>
        private void RestoreTurret()
        {
            if (!_turretLaunched || _turret == null) return;

            var turretRb = _turret.GetComponent<Rigidbody2D>();
            if (turretRb != null)
            {
                turretRb.linearVelocity = Vector2.zero;
                turretRb.angularVelocity = 0f;
                turretRb.bodyType = RigidbodyType2D.Kinematic;
            }

            _turret.SetParent(_turretParent, worldPositionStays: false);
            _turret.localPosition = _turretLocalPosition;
            _turret.localRotation = _turretLocalRotation;
            _turret.localScale = _turretLocalScale;
            _turret.gameObject.SetActive(true);

            _turretLaunched = false;
        }

        private bool _turretLaunched;
        private Transform _turretParent;
        private Vector3 _turretLocalPosition;
        private Quaternion _turretLocalRotation;
        private Vector3 _turretLocalScale;

        /// <summary>
        /// Повертає ворога в пул. Обгортка над ClickableItem.Finish(), яка спершу
        /// збирає башту: Unity забороняє SetParent під час SetActive, тож робити
        /// це в OnDisable не можна - там уже почалась деактивація.
        /// </summary>
        private void FinishAndRestoreTurret()
        {
            // Повертаємо в пул рівно один раз за життя. Шляхів кілька (WreckRoutine,
            // OnBecameInvisible, ReturnInPoolIfNotVisible), і другий виклик віддав би
            // у пул ворога, якого спавнер уже дістав звідти й запустив у бій - той
            // "зникав би і з'являвся" вже полагодженим.
            if (_isReturnedToPool) return;

            _isReturnedToPool = true;

            RestoreTurret();
            Finish();
        }

        private bool _isReturnedToPool;

        private void OnBecameInvisible()
        {
            // Тільки Fall прибирається за вильотом з кадру. Остов має власний
            // таймер, а Instant уже пішов у пул одразу після смерті.
            if (_deathStyle != DeathStyle.Fall) return;

            if (isDead)
            {
                FinishAndRestoreTurret();
            }
        }

        /// <param name="hitSourcePosition">
        /// Звідки прилетів удар. Кров розлітається за вектором від цієї точки
        /// до ворога. null = удар згори (постріл мишкою), тоді кров лишає
        /// симетричну кляксу без напрямку.
        /// </param>
        /// <param name="damage">
        /// Скільки HP зняти. За замовчуванням 1 - один постріл або клік.
        /// </param>
        public void WasStricken(Vector2? hitSourcePosition = null, int damage = 1)
        {
            if (isDead) return;

            // Джерело запам'ятовуємо на кожному влучанні, а не лише на смертельному:
            // кров має розлітатись за напрямком саме того пострілу, що добив.
            _lastHitSource = hitSourcePosition;

            _currentHP -= Mathf.Max(1, damage);

            if (_currentHP > 0)
            {
                PlayHitFlash();
                return;
            }

            isDead = true;

            Collect();
        }

        // Джерело удару, що вбив ворога. null = удар був згори і напрямку
        // розльоту не має (клік мишкою, або смерть не від пострілу взагалі).
        private Vector2? _lastHitSource;

        private SpriteRenderer[] _spriteRenderers;
        private Color[] _originalSpriteColors;
        private Coroutine _hitFlashRoutine;

        /// <summary>
        /// Червоний спалах, коли ворог отримав влучання, але вижив - без нього
        /// незрозуміло, чи постріл узагалі влучив.
        /// </summary>
        private void PlayHitFlash()
        {
            EnsureSpriteCacheBuilt();

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

        /// <summary>
        /// Кешує спрайти й початкові кольори. Кеш будується ДО того, як башта
        /// відлітає, тож у ньому лишається і її рендерер - при поверненні кольору
        /// це коректно, бо башта повертається на корпус.
        /// </summary>
        private void EnsureSpriteCacheBuilt()
        {
            if (_spriteRenderers != null) return;

            _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            _originalSpriteColors = new Color[_spriteRenderers.Length];
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                _originalSpriteColors[i] = _spriteRenderers[i].color;
            }
        }

        private void ResetSpriteColors()
        {
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] != null) _spriteRenderers[i].color = _originalSpriteColors[i];
            }
        }

        private void SpawnBloodSplat()
        {
            if (!_leavesBloodTrail) return;
            if (BloodDecalSystem.Instance == null) return;

            Vector2 myPos = transform.position;

            if (!_lastHitSource.HasValue)
            {
                // Стріляли ніби згори - кров розбризкується рівномірно навколо
                BloodDecalSystem.Instance.PaintBlob(myPos, _bloodSplatScale);
                return;
            }

            // Кров летить далі в тому ж напрямку, у якому рухався снаряд:
            // від джерела пострілу крізь ворога.
            BloodDecalSystem.Instance.PaintSplat(myPos, myPos - _lastHitSource.Value, _bloodSplatScale);
        }

        private void PlayFallingAudio()
        {
            SoundManager.Instance.Play(SoundId.EnemyFalling, transform.position);

            if (Random.Range(0, 3) != 0) return;

            SoundManager.Instance.Play(SoundId.EnemyVoice, transform.position);
        }

        private IEnumerator ReturnInPoolIfNotVisible()
        {
            yield return new WaitForSeconds(1f);
            yield return new WaitUntil(() => !IsInCameraRange(_camera));
            yield return new WaitForSeconds(1f);

            // Остов прибирає WreckRoutine після власного таймера, Instant іде в
            // пул одразу - тут страхуємо лише тіла, що відлітають.
            if (_deathStyle != DeathStyle.Fall) yield break;

            if (isDead)
            {
                FinishAndRestoreTurret();
            }
        }

        [SerializeField, Range(0f, 0.49f)] private float _attackVisibilityMargin = 0.05f;

        private bool IsInCameraRange(Camera camera, float viewportMargin = 0f)
        {
            Vector3 viewportPos = camera.WorldToViewportPoint(transform.position);

            return viewportPos.x >= viewportMargin && viewportPos.x <= 1f - viewportMargin &&
                   viewportPos.y >= viewportMargin && viewportPos.y <= 1f - viewportMargin &&
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
        [Header("Debug")]
        [SerializeField] private bool _debugDrawNavigation;

        private void OnDrawGizmos()
        {
            if (!_debugDrawNavigation) return;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + direction);

            Gizmos.color = isReachTower ? new Color(0f, 1f, 0f, 0.4f) : new Color(1f, 1f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, _effectiveAttackDistance);

            if (_currentPath != null)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < _currentPath.Count - 1; i++)
                {
                    Gizmos.DrawLine(_currentPath[i], _currentPath[i + 1]);
                    Gizmos.DrawWireSphere(_currentPath[i], 0.15f);
                }
                Gizmos.DrawWireSphere(_currentPath[_currentPath.Count - 1], 0.15f);

                if (_currentWaypointIndex < _currentPath.Count)
                {
                    Gizmos.color = Color.blue;
                    Vector2 currentTarget = _currentPath[_currentWaypointIndex];
                    Gizmos.DrawLine(transform.position, currentTarget);
                    Gizmos.DrawWireSphere(currentTarget, _waypointReachDistance);
                }
            }

            if (_targetWall != null)
            {
                Gizmos.color = Color.magenta;
                Vector2 wallMid = Vector2.Lerp(_targetWall.NodeAPosition, _targetWall.NodeBPosition, 0.5f);
                Gizmos.DrawLine(transform.position, wallMid);
                Gizmos.DrawWireSphere(wallMid, 0.2f);
            }

            if (isReachTower)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_attackTargetPosition, 0.25f);
            }

            List<Vector2> navNodes = EnemyPathfinder.GetDebugNavNodes();
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
            foreach (var node in navNodes)
            {
                Gizmos.DrawWireSphere(node, 0.12f);
            }
        }
    }
}
