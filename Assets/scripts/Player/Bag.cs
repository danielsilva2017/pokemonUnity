using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BagEntry
{
    public Item item;
    public int? amount;

    public BagEntry(Item item, int? amount)
    {
        this.item = item;
        this.amount = amount;
    }
}

public class Bag
{
    public List<BagEntry> KeyItems { get; set; }
    public List<BagEntry> PokeballItems { get; set; }
    public List<BagEntry> MiscItems { get; set; }

    public Bag()
    {
        KeyItems = new List<BagEntry>();
        PokeballItems = new List<BagEntry>();
        MiscItems = new List<BagEntry>();
    }

    /// <summary>
    /// Adds an item to the appropriate section of the bag.
    /// </summary>
    public void AddItem(Item item, int amount = 1)
    {
        if (item.Category == ItemCategory.KeyItem)
        {
            KeyItems.Add(new BagEntry(item, null));
            return;
        }

        List<BagEntry> list = item.Category == ItemCategory.PokeballItem ? PokeballItems : MiscItems;

        var index = list.FindIndex(entry => entry.item.Logic == item.Logic);
        if (index == -1) list.Add(new BagEntry(item, amount));
        else list[index].amount += amount;
    }

    /// <summary>
    /// Deducts some quantity of an item from the bag.
    /// </summary>
    public void TakeItem(Item item, int listIndex, int amount = 1)
    {
        if (item.Category == ItemCategory.KeyItem)
            return;

        List<BagEntry> list = item.Category == ItemCategory.PokeballItem ? PokeballItems : MiscItems;
        if (list[listIndex].amount <= amount) list.RemoveAt(listIndex);
        else list[listIndex].amount -= amount;
    }
}
