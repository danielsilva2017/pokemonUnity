using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NPC : MonoBehaviour
{
    public OverworldDialog chatbox;

    public string Name { get; set; }
    protected PlayerLogic PlayerLogic { get; private set; }
    protected bool IsInteracting { get; set; }
    protected string[] Dialogue { get; set; }
    protected string[] PostDialogue { get; set; }

    private string next;
    private int dialogueIndex;

    public bool IsDefeated { get; set; }
    public bool IsBusy { get; set; }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!IsInteracting || IsBusy) return;

        if (Input.GetKeyDown(KeyCode.Z))
        {
            // drop input
            if (chatbox.IsBusy) return;

            if ((next = NextDialogue()) != null)
            {
                chatbox.PrintWithSound(next);
            } 
            else
            {
                chatbox.Hide();
                IsInteracting = false;
                dialogueIndex = 0;
                if (!IsDefeated) StartCoroutine(ActionRunner());
                else PlayerLogic.EndInteraction();
            }
        }
    }

    private string NextDialogue()
    {
        var dialogueList = IsDefeated ? PostDialogue : Dialogue;
        return dialogueIndex < dialogueList.Length ? dialogueList[dialogueIndex++] : null;
    }

    public void Interact(PlayerLogic playerLogic)
    {
        PlayerLogic = playerLogic;
        OnInteractionStart();
        chatbox.Show();
        chatbox.PrintWithSound(NextDialogue());
        IsInteracting = true;
    }

    private IEnumerator ActionRunner()
    {
        IsBusy = true;
        yield return DoAction();
        IsBusy = false;
        PlayerLogic.EndInteraction();
    }

    protected abstract void OnInteractionStart();

    protected abstract IEnumerator DoAction();
}
