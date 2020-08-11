using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An instance of an effect. Effects are created through code only; they do not use the Unity UI.
/// </summary>
public class Effect
{
    public EffectFunctions Functions { get; set; }
    public int Turn { get; set; }

    public Effect(EffectFunctions functions)
    {
        Functions = functions;
        Turn = 1;
    }

    public int? Duration { get { return Functions.Duration; } }
}