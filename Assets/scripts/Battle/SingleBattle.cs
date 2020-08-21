using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using static Utils;

/// <summary>
/// UI and logic manager for a 1v1 battle.
/// </summary>
public class SingleBattle : MonoBehaviour, IBattle
{
    public GameObject battleCanvas;
    public GameObject partyCanvas;
    public Unit playerUnit;
    public Unit enemyUnit;
    public HUD hud;
    public Dialog chatbox;
    public Party party;
    public AudioSource chatSound;
    public AudioSource hitSound;
    public AudioSource notVeryEffectiveSound;
    public AudioSource superEffectiveSound;
    public AudioSource levelUpSound;
    public AudioSource music;

    private int orderIndex;
    private int actionIndex;
    private int moveIndex;
    private int forcedSwitchIndex;
    private bool isForcedSwitch;
    private List<Pokemon> order;
    private List<SwitchCommand> switchQueue; // some elements may be null
    private List<MoveCommand> moveQueue; // some elements may be null

    public BattleState BattleState { get; set; }
    public BattleLogic Logic { get; private set; }

    public IEnumerator Print(string message, bool delay = true)
    {
        yield return chatbox.Print(message);
        if (delay) yield return new WaitForSeconds(1f);
    }

    // Start is called before the first frame update
    void Start()
    {
        // Get init data
        var battleInfo = SceneInfo.GetBattleInfo();

        // Logic setup
        EnsureAllLeadingPokemonAlive(battleInfo);
        Logic = new BattleLogic(this, battleInfo);
        moveQueue = new List<MoveCommand>();
        switchQueue = new List<SwitchCommand>();
        order = Logic.SortBySpeed();

        // UI setup
        Application.targetFrameRate = 60;
        partyCanvas.SetActive(false);
        playerUnit.Setup(Logic.ActiveAllies[0]);
        enemyUnit.Setup(Logic.ActiveEnemies[0]);
        hud.Init(playerUnit.Pokemon, enemyUnit.Pokemon);
        chatbox.RefreshMoves(playerUnit.Pokemon);

        // Battle intro
        BattleState = BattleState.Intro;
        StartCoroutine(BattleIntro());
        music.volume = 0.75f;
        music.Play();
    }

    private IEnumerator BattleIntro()
    {
        chatbox.SetState(ChatState.None);
        yield return hud.IntroEffect(music);

        chatbox.SetState(ChatState.ChatOnly);
        yield return Print($"Wild {enemyUnit.Name} appeared!");

        yield return Logic.Init();
        BeginPlayerAction();
    }

    /// <summary>
    /// Makes sure the player does not deploy a fainted Pokemon at the beginning of the fight.
    /// </summary>
    private void EnsureAllLeadingPokemonAlive(BattleInfo info)
    {
        for (var i = 0; i < info.BattleSize && i < info.Allies.Count; i++)
        {
            if (info.Allies[i].Status == Status.Fainted)
            {
                var swap = info.Allies[i];
                var alive = info.Allies.FindIndex(i + 1, pkmn => pkmn.Health > 0);
                info.Allies[i] = info.Allies[alive];
                info.Allies[alive] = swap;
            }
        }
    }

    /// <summary>
    /// Seek the next mandatory replacement of a Pokemon (because it fainted and more are available), if any.
    /// </summary>
    private void MoveToNextForcedSwitch()
    {
        var actives = Logic.ActivePokemons();
        while (forcedSwitchIndex < actives.Count && actives[forcedSwitchIndex].Health > 0)
            forcedSwitchIndex++;

        if (forcedSwitchIndex < actives.Count) // begin a forced switch
        {
            if (actives[forcedSwitchIndex].IsAlly) // prompt a switch
            {
                isForcedSwitch = true;
                StartCoroutine(BeginSwitch());
            }
            else // AI's decision
            {
                var switchedIn = RandomElement(Logic.PartyEnemies.FindAll(pkmn => pkmn.Health > 0));
                Logic.SwitchPokemonImmediate(Logic.ActivePokemons()[forcedSwitchIndex], switchedIn);
                MoveToNextForcedSwitch();
            }
        }
        else // all forced switches done, proceed with the game
        {
            order = Logic.SortBySpeed();
            BeginPlayerAction();
        }
    }

    private void PerformForcedSwitches()
    {
        BattleState = BattleState.Idle;
        MoveToNextForcedSwitch();
    }

    public void NotifyTurnFinished()
    {
        switch (Logic.Outcome)
        {
            case Outcome.Undecided:
                orderIndex = 0;
                forcedSwitchIndex = 0;
                moveQueue = new List<MoveCommand>();
                switchQueue = new List<SwitchCommand>();
                PerformForcedSwitches();
                break;
            case Outcome.Win:
                StartCoroutine(OnWin());
                break;
            case Outcome.Loss:
                StartCoroutine(OnLoss());
                break;
            case Outcome.Escaped:
                StartCoroutine(OnEscape());
                break;
        }
    }

    private IEnumerator OnWin()
    {
        var battleInfo = SceneInfo.GetBattleInfo();

        if (battleInfo.IsTrainerBattle)
        {
            var dialogue = battleInfo.Trainer.defeatDialogue;
            var playerInfo = SceneInfo.GetPlayerInfo();
            var index = 1;

            battleInfo.Trainer.IsDefeated = true;
            playerInfo.Player.Money += battleInfo.Trainer.money;

            yield return Print(dialogue[0], false);

            // say defeat dialogue + money reward
            while (index < dialogue.Length + 2)
            {
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    chatSound.Play();
                    if (index < dialogue.Length) yield return Print(dialogue[index], false);
                    else if (index == dialogue.Length) yield return Print($"{playerInfo.Player.Name} got {battleInfo.Trainer.money}€ for winning!", false);
                    else SceneInfo.ReturnToOverworldFromBattle();
                    index++;
                }
                else yield return null;
            } 
        }
    }

    private IEnumerator OnLoss()
    {
        var playerInfo = SceneInfo.GetPlayerInfo();
        var isTrainerBattle = SceneInfo.GetBattleInfo().IsTrainerBattle;
        var moneyLoss = playerInfo.Player.Money / 2;
        var index = 0;

        playerInfo.Player.Money -= moneyLoss;

        yield return Print($"{playerInfo.Player.Name} has no Pokemons left!", false);

        // say defeat dialogue
        while (index <= 3)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                chatSound.Play();
                switch (index++)
                {
                    case 0:
                        if (isTrainerBattle) yield return Print($"{playerInfo.Player.Name} had to pay {moneyLoss}€...", false);
                        else yield return Print($"{playerInfo.Player.Name} lost {moneyLoss}€ on the way out...", false);
                        break;
                    case 1:
                        yield return Print("...", false);
                        break;
                    case 2:
                        yield return Print($"{playerInfo.Player.Name} blacked out!", false);
                        break;
                    case 3:
                        SceneInfo.ReturnToOverworldFromBattle();
                        break;
                }
            }
            else yield return null;
        }
    }

    private IEnumerator OnEscape()
    {
        yield return Print("Got away safely!", false);

        while (true)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                SceneInfo.ReturnToOverworldFromBattle();
                break;
            }
            else yield return null;
        }
    }

    public IEnumerator NotifyUpdateHealth(bool immediate = false)
    {
        hud.UpdateStatuses();
        yield return hud.UpdateAllyHealth(immediate);
        yield return hud.UpdateEnemyHealth(immediate);
    }

    public IEnumerator NotifyUpdateExp(bool fill)
    {
        yield return fill ? hud.FillAllyExpBar() : hud.UpdateAllyExp();
    }

    public void NotifySwitchPerformed(Pokemon selection)
    {
        // cancelled
        if (selection == null)
        {
            BeginPlayerAction();
            return;
        }

        // switch without ending turn
        if (isForcedSwitch)
        {
            Logic.SwitchPokemonImmediate(Logic.ActivePokemons()[forcedSwitchIndex], selection);
            MoveToNextForcedSwitch();
            return;
        }

        // switch instead of using a move
        AddSwitchCommand(selection);
    }

    /// <summary>
    /// This Pokemon will be switched out, and it will not use a move.
    /// </summary>
    public void AddSwitchCommand(Pokemon switchedIn)
    {
        switchQueue.Add(new SwitchCommand(switchedIn, order[orderIndex]));
        moveQueue.Add(null);
        MoveToNextInOrder();
    }

    /// <summary>
    /// This Pokemon will use a move, and it will not be switched out.
    /// </summary>
    public void AddMoveCommand(Move move, Pokemon target)
    {
        moveQueue.Add(new MoveCommand(move, order[orderIndex], target));
        switchQueue.Add(null);
        MoveToNextInOrder();
    }

    /// <summary>
    /// This Pokemon will use a move, and it will not be switched out.
    /// </summary>
    public void AddMoveCommand(Move move)
    {
        moveQueue.Add(new MoveCommand(move, order[orderIndex]));
        switchQueue.Add(null);
        MoveToNextInOrder();
    }

    /// <summary>
    /// Moves directed at the Pokemon switched out will now be directed at the Pokemon switched in.
    /// </summary>
    public void UpdateMoveTargets(SwitchCommand cmd)
    {
        foreach (var moveCommand in moveQueue)
        {
            if (moveCommand != null && moveCommand.Target == cmd.SwitchedOut)
                moveCommand.Target = cmd.SwitchedIn;
        }
    }

    public void RegisterSwitch(Pokemon switchedIn)
    {
        if (switchedIn.IsAlly)
        {
            playerUnit.Setup(switchedIn);
            hud.NotifySwitch(switchedIn);
            chatbox.RefreshMoves(switchedIn);
        }
        else
        {
            enemyUnit.Setup(switchedIn);
            hud.NotifySwitch(switchedIn);
        }
    }

    private void BeginPlayerAction(bool immediate = false)
    {
        //AI placeholder
        if (!order[orderIndex].IsAlly)
            AddMoveCommand(RandomNonNullElement(order[orderIndex].Moves), playerUnit.Pokemon);
        else
        {
            chatbox.SetState(ChatState.SelectAction);
            StartCoroutine(chatbox.Print($"What will {order[orderIndex].Name} do?", immediate));
            BattleState = BattleState.SelectingAction;
        }
    }

    private void BeginPlayerMove()
    {
        chatbox.SetState(ChatState.SelectMove);
        BattleState = BattleState.SelectingMove;
    }

    private void BeginTurn()
    {
        chatbox.SetState(ChatState.ChatOnly);
        BattleState = BattleState.TurnHappening;
        StartCoroutine(Logic.Turn(moveQueue, switchQueue));
    }

    private IEnumerator BeginSwitch()
    {
        BattleState = BattleState.Idle;
        yield return hud.FadeOut();
        battleCanvas.SetActive(false);
        partyCanvas.SetActive(true);
        party.Init(this, isForcedSwitch);
        yield return hud.FadeIn();
    }

    /// <summary>
    /// Moves to the next Pokemon in the order. If it was the final Pokemon, begins the turn.
    /// </summary>
    private void MoveToNextInOrder()
    {
        // reset pointers
        chatbox.actions[actionIndex].color = Color.black;
        chatbox.moves[moveIndex].color = Color.black;
        actionIndex = 0;
        moveIndex = 0;

        if (orderIndex + 1 >= order.Count) // all choices made, begin turn
        {
            BeginTurn();
        }
        else // move to next
        {
            orderIndex++;
            BeginPlayerAction();
        }
    }

    // Update is called once per frame
    void Update()
    {
        switch (BattleState)
        {
            case BattleState.SelectingAction:
                ActionPicker();
                break;
            case BattleState.SelectingMove:
                MovePicker();
                break;
        }
    }

    private void ActionPicker()
    {
        var oldIndex = actionIndex;
        chatbox.actions[actionIndex].color = Color.black;

        if (Input.GetKeyDown(KeyCode.UpArrow)) actionIndex = actionIndex < 2 ? actionIndex + 2 : actionIndex - 2;
        if (Input.GetKeyDown(KeyCode.DownArrow)) actionIndex = (actionIndex + 2) % 4;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) actionIndex = actionIndex % 2 == 0 ? actionIndex + 1 : actionIndex - 1;
        if (Input.GetKeyDown(KeyCode.RightArrow)) actionIndex = actionIndex % 2 != 0 ? actionIndex - 1 : actionIndex + 1;

        chatbox.actions[actionIndex].color = Color.blue;

        if (oldIndex != actionIndex) chatSound.Play();

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (chatbox.IsBusy) return;

            chatSound.Play();
            switch (actionIndex)
            {
                case 0:
                    BeginPlayerMove();
                    break;
                case 1:
                    isForcedSwitch = false;
                    StartCoroutine(BeginSwitch());
                    break;
                case 2:
                case 3: break;
            }
        }
    }

    private void MovePicker()
    {
        var oldIndex = moveIndex;
        chatbox.moves[moveIndex].color = Color.black;

        if (Input.GetKeyDown(KeyCode.UpArrow)) moveIndex = moveIndex < 2 ? moveIndex + 2 : moveIndex - 2;
        if (Input.GetKeyDown(KeyCode.DownArrow)) moveIndex = (moveIndex + 2) % 4;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) moveIndex = moveIndex % 2 == 0 ? moveIndex + 1 : moveIndex - 1;
        if (Input.GetKeyDown(KeyCode.RightArrow)) moveIndex = moveIndex % 2 != 0 ? moveIndex - 1 : moveIndex + 1;

        // reset selection
        if (playerUnit.Moves[moveIndex] == null) moveIndex = oldIndex;

        chatbox.moves[moveIndex].color = Color.blue;
        chatbox.ShowMoveInfo(playerUnit.Moves[moveIndex]);

        if (oldIndex != moveIndex) chatSound.Play();

        // back to actions
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (chatbox.IsBusy) return;

            chatSound.Play();
            BeginPlayerAction(true);
            return;
        }

        // perform move
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (chatbox.IsBusy) return;

            chatSound.Play();
            AddMoveCommand(order[orderIndex].Moves[moveIndex], enemyUnit.Pokemon);
        }
    }

    public void PlayHitSound() { hitSound.Play(); }

    public void PlayNotVeryEffectiveHitSound() { notVeryEffectiveSound.Play(); }

    public void PlaySuperEffectiveHitSound() { superEffectiveSound.Play(); }

    public void PlayLevelUpSound() { levelUpSound.Play(); }
}
