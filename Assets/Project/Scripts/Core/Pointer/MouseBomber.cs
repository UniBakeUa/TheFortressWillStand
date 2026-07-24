using UnityEngine;

public class MouseBomber : MonoBehaviour
{
    [SerializeField] private float _radius = 1;
    private Vector2 _position;

    [SerializeField] private GameObject _explosionPrefab;
    private void OnEnable()
    {
        PointerInfo.LeftMouseButtonDown += Bomb;
    }
    private void OnDisable()
    {
        PointerInfo.LeftMouseButtonDown -= Bomb;
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
        Instantiate(_explosionPrefab, _position, Quaternion.identity);
        print($"bombing {enemiesBombed.Length}");
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(PointerInfo.PointerWorldPosition, _radius);
    }
}
