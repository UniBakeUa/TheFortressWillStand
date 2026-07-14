using System;
using System.Collections.Generic;
using Money;
using Towers;
using UnityEngine;

namespace Building
{
    [System.Serializable]
    public struct StructureData
    {
        public int id;
        public int cost;
        public GameObject ghostPrefab;
        public GameObject realPrefab;
    }

    public class BuildManager : MonoBehaviour
    {
        public static BuildManager Instance { get; private set; }
        [SerializeField] List<StructureData> structureLibrary;
        [SerializeField] private int wallCost;
        [SerializeField] private LineRenderer _lineRenderer;

        [SerializeField] GameObject wallGhostPrefab;
        [SerializeField] GameObject wallPrefab;
        [SerializeField] FortificationGraph graph;
        [SerializeField] WallStrengthCalculator strengthCalc;

        [Header("Налаштування")]
        [SerializeField] private float snapRadius = 2f;
        [SerializeField] private float minBuildX = -10f;
        [SerializeField] private float maxBuildX = 10f;
        [SerializeField] private float minBuildY = -10f;
        [SerializeField] private float maxBuildY = 10f;
        [SerializeField] Color allowedGhostColor = Color.white;
        [SerializeField] Color forbiddenGhostColor = new Color(1f, 0.3f, 0.3f, 0.6f);
        [SerializeField] private LayerMask wallmask;
        private int _currentSelectionId = 0;
        private GameObject _towerGhost;
        private GameObject _wallGhost;
        private LineRenderer _wallGhostLR;
        private GameObject _currentRealPrefab;
        private IWallConnectable _firstSelectedTower;
        private MoneyManager _moneyManager;
        private bool _isRepairMode = false;
        private Rect _buildZone;

        public void SetRepairMode(bool active)
        {
            _isRepairMode = active;
            if (active) DeselectStructure(); // Вимикаємо будівництво, якщо включили ремонт
            Debug.Log($"Repair mode: {active}");
        }

        private void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            DrawZone();
            UpdateSelectionPrefabs();
            _wallGhost = Instantiate(wallGhostPrefab,transform);
            _wallGhost.SetActive(false);
            _wallGhostLR = _wallGhost.GetComponent<LineRenderer>();

            _buildZone = new Rect(minBuildX, minBuildY, maxBuildX - minBuildX, maxBuildY - minBuildY);

            _moneyManager = MoneyManager.Instance;
            GameStateManager.Instance.OnStateChange += ToggleBuildMode;
        }

        public void SelectStructure(int id)
        {
            SetRepairMode(false);
            
            if (_moneyManager.GetMoney() < GetCurrentStructureCost(id))
            {
                _moneyManager.ShowNotEnoughPopup();
                _currentSelectionId = -1;
                DeselectStructure();
                return;
            }

            _currentSelectionId = id;
            UpdateSelectionPrefabs();
        }

        private void DrawZone()
        {
            Vector3[] corners = new Vector3[4];
            corners[0] = new Vector2(minBuildX, minBuildY);
            corners[1] = new Vector2(maxBuildX, minBuildY);
            corners[2] = new Vector2(maxBuildX, maxBuildY);
            corners[3] = new Vector2(minBuildX, maxBuildY);

            _lineRenderer.SetPositions(corners);
        }

        private void UpdateSelectionPrefabs()
        {
            if (_currentSelectionId < 0) return;

            var data = structureLibrary.Find(s => s.id == _currentSelectionId);
            if (data.realPrefab == null) return;

            if (_towerGhost != null)
            {
                Destroy(_towerGhost);
                _towerGhost = null;
            }

            _towerGhost = Instantiate(data.ghostPrefab,transform);
            _towerGhost.SetActive(false);
            _currentRealPrefab = data.realPrefab;
        }

        private void Update()
        {
            if (GameStateManager.Instance.CurrentState != GameState.Building)
            {
                if (_towerGhost) _towerGhost.SetActive(false);
                if (_wallGhost) _wallGhost.SetActive(false);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Mouse1))
            {
                ResetSelection();
                return;
            }

            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            UpdateGhosts(mousePos);

            if (Input.GetMouseButtonDown(0)) HandleClick(mousePos);
        }
        private void RepairStructure(Solid target)
        {
            // Знаходимо ціну "нової" такої ж структури
            // Примітка: це працює, якщо ID можна дізнатися через компонент структури
            int originalCost = GetCostByPrefab(target); 
            int repairCost = Mathf.CeilToInt(originalCost * 0.7f);

            if (_moneyManager.GetMoney() >= repairCost)
            {
                _moneyManager.SpendMoney(repairCost);
                target.Model.CurrentHP = target.Model.MaxHP; // Повний ремонт
                Debug.Log("Відремонтовано!");
            }
            else
            {
                _moneyManager.ShowNotEnoughPopup();
            }
        }

        private int GetCostByPrefab(Solid solid)
        {
            // Отримуємо ID з самого префабу (наприклад, через новий компонент або скрипт структури)
            // Якщо у вас немає окремого скрипта ID, можна шукати через ім'я або іншу ознаку.
            // Припустимо, ви додали public int StructureId в Tower.cs / Wall.cs
    
            int id = solid.StructureId;
            
            // Шукаємо ціну в бібліотеці
            var data = structureLibrary.Find(s => s.id == id);
            return data.id == id ? data.cost : 100; // 100 як дефолтне значення
        }

        private void HandleClick(Vector2 pos)
        {
            if (_moneyManager == null) _moneyManager = MoneyManager.Instance;

            bool isShiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // 1. Спочатку перевіряємо, чи ми не примагнітились до існуючої вежі
            Transform snappedTowerTransform = GetNearbyMatchingTower(pos, snapRadius);
            IWallConnectable clickedTower = null;

            if (snappedTowerTransform != null)
            {
                // Якщо примагнітились, підміняємо ціль на цю вежу
                clickedTower = snappedTowerTransform.GetComponent<IWallConnectable>();
            }
            else
            {
                // Якщо поруч нічого немає, шукаємо стандартним способом під курсором
                clickedTower = GetTowerAt(pos);
            }

            // 2. Логіка ремонту
            if (_isRepairMode)
            {
                // Для ремонту можна залишити точний клік, або теж використовувати магніт
                IWallConnectable clickedForRepair = GetTowerAt(pos);
                if (clickedForRepair is Solid solid && solid.Model.CurrentHP < solid.Model.MaxHP)
                {
                    RepairStructure(solid);
                }
                return;
            }

            // 3. Сценарій: Початок будівництва (обрання першої вежі)
            if (_firstSelectedTower == null)
            {
                if (clickedTower != null)
                {
                    if (IsCurrentSelectionWallConnectable())
                    {
                        _firstSelectedTower = clickedTower;
                    }
                    return;
                }

                if (!IsWithinBuildableZone(pos)) return;

                if (IsOccupied(pos))
                {
                    Debug.Log("Тут не можна будувати: місце зайняте!");
                    return;
                }

                int cost = GetCurrentStructureCost(_currentSelectionId);
                if (_moneyManager.GetMoney() < cost)
                {
                    _moneyManager.ShowNotEnoughPopup();
                    return;
                }

                _moneyManager.SpendMoney(cost);
                _firstSelectedTower = CreateStructure(pos);
                return;
            }

            // 4. Сценарій: Будуємо стіну до ІСНУЮЧОЇ вежі (сюди ми потрапляємо завдяки магніту)
            if (clickedTower != null && clickedTower != _firstSelectedTower)
            {
                BuildWall(_firstSelectedTower, clickedTower);
                _firstSelectedTower = isShiftPressed ? clickedTower : null;
                return;
            }

            // 5. Сценарій: Клік на порожнє місце - будуємо НОВУ вежу і стіну
            if (clickedTower == null && IsWithinBuildableZone(pos))
            {
                if (IsOccupied(pos))
                {
                    Debug.Log("Тут не можна будувати: місце зайняте!");
                    return;
                }

                int towerCost = GetCurrentStructureCost(_currentSelectionId);

                if (_moneyManager.GetMoney() >= (towerCost + wallCost))
                {
                    _moneyManager.SpendMoney(towerCost);

                    IWallConnectable newTower = CreateStructure(pos);
                    if (newTower != null)
                    {
                        BuildWall(_firstSelectedTower, newTower);
                        _firstSelectedTower = isShiftPressed ? newTower : null;
                    }
                }
                else
                {
                    _moneyManager.ShowNotEnoughPopup();
                }
            }
        }

        private bool IsOccupied(Vector2 pos)
        {
            // ЄДИНА перевірка через IDamageable покриває Tower/Wall/Drill і БУДЬ-ЯКИЙ
            // майбутній тип Solid-структури автоматично - не треба щоразу дописувати
            // нові типи в цей список вручну, як було раніше (Tower/Wall/Drill окремо)
            Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 0.3f);
            foreach (var hit in hits)
            {
                if (hit.GetComponentInParent<IDamageable>() != null) return true;
            }
            return false;
        }

        private Transform GetNearbyMatchingTower(Vector2 pos, float radius)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(pos, radius);
            Transform closestMatch = null;
            float minDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                // Шукаємо компонент, що містить StructureId
                Solid solid = hit.GetComponentInParent<Solid>();

                // Перевіряємо, чи це та сама структура, яку ми зараз обрали
                if (solid != null && solid.StructureId == _currentSelectionId && solid.IsSlipable)
                {
                    float dist = Vector2.Distance(pos, solid.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestMatch = solid.transform;
                    }
                }
            }
            return closestMatch;
        }

        public void DeselectStructure()
        {
            _currentSelectionId = -1;
            if (_towerGhost != null) _towerGhost.SetActive(false);
            if (_wallGhost != null) _wallGhost.SetActive(false);
            _currentRealPrefab = null;
        }

        private int GetCurrentStructureCost(int ID)
        {
            if (ID < 0) return 999999;
            var data = structureLibrary.Find(s => s.id == ID);
            return data.id == ID ? data.cost : 999999;
        }

        private IWallConnectable CreateStructure(Vector2 pos)
        {
            GameObject go = Instantiate(_currentRealPrefab, pos, Quaternion.identity,transform);
            IWallConnectable t = go.GetComponent<IWallConnectable>();

            t?.EnsureNodeInitialized();

            if (go.TryGetComponent(out MonoBehaviour anim))
                anim.SendMessage("PlaySpawnAnimation", 0.5f, SendMessageOptions.DontRequireReceiver);

            return t;
        }
        public int GetCostById(int id)
        {
            var data = structureLibrary.Find(s => s.id == id);
            return (data.id == id) ? data.cost : 999999;
        }
        private void BuildWall(IWallConnectable a, IWallConnectable b)
        {
            if (a == null || b == null) return;
            if (IsWallIntersecting(a.Transform.position, b.Transform.position)) return;
            
            if (_moneyManager.GetMoney() < wallCost)
            {
                _moneyManager.ShowNotEnoughPopup();
                return;
            }

            a.EnsureNodeInitialized();
            b.EnsureNodeInitialized();

            if (a.Node == null || b.Node == null) return;
            if (graph.HasWall(a.Node, b.Node)) return;

            _moneyManager.SpendMoney(wallCost);

            Wall wall = Instantiate(wallPrefab,transform).GetComponent<Wall>();
            wall.Init(a.Node, b.Node, graph, strengthCalc);
        }

        private void UpdateGhosts(Vector2 mousePos)
        {
            Transform snappedTower = GetNearbyMatchingTower(mousePos, snapRadius);
            bool isSnapped = snappedTower != null;

            Vector2 ghostPos = isSnapped ? (Vector2)snappedTower.position : mousePos;

            bool canBuild;
            if (isSnapped)
            {
                canBuild = true;
            }
            else
            {
                canBuild = IsWithinBuildableZone(ghostPos) && !IsOccupied(ghostPos);
            }

            if (_wallGhost.activeSelf && _firstSelectedTower != null)
            {
                if (IsWallIntersecting(_firstSelectedTower.Transform.position, ghostPos))
                    canBuild = false;
            }

            if (_towerGhost != null)
            {
                _towerGhost.SetActive(_currentSelectionId >= 0);
                _towerGhost.transform.position = ghostPos;
            }

            if (_wallGhost != null)
            {
                if (_firstSelectedTower != null)
                {
                    _wallGhost.SetActive(true);
                    _wallGhostLR.SetPosition(0, _firstSelectedTower.Transform.position);
                    _wallGhostLR.SetPosition(1, ghostPos);
                    TintGhost(_towerGhost, _wallGhost, canBuild);
                }
                else
                {
                    _wallGhost.SetActive(false);
                    TintGhost(_towerGhost, null, canBuild);
                }
            }
        }
        private bool IsWallIntersecting(Vector2 start, Vector2 end)
        {
            // Відступ від кожної вежі (щоб прямокутник не зачіпав самі вежі)
            float padding = 0.8f; 
            float totalDistance = Vector2.Distance(start, end);
    
            // Якщо стіна занадто коротка, перевірка не потрібна або її треба обмежити
            if (totalDistance <= padding * 2) return false;

            // Розраховуємо нову довжину
            float checkedDistance = totalDistance - (padding * 2);
            Vector2 direction = (end - start).normalized;
    
            // Новий центр зміщується від початкової точки на padding + половину нової довжини
            Vector2 center = start + direction * (padding + checkedDistance / 2f);
    
            Vector2 size = new Vector2(0.2f, checkedDistance);
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg - 90f;

            Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, angle, wallmask);
    
            // Виключаємо саму себе, якщо раптом знайдемо (наприклад, якщо стіна вже існує)
            return hits.Length > 0;
        }
        private void TintGhost(GameObject tGhost, GameObject wGhost, bool buildable)
        {
            Color c = buildable ? allowedGhostColor : forbiddenGhostColor;
            if (tGhost != null)
            {
                var sr = tGhost.GetComponentInChildren<SpriteRenderer>();
                if (sr) sr.color = c;
            }
            if (wGhost && _wallGhostLR)
            {
                _wallGhostLR.startColor = c;
                _wallGhostLR.endColor = c;
            }
        }

        bool IsWithinBuildableZone(Vector2 pos) => _buildZone.Contains(pos);

        // Drill (і будь-яка інша не-веже-подібна структура) не реалізує IWallConnectable -
        // тому клік по існуючій вежі не повинен починати wall-drag, якщо зараз обрано таке
        bool IsCurrentSelectionWallConnectable() =>
            _currentRealPrefab != null && _currentRealPrefab.GetComponent<IWallConnectable>() != null;

        private IWallConnectable GetTowerAt(Vector2 pos)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 0.3f);
            foreach (var hit in hits)
            {
                IWallConnectable t = hit.GetComponentInParent<IWallConnectable>();
                if (t != null) return t;
            }
            return null;
        }

        private void ResetSelection()
        {
            _firstSelectedTower = null;
            if (_towerGhost) _towerGhost.SetActive(false);
            if (_wallGhost) _wallGhost.SetActive(false);
    
            // Додаємо сюди:
            SetRepairMode(false); 
        }

        private void ToggleBuildMode(GameState state)
        {
            if (state != GameState.Building) ResetSelection();
        }
    }
}