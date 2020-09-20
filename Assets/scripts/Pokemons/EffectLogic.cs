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
    public virtual IEnumerator OnCreation(Effect effect, Pokemon user, Pokemon target, IBattle battle) { yield return null; }
    /// <summary>
    /// (Optional) Performed every turn.
    /// </summary>
    public virtual IEnumerator Execute(Effect effect, Pokemon user, Pokemon target, IBattle battle) { yield return null; }
    /// <summary>
    /// (Optional) When effect is removed or is or is on its final turn.
    /// </summary>
    public virtual IEnumerator OnDeletion(Effect effect, Pokemon user, Pokemon target, IBattle battle) { yield return null; }
    /// <summary>
    /// (Optional) When effect target is switched out.
    /// </summary>
    public virtual IEnumerator OnSwitchOut(Effect effect, Pokemon user, Pokemon target, IBattle battle) { yield return null; }
    /// <summary>
    /// (Optional) When effect is triggered in the overworld.
    /// </summary>
    public virtual IEnumerator OnOverworld() { yield return null; }
}

/// <summary>
/// Used to select the adequate effect logic for an effect instance.
/// </summary>
public enum EffectLogic
{
    Freeze, Burn, Poison, ToxicPoison, Sleep, Paralysis, LeechSeeded
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

    public override IEnumerator OnCreation(Effect effect, Pokemon user, Pokemon target, IBattle battle)
    {
        target.Status = Status.Frozen;
        target.CanAttack = false;
        yield return battle.Print($"{target.Name} became frozen!");
    }

    public override IEnumerator Execute(Effect effect, Pokemon user, Pokemon target, IBattle battle)
    {
        yield return battle.Print($"{target.Name} is frozen solid!");
    }

    public override IEnumerator OnDeletion(Effect effect, Pokemon user, Pokemon target, IBattle battle)
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

    public override IEnumerator OnCreation(Effect effect, Pokemon user, Pokemon target, IBattle battle)
    {
        target.Status = Status.Burned;
        yield return battle.Print($"{target.Name} is now burning!");
    }

    public override IEnumerator Execute(Effect effect, Pokemon user, Pokemon target, IBattle battle)
    {
        yield return battle.Print($"{target.Name} is suffering from a burn.");
        var damage = Mathf.FloorToInt(target.MaxHealth / 16f);
        target.Health -= damage < 1 ? 1 : damage;
    }
}

public class Poison : EffectFunctions
{
    public Poison()
    {
        Name = "Poisoned";
        Trigger = Trigger.EndOfTurn;
        EndOnSwitch = false;
    }

    public override IEnumerator OnCreation(Effect effect, Pokemon user, Pokemon target, IBattle battle)
    {
        target.Status = Status.Poisoned;
        yield return battle.Print($"{target.Name} is now poisoned!");
    }

    public override IEnumerator Execute(Effect effect, Pokemon user, Pokemon target, IBattle battle)
    {
        yield return battle.Print($"{target.Name} is poisoned.");
        var damage = Mathf.FloorToInt(target.MaxHealth / 16f);
        target.Health -= damage < 1 ? 1 : damage;
    }
}

public class ToxicPoison : EffectFunctions
{
    public ToxicPoison()
    {
        Name = "Badly Poisoned";
        Trigger = Trigger.EndOfTurn;
        EndOnSwitch = false;
    }

    public override IEnumerator OnCreation(Effect effect, Pokemon user, Pokemon target, IBattle battle)
    {
        target.Status = Status.Toxic;
        yield return battle.Print($"{target.Name} is now badly poisoned!");
    }

    public override IEnumerator Execute(Effect effect, Pokemon user, Pokemon target, IBattle battle)
    {
        yield return battle.Print($"{target.Name} is badly poisoned.");
        var damage = Mathf.FloorToInt(target.MaxHealth / (16f / effect.Turn));
        target.Health -= damage < 1 ? 1 : damage;
    }

    public override IEnumerator OnSwitchOut(Effect effect, Pokemon user, Pokemon target, IBattle battle)
    {
        effect.Turn = 1;
        yield break;
    }
}

public class Sleep : EffectFunctions
{
    public Sleep()
    {
        Name = "Asleep";
        Duration = RandomInt(1, 4);
        Trigger = Trigger.StartOfTurn;
        EndOnSwitch = false;
    }

    public override IEnumerator OnCreation(Effect effect, Pokemon user, Pokemon target, IBattle battle)
    {
        target.Status = Status.Sleeping;
        target.CanAttack = false;
        yield return battle.Print($"{target.Name} fell asleep!");
    }

    public override IEnumerator Execute(Effect effect, Pokemon user, Pokemon target, IBattle battle)
    {
        yield return battle.Print($"{target.Name} is asleep!");
    }

    public override IEnumerator OnDeletion(Effect effect, Pokemon user, Pokemon target, IBattle battle)
    {
        target.Status = Status.None;
        target.CanAttack = true;
        yield return battle.Print($"{target.Name} woke up.");
    }
}

public class Paralysis : EffectFunctions
{
    public Paralysis()
    {
        Name = "Paralysed";
        Trigger = Trigger.StartOfTurn;
        EndOnSwitch = false;
    }

    public override IEnumerator OnCreation(Effect effect, Pokemon user, Pokemon target, IBattle battle)
    {
        target.Status = Status.Paralyzed;
        target.CanAttack = Chance(75);
        yield return battle.Print($"{target.Name} became paralysed!");
        if (!target.CanAttack) yield return battle.Print($"{target.Name} can't move!");
    }

    public override IEnumerator Execute(Effect effect, Pokemon user, Pokemon target, IBattle battle)
    {
        target.CanAttack = Chance(75);
        if (!target.CanAttack) yield return battle.Print($"{target.Name} is paralysed and can't move!");
    }

    public override IEnumerator OnDeletion(Effect effect, Pokemon user, Pokemon target, IBattle battle)
    {
        target.Status = Status.None;
        target.CanAttack = true;
        yield return battle.Print($"{target.Name} recovered from paralysis.");
    }
}

public class LeechSeeded : EffectFunctions
{
    public LeechSeeded()
    {
        Name = "Seeded";
        Trigger = Trigger.EndOfTurn;
        EndOnSwitch = true;
    }

    public override IEnumerator OnCreation(Effect effect, Pokemon user, Pokemon target, IBattle battle)
    {
        yield return battle.Print($"{target.Name} is now seeded!");
    }

    public override IEnumerator Execute(Effect effect, Pokemon user, Pokemon target, IBattle battle)
    {
        if (battle.Logic.ActiveAllies.Contains(user) && user.Health > 0)
        {
            var sapped = Limit(1, Mathf.FloorToInt(target.Health * 0.125f), target.Health);
            target.Health -= sapped;
            user.Health += sapped;
            yield return battle.Print($"{user.Name} sapped some of {target.Name}'s health.");
        }
    }
}