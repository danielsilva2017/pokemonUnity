using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;
using System;

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

/// <summary>
/// Used to select the adequate effect logic for an effect instance.
/// </summary>
public enum EffectLogic
{
    Freeze, Burn
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
        Duration = RandomInt(2, 2);
        Trigger = Trigger.StartOfTurn;
        EndOnSwitch = false;
    }

    public override IEnumerator OnCreation(Effect effect, Pokemon user, Pokemon target, Battle battle)
    {
        target.Status = Status.Frozen;
        target.CanAttack = false;
        yield return battle.Print($"{target.Name} became frozen!");
    }

    public override IEnumerator Execute(Effect effect, Pokemon user, Pokemon target, Battle battle)
    {
        yield return battle.Print($"{target.Name} is frozen solid!");
    }

    public override IEnumerator OnDeletion(Effect effect, Pokemon user, Pokemon target, Battle battle)
    {
        target.Status = Status.None;
        target.CanAttack = true;
        yield return battle.Print($"{target.Name} has defrosted.");
    }
}

public class Burn : EffectFunctions
{
    public Burn()
    {
        Name = "Burned";
        Trigger = Trigger.EndOfTurn;
        EndOnSwitch = false;
    }

    public override IEnumerator OnCreation(Effect effect, Pokemon user, Pokemon target, Battle battle)
    {
        target.Status = Status.Burned;
        yield return battle.Print($"{target.Name} is now burning!");
    }

    public override IEnumerator Execute(Effect effect, Pokemon user, Pokemon target, Battle battle)
    {
        yield return battle.Print($"{target.Name} is suffering from a burn.");
        var damage = Mathf.FloorToInt(target.MaxHealth / 16f);
        target.Health -= damage < 1 ? 1 : damage;
    }
}