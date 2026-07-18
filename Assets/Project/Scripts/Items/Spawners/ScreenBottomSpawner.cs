using UnityEngine;

namespace Items.Spawners
{
    public class ScreenBottomSpawner : BaseSpawner<PONCHIC>
    {
        public ScreenBottomSpawner(PONCHIC prefab, Transform container) : base(prefab, container)
        {

        }

        protected override bool TryGetSpawnPosition(out Vector3 position)
        {
            position = Camera.main.ViewportToWorldPoint(new Vector3(Random.Range(0.1f, 0.9f), -0.1f, 10f));

            return true;
        }
    }
}