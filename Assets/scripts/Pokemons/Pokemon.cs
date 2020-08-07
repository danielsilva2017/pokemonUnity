using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pokemon
{
    public PokemonBase Skeleton { get; set; }
    public Move[] Moves { get; set; }
    public int Health { get; set; }
    public int Level { get; set; }

    public Pokemon(PokemonBase skeleton, int level)
    {
        Skeleton = skeleton;
        Level = level;
        Health = MaxHealth;

        Moves = new Move[4];
        int moveIndex = 0;
        foreach (var move in skeleton.learnableMoves)
        {
            if (move.level <= level)
            {
                Moves[moveIndex] = new Move(move.skeleton);
                moveIndex = (moveIndex + 1) % 4;
            }
        }
    }

    private int GetEffectiveStat(int baseStat, bool isHealth = false)
    {
        return Mathf.FloorToInt(baseStat * Level / 100f) + (isHealth ? 10 : 5);
    }

    public int MaxHealth { get { return GetEffectiveStat(Skeleton.hpStat, true); } }
    public int Attack { get { return GetEffectiveStat(Skeleton.atkStat); } }
    public int Defense { get { return GetEffectiveStat(Skeleton.defStat); } }
    public int SpAttack { get { return GetEffectiveStat(Skeleton.spAtkStat); } }
    public int SpDefense { get { return GetEffectiveStat(Skeleton.spDefStat); } }
    public int Speed { get { return GetEffectiveStat(Skeleton.spdStat); } }  
}
