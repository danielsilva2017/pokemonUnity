using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NPC : MonoBehaviour
{
    protected string Name { get; set; }
    protected PlayerMove player;
    protected bool isInteracting;

    private Chatbox chatbox;
    private string[] dialogue;
    private string next;
    private int dialogueIndex;

    public NPC(string name, string[] dialogue)
    {
        this.Name = name;
        this.dialogue = dialogue;
        this.dialogueIndex = 0;
        this.isInteracting = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        this.chatbox = GameObject.Find("Chatbox").GetComponent<Chatbox>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!this.isInteracting) return;

        if (Input.GetKeyDown(KeyCode.Z))
        {
            // drop input
            if (this.chatbox.isBusy()) return;

            if ((next = nextDialogue()) != null)
                this.chatbox.ShowText(next);
            else
            {
                this.isInteracting = false;
                this.dialogueIndex = 0;
                this.player.endInteraction();
                this.chatbox.Hide();
            }
        }
    }

    private string nextDialogue()
    {
        return dialogueIndex < dialogue.Length ? dialogue[this.dialogueIndex++] : null;
    }

    public void Interact(PlayerMove player)
    {
        this.player = player;
        this.chatbox.Show();
        this.chatbox.ShowText(nextDialogue());
        this.isInteracting = true;
    }

    
}
