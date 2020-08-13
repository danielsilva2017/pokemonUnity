using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Pokemon", menuName = "Pokemon")]

/// <summary>
/// The skeleton data for a Pokemon, which can be set through the Unity UI.
/// </summary>
public class PokemonBase : ScriptableObject
{
    public string pokemonName;
    public string dexNumber; //also used to find the correct animations for front and back
    public Sprite icon;
    public Type primaryType;
    public Type secondaryType;
    public int hpStat;
    public int atkStat;
    public int defStat;
    public int spAtkStat;
    public int spDefStat;
    public int spdStat;
    public ExpGroup expGroup;
    public int expStat; //the base xp yield. https://bulbapedia.bulbagarden.net/wiki/List_of_Pok%C3%A9mon_by_effort_value_yield
    public AbilityBase[] learnableAbilities;
    public LearnableMove[] learnableMoves;
}

[System.Serializable]
public class LearnableMove
{
    public MoveBase skeleton;
    public int level;
}

public enum ExpGroup
{
    Erratic, Fast, MediumFast, MediumSlow, Slow, Fluctuating
}