using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Utils
{
    /// <summary>
    /// Min and max are both inclusive.
    /// </summary>
    public static int RandomInt(int min, int max)
    {
        return UnityEngine.Random.Range(min, max + 1);
    }

    /// <summary>
    /// Min and max are both inclusive. Defaults to a [0,1] range.
    /// </summary>
    public static float RandomFloat(float min = 0, float max = 1)
    {
        return UnityEngine.Random.Range(min, max);
    }

    public static T RandomElement<T>(T[] list)
    {
        return list[RandomInt(0, list.Length - 1)];
    }

    /// <summary>
    /// If value is lower than min or greater than max, force it to become min or max, respectively.
    /// </summary>
    public static int Limit(int min, int value, int max)
    {
        return Math.Max(min, Math.Min(value, max));
    }

    /// <summary>
    /// If value is lower than min or greater than max, force it to become min or max, respectively.
    /// </summary>
    public static float Limit(float min, float value, float max)
    {
        return Math.Max(min, Math.Min(value, max));
    }

    /// <summary>
    /// The current Pokemon stat, but considering stat boosts/nerfs. The stat parameter is NOT the base stat value.
    /// </summary>
    public static int GetEffectiveStat(int currentStatValue, int stage)
    {
        var _stage = Limit(-6, stage, 6);

        float num = 2, den = 2;
        if (_stage < 0) den -= _stage;
        else if (_stage > 0) num += _stage;

        return Limit(1, Mathf.FloorToInt(currentStatValue * num / den), 999);
    }

    /// <summary>
    /// The current Pokemon accuracy, but considering stat boosts/nerfs. Returns a [0,1] value.
    /// </summary>
    public static float GetEffectiveAccuracy(int stage)
    {
        var _stage = Limit(-6, stage, 6);

        float num = 3, den = 3;
        if (_stage < 0) den -= _stage;
        else if (_stage > 0) num += _stage;

        return num / den;
    }

    /// <summary>
    /// Check if move rolled a hit.
    /// </summary>
    public static bool IsHit(Move move, Pokemon user, Pokemon target)
    {
        var _stage = Limit(-6, user.AccuracyStage - target.EvasionStage, 6);
        return move.Accuracy / 100f * GetEffectiveAccuracy(_stage) >= RandomFloat();
    }

    /// <summary>
    /// Check if Pokemon rolled a crit.
    /// </summary>
    public static bool IsCrit(int stage)
    {
        bool crit;
        if (stage <= 0) crit = RandomFloat() <= 1f / 24f;
        else if (stage == 1) crit = RandomFloat() <= 1f / 8f;
        else if (stage == 2) crit = RandomFloat() <= 1f / 2f;
        else crit = true;

        if (crit) Debug.Log("critical hit");
        return crit;
    }

    /// <summary>
    /// Returns a damage multipler based on weather.
    /// </summary>
    public static float WeatherAffinity(Move move, Weather weather)
    {
        if ((move.Type == Type.Fire && weather == Weather.Sunny) || (move.Type == Type.Water && weather == Weather.Rain))
            return 1.5f;
        else if ((move.Type == Type.Fire && weather == Weather.Rain) || (move.Type == Type.Water && weather == Weather.Sunny))
            return 0.5f;
        else return 1f;
    }

    /// <summary>
    /// Calculates the damage a move will deal.
    /// </summary>
    public static int CalcDamage(Move move, Pokemon user, Pokemon target, Battle battle, int targetCount)
    {
        var crit = IsCrit(user.CritStage);
        var attack = move.Category == Category.Physical
            ? GetEffectiveStat(user.Attack, crit ? Math.Max(0, user.AttackStage) : user.AttackStage)
            : GetEffectiveStat(user.SpAttack, crit ? Math.Max(0, user.SpAttackStage) : user.SpAttackStage);
        var defense = move.Category == Category.Physical
            ? GetEffectiveStat(user.Defense, crit ? Math.Max(0, user.DefenseStage) : user.DefenseStage)
            : GetEffectiveStat(user.SpDefense, crit ? Math.Max(0, user.SpDefenseStage) : user.SpDefenseStage);
        var targets = (move.Targeting == Targeting.Adjacent && targetCount > 1) ? 0.75f : 1f;
        var weather = WeatherAffinity(move, battle.Logic.Weather);
        var rng = RandomFloat(0.85f, 1f);
        var stab = (move.Type == user.PrimaryType || move.Type == user.SecondaryType) ? 1.5f : 1f;
        var affinity = Types.Affinity(move, target);
        var burn = (move.Category == Category.Physical && user.Status == Status.Burned) ? 0.5f : 1f;

        var total = (((((2f * user.Level / 5f) + 2f) * move.Power * attack / defense) / 50f) + 2f) *
            targets * weather * (crit ? 1.5f : 1f) * rng * stab * affinity * burn;

        // don't overkill
        return Math.Min(Mathf.FloorToInt(total), target.Health);
    }
}
