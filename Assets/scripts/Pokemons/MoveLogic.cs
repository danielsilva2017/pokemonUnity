using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

/// <summary>
/// The actual logic of the move, holding all life cycle functions.
/// </summary>
public abstract class MoveFunctions
{
    /// <summary>
    /// Use a move.
    /// </summary>
    public abstract IEnumerator Execute(Move move, Pokemon user, Pokemon target, Battle battle, int targetCount);
    /// <summary>
    /// (Optional) On move being used.
    /// </summary>
    public virtual IEnumerator OnUse(Move move, Pokemon user, Pokemon target, Battle battle, int targetCount) { yield return null; }
    /// <summary>
    /// (Optional) On move hitting.
    /// </summary>
    public virtual IEnumerator OnHit(Move move, Pokemon user, Pokemon target, Battle battle, int targetCount) { yield return null; }
    /// <summary>
    /// (Optional) On move missing.
    /// </summary>
    public virtual IEnumerator OnMiss(Move move, Pokemon user, Pokemon target, Battle battle, int targetCount) { yield return null; }
    /// <summary>
    /// (Optional) When move is used in the overworld.
    /// </summary>
    public virtual IEnumerator OnOverworld() { yield return null; }
}

/// <summary>
/// Used to select the adequate move logic for a move instance.
/// </summary>
public enum MoveLogic
{
    Struggle, Scratch, Tackle, VineWhip
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
/// Foes - targets active foes. "target" is an active foe. Requires user.
/// <para />
/// All - targets all actives. "target" is an active entity.
/// </summary>
public enum Targeting
{
    Single, Self, Adjacent, Allies, Foes, All
}

public class Struggle : MoveFunctions
{
    private int damage;

    public override IEnumerator Execute(Move move, Pokemon user, Pokemon target, Battle battle, int targetCount)
    {
        damage = CalcDamage(move, user, target, battle, targetCount);
        target.Health -= damage;
        yield return null;
    }

    public override IEnumerator OnHit(Move move, Pokemon user, Pokemon target, Battle battle, int targetCount)
    {
        battle.Print($"{user.Name} received some recoil damage.");
        user.Health -= Mathf.FloorToInt(damage * 0.25f);
        yield return null;
    }
}

public class Scratch : MoveFunctions
{
    public override IEnumerator Execute(Move move, Pokemon user, Pokemon target, Battle battle, int targetCount)
    {
        target.Health -= CalcDamage(move, user, target, battle, targetCount);
        yield return null;
    }
}

public class Tackle : MoveFunctions
{
    public override IEnumerator Execute(Move move, Pokemon user, Pokemon target, Battle battle, int targetCount)
    {
        yield return battle.Print($"{user.Name} received some recoil damage.");
        target.Health -= CalcDamage(move, user, target, battle, targetCount);
        yield return null;
    }
}

public class VineWhip : MoveFunctions
{
    public override IEnumerator Execute(Move move, Pokemon user, Pokemon target, Battle battle, int targetCount)
    {
        target.Health -= CalcDamage(move, user, target, battle, targetCount);
        yield return null;
    }
}