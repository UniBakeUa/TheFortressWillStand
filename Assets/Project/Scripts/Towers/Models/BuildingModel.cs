using System;
using Towers.ScriptableObjects;
using UnityEngine;

namespace Towers.Models
{
    public class BuildingModel
    {
        private float _currentHP;

        public string StructureName { get; private set; }
        public int Id { get; private set; }
        public int BaseCost { get; private set; }
        public float MaxHP { get; private set; }
        public float CurrentHP 
        {
            get => _currentHP;
            set {
                _currentHP = MathF.Max(0, value);
                OnHealthChanged?.Invoke(_currentHP);
            } 
        }
        public float BaseErosionRate { get; private set; }

        public event Action<float> OnHealthChanged;

        public BuildingModel(BuildingConfig buildingConfig)
        {
            StructureName = buildingConfig.StructureName;
            Id = buildingConfig.Id;
            MaxHP = buildingConfig.BaseHP;
            CurrentHP = buildingConfig.BaseHP;
            BaseErosionRate = buildingConfig.BaseErosionRate;
            BaseCost = buildingConfig.BaseCost;
        }
    }
}