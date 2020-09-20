using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;
using System;

/// <summary>
/// The actual logic of the move, holding all life cycle functions.
/// </summary>
public abstract class MoveFunctions
{
    /// <summary>
    /// Use a move.
    /// </summary>
    public abstract IEnumerator Execute(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount);
    /// <summary>
    /// (Optional) On move being used.
    /// </summary>
    public virtual IEnumerator OnUse(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount) { yield break; }
    /// <summary>
    /// (Optional) On move hitting.
    /// </summary>
    public virtual IEnumerator OnHit(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount) { yield break; }
    /// <summary>
    /// (Optional) On move missing.
    /// </summary>
    public virtual IEnumerator OnMiss(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount) { yield break; }
    /// <summary>
    /// (Optional) When move is used in the overworld.
    /// </summary>
    public virtual IEnumerator OnOverworld() { yield break; }
}

/// <summary>
/// Used to select the adequate move logic for a move instance.
/// </summary>
public enum MoveLogic
{
    Struggle, Scratch, Tackle, VineWhip, BlazeKick, Blizzard, Ember, Growl, TailWhip, SandAttack, Growth, RazorLeaf, PoisonPowder, SleepPowder, StunSpore,
    LeechSeed
}

/// <summary>
/// Single - targets someone. "target" should be determined on effect creation.
/// Requires user and target.
/// <para />
/// Self - targets self. "target" is equal to "user". Requires user.
/// <para />
/// Adjacent - targets all enemies or allies around the primary "target",
/// excluding self ("user"). Requires user and target.
/// <para />
/// Allies - targets active allies. "target" is an active ally. Requires user.
/// <para />
/// Enemies - targets active enemies. "target" is an active foe. Requires user.
/// <para />
/// All - targets all actives. "target" is an active entity.
/// </summary>
public enum Targeting
{
    Single, Self, Adjacent, Allies, Enemies, All
}

public class Struggle : MoveFunctions
{
    private int damage;

    public override IEnumerator Execute(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount)
    {
        damage = CalcDamage(move, user, target, battle, targetCount);
        target.Health -= damage;
        yield break;
    }

    public override IEnumerator OnHit(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount)
    {
        yield return battle.Print($"{user.Name} received some recoil damage.");
        var recoil = Mathf.FloorToInt(damage * 0.25f);
        user.Health -= recoil < 1 ? 1 : recoil;
    }
}

public class Scratch : MoveFunctions
{
    public override IEnumerator Execute(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount)
    {
        target.Health -= CalcDamage(move, user, target, battle, targetCount);
        yield break;
    }
}

public class Tackle : MoveFunctions
{
    public override IEnumerator Execute(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount)
    {
        target.Health -= CalcDamage(move, user, target, battle, targetCount);
        yield break;
    }
}

public class VineWhip : MoveFunctions
{
    public override IEnumerator Execute(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount)
    {
        target.Health -= CalcDamage(move, user, target, battle, targetCount);
        yield break;
    }
}

public class BlazeKick : MoveFunctions
{
    public override IEnumerator Execute(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount)
    {
        user.CritStage += 2;
        target.Health -= CalcDamage(move, user, target, battle, targetCount);
        user.CritStage -= 2;

        if (target.Status == Status.None && Chance(10))
            yield return battle.Logic.AddEffect(EffectLogic.Burn, user, target);
    }
}

public class Blizzard : MoveFunctions
{
    public override IEnumerator Execute(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount)
    {
        target.Health -= CalcDamage(move, user, target, battle, targetCount);

        if (target.Status == Status.None && Chance(10))
            yield return battle.Logic.AddEffect(EffectLogic.Freeze, user, target);
    }
}

public class Ember : MoveFunctions
{
    public override IEnumerator Execute(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount)
    {
        target.Health -= CalcDamage(move, user, target, battle, targetCount);

        if (target.Status == Status.None && Chance(10))
            yield return battle.Logic.AddEffect(EffectLogic.Burn, user, target);
    }
}

public class Growl : MoveFunctions
{
    public override IEnumerator Execute(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount)
    {
        target.AttackStage--;
        yield return battle.Print($"{target.Name}'s attack fell!");
    }
}

public class TailWhip : MoveFunctions
{
    public override IEnumerator Execute(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount)
    {
        target.DefenseStage--;
        yield return battle.Print($"{target.Name}'s defense fell!");
    }
}

public class SandAttack : MoveFunctions
{
    public override IEnumerator Execute(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount)
    {
        target.AccuracyStage--;
        yield return battle.Print($"{target.Name}'s accuracy fell!");
    }
}

public class Growth : MoveFunctions
{
    public override IEnumerator Execute(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount)
    {
        if (battle.Logic.Weather == Weather.Sunny)
        {
            user.AttackStage += 2;
            user.SpAttackStage += 2;
            yield return battle.Print($"{user.Name} grew much stronger!");
        }
        else
        {
            user.AttackStage++;
            user.SpAttackStage++;
            yield return battle.Print($"{user.Name} grew stronger!");
        }
    }
}

public class RazorLeaf : MoveFunctions
{
    public override IEnumerator Execute(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount)
    {
        user.CritStage += 2;
        target.Health -= CalcDamage(move, user, target, battle, targetCount);
        user.CritStage -= 2;
        yield break;
    }
}

public class PoisonPowder : MoveFunctions
{
    public override IEnumerator Execute(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount)
    {
        if (target.Status == Status.None && !target.IsType(Type.Grass) && !target.IsType(Type.Poison) && !target.IsType(Type.Steel))
            yield return battle.Logic.AddEffect(EffectLogic.Poison, user, target);
        else
            yield return battle.Print("But it failed!");
    }
}

public class SleepPowder : MoveFunctions
{
    public override IEnumerator Execute(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount)
    {
        if (target.Status == Status.None && !target.IsType(Type.Grass))
            yield return battle.Logic.AddEffect(EffectLogic.Sleep, user, target);
        else
            yield return battle.Print("But it failed!");
    }
}

public class StunSpore : MoveFunctions
{
    public override IEnumerator Execute(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount)
    {
        if (target.Status == Status.None && !target.IsType(Type.Grass) && !target.IsType(Type.Electric))
            yield return battle.Logic.AddEffect(EffectLogic.Paralysis, user, target);
        else
            yield return battle.Print("But it failed!");
    }
}

public class LeechSeed : MoveFunctions
{
    public override IEnumerator Execute(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount)
    {
        if (!battle.Logic.EffectExistsOnTarget(EffectLogic.LeechSeeded, target) && !target.IsType(Type.Grass))
        {
            yield return battle.Logic.AddEffect(EffectLogic.LeechSeeded, user, target);
        }
        else
            yield return battle.Print("But it failed!");
    }
}