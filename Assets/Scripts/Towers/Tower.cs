using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Towers
{
    public class Tower : Solid, IWallConnectable
    {
        public TowerNode Node { get; private set; }
        public Transform Transform => transform;

        // Список стін, які "тримаються" за цю вежу
        readonly List<Wall> _attachedWalls = new();

        public void AddWall(Wall wall) => _attachedWalls.Add(wall);
        public void RemoveWall(Wall wall) => _attachedWalls.Remove(wall);

        public void EnsureNodeInitialized()
        {
            if (Node == null)
                Node = new TowerNode(transform.position, this);
        }

        protected override void Awake()
        {
            base.Awake();
            EnsureNodeInitialized();
        }

        protected override void Start()
        {
            base.Start();
            PlaySpawnAnimation();
        }

        public override void Collapse()
        {
            // Видаляємо всі прикріплені стіни перед тим, як зникне сама вежа
            for (int i = _attachedWalls.Count - 1; i >= 0; i--)
            {
                if (_attachedWalls[i] != null)
                    _attachedWalls[i].Collapse();
            }

            base.Collapse();
        }

        // Легко додати новий тип вежі: успадкуй Tower (або Solid + IWallConnectable напряму)
        // і перевизнач ErosionRate/іншу віртуальну логіку зі своєю формулою -
        // BuildManager і Wall працюватимуть з ним без жодних змін, бо спілкуються через IWallConnectable
        protected override float ErosionRate
        {
            get
            {
                // Чим більше стін (зв'язків), тим менша вразливість:
                // 0 стін = множник 1.0, 1 стіна = 0.8, 2 стіни = 0.6 і т.д. (мінімум 0.2)
                float multiplier = Mathf.Max(0.2f, 1.0f - (_attachedWalls.Count * damageReductionPerWall));
                return baseErosionRate * multiplier;
            }
        }
    }
}