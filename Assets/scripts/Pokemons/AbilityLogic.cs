using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

/// <summary>
/// The actual logic of the ability, holding all life cycle functions.
/// </summary>
public abstract class AbilityFunctions
{
    /// <summary>
    /// (Optional) On user switching in (or battle starting with user as first Pokemon).
    /// </summary>
    public virtual IEnumerator OnSwitchIn(Ability ability, Pokemon user, Battle battle) { yield return null; }
    /// <summary>
    /// (Optional) On turn beginning.
    /// </summary>
    public virtual IEnumerator OnTurnBeginning(Ability ability, Pokemon user, Battle battle) { yield return null; }
    /// <summary>
    /// (Optional) On turn ending.
    /// </summary>
    public virtual IEnumerator OnTurnEnding(Ability ability, Pokemon user, Battle battle) { yield return null; }
    /// <summary>
    /// (Optional) On user switching out.
    /// </summary>
    public virtual IEnumerator OnSwitchOut(Ability ability, Pokemon user, Battle battle) { yield return null; }
    /// <summary>
    /// (Optional) On user dying.
    /// </summary>
    public virtual IEnumerator OnDeath(Ability ability, Pokemon user, Battle battle) { yield return null; }
    /// <summary>
    /// (Optional) When ability is used in the overworld.
    /// </summary>
    public virtual IEnumerator OnOverworld() { yield return null;  }
}

/// <summary>
/// Used to select the adequate ability logic for an ability instance.
/// </summary>
public enum AbilityLogic
{
    Intimidate, SpeedBoost
}

public class Intimidate : AbilityFunctions
{
    public override IEnumerator OnSwitchIn(Ability ability, Pokemon user, Battle battle)
    {
        var targets = user.IsAlly ? battle.Logic.ActiveEnemies : battle.Logic.ActiveAllies;
        targets.ForEach(target =>
        {
            target.AttackStage--;
            battle.Print($"{target.Name}'s attack fell!");
        });
        yield return null;
    }
}

public class SpeedBoost : AbilityFunctions
{
    public override IEnumerator OnTurnEnding(Ability ability, Pokemon user, Battle battle)
    {
        user.SpeedStage++;
        battle.Print($"{user.Name}'s speed rose!");
        yield return null;
    }
}