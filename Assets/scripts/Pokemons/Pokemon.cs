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
    public Gender Gender { get; set; }
    public Status Status { get; set; }
    public ItemLogic Pokeball { get; set; } // type of pokeball caught with
    public bool CanAttack { get; set; }
    public bool WasForcedSwitch { get; set; } // whether it joined the fight as the result of a forced switch
    public List<Pokemon> ExpCandidates { get; set; } // those who will get xp if this pokemon dies
    public Move LastHitByMove { get; set; } // last move to successfully target this pokemon
    public Pokemon LastHitByUser { get; set; } // last pokemon to successfully target this pokemon
    public bool IsAlly { get; set; }
    public int Health {
        get { return health; }
        set { health = Limit(0, value, MaxHealth); } // enforce health limits
    }
    public int Level {
        get { return level; }
        set { level = Limit(1, value, 100); } // enforce level limits
    }
    public int Experience
    {
        get { return exp; }
        set { exp = Math.Min(value, MaxExperience); } // enforce xp limits
    }
    public int NextLevelExp { get; set; } // minimum total xp to reach next level
    public int CurLevelExp { get; set; } // minimum total xp to be at current level

    private readonly int MaxExperience; // xp cap
    private int health;
    private int level;
    private int exp;

    public Pokemon(PokemonBase skeleton, int level, Gender? gender = null, Status status = Status.None, bool canAttack = true)
    {
        Skeleton = skeleton;
        Ability = new Ability(RandomElement(skeleton.learnableAbilities));
        Level = level;
        MaxExperience = GetExpFromLevel(100, ExpGroup);
        Experience = GetExpFromLevel(level, ExpGroup);
        CurLevelExp = Experience;
        NextLevelExp = level + 1 >= 100 ? MaxExperience : GetExpFromLevel(level + 1, ExpGroup);
        Gender = gender ?? RandomGender();
        Health = MaxHealth;
        Status = status;
        CanAttack = canAttack;
        Pokeball = ItemLogic.PokeBall;

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

    public bool IsType(Type type)
    {
        return PrimaryType == type || SecondaryType == type;
    }

    public List<MoveBase> NewMovesFromLevelUp()
    {
        var moves = new List<MoveBase>();

        foreach (var learnableMove in Skeleton.learnableMoves)
        {
            if (learnableMove.level == Level)
                moves.Add(learnableMove.skeleton);
            else if (learnableMove.level > Level)
                break;
        }

        return moves;
    }

    public void LevelUp()
    {
        var oldMaxHp = MaxHealth;
        Level++;
        CurLevelExp = NextLevelExp;
        NextLevelExp = Level + 1 >= 100 ? MaxExperience : GetExpFromLevel(Level + 1, ExpGroup);
        Health += MaxHealth - oldMaxHp;
    }

    public int GetFilledMoveSlots()
    {
        if (Moves[1] == null) return 1;
        else if (Moves[2] == null) return 2;
        else if (Moves[3] == null) return 3;
        return 4;
    }

    public bool NoMoves()
    {
        return Moves.All(move => move.Points <= 0);
    }

    public int CompareTo(Pokemon other)
    {
        var selfSpeed = GetEffectiveStat(Speed, SpeedStage) * (Status == Status.Paralyzed ? 0.5f : 1f);
        var otherSpeed = GetEffectiveStat(other.Speed, other.SpeedStage) * (other.Status == Status.Paralyzed ? 0.5f : 1f);

        if (selfSpeed == otherSpeed) return Chance(50) ? -1 : 1; // tie
        return Mathf.FloorToInt(otherSpeed - selfSpeed);
    }

    /// <summary>
    /// Rolls a random gender for a Pokemon.
    /// </summary>
    private Gender RandomGender()
    {
        if (Skeleton.maleChance < 0) return Gender.None;
        else return Chance(Skeleton.maleChance) ? Gender.Male : Gender.Female;
    }

    // converts base stat to an actual stat
    private int GetStat(int baseStat, bool isHealth = false)
    {
        return Mathf.FloorToInt(baseStat * 2f * Level * 0.01f) + (isHealth ? Level + 10 : 5);
    }

    // some getters for convenience
    public string Name { get { return Skeleton.pokemonName; } }
    public Type PrimaryType { get { return Skeleton.primaryType; } }
    public Type SecondaryType { get { return Skeleton.secondaryType; } }
    public ExpGroup ExpGroup { get { return Skeleton.expGroup; } }

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

// the spritesheet uses the order: // psn, bpsn, slp, par, frz, brn, fnt
public enum Status
{
    Poisoned, Toxic, Sleeping, Paralyzed, Frozen, Burned, Fainted, None
}