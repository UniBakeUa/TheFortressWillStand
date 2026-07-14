// повісити на будь-яку Tower/Wall для спостереження в Console
using Towers.Buildings;
using UnityEngine;

namespace Towers
{
    public class BaseBuildingDebugLogger : MonoBehaviour
    {
        BaseBuilding _building;
        float _timer;

        void Awake() => _building = GetComponent<BaseBuilding>();

        void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < 1f) return;
            _timer = 0f;
            Debug.Log($"{name}: HP={_building.Model.CurrentHP:F1}, Exposure={_building.ExposureFraction:F2}");
        }
    }
}