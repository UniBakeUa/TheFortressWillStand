using UnityEngine;

public class MouseBomber : MonoBehaviour
{
    [SerializeField] private float _radius = 1;
    private float _startRadius;
    private Vector2 _position;
    [SerializeField] private AudioClip shotSound;

    [SerializeField] private Explosion _explosionPrefab;
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
                enemy.WasStricken();
            }
        }
        var explosion = Instantiate(_explosionPrefab, _position, Quaternion.identity);
        explosion.enabled = false;
        explosion.ChangeScaleModifier(_radius);
        explosion.enabled = true;
        AudioSource.PlayClipAtPoint(shotSound, _position, 0.5f);
        print($"bombing {enemiesBombed.Length}");
    }
    public void ModifyRadius(float amount)
    {
        _radius += _startRadius * amount;
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
