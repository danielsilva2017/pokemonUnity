using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Slot
{
    public Pokemon Pokemon { get; set; }
    public Text Name { get; set; }
    public SpriteRenderer Sprite { get; set; }
    public Text Level { get; set; }
    public Text Health { get; set; }
    public Image HealthBar { get; set; }
    public Image HealthBarSelected { get; set; }
    public SpriteRenderer Status { get; set; }
    public Image Background { get; set; }

    public Slot(GameObject gameObject)
    {
        Name = gameObject.transform.GetChild(0).GetComponent<Text>();
        Sprite = gameObject.transform.GetChild(1).GetComponent<SpriteRenderer>();
        Level = gameObject.transform.GetChild(2).GetComponent<Text>();
        Health = gameObject.transform.GetChild(3).GetComponent<Text>();
        HealthBar = gameObject.transform.GetChild(4).GetComponent<Image>();
        HealthBarSelected = gameObject.transform.GetChild(5).GetComponent<Image>();
        Status = gameObject.transform.GetChild(6).GetComponent<SpriteRenderer>();
        Background = gameObject.GetComponent<Image>();
    }
}

public class Party : MonoBehaviour
{
    public GameObject battleCanvas;
    public GameObject partyCanvas;
    public GameObject[] partyMemberObjects;
    public Dialog chatbox;
    public AudioSource chatSound;
    public SpriteRenderer transition;

    private Slot[] slots = new Slot[6];
    private Sprite[] slotBackgrounds; // selected, not selected, none, selected dead, not selected dead
    private Sprite[] statuses; // psn, bpsn, slp, par, frz, brn, fnt
    private int selectionIndex;
    private int amountInParty;
    private bool playerIsSwitching;

    // Start is called before the first frame update
    void Start()
    {
        slotBackgrounds = Resources.LoadAll<Sprite>("Images/party_entries");
        statuses = Resources.LoadAll<Sprite>("Images/status");

        for (var i = 0; i < 6; i++)
            slots[i] = new Slot(partyMemberObjects[i]);

        chatbox.SetState(ChatState.Party);
        StartCoroutine(chatbox.Print("Select your Pokemon.", true));
    }

    // Update is called once per frame
    void Update()
    {
        if (!playerIsSwitching) return;

        SwitchPicker();
    }

    public void Init(BattleLogic logic)
    {
        for (var i = 0; i < logic.ActiveAllies.Count; i++)
        {
            var pkmn = logic.ActiveAllies[i];
            var slot = slots[i];
            FillSlot(pkmn, slot, i == 0);
        }

        for (var i = 0; i < logic.PartyAllies.Count; i++)
        {
            var pkmn = logic.PartyAllies[i];
            var slot = slots[i + logic.ActiveAllies.Count];
            FillSlot(pkmn, slot, false);
        }

        for (var i = logic.ActiveAllies.Count + logic.PartyAllies.Count; i < 6; i++)
            EmptySlot(slots[i]);

        amountInParty = logic.ActiveAllies.Count + logic.PartyAllies.Count;
        playerIsSwitching = true;
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

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (chatbox.IsBusy) return;

            chatSound.Play();
            if (slots[selectionIndex].Pokemon.Status == Status.Fainted)
                StartCoroutine(chatbox.Print($"{slots[selectionIndex].Pokemon.Name} is unable to fight!"));
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
            slot.HealthBarSelected.transform.localScale = new Vector3(((float)pokemon.Health) / pokemon.MaxHealth, 1f, slot.HealthBarSelected.transform.localScale.z);
            slot.Background.sprite = pokemon.Status == Status.Fainted ? slotBackgrounds[4] : slotBackgrounds[0];
        }
        else
        {
            slot.HealthBarSelected.enabled = false;
            slot.HealthBar.transform.localScale = new Vector3(((float)pokemon.Health) / pokemon.MaxHealth, 1f, slot.HealthBar.transform.localScale.z);
            slot.Background.sprite = pokemon.Status == Status.Fainted ? slotBackgrounds[3] : slotBackgrounds[1];
        }

        slot.Status.sprite = pokemon.Status == Status.None ? null : statuses[(int) pokemon.Status];  
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
