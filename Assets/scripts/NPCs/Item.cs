using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

public class Item : MonoBehaviour
{
    public ItemType type;
    public AudioSource audioSource;

    protected PlayerMove player;
    protected bool isInteracting;

    private Chatbox chatbox;

    // Start is called before the first frame update
    void Start()
    {
        chatbox = GameObject.Find("Chatbox").GetComponent<Chatbox>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isInteracting) return;

        // drop input
        if (chatbox.IsBusy()) return;

        if (Input.GetKeyDown(KeyCode.Z))
        {
            isInteracting = false;
            player.EndInteraction();
            chatbox.Hide();
            Destroy(gameObject);
        }
    }

    public void Collect(PlayerMove player)
    {
        audioSource.Play();
        this.player = player;
        chatbox.Show();
        chatbox.ShowTextSilent("You got a <color=#0066cc>" + TypeToString() + "</color>!");
        //add item to bag here
        isInteracting = true;
    }

    public enum ItemType
    {
        POKEBALL, SUPER_POTION, HYPER_POTION
    }

    private string TypeToString()
    {
        switch (type)
        {
            case ItemType.POKEBALL: return "Pokeball";
            case ItemType.SUPER_POTION: return "Super Potion";
            case ItemType.HYPER_POTION: return "Hyper Potion";
            default: return "mystery item";
        }
    }
}
