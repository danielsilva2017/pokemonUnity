using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static Utils;

/// <summary>
/// An instance of a Pokemon, based on a skeleton.
/// </summary>
public class Pokemon : IComparable<Pokemon>
{
    public PokemonBase Skeleton { get; set; }
    public Ability Ability { get; set; }
    public Move[] Moves { get; set; }
    public int Level { get; set; }
    public Gender Gender { get; set; }
    public Status Status { get; set; }
    public bool CanAttack { get; set; }
    public Pokemon LastHitBy { get; set; }
    public bool IsAlly { get; set; }
    public int Health {
        get { return health; }
        set { health = Limit(0, value, MaxHealth); }
    }

    private int health;

    public Pokemon(PokemonBase skeleton, int level, Gender gender, Status status = Status.None, bool canAttack = true)
    {
        Skeleton = skeleton;
        Ability = new Ability(RandomElement(skeleton.learnableAbilities));
        Level = level;
        Gender = gender;
        Health = MaxHealth;
        Status = status;
        CanAttack = canAttack;

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

    public bool NoMoves()
    {
        return Moves.All(move => move.Points <= 0);
    }

    public int CompareTo(Pokemon other)
    {
        var selfSpeed = GetEffectiveStat(Speed, SpeedStage) * (Status == Status.Paralyzed ? 0.5f : 1f);
        var otherSpeed = GetEffectiveStat(other.Speed, other.SpeedStage) * (other.Status == Status.Paralyzed ? 0.5f : 1f);

        if (selfSpeed == otherSpeed) return RandomFloat() >= 0.5f ? -1 : 1; // tie
        return Mathf.FloorToInt(otherSpeed - selfSpeed);
    }

    // converts base stat to an actual stat
    private int GetStat(int baseStat, bool isHealth = false)
    {
        return Mathf.FloorToInt(baseStat * Level / 100f) + (isHealth ? 10 : 5);
    }

    // some getters for convenience
    public string Name { get { return Skeleton.pokemonName; } }
    public Type PrimaryType { get { return Skeleton.primaryType; } }
    public Type SecondaryType { get { return Skeleton.secondaryType; } }

    // raw stat, calculated from base stats
    public int MaxHealth { get { return GetStat(Skeleton.hpStat, true); } }
    public int Attack { get { return GetStat(Skeleton.atkStat); } }
    public int Defense { get { return GetStat(Skeleton.defStat); } }
    public int SpAttack { get { return GetStat(Skeleton.spAtkStat); } }
    public int SpDefense { get { return GetStat(Skeleton.spDefStat); } }
    public int Speed { get { return GetStat(Skeleton.spdStat); } }

    // stage for each stat
    public int AttackStage { get; set; }
    public int DefenseStage { get; set; }
    public int SpAttackStage { get; set; }
    public int SpDefenseStage { get; set; }
    public int SpeedStage { get; set; }
    public int CritStage { get; set; }
    public int AccuracyStage { get; set; }
    public int EvasionStage { get; set; }
}

public enum Gender
{
    Male, Female, None
}

public enum Status
{
    Burned, Poisoned, Frozen, Paralyzed, Sleeping, Fainted, Toxic, None
}