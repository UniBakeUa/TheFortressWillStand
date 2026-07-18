using NUnit.Framework;
using System.Collections.Generic;
using Towers;
using Towers.Buildings;
using Towers.Data;
using Towers.ScriptableObjects;
using UnityEngine;

namespace Managers
{
    public class BuildManager : MonoBehaviour
    {
        public static BuildManager Instance { get; private set; }

        [Header("База даних будівель")]
        [SerializeField] private BuildingLibrary _buildingLibrary;

        [Header("Налаштування стін")]
        [SerializeField] private FortificationGraph graph;
        [SerializeField] private WallStrengthCalculator strengthCalc;

        [Header("Налаштування будівництва")]
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private List<BuildingAreaConfig> _buildingAreaConfigList;
        [SerializeField] private Color allowedGhostColor = Color.white;
        [SerializeField] private Color forbiddenGhostColor = new Color(1f, 0.3f, 0.3f, 0.6f);
        [SerializeField] private LayerMask wallmask;

        private int _currentSelectionId = -1;
        private BuildingConfig _currentConfig;
        private BuildingAreaConfig _buildingAreaConfig;

        private GameObject _towerGhost;
        private GameObject _wallGhost;
        private LineRenderer _wallGhostLR;

        private ITower _firstSelectedTower;
        private MoneyManager _moneyManager;
        private bool _isRepairMode = false;
        private Rect _buildZone;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            InitBuildingArea(_buildingAreaConfigList[0]);
            DrawZone();

            _buildZone = new Rect(
                _buildingAreaConfig.MinBuildX, _buildingAreaConfig.MinBuildY, 
                _buildingAreaConfig.MaxBuildX - _buildingAreaConfig.MinBuildX,
                _buildingAreaConfig.MaxBuildY - _buildingAreaConfig.MinBuildY);

            _moneyManager = MoneyManager.Instance;

            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChange += ToggleBuildMode;
        }

        public void SetRepairMode(bool active)
        {
            _isRepairMode = active;
            if (active) DeselectStructure();
            Debug.Log($"Repair mode: {active}");
        }

        public void SelectStructure(int id)
        {
            SetRepairMode(false);

            _firstSelectedTower = null;

            BuildingConfig config = _buildingLibrary.GetConfigById(id);
            if (config == null) return;

            if (_moneyManager.GetMoney() < config.BaseCost)
            {
                _moneyManager.ShowNotEnoughPopup();
                DeselectStructure();
                return;
            }

            _currentSelectionId = id;
            _currentConfig = config;
            UpdateSelectionPrefabs();
        }

        private void DrawZone()
        {
            Vector3[] corners = new Vector3[4];
            corners[0] = new Vector2(_buildingAreaConfig.MinBuildX, _buildingAreaConfig.MinBuildY);
            corners[1] = new Vector2(_buildingAreaConfig.MaxBuildX, _buildingAreaConfig.MinBuildY);
            corners[2] = new Vector2(_buildingAreaConfig.MaxBuildX, _buildingAreaConfig.MaxBuildY);
            corners[3] = new Vector2(_buildingAreaConfig.MinBuildX, _buildingAreaConfig.MaxBuildY);

            _lineRenderer.SetPositions(corners);
        }

        private void UpdateSelectionPrefabs()
        {
            if (_towerGhost != null) Destroy(_towerGhost);
            if (_wallGhost != null) Destroy(_wallGhost);

            _towerGhost = null;
            _wallGhost = null;
            _wallGhostLR = null;

            if (_currentConfig == null || _currentConfig.Prefab == null) return;

            if (_currentConfig.GhostPrefab != null)
            {
                _towerGhost = Instantiate(_currentConfig.GhostPrefab, transform);
                _towerGhost.SetActive(false);
            }

            if (_currentConfig is ITowerData towerData && towerData.WallConfig != null)
            {
                if (towerData.WallConfig.GhostPrefab != null)
                {
                    _wallGhost = Instantiate(towerData.WallConfig.GhostPrefab, transform);
                    _wallGhost.SetActive(false);

                    _wallGhostLR = _wallGhost.GetComponent<LineRenderer>();
                }
            }
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

        private void InitBuildingArea(BuildingAreaConfig config)
        {
            _buildingAreaConfig = config;
        }

        private void RepairStructure(BaseBuilding target)
        {
            if (target is Fortress) return;

            int originalCost = target.Model.BaseCost;
            int repairCost = Mathf.CeilToInt(originalCost * 0.7f);

            if (_moneyManager.GetMoney() >= repairCost)
            {
                _moneyManager.SpendMoney(repairCost);
                target.Model.CurrentHP = target.Model.MaxHP;
                Debug.Log("Відремонтовано!");
            }
            else
            {
                _moneyManager.ShowNotEnoughPopup();
            }
        }

        private void HandleClick(Vector2 pos)
        {
            bool isShiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            Transform snappedTowerTransform = GetNearbyMatchingTower(pos, _buildingAreaConfig.SnapRadius);
            ITower clickedTower = snappedTowerTransform != null ? snappedTowerTransform.GetComponent<ITower>() : GetTowerAt(pos);

            // 1. Ремонт
            if (_isRepairMode)
            {
                BaseBuilding clickedForRepair = GetBuildingAt(pos);
                if (clickedForRepair != null && clickedForRepair.Model.CurrentHP < clickedForRepair.Model.MaxHP)
                {
                    RepairStructure(clickedForRepair);
                }
                return;
            }

            // 2. Початок будівництва
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

                if (_currentConfig == null) return;

                if (_moneyManager.GetMoney() < _currentConfig.BaseCost)
                {
                    _moneyManager.ShowNotEnoughPopup();
                    return;
                }

                _moneyManager.SpendMoney(_currentConfig.BaseCost);
                _firstSelectedTower = CreateStructure(pos);
                return;
            }

            if (clickedTower != null && clickedTower != _firstSelectedTower)
            {
                BuildWall(_firstSelectedTower, clickedTower);
                _firstSelectedTower = isShiftPressed ? clickedTower : null;
                return;
            }

            if (clickedTower == null && IsWithinBuildableZone(pos))
            {
                if (IsOccupied(pos))
                {
                    Debug.Log("Тут не можна будувати: місце зайняте!");
                    return;
                }

                int wallCost = 0;
                if (_currentConfig is ITowerData towerData && towerData.WallConfig != null)
                {
                    wallCost = towerData.WallConfig.BaseCost;
                }

                int totalCost = _currentConfig.BaseCost + wallCost;

                if (_moneyManager.GetMoney() >= totalCost)
                {
                    _moneyManager.SpendMoney(_currentConfig.BaseCost);

                    ITower newTower = CreateStructure(pos);
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
            Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 0.3f);
            foreach (var hit in hits)
            {
                if (hit.GetComponentInParent<IDamageable>() != null) 
                {
                    Debug.Log(hit);
                    return true;
                }
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
                BaseBuilding building = hit.GetComponentInParent<BaseBuilding>();

                if (building != null && building.Model != null && building.Model.Id == _currentSelectionId && building.IsSlipable)
                {
                    if (building.GetComponent<ITower>() != null)
                    {
                        float dist = Vector2.Distance(pos, building.transform.position);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            closestMatch = building.transform;
                        }
                    }
                }
            }
            return closestMatch;
        }

        public void DeselectStructure()
        {
            _currentSelectionId = -1;
            _currentConfig = null;
            if (_towerGhost != null) _towerGhost.SetActive(false);
            if (_wallGhost != null) _wallGhost.SetActive(false);
        }

        private ITower CreateStructure(Vector2 pos)
        {
            GameObject go = Instantiate(_currentConfig.Prefab, pos, Quaternion.identity, transform);

            // Викликаємо ініціалізацію, яка автоматично підніме HP, запустить анімацію і Footprint!
            if (go.TryGetComponent(out BaseBuilding baseBuilding))
            {
                baseBuilding.Initialize(_currentConfig);
            }

            ITower t = go.GetComponent<ITower>();
            t?.EnsureNodeInitialized();

            return t;
        }

        public int GetCostById(int id)
        {
            var config = _buildingLibrary.GetConfigById(id);
            return (config != null) ? config.BaseCost : 999999;
        }

        private void BuildWall(ITower a, ITower b)
        {
            if (a == null || b == null) return;

            BuildingConfig currentWallConfig = a.WallConfig;

            if (currentWallConfig == null) return;

            if (IsWallIntersecting(a.Transform.position, b.Transform.position)) return;

            if (_moneyManager.GetMoney() < currentWallConfig.BaseCost)
            {
                _moneyManager.ShowNotEnoughPopup();
                return;
            }

            a.EnsureNodeInitialized();
            b.EnsureNodeInitialized();

            if (a.Node == null || b.Node == null) return;
            if (graph.HasWall(a.Node, b.Node)) return;

            _moneyManager.SpendMoney(currentWallConfig.BaseCost);

            // Будуємо специфічну стіну саме для цієї вежі!
            GameObject wallObj = Instantiate(currentWallConfig.Prefab, transform);
            if (wallObj.TryGetComponent(out IWall wall) && wallObj.TryGetComponent(out BaseBuilding baseBuilding))
            {
                baseBuilding.Initialize(currentWallConfig);
                wall.Init(a.Node, b.Node, graph, strengthCalc);
            }
        }

        private void UpdateGhosts(Vector2 mousePos)
        {
            // ФІКС: Перевіряємо, чи об'єкт, який стоїть за інтерфейсом, не був знищений Unity
            if (_firstSelectedTower != null && (_firstSelectedTower as UnityEngine.Object) == null)
            {
                _firstSelectedTower = null; // Скидаємо мертве посилання
            }

            Transform snappedTower = GetNearbyMatchingTower(mousePos, _buildingAreaConfig.SnapRadius);
            bool isSnapped = snappedTower != null;

            Vector2 ghostPos = isSnapped ? (Vector2)snappedTower.position : mousePos;

            bool canBuild = isSnapped || (IsWithinBuildableZone(ghostPos) && !IsOccupied(ghostPos));

            if (_wallGhost != null && _wallGhost.activeSelf && _firstSelectedTower != null)
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
            float padding = 0.8f;
            float totalDistance = Vector2.Distance(start, end);

            if (totalDistance <= padding * 2) return false;

            float checkedDistance = totalDistance - (padding * 2);
            Vector2 direction = (end - start).normalized;
            Vector2 center = start + direction * (padding + checkedDistance / 2f);

            Vector2 size = new Vector2(0.2f, checkedDistance);
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg - 90f;

            Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, angle, wallmask);
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
        private bool IsCurrentSelectionWallConnectable() =>
            _currentConfig != null && _currentConfig.Prefab != null && _currentConfig.Prefab.GetComponent<ITower>() != null;

        private ITower GetTowerAt(Vector2 pos)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 0.3f);
            foreach (var hit in hits)
            {
                ITower t = hit.GetComponentInParent<ITower>();
                if (t != null) return t;
            }
            return null;
        }

        private BaseBuilding GetBuildingAt(Vector2 pos)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 0.3f);
            foreach (var hit in hits)
            {
                BaseBuilding b = hit.GetComponentInParent<BaseBuilding>();
                if (b != null || b is Wall) return b;
            }
            return null;
        }

        private void ResetSelection()
        {
            _firstSelectedTower = null;
            DeselectStructure();
            SetRepairMode(false);
        }

        private void ToggleBuildMode(GameState state)
        {
            if (state != GameState.Building) ResetSelection();
        }
    }
}