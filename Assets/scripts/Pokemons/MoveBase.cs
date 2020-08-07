using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Move", menuName = "Move")]

public class MoveBase : ScriptableObject
{
    public string moveName;
    [TextArea] public string description;
    public Type.TypeName type;
    public int power;
    public int accuracy;
    public int points;
}
