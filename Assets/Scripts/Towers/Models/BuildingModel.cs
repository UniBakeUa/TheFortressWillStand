using System;

namespace Towers.Models
{
    public class BuildingModel
    {
        private float _currentHP;

        public float MaxHP { get; private set; }
        public float CurrentHP 
        {
            get => _currentHP;
            set {
                _currentHP = MathF.Max(0, value);
                OnHealthChanged?.Invoke(_currentHP);
            } 
        }

        public event Action<float> OnHealthChanged;

        public BuildingModel(float maxHP)
        {
            MaxHP = maxHP;
            CurrentHP = maxHP;
        }
    }
}