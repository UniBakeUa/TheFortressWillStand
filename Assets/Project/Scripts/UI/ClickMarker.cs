using UnityEngine;

namespace UI
{
    /// <summary>
    /// Спрайт-позначка в точці удару. Живе задані секунди, згасаючи, і сам себе
    /// знищує - викликачу достатньо зробити Instantiate.
    /// </summary>
    public class ClickMarker : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _sprite;
        [Tooltip("Скільки секунд позначка тримається на екрані")]
        [SerializeField] private float _lifetime = 0.4f;
        [Tooltip("Наскільки виростає за час життя. 1 = без зміни розміру")]
        [SerializeField] private float _endScaleMultiplier = 1.6f;

        private float _timer;
        private Vector3 _startScale;
        private Color _startColor;

        private void Awake()
        {
            if (_sprite == null) _sprite = GetComponentInChildren<SpriteRenderer>();

            _startScale = transform.localScale;
            if (_sprite != null) _startColor = _sprite.color;
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            float t = _lifetime > 0f ? Mathf.Clamp01(_timer / _lifetime) : 1f;

            transform.localScale = _startScale * Mathf.Lerp(1f, _endScaleMultiplier, t);

            if (_sprite != null)
            {
                Color c = _startColor;
                c.a = _startColor.a * (1f - t);
                _sprite.color = c;
            }

            if (t >= 1f) Destroy(gameObject);
        }
    }
}
