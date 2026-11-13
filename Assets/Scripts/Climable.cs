using System;
using UnityEngine;

public enum ClimbPointType { Jump, Move }

[Serializable]
public struct ClimbPoint
{
    public ClimbPointType climbPointType;
    public Climable climablePoint;
    public bool isTwoWay;
}

public class Climable : MonoBehaviour
{
    public ClimbPoint upClimbPoint;
    public ClimbPoint leftClimbPoint;
    public ClimbPoint rightClimbPoint;
    public ClimbPoint downClimbPoint;
}
