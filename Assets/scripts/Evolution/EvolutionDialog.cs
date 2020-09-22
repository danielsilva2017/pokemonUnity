using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EvolutionDialog : MonoBehaviour, IDialog
{
    public GameObject confirmationObject;
    public Image chatbox;
    public Text chatText;
    public Text movePoints;
    public Text moveType;
    public GameObject moveSelector;
    public Text[] moves;

    private readonly int framesPerChar = 2;

    public bool IsBusy { get; set; }
    public ConfirmationBox ConfirmationBox { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;
        if (confirmationObject != null) ConfirmationBox = new ConfirmationBox(confirmationObject);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void PrintSilent(string message, bool immediate = false)
    {
        StartCoroutine(Print(message, immediate));
    }

    public void RefreshMoves(Pokemon ally)
    {
        for (var i = 0; i < moves.Length; i++)
            moves[i].text = ally.Moves[i] == null ? "-" : ally.Moves[i].Name;
    }

    public void ShowMoveInfo(Move move)
    {
        if (move == null) return;

        movePoints.text = move.MaxPoints == 0 ? "PP   --/--" : $"PP   {move.Points}/{move.MaxPoints}";
        moveType.text = $"<size=22>Type / </size>{Types.TypeToString(move.Type)}";
    }

    public void SetState(ChatState state)
    {
        switch (state)
        {
            case ChatState.None:
                chatbox.enabled = false;
                chatText.enabled = false;
                movePoints.enabled = false;
                moveType.enabled = false;
                moveSelector.SetActive(false);
                break;
            case ChatState.ChatOnly:
                chatbox.enabled = true;
                chatText.enabled = true;
                movePoints.enabled = false;
                moveType.enabled = false;
                moveSelector.SetActive(false);
                break;
            case ChatState.SelectMove:
                chatText.enabled = false;
                movePoints.enabled = true;
                moveType.enabled = true;
                moveSelector.SetActive(true);
                break;
        }
    }

    public IEnumerator Print(string message, bool immediate = false)
    {
        if (immediate)
        {
            chatText.text = message;
            yield break;
        }

        IsBusy = true;

        var lastCheckedIndex = 0;
        var tagOpenFound = false;
        var tagsClosedFound = 0;

        //clear
        chatText.text = "";

        var maxLength = message.Length * framesPerChar;
        for (var i = 1; i < maxLength; i++)
        {
            var actualIndex = i / framesPerChar;

            //colored text "support"
            if (actualIndex != lastCheckedIndex && message[actualIndex] == '<') tagOpenFound = true;
            else if (actualIndex != lastCheckedIndex && message[actualIndex] == '>') tagsClosedFound++;

            lastCheckedIndex = actualIndex;

            if (tagOpenFound && tagsClosedFound < 2) continue;
            else
            {
                tagOpenFound = false;
                tagsClosedFound = 0;
            }

            if (i % framesPerChar == 0) chatText.text = message.Substring(0, actualIndex + 1);
            yield return null;
        }

        IsBusy = false;
    }
}
