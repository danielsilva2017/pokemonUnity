using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// An instance of an effect. Effects are created through code only; they do not use the Unity UI.
/// </summary>
public class Effect
{
    public EffectFunctions Functions { get; set; }
    public int Turn { get; set; }

    public Effect(EffectLogic logic)
    {
        Functions = GetFunctions(logic);
        Turn = 1;
    }

    // some getters for convenience
    public string Name { get { return Functions.Name; } }
    public int? Duration { get { return Functions.Duration; } }
    public Trigger Trigger { get { return Functions.Trigger; } }
    public bool EndOnSwitch { get { return Functions.EndOnSwitch; } }

    // use reflection to find the correct effect logic
    private EffectFunctions GetFunctions(EffectLogic logic)
    {
        var _class = System.Type.GetType(Enum.GetName(typeof(EffectLogic), logic));
        return (EffectFunctions) Activator.CreateInstance(_class);
    }
}