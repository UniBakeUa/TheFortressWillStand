using UnityEngine;

namespace Towers
{
    public class Fortress : Solid
    {
        public override void Collapse()
        {
            Debug.Log("Fortress зруйновано водою!");
            base.Collapse();
        }
    }
}