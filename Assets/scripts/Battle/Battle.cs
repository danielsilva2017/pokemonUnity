using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    public AudioSource music;
    
    private BattleState battleState;
    private int actionIndex;
    private int moveIndex;

    public BattleLogic Logic { get; set; }

    public IEnumerator Print(string message)
    {
        yield return chatbox.Print(message);
    }

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;
        //music.Play();
        playerUnit.Setup();
        enemyUnit.Setup();
        hud.Init(playerUnit.Pokemon, enemyUnit.Pokemon);
        chatbox.RefreshMoves(playerUnit.Pokemon);
        Logic = new BattleLogic(this, new List<Pokemon>() { playerUnit.Pokemon }, new List<Pokemon>() { enemyUnit.Pokemon }, 1, Weather.None);
        battleState = BattleState.Intro;
        StartCoroutine(BattleIntro());
    }

    private IEnumerator BattleIntro()
    {
        //chatbox.SetState(ChatState.None);
        //yield return hud.IntroEffect();  

        chatbox.SetState(ChatState.ChatOnly);
        yield return Print($"Wild {enemyUnit.Name} appeared!");
        yield return new WaitForSeconds(1.5f);

        BeginPlayerAction();
    }

    public void NotifyTurnFinished()
    {
        Debug.Log("outcome here " + Logic.Outcome);
        BeginPlayerAction();
    }

    void PerformTurn()
    {
        StartCoroutine(Logic.Turn());
    }

    void BeginPlayerAction(bool immediate = false)
    {
        chatbox.SetState(ChatState.SelectAction);
        StartCoroutine(chatbox.Print($"What will {playerUnit.Name} do?", immediate));
        battleState = BattleState.SelectingAction;
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
        //else if (battleState == BattleState.TurnHappening)
            //PerformTurn();
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
            chatSound.Play();
            BeginPlayerAction(true);
            return;
        }

        // perform move
        if (Input.GetKeyDown(KeyCode.Z))
        {
            chatbox.SetState(ChatState.ChatOnly);
            chatSound.Play();
            BeginTurn();
            //playerUnit.Moves[moveIndex].Functions.Execute(playerUnit.Moves[moveIndex], playerUnit.Pokemon, enemyUnit.Pokemon, this, 1);
            /*StartCoroutine(hud.UpdateAllyHealth());
            StartCoroutine(hud.UpdateAllyExpBar());
            StartCoroutine(hud.UpdateAllyHealthBar());
            StartCoroutine(hud.UpdateEnemyHealthBar());*/
        }
    }
}
