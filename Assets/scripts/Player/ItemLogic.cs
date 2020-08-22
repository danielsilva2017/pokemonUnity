using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// The actual logic of the move, holding all life cycle functions.
/// </summary>
public abstract class ItemFunctions
{
    /// <summary>
    /// (Optional) Use an item on a Pokemon.
    /// </summary>
    public virtual IEnumerator Use(Item item, Pokemon target, IBattle battle) { yield break; }
    /// <summary>
    /// (Optional) On item being used on a Pokemon.
    /// </summary>
    public virtual IEnumerator OnUse(Item item, Pokemon target, IBattle battle) { yield break; }
    /// <summary>
    /// Use an item on the player.
    /// </summary>
    public virtual IEnumerator Use(Item item, Player player) { yield break; }
    /// <summary>
    /// (Optional) On item being used on the player.
    /// </summary>
    public virtual IEnumerator OnUse(Item item, Player player) { yield break; }
}

/// <summary>
/// Used to select the adequate item logic for an item instance.
/// </summary>
public enum ItemLogic
{
    Pokeball, SuperPotion
}

public class Pokeball
{

}

public class SuperPotion : ItemFunctions
{
    public override IEnumerator Use(Item item, Pokemon target, IBattle battle)
    {
        var heal = Math.Max(50, target.MaxHealth - target.Health);
        target.Health += heal;
        yield return battle.Print($"{target.Name} recovered {heal} health!");
    }
}