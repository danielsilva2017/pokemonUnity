using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

/// <summary>
/// Overworld representation of an item that can be collected or interacted with.
/// </summary>
public class OverworldItem : MonoBehaviour
{
    public ItemBase skeleton;
    public OverworldDialog chatbox;
    public AudioSource collectedSound;

    private PlayerLogic playerLogic;
    private bool isInteracting;

    public bool IsCollected { get; set; }

    void Update()
    {
        if (!isInteracting) return;

        // drop input
        if (chatbox.IsBusy) return;

        if (Input.GetKeyDown(KeyCode.Z))
        {
            isInteracting = false;
            playerLogic.EndInteraction();
            IsCollected = true;
            chatbox.Hide();
            Destroy(gameObject);
        }
    }

    public void Collect(PlayerLogic playerLogic)
    {
        this.playerLogic = playerLogic;
        var item = new Item(skeleton);
        playerLogic.Player.Bag.AddItem(item);

        collectedSound.Play();
        chatbox.Show();
        chatbox.PrintSilent($"You got a <color=#0066cc>{item.Name}</color>!");
        isInteracting = true;
    }
}
