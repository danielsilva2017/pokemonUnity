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
    public ItemCategory bagCategory;
    public ItemUsage usage;
    public ItemLogic logic;
}

public enum ItemCategory
{
    KeyItem, PokeballItem, MiscItem
}

public enum ItemUsage
{
    TargetsSelf, TargetsEnemy, TargetsPlayer
}