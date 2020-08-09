using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum BattleState
{
    Watching, SelectingAction, SelectingMove
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

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;
        music.Play();
        battleState = BattleState.Watching;
        StartCoroutine(SetupBattle());
    }

    private IEnumerator SetupBattle()
    {
        playerUnit.Setup();
        enemyUnit.Setup();
        hud.Init(playerUnit.Pokemon, enemyUnit.Pokemon);
        chatbox.RefreshMoves(playerUnit.Pokemon);

        chatbox.SetState(ChatState.None);
        yield return hud.IntroEffect();  

        chatbox.SetState(ChatState.ChatOnly);
        yield return chatbox.PrintChars($"Wild {enemyUnit.Name} appeared!");
        yield return new WaitForSeconds(1.5f);

        BeginPlayerAction(false);
    }

    void BeginPlayerAction(bool immediate)
    {
        chatbox.SetState(ChatState.SelectAction);
        StartCoroutine(chatbox.PrintChars($"What will {playerUnit.Name} do?", immediate));
        battleState = BattleState.SelectingAction;
    }

    void BeginPlayerMove()
    {
        chatbox.SetState(ChatState.SelectMove);
        battleState = BattleState.SelectingMove;
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
            chatSound.Play();
            playerUnit.Pokemon.Health += 4;
            enemyUnit.Pokemon.Health += 4;
            StartCoroutine(hud.UpdateAllyHealth());
            StartCoroutine(hud.UpdateAllyExpBar());
            StartCoroutine(hud.UpdateAllyHealthBar());
            StartCoroutine(hud.UpdateEnemyHealthBar());
        }
    }
}
