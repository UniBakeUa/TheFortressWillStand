using System.Collections.Generic;
using System.Linq;
using Towers.Buildings;
using UnityEngine;

namespace Items
{
    /// <summary>
    /// Будує тимчасовий waypoint-граф навколо стін (по краях кожної стіни, з відступом)
    /// і шукає шлях від старту до фортеці методом A*, оминаючи стіни (шар Wall).
    /// Ребро між двома точками існує, якщо пряма лінія між ними не перетинає жодної стіни.
    /// Прохідність перевіряється до самої фортеці (не до точки кидка гранати), щоб A*
    /// не вважав шлях вільним лише через те, що точка кидка випадково видима по діагоналі
    /// повз стіну, яка насправді блокує пряму лінію до фортеці.
    /// </summary>
    public static class EnemyPathfinder
    {
        private const float DetourOffset = 0.6f;
        private const int CircleDetourPoints = 8;

        public static bool TryFindPath(Vector2 start, Vector2 fortressPos, float attackDistance, LayerMask wallMask, out List<Vector2> path, out float pathLength)
        {
            path = null;
            pathLength = 0f;

            if (!IsBlocked(start, fortressPos, wallMask))
            {
                Vector2 throwPosition = GetThrowPosition(start, fortressPos, attackDistance);
                path = new List<Vector2> { start, throwPosition };
                pathLength = Vector2.Distance(start, throwPosition);
                return true;
            }

            List<Vector2> nodes = new List<Vector2> { start, fortressPos };
            AddAllObstacleDetourNodes(nodes);

            var cameFrom = new Dictionary<int, int>();
            var gScore = new Dictionary<int, float> { [0] = 0f };
            var openSet = new List<int> { 0 };
            var closedSet = new HashSet<int>();

            const int targetIndex = 1;

            while (openSet.Count > 0)
            {
                int current = openSet.OrderBy(i => gScore[i] + Vector2.Distance(nodes[i], nodes[targetIndex])).First();

                if (current == targetIndex)
                {
                    List<Vector2> rawPath = ReconstructPath(cameFrom, current, nodes);

                    // Останню точку (саму фортецю) замінюємо на точку кидка гранати,
                    // яка лежить на AttackDistance від фортеці в напрямку підходу ворога
                    Vector2 approachFrom = rawPath.Count >= 2 ? rawPath[rawPath.Count - 2] : start;
                    Vector2 throwPosition = GetThrowPosition(approachFrom, fortressPos, attackDistance);
                    rawPath[rawPath.Count - 1] = throwPosition;

                    path = rawPath;
                    pathLength = gScore[current] - Vector2.Distance(approachFrom, fortressPos) + Vector2.Distance(approachFrom, throwPosition);
                    return true;
                }

                openSet.Remove(current);
                closedSet.Add(current);

                for (int i = 0; i < nodes.Count; i++)
                {
                    if (i == current || closedSet.Contains(i)) continue;
                    if (IsBlocked(nodes[current], nodes[i], wallMask)) continue;

                    float tentativeG = gScore[current] + Vector2.Distance(nodes[current], nodes[i]);
                    if (!gScore.TryGetValue(i, out float existingG) || tentativeG < existingG)
                    {
                        cameFrom[i] = current;
                        gScore[i] = tentativeG;
                        if (!openSet.Contains(i)) openSet.Add(i);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Повертає вузли waypoint-графа (для debug-візуалізації), без пошуку шляху.
        /// </summary>
        public static List<Vector2> GetDebugNavNodes()
        {
            List<Vector2> nodes = new List<Vector2>();
            AddAllObstacleDetourNodes(nodes);
            return nodes;
        }

        /// <summary>
        /// Додає обхідні вузли навколо всіх перешкод: лінійних (стіни, по краях
        /// відрізка) і точкових (Tower/Turret - башти/турелі, по колу навколо колайдера).
        /// </summary>
        private static void AddAllObstacleDetourNodes(List<Vector2> nodes)
        {
            Wall[] walls = Object.FindObjectsByType<Wall>(FindObjectsSortMode.None);
            foreach (var wall in walls)
            {
                AddDetourNodes(wall.NodeAPosition, wall.NodeBPosition, nodes);
            }

            Tower[] towers = Object.FindObjectsByType<Tower>(FindObjectsSortMode.None);
            foreach (var tower in towers)
            {
                AddCircleDetourNodes(tower.transform.position, tower.ObstacleRadius, nodes);
            }

            Turret[] turrets = Object.FindObjectsByType<Turret>(FindObjectsSortMode.None);
            foreach (var turret in turrets)
            {
                AddCircleDetourNodes(turret.transform.position, turret.ObstacleRadius, nodes);
            }
        }

        private static void AddCircleDetourNodes(Vector2 center, float radius, List<Vector2> nodes)
        {
            float detourRadius = radius + DetourOffset;
            for (int i = 0; i < CircleDetourPoints; i++)
            {
                float angle = i * Mathf.PI * 2f / CircleDetourPoints;
                nodes.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * detourRadius);
            }
        }

        public static Wall FindNearestBlockingWall(Vector2 from, Vector2 fortressPos, LayerMask wallMask)
        {
            Wall[] walls = Object.FindObjectsByType<Wall>(FindObjectsSortMode.None);

            Wall nearest = null;
            float nearestSqrDistance = float.MaxValue;

            foreach (var wall in walls)
            {
                if (!SegmentsIntersect(from, fortressPos, wall.NodeAPosition, wall.NodeBPosition)) continue;

                Vector2 midpoint = (wall.NodeAPosition + wall.NodeBPosition) * 0.5f;
                float sqrDistance = (midpoint - from).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearest = wall;
                }
            }

            return nearest;
        }

        private static Vector2 GetThrowPosition(Vector2 approachFrom, Vector2 fortressPos, float attackDistance)
        {
            Vector2 dir = (approachFrom - fortressPos).normalized;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            return fortressPos + dir * attackDistance;
        }

        private static void AddDetourNodes(Vector2 a, Vector2 b, List<Vector2> nodes)
        {
            Vector2 wallDir = (b - a).normalized;
            Vector2 perpendicular = new Vector2(-wallDir.y, wallDir.x);

            nodes.Add(a + perpendicular * DetourOffset);
            nodes.Add(a - perpendicular * DetourOffset);
            nodes.Add(b + perpendicular * DetourOffset);
            nodes.Add(b - perpendicular * DetourOffset);
        }

        public static bool IsBlocked(Vector2 from, Vector2 to, LayerMask wallMask)
        {
            return Physics2D.Linecast(from, to, wallMask);
        }

        private static List<Vector2> ReconstructPath(Dictionary<int, int> cameFrom, int current, List<Vector2> nodes)
        {
            var result = new List<Vector2> { nodes[current] };
            while (cameFrom.TryGetValue(current, out int previous))
            {
                current = previous;
                result.Add(nodes[current]);
            }
            result.Reverse();
            return result;
        }

        private static bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            float d1 = Cross(p4 - p3, p1 - p3);
            float d2 = Cross(p4 - p3, p2 - p3);
            float d3 = Cross(p2 - p1, p3 - p1);
            float d4 = Cross(p2 - p1, p4 - p1);

            return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                   ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    }
}
