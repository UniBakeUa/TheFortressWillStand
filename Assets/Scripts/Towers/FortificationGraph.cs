using System.Collections.Generic;
using UnityEngine;

namespace Towers
{
    /// <summary>
    /// Один на сцену. Тримає граф вежа-стіна і дає CountTriangles для WallStrengthCalculator.
    /// MonoBehaviour (а не чистий клас), щоб Wall міг тримати SerializeField-посилання
    /// і призначати його в інспекторі/при рантайм-інстанціюванні через FindFirstObjectByType.
    /// </summary>
    public class FortificationGraph : MonoBehaviour
    {
        readonly Dictionary<TowerNode, List<TowerNode>> _adjacency = new();
        readonly List<WallLink> _walls = new();

        public void AddWall(WallLink wall)
        {
            if (!_adjacency.ContainsKey(wall.A)) _adjacency[wall.A] = new List<TowerNode>();
            if (!_adjacency.ContainsKey(wall.B)) _adjacency[wall.B] = new List<TowerNode>();

            if (!_adjacency[wall.A].Contains(wall.B)) _adjacency[wall.A].Add(wall.B);
            if (!_adjacency[wall.B].Contains(wall.A)) _adjacency[wall.B].Add(wall.A);

            _walls.Add(wall);
        }
        public bool HasWall(TowerNode a, TowerNode b)
        {
            // Припустимо, у тебе є список або HashSet WallLink-ів
            foreach (var link in _walls) // або твій внутрішній список зв'язків
            {
                if ((link.A == a && link.B == b) || (link.A == b && link.B == a))
                    return true;
            }
            return false;
        }
        public void RemoveWall(WallLink wall)
        {
            if (_adjacency.TryGetValue(wall.A, out var neighborsA)) neighborsA.Remove(wall.B);
            if (_adjacency.TryGetValue(wall.B, out var neighborsB)) neighborsB.Remove(wall.A);

            _walls.RemoveAll(w => w.ConnectsSameNodes(wall));
        }

        /// <summary>Скільки трикутників містить конкретна стіна A-B (спільні сусіди A і B)</summary>
        public int CountTriangles(WallLink wall)
        {
            if (!_adjacency.TryGetValue(wall.A, out var neighborsA)) return 0;
            if (!_adjacency.TryGetValue(wall.B, out var neighborsB)) return 0;

            int count = 0;
            foreach (var c in neighborsA)
            {
                if (c == wall.B) continue;
                if (neighborsB.Contains(c)) count++;
            }
            return count;
        }

        /// <summary>Для відладки/UI - всі поточні стіни графа</summary>
        public IReadOnlyList<WallLink> AllWalls => _walls;
    }
}