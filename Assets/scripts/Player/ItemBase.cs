using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Item")]

/// <summary>
/// The skeleton data for an item, which can be set through the Unity UI.
/// </summary>
public class ItemBase : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;
    public Sprite sprite;
    public ItemCategory category;
    public ItemLogic logic;
    public bool affectsPlayer; // item either affects player or a Pokemon
    public bool affectsEnemy; // if it affects a Pokemon, it either affects an ally or a target
}

public enum ItemCategory
{
    KeyItem, PokeballItem, MiscItem
}