using System;
using Towers.Buildings;
using Towers.Models;
using Towers.ScriptableObjects;
using UnityEngine;
using UnityEngine.Events;

namespace Towers
{
    //Головна будівля, яку треба захистити. Тому в разі руйнування, треба щоб показувався екран програшу
    public class Fortress : BaseBuilding
    {
        [Header("Налаштування фортеці")]
        [SerializeField] private FortressConfig _fortressConfig;

        public static Action _onFortressCollapsed;

        private void Start()
        {
            Initialize(_fortressConfig);
        }

        public override void Initialize(BuildingConfig config)
        {
            Model = new BuildingModel(config);

            if (_buildingView != null)
            {
                _buildingView.SetupHealth(Model.MaxHP);
                Model.OnHealthChanged += _buildingView.UpdateHealth;
            }

            IsReady = true;
            RegisterFootprint();
        }

        public override void Collapse()
        {
            Debug.Log("Fortress зруйновано водою!");

            GameStateManager.Instance.ChangeState(GameState.Paused);
            EndMenu.Instance.Show();
            
        }
    }
}