using System.Collections.Generic;
using Towers.Models;
using Towers.ScriptableObjects;
using UnityEngine;

namespace Towers.Buildings
{
    public class Tower : BaseBuilding, ITower
    {
        public TowerModel TowerModel;
        public TowerNode Node { get; private set; }
        public Transform Transform => transform;
        public WallConfig WallConfig => TowerModel.WallConfig;

        protected override float ErosionRate
        {
            get
            {
                float multiplier = Mathf.Max(0.2f, 1.0f - (_attachedWalls.Count * TowerModel.DamageReductionPerWall));
                return TowerModel.BaseErosionRate * multiplier;
            }
        }

        private readonly List<IWall> _attachedWalls = new();

        public override void Initialize(BuildingConfig config)
        {
            base.Initialize(config);

            TowerModel = new TowerModel(config as TowerConfig);

            EnsureNodeInitialized();
        }

        public void EnsureNodeInitialized()
        {
            if (Node == null)
                Node = new TowerNode(transform.position, this);
        }
        
        public override void Collapse()
        {
            for (int i = _attachedWalls.Count - 1; i >= 0; i--)
            {
                if (_attachedWalls[i] != null)
                    _attachedWalls[i].Collapse();
            }
            base.Collapse();
        }

        public void AddWall(IWall wall)
        {
            _attachedWalls.Add(wall);
        }

        public void RemoveWall(IWall wall)
        {
            _attachedWalls.Remove(wall);
        }
    }
}
