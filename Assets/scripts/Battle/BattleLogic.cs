using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
    private List<EffectCommand> effectQueue;
    private List<MoveCommand> moveQueue;
    private List<SwitchCommand> switchQueue;

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
        moveQueue = new List<MoveCommand>();
        switchQueue = new List<SwitchCommand>();

        allies.ForEach(pkmn => pkmn.IsAlly = true);
        enemies.ForEach(pkmn => pkmn.IsAlly = false);
    }

    private IEnumerator Print(string message)
    {
        yield return battleUI.Print(message);
    }

    public IEnumerator Turn()
    {
        Debug.Log("gonna print "+TurnNumber);
        yield return Print(TurnNumber+" sdakfljhdsfkjdsh");
        yield return new WaitForSeconds(1f);
        Outcome = Outcome.Win;
        TurnNumber++;
        Debug.Log(ActiveEnemies.Count);
        yield return ActiveAllies[0].Moves[0].Functions.Execute(ActiveAllies[0].Moves[0], ActiveAllies[0], ActiveEnemies[0], battleUI, 1);
        yield return battleUI.hud.UpdateEnemyHealthBar();
        yield return battleUI.chatbox.Print("1111111666666664");
        yield return new WaitForSeconds(1f);
        yield return xdd();
        yield return new WaitForSeconds(1f);
        battleUI.NotifyTurnFinished();
        //yield break;
        /*turnNumber++;
         battleUI.Print("sdfsdfdsfdsdfsa");
         if (turnNumber == 2) return Outcome.Undecided;
         if (turnNumber == 3) return Outcome.Undecided;
         if (turnNumber == 4) return Outcome.Win;
         return Outcome.Loss;*/
    }

    private IEnumerator xdd()
    {
        yield return battleUI.chatbox.Print("lelelelele");
    }

    private List<Pokemon> SortBySpeed()
    {
        var actives = ActivePokemons();
        actives.Sort();
        return actives;
    }

    private bool IsActive(Pokemon pokemon)
    {
        return pokemon.Health > 0 && ActivePokemons().Contains(pokemon);
    }

    private List<Pokemon> ActivePokemons()
    {
        return ActiveAllies.Concat(ActiveEnemies).ToList();
    }

    private Outcome CheckVictory()
    {
        if (PartyAllies.Concat(ActiveAllies).All(pkmn => pkmn.Health <= 0)) return Outcome.Loss;
        if (PartyEnemies.Concat(ActiveEnemies).All(pkmn => pkmn.Health <= 0)) return Outcome.Win;
        return Outcome.Undecided;
    }
}
