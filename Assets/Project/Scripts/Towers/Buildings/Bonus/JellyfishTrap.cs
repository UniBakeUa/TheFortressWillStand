using Items;
using UnityEngine;

namespace Towers.Buildings.Bonus
{
    public class JellyfishTrap : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out Jellyfish jellyfish))
            {
                jellyfish.Collapse();
            }
        }
    }
}