using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Ability", menuName = "Ability")]

/// <summary>
/// The skeleton data for an ability, which can be set through the Unity UI.
/// </summary>
public class AbilityBase : ScriptableObject
{
    public string abilityName;
    [TextArea] public string description;
    public AbilityLogic logic;
}
