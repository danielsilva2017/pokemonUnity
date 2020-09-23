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
    Win, Loss, Escaped, Caught, Undecided
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

public class ItemCommand
{
    public Item Item { get; set; }
    public Pokemon Target { get; set; }
    public ItemCommand(Item item, Pokemon target) { Item = item; Target = target; }
}

public class BattleLogic
{
    private IBattle battleUI;
    private List<EffectCommand> effectQueue; // some elements may be null
    private int orderIndex;
    private List<Pokemon> order;

    public int BattleSize { get; set; }
    public int TurnNumber { get; set; }
    public bool IsTrainerBattle { get; set; }
    public List<Pokemon> PartyAllies { get; set; }
    public List<Pokemon> ActiveAllies { get; set; }
    public List<Pokemon> PartyEnemies { get; set; }
    public List<Pokemon> ActiveEnemies { get; set; }
    public Weather Weather { get; set; }
    public Outcome Outcome { get; set; }
    public bool? Confirmation { get; set; }
    public int? MoveLearningSelection { get; set; }
    public BattleAnimations Animations { get; set; }

    public BattleLogic(IBattle battle, BattleInfo info, BattleAnimations anims)
    {
        battleUI = battle;
        Animations = anims;
        BattleSize = info.BattleSize;
        ActiveAllies = info.Allies.GetRange(0, info.BattleSize);
        PartyAllies = info.Allies.GetRange(info.BattleSize, info.Allies.Count - info.BattleSize);
        ActiveEnemies = info.Enemies.GetRange(0, info.BattleSize);
        PartyEnemies = info.Enemies.GetRange(info.BattleSize, info.Enemies.Count - info.BattleSize);
        Weather = info.Weather;
        Outcome = Outcome.Undecided;
        TurnNumber = 1;
        IsTrainerBattle = info.IsTrainerBattle;
        effectQueue = new List<EffectCommand>();

        info.Allies.ForEach(pkmn => pkmn.IsAlly = true);
        info.Enemies.ForEach(pkmn => pkmn.IsAlly = false);

        foreach (var pkmn in ActivePokemons())
        {
            // only allies can get exp
            if (!pkmn.IsAlly) pkmn.ExpCandidates = new List<Pokemon>(ActiveAllies);
        }
    }

    public List<Pokemon> SortBySpeed()
    {
        var actives = ActivePokemons();
        actives.Sort();
        return actives;
    }

    public List<Pokemon> ActivePokemons()
    {
        return ActiveAllies.Concat(ActiveEnemies).ToList();
    }

    public IEnumerator AddEffect(EffectLogic logic, Pokemon user, Pokemon target)
    {
        if (target.Health <= 0) yield break;

        var addedEffect = new Effect(logic);
        var cmd = new EffectCommand(addedEffect, user, target);
        if (EffectExists(addedEffect, user, target)) yield break;

        yield return battleUI.NotifyUpdateHealth();
        yield return addedEffect.Functions.OnCreation(addedEffect, user, target, battleUI);
        yield return battleUI.NotifyUpdateHealth();
        effectQueue.Add(cmd);
    }

    public bool EffectExists(Effect effect, Pokemon user, Pokemon target)
    {
        return effectQueue.Any(
            cmd => cmd != null && 
            cmd.Effect.Logic == effect.Logic && 
            (user == null || cmd.User == user) && 
            (target == null || cmd.Target == target)
        );
    }

    public bool EffectExists(EffectLogic logic, Pokemon user, Pokemon target)
    {
        return EffectExists(new Effect(logic), user, target);
    }

    public bool EffectExistsOnUser(Effect effect, Pokemon user)
    {
        return EffectExists(effect, user, null);
    }

    public bool EffectExistsOnUser(EffectLogic logic, Pokemon user)
    {
        return EffectExists(new Effect(logic), user, null);
    }

    public bool EffectExistsOnTarget(Effect effect, Pokemon target)
    {
        return EffectExists(effect, null, target);
    }

    public bool EffectExistsOnTarget(EffectLogic logic, Pokemon target)
    {
        return EffectExists(new Effect(logic), null, target);
    }

    public void TryEscape() //todo
    {
        SceneInfo.SetForcedOutcome(Outcome.Escaped);
    }

    /// <summary>
    /// Immediately switches out a Pokemon, doing so outside of the turn loop.
    /// </summary>
    public IEnumerator SwitchPokemonImmediate(Pokemon switchedOut, Pokemon switchedIn)
    {
        // swap spots between field and party
        var activeList = switchedIn.IsAlly ? ActiveAllies : ActiveEnemies;
        var partyList = switchedIn.IsAlly ? PartyAllies : PartyEnemies;
        activeList[activeList.FindIndex(pkmn => pkmn == switchedOut)] = switchedIn;
        partyList[partyList.FindIndex(pkmn => pkmn == switchedIn)] = switchedOut;
        yield return battleUI.RegisterSwitch(switchedIn);
        switchedIn.WasForcedSwitch = true;

        // update exp candidates - new ally gets xp for all active enemies
        if (switchedIn.IsAlly)
        {
            foreach (var enemy in ActiveEnemies)
            {
                if (!enemy.ExpCandidates.Contains(switchedIn))
                    enemy.ExpCandidates.Add(switchedIn);
            }
        }
        // update exp candidates - active allies get xp for new enemy, xp candidates reset for an enemy switched out
        else
        {
            switchedIn.ExpCandidates = new List<Pokemon>(ActiveAllies);
            switchedOut.ExpCandidates = null;
        }
    }

    public IEnumerator Init()
    {
        order = RecalculateOrder();

        foreach (var user in order)
        {
            yield return user.Ability.Functions.OnSwitchIn(user.Ability, user, battleUI);
            yield return battleUI.NotifyUpdateHealth();
        }

        battleUI.NotifyTurnFinished();
    }

    /// <summary>
    /// Performs a full turn of the battle. The queues can have null elements. 
    /// <para/>
    /// For every index, either the move, switch or item queue should include an action to perform (can't both be null at that index).
    /// <para/>
    /// The order of commands in the list should match the order in which Pokemons will act in the turn (sorted by speed).
    /// </summary>
    /// <param name="pendingMoves"> Dictionary of move commands, which can have null entries. </param>
    /// <param name="pendingSwitches"> Dictionary of switch commands, which can have null entries. </param>
    /// <param name="usedItems"> Dictionary of Pokémons that spent their turn using an item, which can have null entries. </param>
    /// <returns></returns>
    public IEnumerator Turn(Dictionary<Pokemon, MoveCommand> pendingMoves, 
                            Dictionary<Pokemon, SwitchCommand> pendingSwitches,
                            Dictionary<Pokemon, bool> usedItems)
    {
        // reset attack flags
        foreach (var user in ActivePokemons())
            user.HasActedThisTurn = false;

        // inform player of weather
        if (Weather != Weather.None) yield return Print(WeatherToString());

        // possible early exit (already won/lost)
        if (CheckVictory() != Outcome.Undecided)
        {
            battleUI.NotifyTurnFinished();
            yield break;
        }

        order = RecalculateOrder();

        // item usage means they've acted
        foreach (var user in order)
            user.HasActedThisTurn = GetValue(usedItems, user);

        // manage forced switches
        for (var i = 0; i < order.Count; i++)
        {
            var user = order[i];
            if (!user.WasForcedSwitch) continue;

            // apply abilities
            yield return user.Ability.Functions.OnSwitchIn(user.Ability, user, battleUI);
            yield return battleUI.NotifyUpdateHealth();

            // apply effects
            for (var e = 0; e < effectQueue.Count; e++)
            {
                var cmd = effectQueue[e];
                if (cmd != null && cmd.Effect.Trigger == Trigger.OnSwitchIn)
                    yield return ApplyEffect(cmd, e, user);
            }

            // reset flags
            user.HasActedThisTurn = false;
            user.WasForcedSwitch = false;
        }

        order = RecalculateOrder();

        // perform non-forced switches and item usages
        for (var i=0; i<order.Count; i++)
        {
            var user = order[i];
            var cmd = GetValue(pendingSwitches, user);

            if (cmd != null)
            {
                cmd.SwitchedIn.HasActedThisTurn = true;
                yield return SwitchPokemon(cmd, order);
            }
        }

        // possible early exit (already won/lost)
        if (CheckVictory() != Outcome.Undecided)
        {
            battleUI.NotifyTurnFinished();
            yield break;
        }

        order = RecalculateOrder();

        // apply abilities (start of turn)
        foreach (var user in order)
        {
            yield return user.Ability.Functions.OnTurnBeginning(user.Ability, user, battleUI);
            yield return battleUI.NotifyUpdateHealth();
        }

        // apply effects (start of turn)
        for (var i=0; i<effectQueue.Count; i++)
        {
            var cmd = effectQueue[i];
            if (cmd != null && cmd.Effect.Trigger == Trigger.StartOfTurn)
                yield return ApplyEffect(cmd, i);
        }

        order = RecalculateOrder();

        // begin turn
        while (!EveryoneHasActed())
        {
            var user = order[orderIndex];
            if (user.Health <= 0 || user.HasActedThisTurn)
            {
                orderIndex++;
                continue; // continue does not increment here
            }

            // possible early exit (already won/lost)
            if (CheckVictory() != Outcome.Undecided)
            {
                battleUI.NotifyTurnFinished();
                yield break;
            }

            // attempt to perform a move
            var cmd = GetValue(pendingMoves, user);
            if (user.CanAttack && cmd != null)
            {
                user.HasActedThisTurn = true;

                var move = cmd.Move;
                // 0 max points = infinite max points
                if (move.MaxPoints > 0) move.Points--;
                yield return Print($"{user.Name} used {move.Name}!");
                yield return user.Ability.Functions.OnMoveUse(user.Ability, user, move, battleUI);

                // based on move targeting, apply move to all targets
                var targetList = GetMoveTargets(cmd);

                // all targets are invalid
                if (targetList.Count == 0) yield return Print("But it failed!");
                else foreach (var target in targetList)
                {
                    yield return move.Functions.OnUse(move, user, target, battleUI, targetList.Count);
                    yield return battleUI.NotifyUpdateHealth();
                    if (IsHit(move, user, target)) // is a hit
                    {
                        if (target.Health > 0) // target is valid
                        {
                            LastMoveWasCrit = false; // clear the global crit flag
                            PlayEffectivenessSound(move, target);
                            yield return move.Functions.Execute(move, user, target, battleUI, targetList.Count);
                            yield return battleUI.NotifyUpdateHealth();
                            target.LastHitByMove = move;
                            target.LastHitByUser = user;
                            if (LastMoveWasCrit) yield return Print("Critical hit!");
                            yield return PrintEffectiveness(move, target);
                            yield return move.Functions.OnHit(move, user, target, battleUI, targetList.Count);
                        }
                    }
                    else // is a miss
                    {
                        Print("But it missed!");
                        yield return move.Functions.OnMiss(move, user, target, battleUI, targetList.Count);
                        yield return battleUI.NotifyUpdateHealth();
                    }
                }

                yield return user.Ability.Functions.AfterMoveUse(user.Ability, user, move, battleUI);
                yield return CheckDeath();
                yield return battleUI.NotifyUpdateHealth();

                // possible early exit (already won/lost)
                if (CheckVictory() != Outcome.Undecided)
                {
                    battleUI.NotifyTurnFinished();
                    yield break;
                }
            }

            order = RecalculateOrder();
        }

        // apply effects (end of turn)
        for (var i = 0; i < effectQueue.Count; i++)
        {
            var cmd = effectQueue[i];
            if (cmd == null) continue;

            var effect = effectQueue[i].Effect;
            if (effect.Trigger == Trigger.EndOfTurn)
                yield return ApplyEffect(cmd, i);

            if (cmd.Target.IsAlly ? ActiveAllies.Contains(cmd.Target) : ActiveEnemies.Contains(cmd.Target))
                effect.Turn++;
        }

        order = RecalculateOrder();

        // apply abilities (end of turn)
        foreach (var user in order)
        {
            if (user.Health > 0)
            {
                yield return user.Ability.Functions.OnTurnEnding(user.Ability, user, battleUI);
                yield return battleUI.NotifyUpdateHealth();
                user.Ability.Turn++;
            }
        }

        // end the turn, updating battle state
        TurnNumber++;
        CheckVictory();
        battleUI.NotifyTurnFinished();
    }

    /// <summary>
    /// Prints to the battle's chatbox.
    /// </summary>
    private IEnumerator Print(string message, bool delay = true)
    {
        yield return battleUI.Print(message, delay);
    }

    /// <summary>
    /// Checks if everyone who can act during the current turn has done so.
    /// </summary>
    private bool EveryoneHasActed()
    {
        return order.All(pokemon => pokemon.HasActedThisTurn || pokemon.Health <= 0);
    }

    private List<Pokemon> RecalculateOrder()
    {
        orderIndex = 0;
        return SortBySpeed();
    }

    private void PlayEffectivenessSound(Move move, Pokemon target)
    {
        if (move.Category == MoveCategory.Status) return;

        var multiplier = Types.Affinity(move, target);
        if (multiplier == 0f) return;
        else if (multiplier < 1f) battleUI.PlayNotVeryEffectiveHitSound();
        else if (multiplier >= 2f) battleUI.PlaySuperEffectiveHitSound();
        else battleUI.PlayHitSound();
    }

    private IEnumerator PrintEffectiveness(Move move, Pokemon target)
    {
        if (move.Category == MoveCategory.Status) yield break;

        var multiplier = Types.Affinity(move, target);
        if (multiplier == 0f) yield return Print("But it had no effect!");
        else if (multiplier < 1f) yield return Print("It's not very effective...");
        else if (multiplier >= 2f) yield return Print("It's super effective!");
    }

    private IEnumerator SwitchPokemon(SwitchCommand cmd, List<Pokemon> order)
    {
        // apply ability (on switch out)
        cmd.SwitchedOut.Ability.Functions.OnSwitchOut(cmd.SwitchedOut.Ability, cmd.SwitchedOut, battleUI);

        // apply effects (on switch out)
        for (var i = 0; i < effectQueue.Count; i++)
        {
            var effectCommand = effectQueue[i];
            if (effectCommand == null) continue;

            // run life cycle method
            if (effectCommand.Target == cmd.SwitchedOut)
                effectCommand.Effect.Functions.OnSwitchOut(effectCommand.Effect, effectCommand.User, effectCommand.Target, battleUI);

            // run OnSwitchOut triggers
            if (effectCommand.Effect.Trigger == Trigger.OnSwitchOut && effectCommand.Target == cmd.SwitchedOut)
            {
                // first apply the effect
                yield return ApplyEffect(effectCommand, i);
                // remove whatever should be removed on switching out
                if (effectCommand.Effect.EndOnSwitch)
                    effectQueue[i] = null;
            }
        }

        yield return CheckDeath();

        // swap spots between field and party
        var activeList = cmd.SwitchedIn.IsAlly ? ActiveAllies : ActiveEnemies;
        var partyList = cmd.SwitchedIn.IsAlly ? PartyAllies : PartyEnemies;
        activeList[activeList.FindIndex(pkmn => pkmn == cmd.SwitchedOut)] = cmd.SwitchedIn;
        partyList[partyList.FindIndex(pkmn => pkmn == cmd.SwitchedIn)] = cmd.SwitchedOut;
        order[order.FindIndex(pkmn => pkmn == cmd.SwitchedOut)] = cmd.SwitchedIn;
        yield return battleUI.RegisterSwitch(cmd.SwitchedIn);

        // update exp candidates - new ally gets xp for all active enemies
        if (cmd.SwitchedIn.IsAlly)
        {
            foreach (var enemy in ActiveEnemies)
            {
                if (!enemy.ExpCandidates.Contains(cmd.SwitchedIn))
                    enemy.ExpCandidates.Add(cmd.SwitchedIn);
            }
        }
        // update exp candidates - active allies get xp for new enemy, xp candidates reset for an enemy switched out
        else
        {
            cmd.SwitchedIn.ExpCandidates = new List<Pokemon>(ActiveAllies);
            cmd.SwitchedOut.ExpCandidates = null;
        }

        // apply ability (on switch in)
        cmd.SwitchedIn.Ability.Functions.OnSwitchIn(cmd.SwitchedIn.Ability, cmd.SwitchedIn, battleUI);

        // apply effects (on switch in)
        for (var i = 0; i < effectQueue.Count; i++)
        {
            var effectCommand = effectQueue[i];
            if (effectCommand != null && effectCommand.Effect.Trigger == Trigger.OnSwitchIn)
                yield return ApplyEffect(effectCommand, i, cmd.SwitchedIn);
        }

        yield return CheckDeath();

        // update move targets so they do not fail
        battleUI.UpdateMoveTargets(cmd);
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
    private IEnumerator ApplyEffect(EffectCommand cmd, int index, Pokemon specificTarget)
    {
        if (cmd == null) yield break;
        var target = specificTarget ?? cmd.Target;
        if (target.IsAlly ? !ActiveAllies.Contains(target) : !ActiveEnemies.Contains(target)) yield break;

        if (cmd.Effect.Turn > cmd.Effect.Duration)
        {
            if (target.Health > 0) yield return cmd.Effect.Functions.OnDeletion(cmd.Effect, cmd.User, cmd.Target, battleUI);
            effectQueue[index] = null; // clear effect
            yield return battleUI.NotifyUpdateHealth();
        }
        else
        {
            if (target.Health > 0) yield return cmd.Effect.Functions.Execute(cmd.Effect, cmd.User, cmd.Target, battleUI);
            yield return battleUI.NotifyUpdateHealth();
        }

        yield return CheckDeath();

        //if (target.Health > 0) yield return Print($"<{cmd.Effect.Name} on {target.Name}>");
    }

    private IEnumerator ApplyEffect(EffectCommand cmd, int index)
    {
        yield return ApplyEffect(cmd, index, null);
    }

    private IEnumerator CheckDeath()
    {
        foreach (var user in order)
        {
            // if 0 hp but not fainted, we haven't yet processed onDeath events for them
            if (user.Health <= 0 && user.Status != Status.Fainted)
            {
                yield return battleUI.NotifyUpdateHealth();
                Animations.GetUnit(user).PlayFaintCry();
                yield return Print($"{user.Name} fainted!");
                yield return Animations.Faint(user);
                user.Status = Status.Fainted;

                // apply effects (on death)
                for (var i=0; i<effectQueue.Count; i++)
                {
                    var cmd = effectQueue[i];
                    if (cmd != null && cmd.User == user && cmd.Effect.Trigger == Trigger.OnDeath)
                        yield return ApplyEffect(cmd, i);
                }

                // apply abilities (on death)
                yield return user.Ability.Functions.OnDeath(user.Ability, user, battleUI);
                yield return battleUI.NotifyUpdateHealth();

                // give exp to others if it was an enemy dying
                if (!user.IsAlly)
                {
                    var expList = user.ExpCandidates.FindAll(pkmn => pkmn.Health > 0);
                    foreach (var candidate in expList)
                    {
                        var expReward = GetExpForKill(candidate, user, expList.Count, IsTrainerBattle);
                        yield return HandleExpGain(candidate, expReward);
                    }
                }

                order = RecalculateOrder();
            }
        }
    }

    private IEnumerator HandleExpGain(Pokemon receiver, int expGained)
    {
        receiver.Experience += expGained;
        yield return Print($"{receiver.Name} gained {expGained} exp. points!");

        var hasPendingEvolution = false;
        var targetLevel = GetLevelFromExp(receiver.Experience, receiver.ExpGroup);
        while (targetLevel > receiver.Level) // skipping levels
        {
            receiver.LevelUp();
            if (ActiveAllies.Contains(receiver)) // otherwise level up offscreen
            {
                yield return battleUI.NotifyUpdateExp(true);
                yield return battleUI.NotifyUpdateHealth(true);
            }
            battleUI.PlayLevelUpSound();
            yield return Print($"{receiver.Name} reached level {receiver.Level}!");
            yield return HandleMoveLearning(receiver);

            if (receiver.Skeleton.levelEvolution != null && receiver.Level >= receiver.Skeleton.levelEvolution.level && !hasPendingEvolution)
            {
                hasPendingEvolution = true;
                SceneInfo.AddPendingEvolution(receiver, receiver.Skeleton.levelEvolution.evolution);
            }
        }

        if (ActiveAllies.Contains(receiver))
            yield return battleUI.NotifyUpdateExp(false);
    }

    private IEnumerator HandleMoveLearning(Pokemon learner)
    {
        foreach (var moveSkeleton in learner.NewMovesFromLevelUp())
        {
            // clear the flags
            Confirmation = null;
            MoveLearningSelection = null;

            // there's room, automatically learn
            var usedSlots = learner.GetFilledMoveSlots();
            if (usedSlots < 4)
            {
                learner.Moves[usedSlots] = new Move(moveSkeleton);
                yield return Print($"{learner.Name} learned {moveSkeleton.moveName}!");
                continue;
            }

            // no room, replace a move
            yield return Print($"{learner.Name} is trying to learn {moveSkeleton.moveName}.");
            yield return Print("Should it do so?", false);

            battleUI.RequestConfirmationBox();
            yield return Await(() => Confirmation != null);
            
            if (Confirmation == false) // rejected learning move
                continue;

            yield return Print("Which move should be replaced?");
            battleUI.RequestMoveReplacement(learner);
            yield return Await(() => MoveLearningSelection != null);
            battleUI.GoIdle();

            if (MoveLearningSelection < 0) // rejected learning move
            {
                yield return Print($"{learner.Name} did not learn {moveSkeleton.moveName}.");
                continue;
            }

            learner.Moves[MoveLearningSelection.Value] = new Move(moveSkeleton);
            yield return Print($"{learner.Name} learned {moveSkeleton.moveName}!");
            if (ActiveAllies.Contains(learner)) battleUI.RefreshMoves(learner);
        }
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

    /// <summary>
    /// Sets the outcome value based on the state of all Pokemon, also returning that value.
    /// </summary>
    private Outcome CheckVictory()
    {
        var forcedOutcome = SceneInfo.ConsumeForcedOutcome();
        if (forcedOutcome != Outcome.Undecided)
        {
            Outcome = forcedOutcome;
            return Outcome;
        }

        if (PartyAllies.Concat(ActiveAllies).All(pkmn => pkmn.Health <= 0)) Outcome = Outcome.Loss;
        else if (PartyEnemies.Concat(ActiveEnemies).All(pkmn => pkmn.Health <= 0)) Outcome = Outcome.Win;
        else Outcome = Outcome.Undecided;
        return Outcome;
    }
}
