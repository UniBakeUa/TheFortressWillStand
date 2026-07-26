using UnityEngine;

namespace Managers
{
    public class TurretHoverManager : MonoBehaviour
    {
        [SerializeField] private float _hoverRadius = 0.7f;

        private Turret _hoveredTurret;

        private void Update()
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            Turret closest = FindClosestTurret(mousePos);

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

        private Turret FindClosestTurret(Vector2 point)
        {
            var turrets = FindObjectsByType<Turret>(FindObjectsSortMode.None);

            Turret closest = null;
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
