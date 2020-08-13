using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Utils;

public enum BattleState
{
    Intro, TurnHappening, SelectingAction, SelectingMove
}

public class Battle : MonoBehaviour
{
    public Unit playerUnit;
    public Unit enemyUnit;
    public HUD hud;
    public Dialog chatbox;
    public AudioSource chatSound;
    public AudioSource hitSound;
    public AudioSource notVeryEffectiveSound;
    public AudioSource superEffectiveSound;
    public AudioSource levelUpSound;
    public AudioSource music;
    public bool isTrainerBattle;
    
    private BattleState battleState;
    private int orderIndex;
    private int actionIndex;
    private int moveIndex;
    private List<Pokemon> order;
    private List<MoveCommand> moveQueue;

    public BattleLogic Logic { get; set; }

    /// <summary>
    /// Prints to the battle's chatbox.
    /// </summary>
    public IEnumerator Print(string message)
    {
        yield return chatbox.Print(message);
        yield return new WaitForSeconds(1f);
    }

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;
        music.Play();
        playerUnit.Setup();
        enemyUnit.Setup();
        hud.Init(playerUnit.Pokemon, enemyUnit.Pokemon);
        chatbox.RefreshMoves(playerUnit.Pokemon);
        Logic = new BattleLogic(this, new List<Pokemon>() { playerUnit.Pokemon }, new List<Pokemon>() { enemyUnit.Pokemon }, 1, Weather.None);
        moveQueue = new List<MoveCommand>();
        order = Logic.SortBySpeed();
        battleState = BattleState.Intro;
        StartCoroutine(BattleIntro());
    }

    private IEnumerator BattleIntro()
    {
        chatbox.SetState(ChatState.None);
        yield return hud.IntroEffect();  

        chatbox.SetState(ChatState.ChatOnly);
        yield return Print($"Wild {enemyUnit.Name} appeared!");

        yield return Logic.Init();
        BeginPlayerAction();
    }

    public void NotifyTurnFinished()
    {
        switch (Logic.Outcome)
        {
            case Outcome.Undecided:
                orderIndex = 0;
                moveQueue = new List<MoveCommand>();
                order = Logic.SortBySpeed();
                BeginPlayerAction();
                break;
            case Outcome.Win:
                StartCoroutine(Print("win"));
                break;
            case Outcome.Loss:
                StartCoroutine(Print("loss"));
                break;
            case Outcome.Escaped:
                StartCoroutine(Print("Got away safely!"));
                break;
        }      
    }

    public void NotifyUpdateHealth(bool immediate = false)
    {
        StartCoroutine(hud.UpdateAllyHealth(immediate));
        StartCoroutine(hud.UpdateAllyHealthBar(immediate));
        StartCoroutine(hud.UpdateEnemyHealthBar(immediate));
        hud.UpdateStatuses();
    }

    public IEnumerator NotifyUpdateExp(bool fill)
    {
        yield return fill ? hud.FillAllyExpBar() : hud.UpdateAllyExpBar();
    }

    void PerformTurn()
    {
        StartCoroutine(Logic.Turn(moveQueue));
    }

    void BeginPlayerAction(bool immediate = false)
    {
        //AI placeholder
        if (!order[orderIndex].IsAlly)
        {
            //Debug.Log("picking for " + order[orderIndex].Name);
            moveQueue.Add(new MoveCommand(RandomNonNullElement(order[orderIndex].Moves), enemyUnit.Pokemon, playerUnit.Pokemon));
            orderIndex++;
            if (orderIndex >= order.Count) BeginTurn();
        }
        else
        {
            chatbox.SetState(ChatState.SelectAction);
            StartCoroutine(chatbox.Print($"What will {order[orderIndex].Name} do?", immediate));
            battleState = BattleState.SelectingAction;
        }
    }

    void BeginPlayerMove()
    {
        chatbox.SetState(ChatState.SelectMove);
        battleState = BattleState.SelectingMove;
    }

    void BeginTurn()
    {
        chatbox.SetState(ChatState.ChatOnly);
        battleState = BattleState.TurnHappening;
        PerformTurn();
    }

    // Update is called once per frame
    void Update()
    {
        if (battleState == BattleState.SelectingAction)
            ActionPicker();
        else if (battleState == BattleState.SelectingMove)
            MovePicker();
    }

    void ActionPicker()
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
                case 2:
                case 3: break;
            }
        }
    }

    void MovePicker()
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
            moveQueue.Add(new MoveCommand(order[orderIndex].Moves[moveIndex], order[orderIndex], enemyUnit.Pokemon)); //placeholder
            if (orderIndex + 1 >= order.Count) // all choices made, begin turn
            {
                BeginTurn();
            }
            else // move to next
            {
                orderIndex++;
                BeginPlayerAction();
            }
            
            //playerUnit.Moves[moveIndex].Functions.Execute(playerUnit.Moves[moveIndex], playerUnit.Pokemon, enemyUnit.Pokemon, this, 1);
            /*StartCoroutine(hud.UpdateAllyHealth());
            StartCoroutine(hud.UpdateAllyExpBar());
            StartCoroutine(hud.UpdateAllyHealthBar());
            StartCoroutine(hud.UpdateEnemyHealthBar());*/
        }
    }
}
