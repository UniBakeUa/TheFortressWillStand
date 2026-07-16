using UnityEngine;

namespace Items.Spawners
{
    public class AirplaneSpawner : BaseSpawner<Airplane>
    {
        private Camera _camera;

        public AirplaneSpawner(
            Airplane prefab,
            Transform container)
            : base(prefab, container)
        {
            _camera = Camera.main;
        }

        protected override bool TryGetSpawnPosition(
            out Vector3 position)
        {
            Vector3 min =
                _camera.ViewportToWorldPoint(
                    new Vector3(0, 0));

            Vector3 max =
                _camera.ViewportToWorldPoint(
                    new Vector3(1, 1));

            int side = Random.Range(0, 4);

            switch (side)
            {
                case 0:
                    position = new Vector3(
                        Random.Range(min.x, max.x),
                        max.y + 2f);

                    break;

                case 1:
                    position = new Vector3(
                        Random.Range(min.x, max.x),
                        min.y - 2f);

                    break;

                case 2:
                    position = new Vector3(
                        min.x - 2f,
                        Random.Range(min.y, max.y));

                    break;

                default:
                    position = new Vector3(
                        max.x + 2f,
                        Random.Range(min.y, max.y));

                    break;
            }

            return true;
        }

        protected override void SetupSpawnedItem(
            Airplane item)
        {
            Vector3 center =
                _camera.ViewportToWorldPoint(
                    new Vector3(0.5f, 0.5f));

            Vector2 direction =
                (center - item.transform.position)
                .normalized;

            item.StartFlight(direction);
        }
    }
}