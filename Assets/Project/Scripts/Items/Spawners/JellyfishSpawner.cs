using Towers;
using UnityEngine;
using Waves;

namespace Items.Spawners
{
    public class JellyfishSpawner : BaseSpawner<Jellyfish>
    {
        private readonly WaterGrid _waterGrid;
        private readonly Collider2D _buildZone;

        private float _swimDuration = 2.0f;

        public JellyfishSpawner(Jellyfish prefab, Transform container, WaterGrid waterGrid, Collider2D buildZone) : base(prefab, container)
        {
            _waterGrid = waterGrid;
            _buildZone = buildZone;
        }

        protected override bool TryGetSpawnPosition(out Vector3 position)
        {
            position = Vector3.zero;
            if (_buildZone == null || _waterGrid == null) return false;

            Bounds bounds = _buildZone.bounds;

            Vector2 waveCheckPoint = new Vector2(bounds.max.x - 0.2f, bounds.center.y);
            if (!_waterGrid.IsFlooded(waveCheckPoint))
            {
                return false; // ’вил≥ ще немаЇ або вона вже п≥шла - скасовуЇмо спавн
            }

            for (int i = 0; i < 20; i++)
            {
                float randomX = Random.Range(bounds.min.x + 0.3f, bounds.max.x - 0.3f);
                float randomY = Random.Range(bounds.min.y + 0.5f, bounds.max.y - 0.5f);
                Vector2 randomPoint = new Vector2(randomX, randomY);

                Collider2D[] hits = Physics2D.OverlapCircleAll(randomPoint, 0.3f);
                bool isOccupied = false;
                foreach (var hit in hits)
                {
                    if (hit.GetComponentInParent<IDamageable>() != null)
                    {
                        isOccupied = true;
                        break;
                    }
                }

                if (!isOccupied)
                {
                    position = randomPoint;
                    return true;
                }
            }

            return false;
        }

        protected override void SetupSpawnedItem(Jellyfish item)
        {
            Vector3 targetPos = item.transform.position;

            float startX = Camera.main.ViewportToWorldPoint(new Vector3(1.1f, 0, 0)).x;
            Vector3 startPos = new Vector3(startX, targetPos.y, targetPos.z);

            item.SwimTo(startPos, targetPos, _swimDuration);
        }
    }
}