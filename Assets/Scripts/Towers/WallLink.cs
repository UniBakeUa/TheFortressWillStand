using UnityEngine;

namespace Towers
{
    /// <summary>
    /// Ребро графа фортифікації. Порівняння двох WallLink робиться по парі вузлів
    /// незалежно від порядку (A-B == B-A), це важливо для CountTriangles/RemoveWall.
    /// </summary>
    public class WallLink
    {
        public TowerNode A;
        public TowerNode B;

        public float Length => Vector2.Distance(A.Position, B.Position);

        public bool ConnectsSameNodes(WallLink other) =>
            (A == other.A && B == other.B) || (A == other.B && B == other.A);
    }
}