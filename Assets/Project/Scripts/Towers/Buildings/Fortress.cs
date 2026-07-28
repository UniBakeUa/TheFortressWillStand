using System;
using Managers;
using Towers.Buildings;
using Towers.Models;
using Towers.ScriptableObjects;
using UnityEngine;
using UnityEngine.Events;

namespace Towers
{
    //������� ������, ��� ����� ���������. ���� � ��� ����������, ����� ��� ����������� ����� ��������
    public class Fortress : BaseBuilding, IDamageFlashTarget
    {
        [Header("������������ �������")]
        [SerializeField] private FortressConfig _fortressConfig;
        [SerializeField] private Color _hitFlashColor = Color.red;
        [SerializeField] private float _hitAnimationDuration = 0.05f;
        [SerializeField] private float _hitShakeOffset = 0.05f;

        public static Action _onFortressCollapsed;

        private int _lastHealthDecile = 10;
        private float _lastKnownHP;
        private SpriteRenderer[] _spriteRenderers;
        private Color[] _originalColors;
        private Vector3 _originalLocalPosition;
        private Coroutine _hitRoutine;

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

            _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            _originalColors = new Color[_spriteRenderers.Length];
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                _originalColors[i] = _spriteRenderers[i].color;
            }
            _originalLocalPosition = transform.localPosition;

            _lastHealthDecile = 10;
            _lastKnownHP = Model.CurrentHP;
            Model.OnHealthChanged += OnHealthChanged;

            IsReady = true;
            RegisterInRegistry();
            RegisterFootprint();
        }

        /// <summary>Фортеця відрізняється від решти будівель для перків авторемонту.</summary>
        public override bool IsFortress => true;

        private void OnHealthChanged(float currentHP)
        {
            if (currentHP < _lastKnownHP)
            {
                _hitRoutine = DamageFlashEffect.Play(
                    this, this, _hitRoutine, _hitFlashColor, _hitAnimationDuration, _hitAnimationDuration, _hitShakeOffset,
                    onComplete: () => _hitRoutine = null);
            }
            _lastKnownHP = currentHP;

            int decile = Mathf.FloorToInt(currentHP / Model.MaxHP * 10f - 0.0001f);

            if (decile < _lastHealthDecile)
            {
                _lastHealthDecile = decile;
                if (CameraShake.Instance != null)
                {
                    CameraShake.Instance.Shake();
                }
            }
            else if (decile > _lastHealthDecile)
            {
                _lastHealthDecile = decile;
            }
        }

        public void SetFlashColor(Color color)
        {
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] != null)
                {
                    _spriteRenderers[i].color = color;
                }
            }
        }

        public void ResetColor()
        {
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] != null)
                {
                    _spriteRenderers[i].color = _originalColors[i];
                }
            }
        }

        public void Shake(Vector2 offset)
        {
            transform.localPosition = _originalLocalPosition + new Vector3(offset.x, offset.y, 0f);
        }

        public void ResetPosition()
        {
            transform.localPosition = _originalLocalPosition;
        }

        public override void Collapse()
        {
            Debug.Log("Fortress ���������� �����!");

            GameStateManager.Instance.ChangeState(GameState.Paused);
            EndMenu.Instance.Show();

        }
    }
}