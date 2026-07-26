using UnityEngine;

namespace Towers
{
    public interface IDamageFlashTarget
    {
        void SetFlashColor(Color color);
        void ResetColor();
        void Shake(Vector2 offset);
        void ResetPosition();
    }
}
