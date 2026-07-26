using Towers.Buildings;
using UnityEngine;

namespace Managers
{
    public class TurretHoverManager : MonoBehaviour
    {
        [SerializeField] private float _hoverRadius = 0.7f;

        private TurretBase _hoveredTurret;

        private void Update()
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            TurretBase closest = FindClosestTurret(mousePos);

            if (closest != _hoveredTurret)
            {
                if (_hoveredTurret != null)
                {
                    _hoveredTurret.HideRange();
                }

                _hoveredTurret = closest;

                if (_hoveredTurret != null)
                {
                    _hoveredTurret.ShowRange(Color.green);
                }
            }
        }

        private TurretBase FindClosestTurret(Vector2 point)
        {
            var turrets = FindObjectsByType<TurretBase>(FindObjectsSortMode.None);

            TurretBase closest = null;
            float closestSqrDistance = _hoverRadius * _hoverRadius;

            foreach (var turret in turrets)
            {
                if (turret == null) continue;

                float sqrDistance = ((Vector2)turret.transform.position - point).sqrMagnitude;
                if (sqrDistance <= closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closest = turret;
                }
            }

            return closest;
        }
    }
}
