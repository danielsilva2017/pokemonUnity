using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Pokemon", menuName = "Pokemon")]

public class PokemonBase : ScriptableObject
{
    public string pokemonName;
    public string dexNumber; //also used to find the correct animations for front and back
    public Sprite icon;
    public Type.TypeName primaryType;
    public Type.TypeName secondaryType;
    public int hpStat;
    public int atkStat;
    public int defStat;
    public int spAtkStat;
    public int spDefStat;
    public int spdStat;
    public LearnableMove[] learnableMoves;
}

[System.Serializable]
public class LearnableMove
{
    public MoveBase skeleton;
    public int level;
}