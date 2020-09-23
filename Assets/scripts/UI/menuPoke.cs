using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static Utils;

public class MenuPoke : MonoBehaviour, ITransitionable
{
    public GameObject menu;
    public PlayerLogic playerLogic;
    public OverworldParty partyMenu;
    public OverworldBag bagMenu;
    public SpriteRenderer transition;
    public AudioSource chatSound;
    public AudioSource menuSound;
    public Text pokedex;
    public Text pokemon;
    public Text bag;
    public Text trainer;
    public Text save;
    public Text options;

    private MenuEntry[] entries;
    private int selectionIndex = -1;

    public bool IsBusy { get; set; }
    public GameObject GameObject { get { return gameObject; } }

    private class MenuEntry
    {
        private Func<bool> selectionCondition;
        public Text Text { get; set; }
        public bool IsSelectable { get; set; }

        public MenuEntry(Text text, Func<bool> selectionCondition) {
            Text = text;
            this.selectionCondition = selectionCondition;
        }

        public void CalculateSelectability()
        {
            IsSelectable = selectionCondition.Invoke();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        entries = new MenuEntry[] {
            new MenuEntry(pokedex, () => false),
            new MenuEntry(pokemon, () => playerLogic.Player.Pokemons.Count > 0),
            new MenuEntry(bag, () => true),
            new MenuEntry(trainer, () => false),
            new MenuEntry(save, () => false),
            new MenuEntry(options, () => false)
        };

        Hide();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsBusy)
            OptionPicker();
    }

    public bool Toggle()
    {
        if (menu.activeInHierarchy) Hide();
        else Show();

        return menu.activeInHierarchy;
    }

    public void SetSelectionIndex(int index)
    {
        RemoveHighlight(entries[selectionIndex]);
        AddHighlight(entries[index]);
        selectionIndex = index;
    }

    private void OptionPicker()
    {
        var oldIndex = selectionIndex;

        if (Input.GetKeyDown(KeyCode.UpArrow)) selectionIndex = FindPreviousSelectableIndex(selectionIndex);
        if (Input.GetKeyDown(KeyCode.DownArrow)) selectionIndex = FindNextSelectableIndex(selectionIndex);

        if (oldIndex != selectionIndex)
        {
            chatSound.Play();
            AddHighlight(entries[selectionIndex]);
            RemoveHighlight(entries[oldIndex]);
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            IsBusy = true;
            chatSound.Play();

            switch (selectionIndex)
            {
                case 0:
                    break;
                case 1:
                    playerLogic.playerUI.MenuTransition(this, partyMenu);
                    break;
                case 2:
                    playerLogic.playerUI.MenuTransition(this, bagMenu);
                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    break;
            }
        }
    }

    private int FindNextSelectableIndex(int currentIndex)
    {
        int index = currentIndex;

        do
        {
            index = (index + 1) % entries.Length;
        }
        while (!entries[index].IsSelectable);

        return index;
    }

    private int FindPreviousSelectableIndex(int currentIndex)
    {
        int index = currentIndex;

        do
        {
            index = index == 0 ? entries.Length - 1 : index - 1;
        }
        while (!entries[index].IsSelectable);

        return index;
    }

    private void MakeUnselectable(MenuEntry entry)
    {
        entry.Text.color = new Color(0.59f, 0.59f, 0.59f, 1);
    }

    private void AddHighlight(MenuEntry entry)
    {
        entry.Text.color = new Color(1, 0, 0, 1);
    }

    private void RemoveHighlight(MenuEntry entry)
    {
        entry.Text.color = new Color(0, 0, 0, 1);
    }

    public void Show()
    {
        foreach (var entry in entries)
        {
            entry.CalculateSelectability();
            if (!entry.IsSelectable) MakeUnselectable(entry);
        }

        if (selectionIndex == -1) selectionIndex = FindNextSelectableIndex(0);

        AddHighlight(entries[selectionIndex]);
        menuSound.Play();
        menu.SetActive(true);
    }

    public void Hide()
    {
        menu.SetActive(false);
    }

    public void Init()
    {
        IsBusy = false;
    }
}
