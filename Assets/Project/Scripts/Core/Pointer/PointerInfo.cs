using System;
using UnityEngine;

public class PointerInfo : MonoBehaviour
{
    public static Vector2 PointerWorldPosition { get; private set; }
    public static Action<Vector2, bool> LeftMouseButtonDown;

    private void Update()
    {
        PointerWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButtonDown(0))
        {
            LeftMouseButtonDown?.Invoke(PointerWorldPosition, true);
        }
        if (Input.GetMouseButtonUp(0))
        {
            LeftMouseButtonDown?.Invoke(PointerWorldPosition, false);
        }
    } 
}
