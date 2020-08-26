using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using static Utils;

public class PartySlot
{
    public Pokemon Pokemon { get; set; }
    public Text Name { get; set; }
    public SpriteRenderer Sprite { get; set; }
    public Text Level { get; set; }
    public Text Health { get; set; }
    public Image HealthBar { get; set; }
    public Image HealthBarSelected { get; set; }
    public Image Status { get; set; }
    public Image Background { get; set; }

    public PartySlot(GameObject gameObject)
    {
        Name = gameObject.transform.GetChild(0).GetComponent<Text>();
        Sprite = gameObject.transform.GetChild(1).GetComponent<SpriteRenderer>();
        Level = gameObject.transform.GetChild(2).GetComponent<Text>();
        Health = gameObject.transform.GetChild(3).GetComponent<Text>();
        HealthBar = gameObject.transform.GetChild(4).GetComponent<Image>();
        HealthBarSelected = gameObject.transform.GetChild(5).GetComponent<Image>();
        Status = gameObject.transform.GetChild(6).GetComponent<Image>();
        Background = gameObject.GetComponent<Image>();
    }
}

public class OverworldParty : MonoBehaviour, ITransitionable
{
    public GameObject party;
    public GameObject[] partyMemberObjects;
    public OverworldBag bag;
    public MenuPoke menu;
    public OverworldDialog chatbox;
    public PlayerLogic playerLogic;
    public SpriteRenderer transition;
    public AudioSource chatSound;
    public AudioSource healthUpSound;

    private readonly PartySlot[] slots = new PartySlot[6];
    private Sprite[] slotBackgrounds; // selected, not selected, none, selected dead, not selected dead
    private Sprite[] statuses; // psn, bpsn, slp, par, frz, brn, fnt
    private int selectionIndex;
    private int amountInParty;
    private int confirmationIndex; // confirmation box selection index
    private bool askingConfirmation;
    private readonly float updateSpeed = 160f; // amount of frames required to fill/empty a bar

    public bool IsBusy { get; set; }
    public PartySlot SlotToUse { get; set; }
    public GameObject GameObject { get { return gameObject; } }

    // Start is called before the first frame update
    void Start()
    {
        slotBackgrounds = Resources.LoadAll<Sprite>("Images/party_entries");
        statuses = Resources.LoadAll<Sprite>("Images/status");

        for (var i = 0; i < 6; i++)
            slots[i] = new PartySlot(partyMemberObjects[i]);

        party.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsBusy)
        {
            if (askingConfirmation) ConfirmationPicker();
            else PokemonPicker();
        }
    }

    public void Init()
    {
        chatbox.gameObject.SetActive(true);
        chatbox.PrintWithSound("Select a Pokemon.", true);

        // reset pointer
        selectionIndex = 0;

        // add the pokemons in the field
        for (var i = 0; i < playerLogic.Player.Pokemons.Count; i++)
        {
            var pkmn = playerLogic.Player.Pokemons[i];
            var slot = slots[i];
            FillSlot(pkmn, slot, i == 0);
        }

        // fill the remaining slots with nothing
        for (var i = playerLogic.Player.Pokemons.Count; i < 6; i++)
            EmptySlot(slots[i]);

        amountInParty = playerLogic.Player.Pokemons.Count;
    }

    private IEnumerator UpdateSlot(PartySlot slot)
    {
        var pokemon = slot.Pokemon;

        if (pokemon.Status == Status.None)
            MakeInvisible(slot.Status);
        else
        {
            MakeVisible(slot.Status);
            slot.Status.sprite = statuses[(int) pokemon.Status];
        }

        var currentScale = slot.HealthBarSelected.transform.localScale.x;
        var newScale = pokemon.Health * 1f / pokemon.MaxHealth;
        var bardiff = newScale - currentScale;

        // stop when there's nothing to update - consider fp inaccuracies
        if (Math.Abs(bardiff) < 0.0001f) yield break;

        slot.Background.sprite = pokemon.Status == Status.Fainted ? slotBackgrounds[4] : slotBackgrounds[0];

        var oldHealth = Mathf.FloorToInt(currentScale * pokemon.MaxHealth);
        var numdiff = pokemon.Health - oldHealth;
        var frames = Math.Abs(bardiff) * updateSpeed;

        healthUpSound.Play();
        for (var i = 0; i <= frames; i++)
        {
            slot.Health.text = $"{Math.Max(0, Mathf.FloorToInt(oldHealth + numdiff * i / frames))}<size=5> </size>/<size=5> </size>{pokemon.MaxHealth}";
            slot.HealthBarSelected.transform.localScale = new Vector3(currentScale + bardiff * i / frames, 1f, slot.HealthBarSelected.transform.localScale.z);
            yield return null;
        }
        healthUpSound.Stop();

        // ensure correct numbers are shown at the end
        slot.Health.text = $"{pokemon.Health}<size=5> </size>/<size=5> </size>{pokemon.MaxHealth}";
        slot.HealthBarSelected.transform.localScale = new Vector3(newScale, 1f, slot.HealthBarSelected.transform.localScale.z);
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

        // back to bag
        if (Input.GetKeyDown(KeyCode.X))
        {
            ReturnToPokemonSelection();
            return;
        }

        // use or cancel
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (confirmationIndex == 0) // yes
            {
                StartCoroutine(UseItemOnPokemon());
            }
            else // no
            {
                ReturnToPokemonSelection();
            }
        }
    }

    private IEnumerator UseItemOnPokemon()
    {
        IsBusy = true;
        chatbox.confirmationObject.SetActive(false);
        if (!bag.ItemToUse.Functions.CanBeUsed(bag.ItemToUse, SlotToUse.Pokemon))
        {
            yield return chatbox.Print($"This item can't be used on {SlotToUse.Pokemon.Name} right now.");
        }
        else
        {
            playerLogic.Player.Bag.TakeItem(bag.ItemToUse, bag.ItemToUseIndex, 1);
            yield return bag.ItemToUse.Functions.Use(bag.ItemToUse, SlotToUse.Pokemon, chatbox);
            yield return UpdateSlot(SlotToUse);
            yield return bag.ItemToUse.Functions.OnUse(bag.ItemToUse, SlotToUse.Pokemon, chatbox);
        }

        while (true)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                IsBusy = false;
                ReturnToItemSelection();
                break;
            }
            else yield return null;
        }
    }

    private void ReturnToPokemonSelection()
    {
        chatbox.gameObject.SetActive(false);
        chatbox.confirmationObject.SetActive(false);
        SlotToUse = null;
        askingConfirmation = false;
    }

    private void ReturnToItemSelection()
    {
        chatbox.gameObject.SetActive(false);
        chatbox.confirmationObject.SetActive(false);
        SlotToUse = null;
        askingConfirmation = false;
        playerLogic.playerUI.MenuTransition(this, bag);
    }

    private IEnumerator SummonConfirmationBox()
    {
        IsBusy = true;
        chatbox.gameObject.SetActive(true);
        yield return chatbox.Print($"Use the {bag.ItemToUse.Name} on {SlotToUse.Pokemon.Name}?");
        chatbox.confirmationObject.SetActive(true);
        confirmationIndex = 0;
        chatbox.ConfirmationBox.CursorYes();
        askingConfirmation = true;
        IsBusy = false;
    }

    private void PokemonPicker()
    {
        var oldIndex = selectionIndex;

        if (Input.GetKeyDown(KeyCode.UpArrow)) selectionIndex = selectionIndex == 0 ? amountInParty - 1 : selectionIndex - 1;
        if (Input.GetKeyDown(KeyCode.DownArrow)) selectionIndex = selectionIndex == amountInParty - 1 ? 0 : selectionIndex + 1;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) selectionIndex = selectionIndex == 0 ? amountInParty - 1 : selectionIndex - 1;
        if (Input.GetKeyDown(KeyCode.RightArrow)) selectionIndex = selectionIndex == amountInParty - 1 ? 0 : selectionIndex + 1;

        if (oldIndex != selectionIndex)
        {
            if (slots[selectionIndex].Pokemon == null) return;

            chatSound.Play();
            AddHighlight(slots[selectionIndex]);
            RemoveHighlight(slots[oldIndex]);
        }

        // back to actions
        if (Input.GetKeyDown(KeyCode.X))
        {
            chatbox.gameObject.SetActive(false);
            menu.SetSelectionIndex(0);
            playerLogic.playerUI.MenuTransition(this, menu);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (chatbox.IsBusy) return;

            chatSound.Play();
            if (bag.ItemToUse == null) return;
            else // picked a pokemon to use an item on
            {
                SlotToUse = slots[selectionIndex];
                StartCoroutine(SummonConfirmationBox());
            }
        }
    }

    private void AddHighlight(PartySlot slot)
    {
        slot.HealthBarSelected.enabled = true;
        slot.HealthBarSelected.transform.localScale = slot.HealthBar.transform.localScale;
        slot.HealthBar.enabled = false;
        slot.Background.sprite = slot.Pokemon.Status == Status.Fainted ? slotBackgrounds[4] : slotBackgrounds[0];
    }

    private void RemoveHighlight(PartySlot slot)
    {
        slot.HealthBar.enabled = true;
        slot.HealthBar.transform.localScale = slot.HealthBarSelected.transform.localScale;
        slot.HealthBarSelected.enabled = false;
        slot.Background.sprite = slot.Pokemon.Status == Status.Fainted ? slotBackgrounds[3] : slotBackgrounds[1];
    }

    private string GetSpriteID(Pokemon pokemon)
    {
        var dex = pokemon.Skeleton.dexNumber;
        if (dex < 10) return $"00{dex}MS";
        if (dex < 100) return $"0{dex}MS";
        return $"{dex}MS";
    }

    private void FillSlot(Pokemon pokemon, PartySlot slot, bool isSelected)
    {
        slot.Pokemon = pokemon;
        slot.Name.text = pokemon.Name;
        slot.Sprite.sprite = Resources.Load<Sprite>($"Images/{GetSpriteID(pokemon)}");
        slot.Level.text = $"Lv. {pokemon.Level}";
        slot.Health.text = $"{pokemon.Health}<size=5> </size>/<size=5> </size>{pokemon.MaxHealth}";

        if (isSelected)
        {
            slot.HealthBar.enabled = false;
            slot.HealthBarSelected.enabled = true;
            slot.HealthBarSelected.transform.localScale = new Vector3(((float)pokemon.Health) / pokemon.MaxHealth, 1f, slot.HealthBarSelected.transform.localScale.z);
            slot.Background.sprite = pokemon.Status == Status.Fainted ? slotBackgrounds[4] : slotBackgrounds[0];
        }
        else
        {
            slot.HealthBarSelected.enabled = false;
            slot.HealthBar.enabled = true;
            slot.HealthBar.transform.localScale = new Vector3(((float)pokemon.Health) / pokemon.MaxHealth, 1f, slot.HealthBar.transform.localScale.z);
            slot.Background.sprite = pokemon.Status == Status.Fainted ? slotBackgrounds[3] : slotBackgrounds[1];
        }

        if (pokemon.Status == Status.None)
            MakeInvisible(slot.Status);
        else
        {
            MakeVisible(slot.Status);
            slot.Status.sprite = statuses[(int)pokemon.Status];
        }
    }

    private void EmptySlot(PartySlot slot)
    {
        slot.Name.enabled = false;
        slot.Sprite.enabled = false;
        slot.Level.enabled = false;
        slot.Health.enabled = false;
        slot.HealthBar.enabled = false;
        slot.HealthBarSelected.enabled = false;
        slot.Status.enabled = false;
        slot.Background.sprite = slotBackgrounds[2];
    }
}
