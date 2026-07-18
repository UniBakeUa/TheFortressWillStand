using System.Collections;
using Towers.Models;
using Towers.ScriptableObjects;
using UnityEngine;

namespace Towers.Buildings
{
    [RequireComponent(typeof(LineRenderer), typeof(EdgeCollider2D))]
    public class Wall : BaseBuilding, IWall
    {
        private TowerNode _nodeA;
        private TowerNode _nodeB;
        private FortificationGraph _graph;
        private LineRenderer _lineRenderer;
        private WallStrengthCalculator _strengthCalc;

        public WallModel WallModel;

        protected override float ErosionRate
        {
            get
            {
                if (_graph == null || _strengthCalc == null) return WallModel.BaseErosionRate;

                var wallLink = new WallLink { A = _nodeA, B = _nodeB };
                int triangleCount = _graph.CountTriangles(wallLink);
                return _strengthCalc.GetErosionRate(wallLink, triangleCount);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _lineRenderer = GetComponent<LineRenderer>();
        }

        public override void Initialize(BuildingConfig config)
        {
            Model = new BuildingModel(config);
            WallModel = new WallModel(config as WallConfig);

            if (_buildingView != null)
            {
                _buildingView.SetupHealth(Model.MaxHP);
                Model.OnHealthChanged += _buildingView.UpdateHealth;
            }
        }

        public void Init(TowerNode a, TowerNode b, FortificationGraph fortGraph, WallStrengthCalculator wallStrengthCalc)
        {
            _nodeA = a;
            _nodeB = b;
            _graph = fortGraph;

            if (wallStrengthCalc != null) _strengthCalc = wallStrengthCalc;

            _graph.AddWall(new WallLink { A = a, B = b });

            _nodeA.Owner.AddWall(this);
            _nodeB.Owner.AddWall(this);

            transform.position = (a.Position + b.Position) / 2f;
            transform.localScale = Vector3.one;

            Vector2 center = (Vector2)transform.position;
            EdgeCollider2D ec = Collider as EdgeCollider2D;
            if (ec != null)
            {
                ec.points = new Vector2[]
                {
                a.Position - center,
                b.Position - center
                };
                ec.edgeRadius = WallModel.WallHalfWidth;
            }

            _lineRenderer.useWorldSpace = true;
            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPosition(0, a.Position);
            _lineRenderer.SetPosition(1, b.Position);

            RegisterFootprint();
            IsReady = true;
            StartCoroutine(WallSpawnRoutine(0.4f));
        }

        protected override void RegisterFootprint()
        {
            if (WaterGrid != null && _nodeA != null && _nodeB != null)
            {
                WaterGrid.RegisterObstacleCapsule(_nodeA.Position, _nodeB.Position, WallModel.WallHalfWidth);
            }
        }

        protected override void OnGridRebuilt()
        {
            if (!IsReady || _nodeA == null || _nodeB == null) return;
            WaterGrid.RegisterObstacleCapsule(_nodeA.Position, _nodeB.Position, WallModel.WallHalfWidth);
        }

        public override void Collapse()
        {
            if (_nodeA?.Owner != null) _nodeA.Owner.RemoveWall(this);
            if (_nodeB?.Owner != null) _nodeB.Owner.RemoveWall(this);

            if (WaterGrid != null)
            {
                WaterGrid.UnregisterObstacleCapsule(_nodeA.Position, _nodeB.Position, WallModel.WallHalfWidth);
            }

            if (_graph != null)
            {
                _graph.RemoveWall(new WallLink { A = _nodeA, B = _nodeB });
            }

            Destroy(gameObject);
        }

        private IEnumerator WallSpawnRoutine(float duration)
        {
            Vector3 startPos = _nodeA.Position;
            Vector3 endPos = _nodeB.Position;

            float timer = 0;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;

                Vector3 currentEnd = Vector3.Lerp(startPos, endPos, t);
                _lineRenderer.SetPosition(0, startPos);
                _lineRenderer.SetPosition(1, currentEnd);

                yield return null;
            }
            _lineRenderer.SetPosition(1, endPos);
        }
    }
}
