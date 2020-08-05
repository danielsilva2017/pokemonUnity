using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class NPC : MonoBehaviour
{
    protected string characterName;
    protected PlayerMove player;
    protected bool isInteracting;
    protected Behaviour behaviour;
    protected Pokemon[] pokemons;

    private Chatbox chatbox;
    private string[] dialogue;
    private string next;
    private int dialogueIndex;

    public enum Behaviour
    {
        STARTER, BATTLE
    }

    public NPC(string name, string[] dialogue, Behaviour behaviour)
    {
        characterName = name;
        this.dialogue = dialogue;
        this.behaviour = behaviour;
        dialogueIndex = 0;
        isInteracting = false;
    }

    public NPC(string name, string[] dialogue, Pokemon[] pokemons, Behaviour behaviour)
    {
        characterName = name;
        this.dialogue = dialogue;
        this.behaviour = behaviour;
        this.pokemons = pokemons;
        dialogueIndex = 0;
        isInteracting = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        chatbox = GameObject.Find("Chatbox").GetComponent<Chatbox>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isInteracting) return;

        if (Input.GetKeyDown(KeyCode.Z))
        {

            // drop input
            if (chatbox.IsBusy()) return;

            if ((next = NextDialogue()) != null)
            {
                chatbox.ShowText(next);
            }
            else
            {
                isInteracting = false;
                dialogueIndex = 0;
                player.EndInteraction();
                chatbox.Hide();
                DoAction();
            }
        }
    }

    private string NextDialogue()
    {
        return dialogueIndex < dialogue.Length ? dialogue[dialogueIndex++] : null;
    }

    public void Interact(PlayerMove player)
    {
        this.player = player;
        chatbox.Show();
        chatbox.ShowText(NextDialogue());
        isInteracting = true;
    }

    private void DoAction()
    {
        switch (behaviour)
        {
            case Behaviour.STARTER:
                SceneManager.LoadScene(2);
                break;
            case Behaviour.BATTLE:
                if (pokemons != null) BeginBattle();
                break;
        }
    }

    private void BeginBattle()
    {
        
    }
    
}
