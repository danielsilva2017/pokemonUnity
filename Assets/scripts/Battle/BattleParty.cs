using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using static Utils;

public class BattleParty : MonoBehaviour
{
    public GameObject battleCanvas;
    public GameObject bagCanvas;
    public GameObject partyCanvas;
    public GameObject[] partyMemberObjects;
    public Dialog chatbox;
    public BattleBag bag;
    public HUD hud;
    public AudioSource chatSound;
    public AudioSource healthUpSound;

    private IBattle Battle;
    private readonly PartySlot[] slots = new PartySlot[6];
    private Sprite[] slotBackgrounds; // selected, not selected, none, selected dead, not selected dead
    private Sprite[] statuses; // psn, bpsn, slp, par, frz, brn, fnt
    private int selectionIndex;
    private int amountInParty;
    private int confirmationIndex; // confirmation box selection index
    private bool askingConfirmation;
    private bool isForcedSwitch; // whether player will have to choose a move right after switching
    private readonly float updateSpeed = 160f; // amount of frames required to fill/empty a bar

    public bool IsBusy { get; set; }
    public PartySlot SlotToUse { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        slotBackgrounds = Resources.LoadAll<Sprite>("Images/party_entries");
        statuses = Resources.LoadAll<Sprite>("Images/status");

        for (var i = 0; i < 6; i++)
            slots[i] = new PartySlot(partyMemberObjects[i]);

        partyCanvas.SetActive(false);
        IsBusy = true;
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

    public void Init(IBattle battle, bool forcedSwitch)
    {
        IsBusy = false;
        Battle = battle;
        chatbox.confirmationObject.SetActive(false);
        chatbox.SetState(ChatState.Party);
        StartCoroutine(chatbox.Print("Select a Pokemon.", true));

        // reset pointer
        selectionIndex = 0;

        // add the pokemons in the field
        for (var i = 0; i < Battle.Logic.ActiveAllies.Count; i++)
        {
            var pkmn = Battle.Logic.ActiveAllies[i];
            var slot = slots[i];
            FillSlot(pkmn, slot, i == 0);
        }

        // add the pokemons not in the field
        for (var i = 0; i < Battle.Logic.PartyAllies.Count; i++)
        {
            var pkmn = Battle.Logic.PartyAllies[i];
            var slot = slots[i + Battle.Logic.ActiveAllies.Count];
            FillSlot(pkmn, slot, false);
        }

        // fill the remaining slots with nothing
        for (var i = Battle.Logic.ActiveAllies.Count + Battle.Logic.PartyAllies.Count; i < 6; i++)
            EmptySlot(slots[i]);

        amountInParty = Battle.Logic.ActiveAllies.Count + Battle.Logic.PartyAllies.Count;
        isForcedSwitch = forcedSwitch;
    }

    private IEnumerator UpdateSlot(PartySlot slot)
    {
        var pokemon = slot.Pokemon;

        if (pokemon.Status == Status.None)
            MakeInvisible(slot.Status);
        else
        {
            MakeVisible(slot.Status);
            slot.Status.sprite = statuses[(int)pokemon.Status];
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

    private IEnumerator EndSwitch(Pokemon selection)
    {
        IsBusy = true;
        yield return hud.FadeInTransition();
        partyCanvas.SetActive(false);
        battleCanvas.SetActive(true);
        yield return hud.FadeOutTransition();
        Battle.NotifySwitchPerformed(selection);
    }

    private IEnumerator EndItemUsage()
    {
        IsBusy = true;
        askingConfirmation = false;
        yield return hud.FadeInTransition();
        partyCanvas.SetActive(false);
        battleCanvas.SetActive(true);
        yield return hud.FadeOutTransition();
        Battle.NotifyItemUsed(bag.ItemToUse);
        bag.ItemToUse = null;
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

    private void ReturnToPokemonSelection()
    {
        chatbox.confirmationObject.SetActive(false);
        StartCoroutine(chatbox.Print("Select a Pokemon.", true));
        SlotToUse = null;
        askingConfirmation = false;
    }

    private IEnumerator ReturnToBag()
    {
        IsBusy = true;
        askingConfirmation = false;
        bag.ItemToUse = null;
        yield return hud.FadeInTransition();
        partyCanvas.SetActive(false);
        bagCanvas.SetActive(true);
        bag.Init(Battle);
        yield return hud.FadeOutTransition();
    }

    private IEnumerator UseItemOnPokemon()
    {
        IsBusy = true;
        chatbox.confirmationObject.SetActive(false);
        if (!bag.ItemToUse.Functions.CanBeUsed(bag.ItemToUse, SlotToUse.Pokemon))
        {
            yield return chatbox.Print($"This item can't be used on {SlotToUse.Pokemon.Name} right now.");
            while (!Input.GetKeyDown(KeyCode.Z)) yield return null;
            IsBusy = false;
            ReturnToPokemonSelection();
            yield break;
        }
        else
        {
            Battle.PlayerInfo.Player.Bag.TakeItem(bag.ItemToUse, bag.ItemToUseIndex, 1);
            yield return bag.ItemToUse.Functions.Use(bag.ItemToUse, SlotToUse.Pokemon, chatbox, null);
            yield return UpdateSlot(SlotToUse);
            yield return bag.ItemToUse.Functions.OnUse(bag.ItemToUse, SlotToUse.Pokemon, chatbox, null);
        }

        while (!Input.GetKeyDown(KeyCode.Z)) yield return null;

        IsBusy = false;
        yield return EndItemUsage();
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

        if (Input.GetKeyDown(KeyCode.X))
        {
            if (chatbox.IsBusy) return;

            chatSound.Play();

            if (bag.ItemToUse != null) // return to bag
            {
                StartCoroutine(ReturnToBag());
                return;
            }
            else // end switch
            {
                if (isForcedSwitch)
                {
                    StartCoroutine(chatbox.Print("You need to select a Pokemon!"));
                    return;
                }

                StartCoroutine(EndSwitch(null));
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (chatbox.IsBusy) return;

            chatSound.Play();

            if (bag.ItemToUse != null) // using item on party member
            {
                SlotToUse = slots[selectionIndex];
                StartCoroutine(SummonConfirmationBox());
                return;
            }
            else // switching active members
            {
                if (slots[selectionIndex].Pokemon.Status == Status.Fainted)
                {
                    StartCoroutine(chatbox.Print($"{slots[selectionIndex].Pokemon.Name} is unable to fight!"));
                    return;
                }

                if (Battle.Logic.ActiveAllies.Contains(slots[selectionIndex].Pokemon))
                {
                    StartCoroutine(chatbox.Print($"{slots[selectionIndex].Pokemon.Name} is already in the fight!"));
                    return;
                }

                // actually perform the selection
                StartCoroutine(EndSwitch(slots[selectionIndex].Pokemon));
            }
        }
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
