using UnityEngine;

[CreateAssetMenu(fileName = "ParkourActionSO", menuName = "Scriptable Objects/ParkourActionSO")]
public class ParkourActionSO : ScriptableObject
{
    public float minHeight;
    public float maxHeight;
    public AvatarTarget matchBodyPart;
    public Vector3 weightMaskVector;
    [Range(0, 1)] public float matchStartTime;
    [Range(0, 1)] public float matchTargetTime;
}
