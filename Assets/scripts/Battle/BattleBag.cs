using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Utils;

public class BattleBag : MonoBehaviour
{
    public GameObject battleCanvas;
    public GameObject bagCanvas;
    public GameObject partyCanvas;
    public HUD hud;
    public BattleParty party;
    public BattleBag bag;
    public Dialog chatbox;
    public Text bagType;
    public Image itemIcon;
    public Text itemDesc;
    public GameObject[] bagObjects;
    public SpriteRenderer arrowUp;
    public SpriteRenderer arrowDown;
    public Sprite itemSelected;
    public Sprite itemUnselected;
    public AudioSource chatSound;

    private IBattle Battle;
    private readonly BagSlot[] slots = new BagSlot[6];
    private int selectionIndex; // UI selection index (up/down)
    private int bagTypeIndex; // UI selection index (left/right)
    private int itemIndex; // actual bag list index of selected item
    private int confirmationIndex; // confirmation box selection index
    private bool askingConfirmation;

    public bool IsBusy { get; set; }
    public Item ItemToUse { get; set; }
    public int ItemToUseIndex { get; set; }
    public GameObject GameObject { get { return gameObject; } }

    // Start is called before the first frame update
    void Start()
    {
        for (var i = 0; i < 6; i++)
            slots[i] = new BagSlot(bagObjects[i]);

        bagCanvas.SetActive(false);
        chatbox.gameObject.SetActive(false);
        chatbox.confirmationObject.SetActive(false);
        IsBusy = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsBusy)
        {
            if (askingConfirmation) ConfirmationPicker();
            else ItemPicker();
        }
    }

    public void Init(IBattle battle)
    {
        IsBusy = false;
        Battle = battle;
        itemIcon.sprite = null;
        itemDesc.text = "";

        bagType.text = GetBagType();
        FocusListOnSelection();
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
            ReturnToItemSelection();
            return;
        }

        // use or cancel
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (confirmationIndex == 0) // yes
            {
                if (ItemToUse.Usage == ItemUsage.TargetsEnemy)
                {
                    IsBusy = true;
                    askingConfirmation = false;
                    chatbox.confirmationObject.SetActive(false);
                    chatbox.gameObject.SetActive(false);
                    Battle.NotifyItemUsed(ItemToUse);
                }
                else
                {
                    askingConfirmation = false;
                    chatbox.confirmationObject.SetActive(false);
                    chatbox.gameObject.SetActive(false);
                    StartCoroutine(GoToParty());
                }
            }
            else // no
            {
                ReturnToItemSelection();
            }
        }
    }

    private void ReturnToItemSelection()
    {
        chatbox.gameObject.SetActive(false);
        chatbox.confirmationObject.SetActive(false);
        ItemToUse = null;
        askingConfirmation = false;
    }

    private void ItemPicker()
    {
        var oldSelectionIndex = selectionIndex;
        var oldBagTypeIndex = bagTypeIndex;
        var list = GetEntryList();

        if (Input.GetKeyDown(KeyCode.UpArrow) && list.Count >= 1)
        {
            if (selectionIndex > 0)
                itemIndex--;

            if (selectionIndex == 1 && itemIndex > 0)
                FocusListOnSelection();
            else if (selectionIndex > 0)
                selectionIndex--;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow) && list.Count >= 1)
        {
            if (selectionIndex < list.Count - 1)
                itemIndex++;

            if (selectionIndex == 4 && itemIndex < list.Count - 1)
                FocusListOnSelection();
            else if (selectionIndex < list.Count - 1)
                selectionIndex++;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow)) bagTypeIndex = bagTypeIndex == 0 ? 2 : bagTypeIndex - 1;
        if (Input.GetKeyDown(KeyCode.RightArrow)) bagTypeIndex = (bagTypeIndex + 1) % 3;

        // up/down
        if (oldSelectionIndex != selectionIndex)
        {
            chatSound.Play();
            AddHighlight(slots[selectionIndex], list[selectionIndex].item);
            RemoveHighlight(slots[oldSelectionIndex]);
        }

        // left/down
        if (oldBagTypeIndex != bagTypeIndex)
        {
            var newList = GetEntryList();
            // try to transfer indexes to next list
            selectionIndex = Limit(0, selectionIndex, newList.Count - 1);
            itemIndex = Limit(0, itemIndex, newList.Count - 1);
            Init(Battle);
        }

        // back to menu
        if (Input.GetKeyDown(KeyCode.X))
        {
            ItemToUse = null;
            StartCoroutine(EndOpenBag());
            return;
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (chatbox.IsBusy || list.Count == 0) return;

            var item = list[selectionIndex].item;

            chatSound.Play();
            if (item.Usage == ItemUsage.TargetsEnemy && Battle.BattleInfo.IsTrainerBattle)
                StartCoroutine(PrintAndAwait("This can't be used during a trainer battle!"));
            else if (item.Usage == ItemUsage.TargetsPlayer)
                StartCoroutine(PrintAndAwait("This can't be used right now."));
            else
            {
                ItemToUse = item;
                ItemToUseIndex = itemIndex;
                StartCoroutine(SummonConfirmationBox());
            }
        }
    }

    private IEnumerator PrintAndAwait(string message)
    {
        chatbox.gameObject.SetActive(true);
        IsBusy = true;
        yield return chatbox.Print(message);
        yield return AwaitKey(KeyCode.Z);
        chatSound.Play();
        chatbox.gameObject.SetActive(false);
        IsBusy = false;
    }

    private IEnumerator SummonConfirmationBox()
    {
        IsBusy = true;
        chatbox.gameObject.SetActive(true);
        yield return chatbox.Print($"Use the {ItemToUse.Name}?");
        chatbox.confirmationObject.SetActive(true);
        confirmationIndex = 0;
        chatbox.ConfirmationBox.CursorYes();
        askingConfirmation = true;
        IsBusy = false;
    }

    private IEnumerator EndOpenBag()
    {
        IsBusy = true;
        askingConfirmation = false;
        yield return hud.FadeInTransition();
        bagCanvas.SetActive(false);
        battleCanvas.SetActive(true);
        yield return hud.FadeOutTransition();
        Battle.NotifyItemUsed(ItemToUse);
    }

    private IEnumerator GoToParty()
    {
        IsBusy = true;
        askingConfirmation = false;
        yield return hud.FadeInTransition();
        bagCanvas.SetActive(false);
        partyCanvas.SetActive(true);
        party.Init(Battle, false);
        yield return hud.FadeOutTransition();
    }

    private void AddHighlight(BagSlot slot, Item item)
    {
        slot.background.sprite = itemSelected;
        itemIcon.sprite = item.Sprite;
        itemDesc.text = item.Description;
    }

    private void RemoveHighlight(BagSlot slot)
    {
        slot.background.sprite = itemUnselected;
    }

    // item index reflects true position in item list
    private void FocusListOnSelection()
    {
        var list = GetEntryList();

        for (var i = 0; i < 6; i++)
        {
            var entry = i + itemIndex - selectionIndex < list.Count ? list[i + itemIndex - selectionIndex] : null;
            if (entry != null)
                FillSlot(slots[i], entry, i == selectionIndex);
            else
                EmptySlot(slots[i]);
        }

        arrowUp.enabled = itemIndex > 1;
        arrowDown.enabled = list.Count > 6 && itemIndex < list.Count - 2;
    }

    private void FillSlot(BagSlot slot, BagEntry entry, bool isSelected)
    {
        slot.itemName.text = entry.item.Name;
        slot.itemAmount.text = $"x{entry.amount.ToString()}" ?? "";
        slot.background.enabled = true;
        if (isSelected) AddHighlight(slot, entry.item);
        else RemoveHighlight(slot);
    }

    private void EmptySlot(BagSlot slot)
    {
        slot.itemName.text = "";
        slot.itemAmount.text = "";
        slot.background.enabled = false;
    }

    private string GetBagType()
    {
        switch (bagTypeIndex)
        {
            case 0: return "Items";
            case 1: return "Pokeballs";
            case 2: return "Key Items";
            default: return "";
        }
    }

    private List<BagEntry> GetEntryList()
    {
        switch (bagTypeIndex)
        {
            case 0: return Battle.PlayerInfo.Player.Bag.MiscItems;
            case 1: return Battle.PlayerInfo.Player.Bag.PokeballItems;
            case 2: return Battle.PlayerInfo.Player.Bag.KeyItems;
            default: return null;
        }
    }
}