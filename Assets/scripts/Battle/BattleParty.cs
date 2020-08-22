using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Utils;

public class BattleParty : MonoBehaviour
{
    public GameObject battleCanvas;
    public GameObject partyCanvas;
    public GameObject[] partyMemberObjects;
    public Dialog chatbox;
    public AudioSource chatSound;
    public SpriteRenderer transition;
    public HUD hud;

    private IBattle Battle;
    private readonly Slot[] slots = new Slot[6];
    private Sprite[] slotBackgrounds; // selected, not selected, none, selected dead, not selected dead
    private Sprite[] statuses; // psn, bpsn, slp, par, frz, brn, fnt
    private int selectionIndex;
    private int amountInParty;
    private bool playerIsSwitching;
    private bool isForcedSwitch; // whether player will have to choose a move right after switching

    // Start is called before the first frame update
    void Start()
    {
        slotBackgrounds = Resources.LoadAll<Sprite>("Images/party_entries");
        statuses = Resources.LoadAll<Sprite>("Images/status");

        for (var i = 0; i < 6; i++)
            slots[i] = new Slot(partyMemberObjects[i]);
    }

    // Update is called once per frame
    void Update()
    {
        if (!playerIsSwitching) return;

        SwitchPicker();
    }

    public void Init(IBattle battle, bool forcedSwitch)
    {
        Battle = battle;
        chatbox.SetState(ChatState.Party);
        StartCoroutine(chatbox.Print("Select your Pokemon.", true));

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
        playerIsSwitching = true;
        isForcedSwitch = forcedSwitch;
    }

    private IEnumerator EndSwitch(Pokemon selection)
    {
        playerIsSwitching = false;
        yield return hud.FadeOut();
        partyCanvas.SetActive(false);
        battleCanvas.SetActive(true);
        yield return hud.FadeIn();
        Battle.NotifySwitchPerformed(selection);
    }

    private void SwitchPicker()
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
            if (chatbox.IsBusy) return;

            chatSound.Play();
            if (isForcedSwitch)
            {
                StartCoroutine(chatbox.Print("You need to select a Pokemon!"));
                return;
            }

            StartCoroutine(EndSwitch(null));
            return;
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (chatbox.IsBusy) return;

            chatSound.Play();
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

    private void AddHighlight(Slot slot)
    {
        slot.HealthBarSelected.enabled = true;
        slot.HealthBarSelected.transform.localScale = slot.HealthBar.transform.localScale;
        slot.HealthBar.enabled = false;
        slot.Background.sprite = slot.Pokemon.Status == Status.Fainted ? slotBackgrounds[4] : slotBackgrounds[0];
    }

    private void RemoveHighlight(Slot slot)
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

    private void FillSlot(Pokemon pokemon, Slot slot, bool isSelected)
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

    private void EmptySlot(Slot slot)
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
