using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using static Utils;

/// <summary>
/// The actual logic of the move, holding all life cycle functions.
/// </summary>
public abstract class ItemFunctions
{
    /// <summary>
    /// (Optional) Can the item be used on the target Pokemon right now?
    /// </summary>
    public virtual bool CanBeUsed(Item item, Pokemon target) { return true; }
    /// <summary>
    /// (Optional) Use an item on a Pokemon (e.g. potion). BattleAnimations can be null.
    /// </summary>
    public virtual IEnumerator Use(Item item, Pokemon target, IDialog chatbox, BattleAnimations anims) { yield break; }
    /// <summary>
    /// (Optional) On item used on a Pokemon (e.g. potion). BattleAnimations can be null.
    /// </summary>
    public virtual IEnumerator OnUse(Item item, Pokemon target, IDialog chatbox, BattleAnimations anims) { yield break; }
    /// <summary>
    /// (Optional) Can the item be used on the player right now?
    /// </summary>
    public virtual bool CanBeUsed(Item item, PlayerLogic playerLogic) { return true; }
    /// <summary>
    /// Use an item on the player (e.g. repel).
    /// </summary>
    public virtual IEnumerator Use(Item item, PlayerLogic playerLogic, IDialog chatbox) { yield break; }
    /// <summary>
    /// (Optional) On item used on the player (e.g. repel).
    /// </summary>
    public virtual IEnumerator OnUse(Item item, PlayerLogic playerLogic, IDialog chatbox) { yield break; }
}

/// <summary>
/// Used to select the adequate item logic for an item instance.
/// </summary>
public enum ItemLogic
{
    PokeBall, GreatBall, UltraBall, MasterBall, Potion, SuperPotion, HyperPotion, Repel, Revive
}

public class PokeBall : ItemFunctions
{
    protected float catchMultiplier = 1f;
    protected bool success;

    public override bool CanBeUsed(Item item, Pokemon target)
    {
        return !target.IsAlly;
    }

    public override IEnumerator Use(Item item, Pokemon target, IDialog chatbox, BattleAnimations anims)
    {
        yield return anims.ThrowPokeball(item.Logic, target);
        yield return new WaitForSeconds(0.5f);

        var checkChance = GetCatchChance(target, catchMultiplier);

        for (var i = 1; i <= 4; i++)
        {
            if (Chance(checkChance) && i < 4)
            {
                yield return anims.ShakePokeball();
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                success = false;
                yield return anims.FailPokeball(item.Logic, target);
                yield break;
            }
        }

        success = true;
    }

    public override IEnumerator OnUse(Item item, Pokemon target, IDialog chatbox, BattleAnimations anims)
    {
        if (success)
        {
            target.Pokeball = item.Logic;
            SceneInfo.SetForcedOutcome(Outcome.Caught);
        }
        else
        {
            yield return chatbox.Print("Oh no! It broke free!");
            yield return new WaitForSeconds(1f);
        }
    }
}

public class GreatBall : PokeBall
{
    public GreatBall() { catchMultiplier = 1.5f; }
}

public class UltraBall : PokeBall
{
    public UltraBall() { catchMultiplier = 2f; }
}

public class MasterBall : PokeBall
{
    public MasterBall() { success = true; }

    public override IEnumerator Use(Item item, Pokemon target, IDialog chatbox, BattleAnimations anims)
    {
        yield return anims.ThrowPokeball(item.Logic, target);
        yield return new WaitForSeconds(0.5f);

        for (var i = 1; i <= 3; i++)
        {
            yield return anims.ShakePokeball();
            yield return new WaitForSeconds(0.5f);
        }
    }
}

public class Potion : ItemFunctions
{
    protected int heal = 20;
    int healed;

    public override bool CanBeUsed(Item item, Pokemon target)
    {
        return target.Status != Status.Fainted && target.Health < target.MaxHealth;
    }

    public override IEnumerator Use(Item item, Pokemon target, IDialog chatbox, BattleAnimations anims)
    {
        healed = Math.Min(heal, target.MaxHealth - target.Health);
        target.Health += healed;
        yield break;
    }

    public override IEnumerator OnUse(Item item, Pokemon target, IDialog chatbox, BattleAnimations anims)
    {
        yield return chatbox.Print($"{target.Name} recovered {healed} health!");
    }
}

public class SuperPotion : Potion
{
    public SuperPotion() { heal = 50; }
}

public class HyperPotion : Potion
{
    public HyperPotion() { heal = 200; }
}

public class Repel : ItemFunctions
{
    protected int steps = 100;

    public override IEnumerator Use(Item item, PlayerLogic playerLogic, IDialog chatbox)
    {
        yield return chatbox.Print("dont care didnt ask");
    }
}

public class Revive : ItemFunctions
{
    int healed;

    public override bool CanBeUsed(Item item, Pokemon target)
    {
        return target.Status == Status.Fainted;
    }

    public override IEnumerator Use(Item item, Pokemon target, IDialog chatbox, BattleAnimations anims)
    {
        target.Status = Status.None;
        healed = target.MaxHealth / 2;
        target.Health += healed;
        yield break;
    }

    public override IEnumerator OnUse(Item item, Pokemon target, IDialog chatbox, BattleAnimations anims)
    {
        yield return chatbox.Print($"{target.Name} recovered {healed} health!");
    }
}