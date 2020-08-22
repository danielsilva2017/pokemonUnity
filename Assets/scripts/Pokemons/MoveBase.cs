using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Move", menuName = "Move")]

/// <summary>
/// The skeleton data for a move, which can be set through the Unity UI.
/// </summary>
public class MoveBase : ScriptableObject
{
    public string moveName;
    [TextArea] public string description;
    public Type type;
    public MoveCategory category;
    public Targeting targeting;
    public int points; // 0 = infinite
    public int accuracy; // 1-100. 0 = never misses
    public int power;
    public MoveLogic logic;
}

public enum MoveCategory
{
    Physical, Special, Status
}