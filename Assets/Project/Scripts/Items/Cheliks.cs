using UnityEngine;

namespace Items
{
    public class Cheliks : ClickableItem
    {
        private void Start()
        {
            transform.rotation = new Quaternion(transform.rotation.x,
                                                transform.rotation.y,
                                                Random.Range(0f, 1f),
                                                transform.rotation.w);
        }
    }
}