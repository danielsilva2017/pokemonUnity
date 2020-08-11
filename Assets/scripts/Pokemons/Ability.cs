using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// An instance of an ability, based on a skeleton.
/// </summary>
public class Ability
{
    public AbilityBase Skeleton { get; set; }
    public AbilityFunctions Functions { get; set; }
    public int Turn { get; set; }

    public Ability(AbilityBase skeleton)
    {
        Skeleton = skeleton;
        Functions = GetFunctions(skeleton.logic);
    }

    // use reflection to find the correct ability logic
    private AbilityFunctions GetFunctions(AbilityLogic logic)
    {
        var _class = System.Type.GetType(Enum.GetName(typeof(AbilityLogic), logic));
        return (AbilityFunctions) Activator.CreateInstance(_class);
    }
}
