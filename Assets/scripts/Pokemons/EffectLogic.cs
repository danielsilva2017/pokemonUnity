using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

/// <summary>
/// The actual logic of the effect, holding all life cycle functions.
/// </summary>
public abstract class EffectFunctions
{
    public string Name { get; set; }
    public int? Duration { get; set; } // can be null, meaning infinite duration
    public Trigger Trigger { get; set; }
    public bool EndOnSwitch { get; set; }
    /// <summary>
    /// (Optional) When effect is first applied.
    /// </summary>
    public virtual IEnumerator OnCreation(Effect effect, Pokemon user, Pokemon target, Battle battle) { yield return null; }
    /// <summary>
    /// (Optional) Performed every turn.
    /// </summary>
    public virtual IEnumerator Execute(Effect effect, Pokemon user, Pokemon target, Battle battle) { yield return null; }
    /// <summary>
    /// (Optional) When effect is removed or is or is on its final turn.
    /// </summary>
    public virtual IEnumerator OnDeletion(Effect effect, Pokemon user, Pokemon target, Battle battle) { yield return null; }
    /// <summary>
    /// (Optional) When effect is  triggered in the overworld.
    /// </summary>
    public virtual IEnumerator OnOverworld() { yield return null; }
}

public enum Trigger
{
    StartOfTurn, EndOfTurn, OnDeath, OnSwitchIn, OnSwitchOut
}

public class Freeze : EffectFunctions
{
    public Freeze()
    {
        Name = "Frozen";
        Duration = RandomInt(1, 4);
        Trigger = Trigger.StartOfTurn;
        EndOnSwitch = false;
    }

    public override IEnumerator OnCreation(Effect effect, Pokemon user, Pokemon target, Battle battle)
    {
        target.Status = Status.Frozen;
        target.CanAttack = false;
        battle.Print($"{target.Name} became frozen!");
        yield return null;
    }

    public override IEnumerator Execute(Effect effect, Pokemon user, Pokemon target, Battle battle)
    {
        battle.Print($"{target.Name} is frozen solid!");
        yield return null;
    }

    public override IEnumerator OnDeletion(Effect effect, Pokemon user, Pokemon target, Battle battle)
    {
        target.Status = Status.None;
        target.CanAttack = true;
        battle.Print($"{target.Name} has defrosted.");
        yield return null;
    }
}