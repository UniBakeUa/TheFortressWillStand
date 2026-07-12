using UnityEngine;

namespace Towers
{
    /// <summary>
    /// Контракт для будь-якого об'єкта, що може бути вузлом графа фортифікації -
    /// тобто до нього можна прикріпити Wall. BuildManager і Wall працюють через
    /// цей інтерфейс, а не напряму через клас Tower - тому новий тип вежі
    /// (наприклад "важка вежа" чи "маяк") достатньо зробити IWallConnectable,
    /// і вся система будівництва стін запрацює з ним без жодних змін в BuildManager/Wall.
    /// </summary>
    public interface IWallConnectable
    {
        TowerNode Node { get; }
        Transform Transform { get; } // для позиції/анімацій без каста до конкретного класу

        void EnsureNodeInitialized();
        void AddWall(Wall wall);
        void RemoveWall(Wall wall);
    }
}