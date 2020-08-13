using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static Utils;

public enum Weather
{
    Sunny, Rain, Hail, Sandstorm, None
}

public enum Outcome
{
    Win, Loss, Escaped, Undecided
}

public class EffectCommand
{
    public Effect Effect { get; set; }
    public Pokemon User { get; set; }
    public Pokemon Target { get; set; }
    public EffectCommand(Effect effect, Pokemon user, Pokemon target) { Effect = effect; User = user; Target = target; }
}

public class MoveCommand
{
    public Move Move { get; set; }
    public Pokemon User { get; set; }
    /// <summary> This value may be null. </summary>
    public Pokemon Target { get; set; }
    public MoveCommand(Move move, Pokemon user) { Move = move; User = user; }
    public MoveCommand(Move move, Pokemon user, Pokemon target) { Move = move; User = user; Target = target; }
}

public class SwitchCommand
{
    public Pokemon SwitchedIn { get; set; }
    public Pokemon SwitchedOut { get; set; }
    public SwitchCommand(Pokemon switchedIn, Pokemon switchedOut) { SwitchedIn = switchedIn; SwitchedOut = switchedOut; }
}

public class BattleLogic
{
    private Battle battleUI;
    private List<EffectCommand> effectQueue; // some elements may be null
    private List<SwitchCommand> switchQueue; // some elements may be null

    public int BattleSize { get; set; }
    public int TurnNumber { get; set; }
    public List<Pokemon> PartyAllies { get; set; }
    public List<Pokemon> ActiveAllies { get; set; }
    public List<Pokemon> PartyEnemies { get; set; }
    public List<Pokemon> ActiveEnemies { get; set; }
    public Weather Weather { get; set; }
    public Outcome Outcome { get; set; }

    public BattleLogic(Battle battle, List<Pokemon> allies, List<Pokemon> enemies, int battleSize, Weather weather)
    {
        battleUI = battle;
        BattleSize = battleSize;
        ActiveAllies = allies.GetRange(0, battleSize);
        PartyAllies = allies.GetRange(battleSize, allies.Count - battleSize);
        ActiveEnemies = enemies.GetRange(0, battleSize);
        PartyEnemies = enemies.GetRange(battleSize, enemies.Count - battleSize);
        Weather = Weather.None;
        Outcome = Outcome.Undecided;
        TurnNumber = 1;
        effectQueue = new List<EffectCommand>();
        switchQueue = new List<SwitchCommand>();

        allies.ForEach(pkmn => pkmn.IsAlly = true);
        enemies.ForEach(pkmn => pkmn.IsAlly = false);

        foreach (var pkmn in ActivePokemons())
            pkmn.ExpCandidates = new List<Pokemon>(pkmn.IsAlly ? ActiveEnemies : ActiveAllies);
    }

    public List<Pokemon> SortBySpeed()
    {
        var actives = ActivePokemons();
        actives.Sort();
        return actives;
    }

    public IEnumerator AddEffect(EffectLogic logic, Pokemon user, Pokemon target)
    {
        if (target.Health <= 0) yield break;

        var addedEffect = new Effect(logic);
        var cmd = new EffectCommand(addedEffect, user, target);
        if (EffectExists(addedEffect, user, target)) yield break;

        battleUI.NotifyUpdateHealth();
        yield return addedEffect.Functions.OnCreation(addedEffect, user, target, battleUI);
        battleUI.NotifyUpdateHealth();
        effectQueue.Add(cmd);
    }

    public bool EffectExists(Effect effect, Pokemon user, Pokemon target)
    {
        return effectQueue.Any(
            cmd => cmd != null && 
            cmd.Effect.Name == effect.Name && 
            cmd.User == user && 
            (target == null || cmd.Target == target)
        );
    }

    public bool EffectExists(EffectLogic logic, Pokemon user, Pokemon target)
    {
        return EffectExists(new Effect(logic), user, target);
    }

    public bool EffectExists(Effect effect, Pokemon user)
    {
        return EffectExists(effect, user, null);
    }

    public bool EffectExists(EffectLogic logic, Pokemon user)
    {
        return EffectExists(new Effect(logic), user, null);
    }

    public IEnumerator Init()
    {
        var order = SortBySpeed();

        foreach (var user in order)
        {
            yield return user.Ability.Functions.OnSwitchIn(user.Ability, user, battleUI);
            battleUI.NotifyUpdateHealth();
        }

        battleUI.NotifyTurnFinished();
    }

    /// <summary>
    /// Performs a full turn of the battle. The move queue can have null elements.
    /// </summary>
    public IEnumerator Turn(List<MoveCommand> moveQueue)
    {
        if (Weather != Weather.None) yield return Print(WeatherToString());

        // possible early exit (already won/lost)
        if (CheckVictory() != Outcome.Undecided)
        {
            battleUI.NotifyTurnFinished();
            yield break;
        }

        var order = SortBySpeed();

        // apply abilities (start of turn)
        foreach (var user in order)
        {
            yield return user.Ability.Functions.OnTurnBeginning(user.Ability, user, battleUI);
            battleUI.NotifyUpdateHealth();
        }

        // apply effects (start of turn)
        for (var i=0; i<effectQueue.Count; i++)
        {
            var cmd = effectQueue[i];
            if (cmd != null && cmd.Effect.Trigger == Trigger.StartOfTurn)
                yield return ApplyEffect(cmd, i, order);
        }

        // begin turn
        for (var i=0; i<order.Count; i++)
        {
            var user = order[i];
            if (user.Health <= 0) continue;

            // possible early exit (already won/lost)
            if (CheckVictory() != Outcome.Undecided)
            {
                battleUI.NotifyTurnFinished();
                yield break;
            }

            // attempt to perform a move
            if (user.CanAttack && moveQueue[i] != null)
            {
                var move = moveQueue[i].Move;
                // 0 max points = infinite max points
                if (move.MaxPoints > 0) move.Points--;
                yield return Print($"{user.Name} used {move.Name}!");

                // based on move targeting, apply move to all targets
                var targetList = GetMoveTargets(moveQueue[i]);
                foreach (var target in targetList)
                {
                    yield return move.Functions.OnUse(move, user, target, battleUI, targetList.Count);
                    battleUI.NotifyUpdateHealth();
                    if (IsHit(move, user, target)) // is a hit
                    {
                        if (target.Health > 0) // target is valid
                        {
                            LastMoveWasCrit = false; // clear the global crit flag
                            PlayEffectivenessSound(move, target);
                            yield return move.Functions.Execute(move, user, target, battleUI, targetList.Count);
                            battleUI.NotifyUpdateHealth();
                            target.LastHitByMove = move;
                            target.LastHitByUser = user;
                            if (LastMoveWasCrit) yield return Print("Critical hit!");
                            yield return PrintEffectiveness(move, target);
                            yield return move.Functions.OnHit(move, user, target, battleUI, targetList.Count);
                        }
                        // target is invalid
                        else yield return Print("But it failed!");
                    }
                    else // is a miss
                    {
                        Print("But it missed!");
                        yield return move.Functions.OnMiss(move, user, target, battleUI, targetList.Count);
                        battleUI.NotifyUpdateHealth();
                    }
                }

                yield return CheckDeath(order);
                battleUI.NotifyUpdateHealth();

                // possible early exit (already won/lost)
                if (CheckVictory() != Outcome.Undecided)
                {
                    battleUI.NotifyTurnFinished();
                    yield break;
                }
            }
        }

        // apply effects (end of turn)
        for (var i = 0; i < effectQueue.Count; i++)
        {
            var cmd = effectQueue[i];
            if (cmd == null) continue;

            var effect = effectQueue[i].Effect;
            if (effect.Trigger == Trigger.EndOfTurn)
                yield return ApplyEffect(cmd, i, order);

            effect.Turn++;
        }

        // apply abilities (end of turn)
        foreach (var user in order)
        {
            yield return user.Ability.Functions.OnTurnEnding(user.Ability, user, battleUI);
            battleUI.NotifyUpdateHealth();
            user.Ability.Turn++;
        }

        // end the turn, updating battle state
        TurnNumber++;
        CheckVictory();
        battleUI.NotifyTurnFinished();
    }

    /// <summary>
    /// Prints to the battle's chatbox.
    /// </summary>
    private IEnumerator Print(string message)
    {
        yield return battleUI.Print(message);
    }

    private void PlayEffectivenessSound(Move move, Pokemon target)
    {
        if (move.Category == Category.Status) return;

        var multiplier = Types.Affinity(move, target);
        if (multiplier == 0f) return;
        else if (multiplier < 1f) battleUI.notVeryEffectiveSound.Play();
        else if (multiplier >= 2f) battleUI.superEffectiveSound.Play();
        else battleUI.hitSound.Play();
    }

    private IEnumerator PrintEffectiveness(Move move, Pokemon target)
    {
        if (move.Category == Category.Status) yield break;

        var multiplier = Types.Affinity(move, target);
        if (multiplier == 0f) yield return Print("But it had no effect!");
        else if (multiplier < 1f) yield return Print("It's not very effective...");
        else if (multiplier >= 2f) yield return Print("It's super effective!");
    }

    private List<Pokemon> GetMoveTargets(MoveCommand cmd)
    {
        if (cmd == null) return new List<Pokemon>();
        List<Pokemon> targets = null;
        var targeting = cmd.Move.Targeting;

        switch (targeting)
        {
            case Targeting.Self:
                targets = new List<Pokemon>() { cmd.User };
                break;
            case Targeting.Single:
                targets = IsActive(cmd.Target) ? new List<Pokemon>() { cmd.Target } : new List<Pokemon>();
                break;
            case Targeting.Adjacent:
                var primaryTarget = cmd.Target;
                var targetList = primaryTarget.IsAlly ? ActiveAllies : ActiveEnemies;
                var index = targetList.FindIndex(pkmn => pkmn == primaryTarget);
                targets = new List<Pokemon>();
                if (index - 1 >= 0) targets.Add(targetList[index - 1]);
                targets.Add(targetList[index]);
                if (index + 1 < targetList.Count) targets.Add(targetList[index + 1]);
                break;
            case Targeting.Allies:
                targets = cmd.User.IsAlly ? ActiveAllies : ActiveEnemies;
                break;
            case Targeting.Enemies:
                targets = cmd.User.IsAlly ? ActiveEnemies : ActiveAllies;
                break;
            case Targeting.All:
                targets = ActivePokemons();
                break;
        }

        return targeting == Targeting.Self ? targets : targets.FindAll(pkmn => pkmn.Health > 0);
    }

    /// <summary>
    /// "Specific target" ignores whatever target may be assigned to the command.
    /// </summary>
    private IEnumerator ApplyEffect(EffectCommand cmd, int index, List<Pokemon> order, Pokemon specificTarget)
    {
        if (cmd == null) yield break;
        var target = specificTarget ?? cmd.Target;

        if (cmd.Effect.Turn > cmd.Effect.Duration)
        {
            if (target.Health > 0) yield return cmd.Effect.Functions.OnDeletion(cmd.Effect, cmd.User, cmd.Target, battleUI);
            effectQueue[index] = null; // clear effect
            battleUI.NotifyUpdateHealth();
        }
        else
        {
            if (target.Health > 0) yield return cmd.Effect.Functions.Execute(cmd.Effect, cmd.User, cmd.Target, battleUI);
            battleUI.NotifyUpdateHealth();
        }

        yield return CheckDeath(order);

        //if (target.Health > 0) yield return Print($"<{cmd.Effect.Name} on {target.Name}>");
    }

    private IEnumerator ApplyEffect(EffectCommand cmd, int index, List<Pokemon> order)
    {
        yield return ApplyEffect(cmd, index, order, null);
    }

    private IEnumerator CheckDeath(List<Pokemon> order)
    {
        foreach (var user in order)
        {
            // if 0 hp but not fainted, we haven't yet processed onDeath events for them
            if (user.Health <= 0 && user.Status != Status.Fainted)
            {
                battleUI.NotifyUpdateHealth();
                yield return Print($"{user.Name} fainted!");
                user.Status = Status.Fainted;

                // apply effects (on death)
                for (var i=0; i<effectQueue.Count; i++)
                {
                    var cmd = effectQueue[i];
                    if (cmd != null && cmd.User == user && cmd.Effect.Trigger == Trigger.OnDeath)
                        yield return ApplyEffect(cmd, i, order);
                }

                // apply abilities (on death)
                yield return user.Ability.Functions.OnDeath(user.Ability, user, battleUI);
                battleUI.NotifyUpdateHealth();

                // give exp to others if it was an enemy dying
                if (!user.IsAlly)
                {
                    var expList = user.ExpCandidates.FindAll(pkmn => pkmn.Health > 0);
                    foreach (var candidate in expList)
                    {
                        var expReward = GetExpForKill(candidate, user, battleUI);
                        yield return HandleExpGain(candidate, expReward);
                    }
                }
            }
        }
    }

    private IEnumerator HandleExpGain(Pokemon receiver, int expGained)
    {
        receiver.Experience += expGained;
        yield return Print($"{receiver.Name} gained {expGained} exp. points!");

        var targetLevel = GetLevelFromExp(receiver.Experience, receiver.ExpGroup);
        while (targetLevel > receiver.Level) // skipping levels
        {
            receiver.LevelUp();  
            yield return battleUI.NotifyUpdateExp(true);
            battleUI.NotifyUpdateHealth(true);
            battleUI.levelUpSound.Play();
            yield return Print($"{receiver.Name} reached level {receiver.Level}!");   
        }
        yield return battleUI.NotifyUpdateExp(false);
    }
    
    private string WeatherToString()
    {
        switch (Weather)
        {
            case Weather.Hail: return "It's hailing.";
            case Weather.Rain: return "It's raining.";
            case Weather.Sandstorm: return "A sandstorm rages.";
            case Weather.Sunny: return "It's very sunny.";
            default: return null;
        }
    }

    private bool IsActive(Pokemon pokemon)
    {
        return pokemon.Health > 0 && ActivePokemons().Contains(pokemon);
    }

    private List<Pokemon> ActivePokemons()
    {
        return ActiveAllies.Concat(ActiveEnemies).ToList();
    }

    /// <summary>
    /// Sets the outcome value based on the state of all Pokemon, also returning that value.
    /// </summary>
    private Outcome CheckVictory()
    {
        if (PartyAllies.Concat(ActiveAllies).All(pkmn => pkmn.Health <= 0)) Outcome = Outcome.Loss;
        else if (PartyEnemies.Concat(ActiveEnemies).All(pkmn => pkmn.Health <= 0)) Outcome = Outcome.Win;
        else Outcome = Outcome.Undecided;
        return Outcome;
    }
}
