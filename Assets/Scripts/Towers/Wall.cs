using System.Collections;
using UnityEngine;

namespace Towers
{
    [RequireComponent(typeof(LineRenderer), typeof(EdgeCollider2D))]
    public class Wall : Solid //бля вона чогось не ламається, ну і похуй
    {
        TowerNode nodeA, nodeB;
        FortificationGraph graph;
        [SerializeField] WallStrengthCalculator strengthCalc;
        [SerializeField] float wallHalfWidth = 0.15f;

        public void Init(TowerNode a, TowerNode b, FortificationGraph fortGraph, WallStrengthCalculator wallStrengthCalc)
        {
            nodeA = a;
            nodeB = b;
            graph = fortGraph;
            strengthCalc = wallStrengthCalc;
            graph.AddWall(new WallLink { A = a, B = b });

            nodeA.Owner.AddWall(this);
            nodeB.Owner.AddWall(this);

            // 1. Позиція об'єкта = центр лінії
            transform.position = (a.Position + b.Position) / 2f;
            // Обов'язково скидаємо локальний масштаб, щоб Canvas не "роздувався"
            transform.localScale = Vector3.one; 

            // 2. Колайдер - працює в локальних координатах
            // Оскільки об'єкт у центрі, нам треба змістити точки відносно нього
            Vector2 center = (Vector2)transform.position;
            EdgeCollider2D ec = GetComponent<EdgeCollider2D>();
            ec.points = new Vector2[]
            {
                (Vector2)a.Position - center,
                (Vector2)b.Position - center
            };
            ec.edgeRadius = wallHalfWidth;

            // 3. Візуал (LineRenderer)
            LineRenderer lr = GetComponent<LineRenderer>();
            lr.useWorldSpace = true; // Важливо: WorldSpace ігнорує transform.position батька
            lr.positionCount = 2;
            lr.SetPosition(0, a.Position);
            lr.SetPosition(1, b.Position);

            IsReady = true;

            RegisterFootprint();


            PlaySpawnAnimation(); 
        }

        public void PlaySpawnAnimation(float duration = 0.4f)
        {
            StartCoroutine(WallSpawnRoutine(duration));
        }

        IEnumerator WallSpawnRoutine(float duration)
        {
            LineRenderer lr = GetComponent<LineRenderer>();
            Vector3 startPos = nodeA.Position;
            Vector3 endPos = nodeB.Position;

            float timer = 0;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;

                Vector3 currentEnd = Vector3.Lerp(startPos, endPos, t);
                lr.SetPosition(0, startPos);
                lr.SetPosition(1, currentEnd);

                yield return null;
            }
            lr.SetPosition(1, endPos);
        }

        public override void Collapse()
        {
            if (nodeA?.Owner != null) nodeA.Owner.RemoveWall(this);
            if (nodeB?.Owner != null) nodeB.Owner.RemoveWall(this);

            WaterGrid.UnregisterObstacleCapsule(nodeA.Position, nodeB.Position, wallHalfWidth);
            graph.RemoveWall(new WallLink { A = nodeA, B = nodeB });
            Destroy(gameObject);
        }

        // Start НЕ реєструє footprint автоматично - nodeA/nodeB заповнюються лише через Init()
        protected override void Start() { }

        protected override float ErosionRate
        {
            get
            {
                var wall = new WallLink { A = nodeA, B = nodeB };
                int triangleCount = graph.CountTriangles(wall);
                return strengthCalc.GetErosionRate(wall, triangleCount);
            }
        }

        protected override void RegisterFootprint()
        {
            WaterGrid.RegisterObstacleCapsule(nodeA.Position, nodeB.Position, wallHalfWidth);
        }
    }
}