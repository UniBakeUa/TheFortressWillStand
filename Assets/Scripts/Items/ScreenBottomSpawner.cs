using UnityEngine;

namespace Items
{
    public class ScreenBottomSpawner : BaseSpawner<PONCHIC>
    {
        protected override bool TryGetSpawnPosition(out Vector3 position)
        {
            position = Camera.main.ViewportToWorldPoint(new Vector3(Random.Range(0.1f, 0.9f), -0.1f, 10f));

            return true;
        }
    }
}