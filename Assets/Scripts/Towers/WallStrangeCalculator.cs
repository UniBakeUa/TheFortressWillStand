using UnityEngine;

namespace Towers
{
    /// <summary>
    /// Один на сцену (або ScriptableObject, якщо треба кілька пресетів складності - легко перенести).
    /// Wall.ErosionRate звертається сюди щотіка, тому тут не повинно бути важких обчислень.
    /// </summary>
    public class WallStrengthCalculator : MonoBehaviour
    {
        [Tooltip("X: довжина стіни / maxWallLength (0..1), Y: множник міцності (1 = найкраще)")]
        [SerializeField] AnimationCurve lengthToResistance = AnimationCurve.Linear(0, 1, 1, 0.2f);

        [SerializeField] float maxWallLength = 8f;
        [SerializeField] float triangleBonusPerCount = 0.4f; // +40% міцності за кожен трикутник
        [SerializeField] float baseErosionRate = 1f;
        [SerializeField] float minResistance = 0.1f; // страховка від ділення на ~0

        public float GetResistance(WallLink wall, int triangleCount)
        {
            float t = Mathf.Clamp01(wall.Length / maxWallLength);
            float lengthFactor = lengthToResistance.Evaluate(t);
            float triangleMultiplier = 1f + triangleBonusPerCount * triangleCount;
            return lengthFactor * triangleMultiplier;
        }

        public float GetErosionRate(WallLink wall, int triangleCount)
        {
            float resistance = GetResistance(wall, triangleCount);
            return baseErosionRate / Mathf.Max(resistance, minResistance);
        }
    }
}