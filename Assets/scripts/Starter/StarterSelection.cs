using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

[System.Serializable]
public class StarterSelectionBubble
{
    public PokemonBase pokemon;
    public SpriteRenderer sprite;
    public SpriteRenderer bubble;
}

public class StarterSelection : MonoBehaviour
{
    public OverworldDialog chatbox;
    public StarterSelectionBubble[] starters;
    public AudioSource chatSound;
    public SpriteRenderer transition;

    private Player player;
    private int selectionIndex;
    private int confirmationIndex;
    private bool? confirmation;
    private InputRequest inputRequestMode;
    private Color active = new Color(1, 1, 1, 1);
    private Color faded = new Color(0.59f, 0.59f, 0.59f, 0.59f);

    public bool IsBusy { get; set; }

    private enum InputRequest
    {
        StarterSelection, Confirmation
    }

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;
        player = SceneInfo.GetPlayerInfo().Player;
        chatbox.confirmationObject.SetActive(false);
        chatbox.Hide();
        StartCoroutine(HandleStarterSelection());
    }

    // Update is called once per frame
    void Update()
    {
        if (IsBusy) return;

        switch (inputRequestMode)
        {
            case InputRequest.StarterSelection:
                StarterPicker();
                break;
            case InputRequest.Confirmation:
                ConfirmationPicker();
                break;
        }
    }

    private IEnumerator HandleStarterSelection()
    {
        IsBusy = true;
        FocusOnBubble(0);
        transition.gameObject.SetActive(true);
        MakeVisible(transition);
        yield return FadeOut(transition, 20);
        transition.gameObject.SetActive(false);
        chatbox.Show();

        while (confirmation != true)
        {
            confirmation = null; // clear it in case it's false
            yield return chatbox.Print("Which Pokemon will you choose?");
            inputRequestMode = InputRequest.StarterSelection;
            IsBusy = false;
            yield return AwaitKey(KeyCode.Z);
            IsBusy = true;
            chatSound.Play();
            var selection = starters[selectionIndex].pokemon;
            yield return chatbox.Print($"So you want {selection.name}, the {Types.TypeToString(selection.primaryType)}-type Pokemon?");
            chatbox.confirmationObject.SetActive(true);
            chatbox.ConfirmationBox.CursorYes();
            confirmationIndex = 0;
            inputRequestMode = InputRequest.Confirmation;
            IsBusy = false;
            yield return Await(() => confirmation != null);
            IsBusy = true;
            chatbox.confirmationObject.SetActive(false);
        }

        player.Pokemons.Add(new Pokemon(starters[selectionIndex].pokemon, 5));
        transition.gameObject.SetActive(true);
        yield return FadeIn(transition, 20);
        SceneInfo.ReturnToOverworld();
    }

    private void StarterPicker()
    {
        var oldSelectionIndex = selectionIndex;

        if (Input.GetKeyDown(KeyCode.LeftArrow)) selectionIndex = selectionIndex == 0 ? 2 : selectionIndex - 1;
        if (Input.GetKeyDown(KeyCode.RightArrow)) selectionIndex = (selectionIndex + 1) % 3;

        if (oldSelectionIndex != selectionIndex)
        {
            chatSound.Play();
            FocusOnBubble(selectionIndex);
        }
    }

    private void ConfirmationPicker()
    {
        var oldConfirmationIndex = confirmationIndex;

        if (Input.GetKeyDown(KeyCode.UpArrow)) confirmationIndex = confirmationIndex == 1 ? 0 : 1;
        if (Input.GetKeyDown(KeyCode.DownArrow)) confirmationIndex = confirmationIndex == 0 ? 1 : 0;

        if (oldConfirmationIndex != confirmationIndex)
        {
            chatSound.Play();
            if (confirmationIndex == 0) chatbox.ConfirmationBox.CursorYes();
            else chatbox.ConfirmationBox.CursorNo();
        }

        // don't learn move
        if (Input.GetKeyDown(KeyCode.X))
        {
            confirmation = false;
            chatSound.Play();
            return;
        }

        // use or cancel
        if (Input.GetKeyDown(KeyCode.Z))
        {
            confirmation = confirmationIndex == 0; // 0 = yes
            chatSound.Play();
        }
    }

    private void FocusOnBubble(int index)
    {
        for (var i = 0; i < starters.Length; i++)
        {
            if (i == index)
            {
                starters[i].sprite.color = active;
                starters[i].bubble.color = active;
            }
            else
            {
                starters[i].sprite.color = faded;
                starters[i].bubble.color = faded;
            }
        }
    }
}
