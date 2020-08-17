using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

public class Item : MonoBehaviour
{
    public ItemType type;
    public OverworldDialog chatbox;
    public AudioSource collectedSound;

    protected PlayerLogic player;
    protected bool isInteracting;

    public bool IsCollected { get; set; }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!isInteracting) return;

        // drop input
        if (chatbox.IsBusy) return;

        if (Input.GetKeyDown(KeyCode.Z))
        {
            isInteracting = false;
            player.EndInteraction();
            IsCollected = true;
            chatbox.Hide();
            Destroy(gameObject);
        }
    }

    public void Collect(PlayerLogic player)
    {
        collectedSound.Play();
        this.player = player;
        chatbox.Show();
        chatbox.PrintSilent($"You got a <color=#0066cc>{TypeToString()}</color>!");
        //add item to bag here
        isInteracting = true;
    }

    public enum ItemType
    {
        Pokeball, SuperPotion, HyperPotion
    }

    private string TypeToString()
    {
        switch (type)
        {
            case ItemType.Pokeball: return "Pokeball";
            case ItemType.SuperPotion: return "Super Potion";
            case ItemType.HyperPotion: return "Hyper Potion";
            default: return "mystery item";
        }
    }
}
