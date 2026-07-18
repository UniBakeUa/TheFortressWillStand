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
    public Vector2 Scale;
    public Vector3 Position;

    [Header("Water Grid")]
    public float Width;
    public float Height;


    [Header("Camera Options")]
    public float Size;
}
