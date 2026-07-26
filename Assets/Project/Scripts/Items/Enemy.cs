using UnityEngine;
using Managers;
using Managers.Audio;
using System.Collections;
using System.Collections.Generic;
using Towers;
using Towers.Buildings;

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
        }

        private void OnEnable()
        {
            isDead = false;
            isGrenadeInFlight = false;
            isReachTower = false;
            hasAttackedOnce = false;
            timeSinceLastAttack = 0f;

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

            base.OnMouseDown();
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
            SoundManager.Instance.Play(SoundId.EnemyFalling, transform.position);

            if (Random.Range(0, 3) != 0) return;

            SoundManager.Instance.Play(SoundId.EnemyVoice, transform.position);
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
