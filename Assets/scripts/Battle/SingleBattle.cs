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
    public GameObject bagCanvas;
    public Unit playerUnit;
    public Unit enemyUnit;
    public HUD hud;
    public BattleAnimations anims;
    public Dialog chatbox;
    public BattleParty party;
    public BattleBag bag;
    public AudioSource audioPlayer;
    public AudioSource chatSound;
    public AudioSource hitSound;
    public AudioSource notVeryEffectiveSound;
    public AudioSource superEffectiveSound;
    public AudioSource levelUpSound;

    private int orderIndex;
    private int actionIndex;
    private int moveIndex;
    private int confirmationIndex;
    private int forcedSwitchIndex;
    private bool isForcedSwitch;
    private List<Pokemon> order;
    private List<SwitchCommand> switchQueue; // some elements may be null
    private List<MoveCommand> moveQueue; // some elements may be null
    private List<ItemCommand> itemQueue; // some elements may be null

    public BattleState BattleState { get; set; }
    public BattleLogic Logic { get; private set; }
    public IDialog Chatbox { get { return chatbox; } }
    public BattleInfo BattleInfo { get; private set; }
    public PlayerInfo PlayerInfo { get; private set; }

    public IEnumerator Print(string message, bool delay = true)
    {
        yield return chatbox.Print(message);
        if (delay) yield return new WaitForSeconds(1f);
    }

    // Start is called before the first frame update
    void Start()
    {
        // Get init data
        BattleInfo = SceneInfo.GetBattleInfo();
        PlayerInfo = SceneInfo.GetPlayerInfo();

        // Logic setup
        EnsureAllLeadingPokemonAlive(BattleInfo);
        Logic = new BattleLogic(this, BattleInfo, anims);
        moveQueue = new List<MoveCommand>();
        switchQueue = new List<SwitchCommand>();
        itemQueue = new List<ItemCommand>();
        order = Logic.SortBySpeed();

        // UI setup
        Application.targetFrameRate = 60;
        playerUnit.Setup(Logic.ActiveAllies[0], true);
        enemyUnit.Setup(Logic.ActiveEnemies[0], BattleInfo.IsTrainerBattle);
        hud.Init(playerUnit.Pokemon, enemyUnit.Pokemon);
        chatbox.RefreshMoves(playerUnit.Pokemon);
        chatbox.confirmationObject.SetActive(false);

        if (BattleInfo.IsTrainerBattle) anims.SetupNPCIntro(BattleInfo.Trainer);
        else anims.DisableNPCIntro();

        // Battle intro
        BattleState = BattleState.Intro;
        StartCoroutine(BattleIntro());
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
            case BattleState.SelectingReplacedMove:
                MovePicker();
                break;
            case BattleState.Confirming:
                ConfirmationPicker();
                break;
        }
    }

    private IEnumerator BattleIntro()
    {
        chatbox.SetState(ChatState.None);
        yield return hud.IntroEffect();

        if (BattleInfo.IsTrainerBattle) yield return TrainerBattleIntro();
        else yield return WildBattleIntro();

        yield return Logic.Init();
        BeginPlayerAction();
    }

    private IEnumerator WildBattleIntro()
    {
        chatbox.SetState(ChatState.ChatOnly);
        hud.ShowEnemyHUD();
        enemyUnit.PlayCry();
        yield return Print($"Wild {enemyUnit.Name} appeared!");

        yield return Print($"Go, {playerUnit.Pokemon.Name}!");
        hud.ShowAllyHUD();
        playerUnit.PlayEnterCry();
        yield return anims.SwitchInPokemon(playerUnit.Pokemon);
    }

    private IEnumerator TrainerBattleIntro()
    {
        var trainer = BattleInfo.Trainer;

        yield return anims.PlayNPCIntro();
        chatbox.SetState(ChatState.ChatOnly);
        yield return Print($"{trainer.skeleton.className} {trainer.Name} wants to fight!");
        yield return anims.PlayNPCSlideOut();

        yield return Print($"{trainer.skeleton.className} {trainer.Name} sent out {enemyUnit.Name}!");
        hud.ShowEnemyHUD();
        enemyUnit.PlayEnterCry();
        yield return anims.SwitchInPokemon(enemyUnit.Pokemon);

        yield return Print($"Go, {playerUnit.Pokemon.Name}!");
        hud.ShowAllyHUD();
        playerUnit.PlayEnterCry();
        yield return anims.SwitchInPokemon(playerUnit.Pokemon);
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
    private IEnumerator MoveToNextForcedSwitch()
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
                yield return Logic.SwitchPokemonImmediate(Logic.ActivePokemons()[forcedSwitchIndex], switchedIn);
                yield return MoveToNextForcedSwitch();
            }
        }
        else // all forced switches done, proceed with the game
        {
            order = Logic.SortBySpeed();
            BeginPlayerAction();
        }
    }

    private IEnumerator PerformForcedSwitches()
    {
        BattleState = BattleState.Idle;
        yield return MoveToNextForcedSwitch();
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
                itemQueue = new List<ItemCommand>();
                StartCoroutine(PerformForcedSwitches());
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
            case Outcome.Caught:
                StartCoroutine(OnCaught());
                break;
        }
    }

    private IEnumerator OnWin()
    {
        if (BattleInfo.IsTrainerBattle)
        {
            var dialogue = BattleInfo.Trainer.defeatDialogue;
            var index = 1;

            BattleInfo.Trainer.IsDefeated = true;
            PlayerInfo.Player.Money += BattleInfo.Trainer.money;

            SceneInfo.StopBattleMusic();
            audioPlayer.clip = BattleInfo.Trainer.skeleton.victoryMusic;
            audioPlayer.volume = 0.4f;
            audioPlayer.Play();
            hud.HideEnemyHUD();
            yield return anims.PlayNPCSlideIn();
            yield return Print(dialogue[0], false);

            // say defeat dialogue + money reward
            while (index < dialogue.Length + 2)
            {
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    chatSound.Play();
                    if (index < dialogue.Length) yield return Print(dialogue[index], false);
                    else if (index == dialogue.Length) yield return Print($"{PlayerInfo.Player.Name} got {BattleInfo.Trainer.money}€ for winning!", false);
                    else
                    {
                        BattleState = BattleState.Idle;
                        StartCoroutine(hud.ReturnToOverworld());
                    }
                    index++;
                }
                else yield return null;
            } 
        }
        else
        {
            BattleState = BattleState.Idle;
            StartCoroutine(hud.ReturnToOverworld());
        }
    }

    private IEnumerator OnLoss()
    {
        var isTrainerBattle = BattleInfo.IsTrainerBattle;
        var moneyLoss = PlayerInfo.Player.Money / 2;
        var index = 0;

        PlayerInfo.Player.Money -= moneyLoss;

        yield return Print($"{PlayerInfo.Player.Name} has no Pokemons left!", false);

        // say defeat dialogue
        while (index <= 3)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                chatSound.Play();
                switch (index++)
                {
                    case 0:
                        if (isTrainerBattle) yield return Print($"{PlayerInfo.Player.Name} had to pay {moneyLoss}€...", false);
                        else yield return Print($"{PlayerInfo.Player.Name} lost {moneyLoss}€ on the way out...", false);
                        break;
                    case 1:
                        yield return Print("...", false);
                        break;
                    case 2:
                        yield return Print($"{PlayerInfo.Player.Name} blacked out!", false);
                        break;
                    case 3:
                        BattleState = BattleState.Idle;
                        StartCoroutine(hud.ReturnToOverworld());
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
                BattleState = BattleState.Idle;
                StartCoroutine(hud.ReturnToOverworld());
                break;
            }
            else yield return null;
        }
    }

    private IEnumerator OnCaught()
    {
        var pokemonsInParty = PlayerInfo.Player.Pokemons;
        var index = 0;

        pokemonsInParty.Add(enemyUnit.Pokemon);
        yield return Print($"{enemyUnit.Pokemon.Name} was caught!", false);
        if (pokemonsInParty.Count < 6) index++;

        while (index <= 2)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                chatSound.Play();
                switch (index++)
                {
                    case 0:
                        yield return Print("<pokemon sent to PC (soon)>", false);
                        break;
                    case 1:
                        yield return Print("<pokedex data here soon>", false);
                        break;
                    case 2:
                        BattleState = BattleState.Idle;
                        StartCoroutine(hud.ReturnToOverworld());
                        break;
                }
            }
            else yield return null;
        }
    }

    public IEnumerator PresentSwitch(Pokemon switchedIn)
    {
        var originalState = BattleState;
        BattleState = BattleState.Idle;

        if (switchedIn.IsAlly) yield return Print($"Go, {switchedIn.Name}!");
        else
        {
            yield return Print($"{BattleInfo.Trainer.skeleton.className} {BattleInfo.Trainer.Name} sent out {switchedIn.Name}!");
            hud.NotifySwitch(switchedIn);
        }
        anims.GetUnit(switchedIn).PlayEnterCry();
        yield return anims.SwitchInPokemon(switchedIn);
        
        BattleState = originalState;
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

    public void SwitchUpdateUI(Pokemon switchedIn)
    {
        if (switchedIn.IsAlly) playerUnit.Setup(switchedIn, true);
        else enemyUnit.Setup(switchedIn, true);
    }

    public IEnumerator NotifySwitchPerformed(Pokemon selection)
    {
        // cancelled
        if (selection == null)
        {
            BeginPlayerAction();
            yield break;
        }

        // switch without ending turn
        if (isForcedSwitch)
        {
            yield return Logic.SwitchPokemonImmediate(Logic.ActivePokemons()[forcedSwitchIndex], selection);
            yield return MoveToNextForcedSwitch();
            yield break;
        }

        // switch instead of using a move
        AddSwitchCommand(selection);
    }
    
    public void NotifyItemUsed(Item item)
    {
        if (item == null) // cancelled
            BeginPlayerAction();
        else if (item.Usage == ItemUsage.TargetsAlly) // already used, done
            AddEmptyCommand();
        else // going to use
            StartCoroutine(UseItemOnEnemy(item));
    }

    private IEnumerator UseItemOnEnemy(Item item)
    {
        chatbox.SetState(ChatState.ChatOnly);
        bag.chatbox.confirmationObject.SetActive(false);
        var target = Logic.ActiveEnemies[0]; // for now, can only use enemy-targeting items in 1v1

        if (!bag.ItemToUse.Functions.CanBeUsed(bag.ItemToUse, target))
        {
            yield return chatbox.Print($"This item can't be used on {target.Name} right now.");
            while (!Input.GetKeyDown(KeyCode.Z)) yield return null;
            bag.Init(this); // back to bag
            yield break;
        }

        yield return hud.FadeInTransition();
        bagCanvas.SetActive(false);
        battleCanvas.SetActive(true);
        yield return hud.FadeOutTransition();
        
        PlayerInfo.Player.Bag.TakeItem(bag.ItemToUse, bag.ItemToUseIndex, 1);
        yield return bag.ItemToUse.Functions.Use(bag.ItemToUse, target, chatbox, anims);
        yield return NotifyUpdateHealth();
        yield return bag.ItemToUse.Functions.OnUse(bag.ItemToUse, target, chatbox, anims);

        bag.ItemToUse = null;
        AddEmptyCommand();
    }

    /// <summary>
    /// This Pokemon will be switched out.
    /// </summary>
    public void AddSwitchCommand(Pokemon switchedIn)
    {
        switchQueue.Add(new SwitchCommand(switchedIn, order[orderIndex]));
        moveQueue.Add(null);
        itemQueue.Add(null);
        MoveToNextInOrder();
    }

    /// <summary>
    /// This Pokemon will use a move.
    /// </summary>
    public void AddMoveCommand(Move move, Pokemon target)
    {
        moveQueue.Add(new MoveCommand(move, order[orderIndex], target));
        switchQueue.Add(null);
        itemQueue.Add(null);
        MoveToNextInOrder();
    }

    /// <summary>
    /// This Pokemon will use a move.
    /// </summary>
    public void AddMoveCommand(Move move)
    {
        moveQueue.Add(new MoveCommand(move, order[orderIndex]));
        switchQueue.Add(null);
        itemQueue.Add(null);
        MoveToNextInOrder();
    }

    /// <summary>
    /// This Pokemon will use an item.
    /// </summary>
   /* public void AddItemCommand(Item item)
    {
        itemQueue.Add(new ItemCommand(item, order[orderIndex]));
        moveQueue.Add(null);
        switchQueue.Add(null);
        MoveToNextInOrder();
    }

    /// <summary>
    /// This Pokemon will use an item on itself.
    /// </summary>
    public void AddItemCommand(Item item, Pokemon target)
    {
        itemQueue.Add(new ItemCommand(item, target));
        moveQueue.Add(null);
        switchQueue.Add(null);
        MoveToNextInOrder();
    }*/

    public void AddEmptyCommand()
    {
        moveQueue.Add(null);
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

    public IEnumerator RegisterSwitch(Pokemon switchedIn)
    {
        if (switchedIn.IsAlly)
        {
            chatbox.RefreshMoves(switchedIn);
        }
        else
        {
            enemyUnit.Setup(switchedIn, true);
        }

        yield return PresentSwitch(switchedIn);
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
        chatbox.RefreshMoves(order[orderIndex]);
        if (moveIndex >= order[orderIndex].GetFilledMoveSlots())
        {
            chatbox.moves[moveIndex].color = Color.black;
            moveIndex = 0;
        }
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
        yield return hud.FadeInTransition();
        battleCanvas.SetActive(false);
        partyCanvas.SetActive(true);
        party.Init(this, isForcedSwitch);
        yield return hud.FadeOutTransition();
    }

    private IEnumerator BeginOpenBag()
    {
        BattleState = BattleState.Idle;
        yield return hud.FadeInTransition();
        battleCanvas.SetActive(false);
        bagCanvas.SetActive(true);
        bag.Init(this);
        yield return hud.FadeOutTransition();
    }

    /// <summary>
    /// Moves to the next Pokemon in the order. If it was the final Pokemon, begins the turn.
    /// </summary>
    private void MoveToNextInOrder()
    {
        // reset pointers
        chatbox.actions[actionIndex].color = Color.black;
        chatbox.moves[moveIndex].color = Color.black;
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

    public void RequestConfirmationBox()
    {
        BattleState = BattleState.Confirming;
        chatbox.confirmationObject.SetActive(true);
        chatbox.ConfirmationBox.CursorYes();
        confirmationIndex = 0;
    }

    public void RequestMoveReplacement(Pokemon learner)
    {
        chatbox.RefreshMoves(learner);
        chatbox.SetState(ChatState.SelectMove);
        BattleState = BattleState.SelectingReplacedMove;
    }

    public void GoIdle()
    {
        chatbox.SetState(ChatState.ChatOnly);
        BattleState = BattleState.Idle;
    }

    public void RefreshMoves(Pokemon ally)
    {
        chatbox.RefreshMoves(ally);
    }

    private void ConfirmationPicker()
    {
        var oldConfirmationIndex = confirmationIndex;

        if (Input.GetKeyDown(KeyCode.UpArrow)) confirmationIndex = confirmationIndex == 1 ? 0 : 1;
        if (Input.GetKeyDown(KeyCode.DownArrow)) confirmationIndex = confirmationIndex == 0 ? 1 : 0;

        if (oldConfirmationIndex != confirmationIndex)
        {
            chatSound.Play();
            if (confirmationIndex == 0) chatbox.ConfirmationBox.CursorYes();
            else chatbox.ConfirmationBox.CursorNo();
        }

        // don't learn move
        if (Input.GetKeyDown(KeyCode.X))
        {
            Logic.Confirmation = false;
            BattleState = BattleState.Idle;
            chatbox.confirmationObject.SetActive(false);
            return;
        }

        // use or cancel
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (confirmationIndex == 0) // yes
            {
                Logic.Confirmation = true;
            }
            else // no
            {
                Logic.Confirmation = false;
            }

            BattleState = BattleState.Idle;
            chatbox.confirmationObject.SetActive(false);
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
                    chatSound.Play();
                    StartCoroutine(BeginOpenBag());
                    break;
                case 2:
                    isForcedSwitch = false;
                    StartCoroutine(BeginSwitch());
                    break;
                case 3:
                    Logic.TryEscape();
                    AddEmptyCommand();
                    break;
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
        if (chatbox.moves[moveIndex].text == "-") moveIndex = oldIndex;

        chatbox.moves[moveIndex].color = Color.blue;
        chatbox.ShowMoveInfo(playerUnit.Moves[moveIndex]);

        if (oldIndex != moveIndex) chatSound.Play();

        // back to actions
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (chatbox.IsBusy) return;

            chatSound.Play();

            if (BattleState == BattleState.SelectingReplacedMove)
            {
                Logic.MoveLearningSelection = -1;
                return;
            }

            BeginPlayerAction(true);
            return;
        }

        // perform move
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (chatbox.IsBusy) return;

            chatSound.Play();

            if (BattleState == BattleState.SelectingReplacedMove)
            {
                Logic.MoveLearningSelection = moveIndex;
                return;
            }

            AddMoveCommand(order[orderIndex].Moves[moveIndex], enemyUnit.Pokemon);
        }
    }

    public void PlayHitSound() { hitSound.Play(); }

    public void PlayNotVeryEffectiveHitSound() { notVeryEffectiveSound.Play(); }

    public void PlaySuperEffectiveHitSound() { superEffectiveSound.Play(); }

    public void PlayLevelUpSound() { levelUpSound.Play(); }
}
