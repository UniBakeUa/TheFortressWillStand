using System;
using System.Collections;
using Managers;
using Towers.Buildings;
using Towers.Models;
using Towers.ScriptableObjects;
using UnityEngine;
using UnityEngine.Events;

namespace Towers
{
    //������� ������, ��� ����� ���������. ���� � ��� ����������, ����� ��� ����������� ����� ��������
    public class Fortress : BaseBuilding
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
            RegisterFootprint();
        }

        private void OnHealthChanged(float currentHP)
        {
            if (currentHP < _lastKnownHP)
            {
                PlayHitAnimation();
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

        private void PlayHitAnimation()
        {
            if (_hitRoutine != null) return;

            _hitRoutine = StartCoroutine(HitAnimationRoutine());
        }

        private IEnumerator HitAnimationRoutine()
        {
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] != null)
                {
                    _spriteRenderers[i].color = _hitFlashColor;
                }
            }

            Vector2 shakeOffset = UnityEngine.Random.insideUnitCircle * _hitShakeOffset;
            transform.localPosition = _originalLocalPosition + new Vector3(shakeOffset.x, shakeOffset.y, 0f);

            yield return new WaitForSeconds(_hitAnimationDuration);

            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] != null)
                {
                    _spriteRenderers[i].color = _originalColors[i];
                }
            }
            transform.localPosition = _originalLocalPosition;

            yield return new WaitForSeconds(_hitAnimationDuration);

            _hitRoutine = null;
        }

        public override void Collapse()
        {
            Debug.Log("Fortress ���������� �����!");

            GameStateManager.Instance.ChangeState(GameState.Paused);
            EndMenu.Instance.Show();
            
        }
    }
}