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
    /// (Optional) Use an item on a Pokemon (e.g. potion).
    /// </summary>
    public virtual IEnumerator Use(Item item, Pokemon target, IBattle battle) { yield break; }
    /// <summary>
    /// (Optional) On item used on a Pokemon (e.g. potion).
    /// </summary>
    public virtual IEnumerator OnUse(Item item, Pokemon target, IBattle battle) { yield break; }
    /// <summary>
    /// Use an item on the player (e.g. repel).
    /// </summary>
    public virtual IEnumerator Use(Item item, Player player) { yield break; }
    /// <summary>
    /// (Optional) On item used on the player (e.g. repel).
    /// </summary>
    public virtual IEnumerator OnUse(Item item, Player player) { yield break; }
}

/// <summary>
/// Used to select the adequate item logic for an item instance.
/// </summary>
public enum ItemLogic
{
    PokeBall, SuperPotion, HyperPotion
}

public class PokeBall : ItemFunctions
{
    float catchMultiplier = 1f;
    bool success;

    public override IEnumerator Use(Item item, Pokemon target, IBattle battle)
    {
        var checkChance = GetCatchChance(target, catchMultiplier);

        for (var i = 1; i <= 4; i++)
        {
            if (Chance(checkChance)) yield return battle.Print($"<check {i}/4>");
            else
            {
                success = false;
                yield break;
            }
        }

        success = true;
    }

    public override IEnumerator OnUse(Item item, Pokemon target, IBattle battle)
    {
        if (success)
        {
            var party = battle.PlayerInfo.Player.Pokemons;
            if (party.Count >= 6) yield return battle.Print("<pokemon sent to PC (soon)>");
            else
            {
                party.Add(target);
                yield return battle.Print($"{target.Name} was caught!");
            }

            battle.Logic.SetForcedOutcome(Outcome.Caught);
        }
        else
        {
            yield return battle.Print("Oh no! It broke free!");
        }
    }
}

public class SuperPotion : ItemFunctions
{
    int heal;

    public override IEnumerator Use(Item item, Pokemon target, IBattle battle)
    {
        heal = Math.Min(50, target.MaxHealth - target.Health);
        target.Health += heal;
        yield break;
    }

    public override IEnumerator OnUse(Item item, Pokemon target, IBattle battle)
    {
        yield return battle.Print($"{target.Name} recovered {heal} health!");
    }
}

public class HyperPotion : ItemFunctions
{
    int heal;

    public override IEnumerator Use(Item item, Pokemon target, IBattle battle)
    {
        heal = Math.Min(200, target.MaxHealth - target.Health);
        target.Health += heal;
        yield break;
    }

    public override IEnumerator OnUse(Item item, Pokemon target, IBattle battle)
    {
        yield return battle.Print($"{target.Name} recovered {heal} health!");
    }
}