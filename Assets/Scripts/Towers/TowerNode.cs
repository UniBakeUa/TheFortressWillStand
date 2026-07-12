using UnityEngine;

namespace Towers
{
    /// <summary>
    /// Вузол графа фортифікації. Це не MonoBehaviour - просто дані про позицію вежі,
    /// на яку граф спирається при підрахунку трикутників.
    /// Owner типізований як IWallConnectable, а не конкретний Tower - тому будь-який
    /// новий тип вежі, що реалізує цей інтерфейс, автоматично може бути вузлом.
    /// </summary>
    public class TowerNode
    {
        public Vector2 Position;
        public IWallConnectable Owner; // null, якщо вежа зруйнована

        public TowerNode(Vector2 position, IWallConnectable owner)
        {
            Position = position;
            Owner = owner;
        }
    }
}