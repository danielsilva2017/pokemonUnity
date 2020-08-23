using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// An instance of an item, based on a skeleton.
/// </summary>
public class Item
{
    public ItemBase Skeleton { get; set; }
    public ItemFunctions Functions { get; set; }

    public Item(ItemBase skeleton)
    {
        Skeleton = skeleton;
        Functions = GetFunctions(skeleton.logic);
    }

    // some getters for convenience
    public string Name { get { return Skeleton.itemName; } }
    public string Description { get { return Skeleton.description; } }
    public Sprite Sprite { get { return Skeleton.sprite; } }
    public ItemCategory Category { get { return Skeleton.bagCategory; } }
    public ItemLogic Logic { get { return Skeleton.logic; } }

    // use reflection to find the correct item logic
    private ItemFunctions GetFunctions(ItemLogic logic)
    {
        var _class = System.Type.GetType(Enum.GetName(typeof(ItemLogic), logic));
        return (ItemFunctions) Activator.CreateInstance(_class);
    }
}
