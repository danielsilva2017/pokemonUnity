using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NPC : MonoBehaviour
{
    public OverworldDialog chatbox;

    protected string Name { get; set; }
    protected string Class { get; set; }
    protected PlayerLogic PlayerLogic { get; set; }
    protected bool IsInteracting { get; set; }
    protected string[] Dialogue { get; set; }
    protected string[] PostDialogue { get; set; }

    private string next;
    private int dialogueIndex;

    public bool IsDefeated { get; set; }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!IsInteracting) return;

        if (Input.GetKeyDown(KeyCode.Z))
        {

            // drop input
            if (chatbox.IsBusy) return;

            if ((next = NextDialogue()) != null)
                chatbox.Print(next);
            else
            {
                IsInteracting = false;
                dialogueIndex = 0;
                PlayerLogic.EndInteraction();
                chatbox.Hide();
                if (!IsDefeated) DoAction();
            }
        }
    }

    private string NextDialogue()
    {
        var dialogueList = IsDefeated ? PostDialogue : Dialogue;
        return dialogueIndex < dialogueList.Length ? dialogueList[dialogueIndex++] : null;
    }

    public void Interact(PlayerLogic player)
    {
        PlayerLogic = player;
        chatbox.Show();
        chatbox.Print(NextDialogue());
        IsInteracting = true;
    }

    protected abstract void DoAction();
}
