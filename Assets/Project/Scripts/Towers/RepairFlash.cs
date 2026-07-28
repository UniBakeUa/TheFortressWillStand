using System.Collections;
using UnityEngine;

namespace Towers
{
    /// <summary>
    /// Короткий зелений спалах спрайтів будівлі під час ремонту.
    ///
    /// Окремий компонент, а не гілка в DamageFlashEffect, бо Fortress уже реалізує
    /// IDamageFlashTarget по-своєму (трясіння + камера), і ремонт мав би з ним
    /// конфліктувати за ті самі поля кольору. Тут стан свій і незалежний.
    /// </summary>
    public class RepairFlash : MonoBehaviour
    {
        [SerializeField] private Color _repairColor = new Color(0.35f, 1f, 0.4f, 1f);
        [Tooltip("Дуже коротко, як і спалах від шкоди")]
        [SerializeField] private float _duration = 0.12f;

        private SpriteRenderer[] _renderers;
        private Color[] _originalColors;
        private Coroutine _routine;

        private void Awake()
        {
            CacheRenderers();
        }

        private void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<SpriteRenderer>();
            _originalColors = new Color[_renderers.Length];

            for (int i = 0; i < _renderers.Length; i++)
            {
                _originalColors[i] = _renderers[i].color;
            }
        }

        /// <summary>Програє спалах. Повторний виклик перезапускає його з початку.</summary>
        public void Play()
        {
            if (_renderers == null || _renderers.Length == 0) return;
            if (!isActiveAndEnabled) return;

            if (_routine != null)
            {
                StopCoroutine(_routine);
                // Колір міг лишитись зеленим від перерваного спалаху - вертаємо
                // оригінал, інакше він запишеться як "оригінальний".
                ResetColors();
            }

            _routine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null) _renderers[i].color = _repairColor;
            }

            yield return new WaitForSeconds(_duration);

            ResetColors();
            _routine = null;
        }

        private void ResetColors()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null) _renderers[i].color = _originalColors[i];
            }
        }

        private void OnDisable()
        {
            // Будівлю вимкнули посеред спалаху - лишати її зеленою не можна.
            if (_routine == null) return;

            StopCoroutine(_routine);
            _routine = null;
            ResetColors();
        }
    }
}
