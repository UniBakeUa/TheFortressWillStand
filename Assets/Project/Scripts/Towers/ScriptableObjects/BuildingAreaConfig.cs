using UnityEngine;

[CreateAssetMenu(fileName = "BuildingArea", menuName = "Building/Building Area")]
public class BuildingAreaConfig : ScriptableObject
{
    [Header("Building area")]
    public float SnapRadius = 2f;
    public float MinBuildX = -10f;
    public float MaxBuildX = 10f;
    public float MinBuildY = -10f;
    public float MaxBuildY = 10f;

    [Header("Sand area")]
    public Vector2 SandScale;
    public Vector3 SandPosition;

    [Header("Water Grid")]
    public int GridWidth;
    public int GridHeight;
    public Vector2 GridOrigin;
    public float RisingTime;


    [Header("Camera Options")]
    public float CameraSize;
}
