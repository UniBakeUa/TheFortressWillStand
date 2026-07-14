// повісити на будь-яку Tower/Wall для спостереження в Console
using UnityEngine;

public class SolidDebugLogger : MonoBehaviour
{
    Towers.Solid _solid;
    float _timer;

    void Awake() => _solid = GetComponent<Towers.Solid>();

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < 1f) return;
        _timer = 0f;
        Debug.Log($"{name}: HP={_solid.Model.CurrentHP:F1}, Exposure={_solid.ExposureFraction:F2}");
    }
}