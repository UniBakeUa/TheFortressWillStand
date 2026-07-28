using System.Collections;
using Managers.Audio;
using Towers.Buildings;
using UnityEngine;

public class MouseBomber : MonoBehaviour
{
    [SerializeField] private float _radius = 1;
    private float _startRadius;
    private Vector2 _position;

    [SerializeField] private Explosion _explosionPrefab;

    [Header("Range Indicator")]
    [SerializeField] private RangeCircle _rangeCircle;
    [SerializeField] private Color _rangeColor = Color.cyan;
    [SerializeField] private Color _upgradeFlashColor = Color.yellow;
    [SerializeField] private float _upgradeFlashDuration = 0.3f;

    private Coroutine _upgradeFlashRoutine;

    private void OnEnable()
    {
        PointerInfo.LeftMouseButtonDown += Bomb;
    }
    private void OnDisable()
    {
        PointerInfo.LeftMouseButtonDown -= Bomb;
    }
    private void Start()
    {
        _startRadius = _radius;
    }

    private void Update()
    {
        if (_rangeCircle == null) return;

        bool isWaveActive = GameStateManager.Instance.CurrentState == GameState.Playing;

        if (!isWaveActive)
        {
            _rangeCircle.Hide();
            return;
        }

        _rangeCircle.transform.position = PointerInfo.PointerWorldPosition;
        if (_upgradeFlashRoutine == null)
        {
            _rangeCircle.Show(_radius, _rangeColor);
        }
    }
    private void Bomb(Vector2 position, bool state)
    {
        if (GameStateManager.Instance.CurrentState != GameState.Playing) return;
        if (!state) return;

        _position = PointerInfo.PointerWorldPosition;
        var enemiesBombed = Physics2D.CircleCastAll(_position, _radius, Vector2.zero, 0, LayerMask.GetMask("Enemy"));
        foreach (RaycastHit2D e in enemiesBombed)
        {
            if (e.transform.TryGetComponent(out Items.Enemy enemy))
            {
                // Б'ємо ніби згори - напрямку розльоту немає, лишається клякса
                enemy.WasStricken();
            }
        }
        var explosion = Instantiate(_explosionPrefab, _position, Quaternion.identity);
        explosion.enabled = false;
        explosion.ChangeScaleModifier(_radius);
        explosion.enabled = true;
        Managers.SoundManager.Instance.Play(SoundId.MouseBomberShot, _position);
        print($"bombing {enemiesBombed.Length}");
    }
    public void ModifyRadius(float amount)
    {
        _radius += _startRadius * amount;

        if (_rangeCircle != null)
        {
            if (_upgradeFlashRoutine != null)
            {
                StopCoroutine(_upgradeFlashRoutine);
            }
            _upgradeFlashRoutine = StartCoroutine(UpgradeFlashRoutine());
        }
    }

    private IEnumerator UpgradeFlashRoutine()
    {
        _rangeCircle.transform.position = PointerInfo.PointerWorldPosition;
        _rangeCircle.Show(_radius, _upgradeFlashColor);

        yield return new WaitForSeconds(_upgradeFlashDuration);

        _upgradeFlashRoutine = null;
    }
    public float GetRadiusFraction()
    {
        float v = (_radius / _startRadius) * 100;
        if (v != Mathf.Infinity)
            return v;

        return 0;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(PointerInfo.PointerWorldPosition, _radius);
    }
}
