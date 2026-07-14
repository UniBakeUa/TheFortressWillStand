using UnityEngine;
using TMPro;

namespace Towers
{
    public class HealthDisplay : MonoBehaviour
    {
        private Solid _solid;
        [SerializeField] private Canvas hpCanvasPrefab; // Ваш оригінальний префаб
        private Canvas _spawnedCanvas;
        private TextMeshProUGUI _hpText;

        void Awake()
        {
            _solid = GetComponent<Solid>();
            
            // Створюємо копію один раз
            _spawnedCanvas = Instantiate(hpCanvasPrefab, transform);
            
            // Скидаємо локальну позицію, щоб він був над об'єктом (задайте Y на свій смак)
            _spawnedCanvas.transform.localPosition = new Vector3(0, 1.2f, 0); 
            
            // Отримуємо посилання на текст у КОПІЇ
            _hpText = _spawnedCanvas.GetComponentInChildren<TextMeshProUGUI>();
            
            // Одразу вимикаємо
            _spawnedCanvas.gameObject.SetActive(false);
        }

        void Update()
        {
            if (_solid == null || _spawnedCanvas == null) return;

            bool show = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            
            // Керуємо станом GameObject (активний/неактивний)
            if (_spawnedCanvas.gameObject.activeSelf != show)
                _spawnedCanvas.gameObject.SetActive(show);

            if (show)
            {
                // Оновлюємо текст КОПІЇ
                _hpText.text = $"{Mathf.CeilToInt(_solid.Model.CurrentHP)} / {Mathf.CeilToInt(_solid.Model.MaxHP)}";
            }
        }
    }
}