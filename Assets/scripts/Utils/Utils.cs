using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

    /// <summary>
    /// Performs a roll with a certain chance (from 0 to 100) of succeeding.
    /// </summary>
    public static bool Chance(int successChance)
    {
        if (successChance <= 0) return false;
        else if (successChance >= 100) return true;
        else return RandomFloat() <= successChance / 100f;
    }

    /// <summary>
    /// Performs a roll with a certain chance (from 0 to 100) of succeeding.
    /// </summary>
    public static bool Chance(float successChance)
    {
        if (successChance <= 0) return false;
        else if (successChance >= 100) return true;
        else return RandomFloat() <= successChance / 100f;
    }

    /// <summary>
    /// Random element from a list.
    /// </summary>
    public static T RandomElement<T>(T[] list)
    {
        return list[RandomInt(0, list.Length - 1)];
    }

    /// <summary>
    /// Random element from a list.
    /// </summary>
    public static T RandomElement<T>(List<T> list)
    {
        return list[RandomInt(0, list.Count - 1)];
    }

    /// <summary>
    /// Random non-null element from a list. This method will not terminate if the list only contains null values.
    /// </summary>
    public static T RandomNonNullElement<T>(T[] list)
    {
        int choice;
        do { choice = RandomInt(0, list.Length - 1); }
        while (list[choice] == null);
        return list[choice];
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
    /// Makes an image invisible.
    /// </summary>
    public static void MakeInvisible(Image image)
    {
        image.color = new Color(image.color.r, image.color.g, image.color.b, 0);
    }

    /// <summary>
    /// Makes a sprite invisible.
    /// </summary>
    public static void MakeInvisible(SpriteRenderer sprite)
    {
        sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 0);
    }

    /// <summary>
    /// Makes an image visible.
    /// </summary>
    public static void MakeVisible(Image image)
    {
        image.color = new Color(image.color.r, image.color.g, image.color.b, 1);
    }

    /// <summary>
    /// Fades a sprite in (true) or out (false).
    /// </summary>
    private static IEnumerator Fade(SpriteRenderer sprite, int frames, bool fadeIn)
    {
        for (float i = 0; i <= frames; i++)
        {
            sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, fadeIn ? (i / frames) : ((frames - i) / frames));
            yield return null;
        }
    }

    /// <summary>
    /// Fades a sprite in.
    /// </summary>
    public static IEnumerator FadeIn(SpriteRenderer sprite, int frames)
    {
        yield return Fade(sprite, frames, true);
    }

    /// <summary>
    /// Fades a sprite out.
    /// </summary>
    public static IEnumerator FadeOut(SpriteRenderer sprite, int frames)
    {
        yield return Fade(sprite, frames, false);
    }

    /// <summary>
    /// Creates a Pokemon given a species name.
    /// </summary>
    public static Pokemon CreatePokemon(string speciesName, int level)
    {
        var skeleton = Resources.Load<PokemonBase>($"Pokemon/{speciesName}");
        return new Pokemon(skeleton, level);
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
        if (move.Accuracy == 0f) return true;

        var _stage = Limit(-6, user.AccuracyStage - target.EvasionStage, 6);
        return move.Accuracy / 100f * GetEffectiveAccuracy(_stage) >= RandomFloat();
    }

    /// <summary>
    /// A flag holding the result of the last IsCrit call.
    /// </summary>
    public static bool LastMoveWasCrit = false;

    /// <summary>
    /// Check if Pokemon rolled a crit.
    /// </summary>
    public static bool IsCrit(int stage)
    {
        if (stage <= 0) LastMoveWasCrit = RandomFloat() <= 1f / 24f;
        else if (stage == 1) LastMoveWasCrit = RandomFloat() <= 1f / 8f;
        else if (stage == 2) LastMoveWasCrit = RandomFloat() <= 1f / 2f;
        else LastMoveWasCrit = true;

        return LastMoveWasCrit;
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
    public static int CalcDamage(Move move, Pokemon user, Pokemon target, IBattle battle, int targetCount)
    {
        var crit = IsCrit(user.CritStage);
        var attack = move.Category == MoveCategory.Physical
            ? GetEffectiveStat(user.Attack, crit ? Math.Max(0, user.AttackStage) : user.AttackStage)
            : GetEffectiveStat(user.SpAttack, crit ? Math.Max(0, user.SpAttackStage) : user.SpAttackStage);
        var defense = move.Category == MoveCategory.Physical
            ? GetEffectiveStat(user.Defense, crit ? Math.Max(0, user.DefenseStage) : user.DefenseStage)
            : GetEffectiveStat(user.SpDefense, crit ? Math.Max(0, user.SpDefenseStage) : user.SpDefenseStage);
        var targets = (move.Targeting == Targeting.Adjacent && targetCount > 1) ? 0.75f : 1f;
        var weather = WeatherAffinity(move, battle.Logic.Weather);
        var rng = RandomFloat(0.85f, 1f);
        var stab = (move.Type == user.PrimaryType || move.Type == user.SecondaryType) ? 1.5f : 1f;
        var affinity = Types.Affinity(move, target);
        var burn = (move.Category == MoveCategory.Physical && user.Status == Status.Burned) ? 0.5f : 1f;

        var total = (((((2f * user.Level / 5f) + 2f) * move.Power * attack / defense) / 50f) + 2f) *
            targets * weather * (crit ? 1.5f : 1f) * rng * stab * affinity * burn;

        // don't overkill
        return Math.Min(Mathf.FloorToInt(total), target.Health);
    }

    /// <summary>
    /// Calculates the total experience required to reach a certain level.
    /// </summary>
    public static int GetExpFromLevel(int level, ExpGroup expGroup)
    {
        if (level == 1) return 0;

        switch (expGroup)
        {
            case ExpGroup.Erratic:
                if (level <= 50) return Mathf.FloorToInt(level * level * level * (100 - level) * 0.02f);
                else if (level <= 68) return Mathf.FloorToInt(level * level * level * (150 - level) * 0.01f);
                else if (level <= 98) return Mathf.FloorToInt(level * level * level * (1911 - 10 * level) * 0.333f * 0.002f);
                else return Mathf.FloorToInt(level * level * level * (160 - level) * 0.01f);
            case ExpGroup.Fast:
                return Mathf.FloorToInt(4 * level * level * level * 0.2f);
            case ExpGroup.MediumFast:
                return level * level * level;
            case ExpGroup.MediumSlow:
                return Mathf.FloorToInt(1.2f * level * level * level - 15 * level * level + 100 * level - 140);
            case ExpGroup.Slow:
                return Mathf.FloorToInt(5 * level * level * level * 0.25f);
            case ExpGroup.Fluctuating:
                if (level <= 15) return Mathf.FloorToInt(level * level * level * ((level + 1) * 0.333f + 24) * 0.02f);
                else if (level <= 36) return Mathf.FloorToInt(level * level * level * (level + 14) * 0.02f);
                else return Mathf.FloorToInt(level * level * level * (level * 0.5f + 32) * 0.02f);
            default:
                return -1;
        }
    }

    /// <summary>
    /// Calculates the level a Pokemon will have given a certain amount of total xp.
    /// </summary>
    public static int GetLevelFromExp(int exp, ExpGroup expGroup)
    {
        int level = 1;
        int lastCalculatedExp = 0;
        while (lastCalculatedExp < exp)
            lastCalculatedExp = GetExpFromLevel(++level, expGroup);
        return Math.Min(100, lastCalculatedExp > exp ? level - 1 : level);
    }

    /// <summary>
    /// Calculates the exp reward for killing a Pokemon.
    /// </summary>
    public static int GetExpForKill(Pokemon candidate, Pokemon fainted, int candidates, bool isTrainerBattle)
    {
        var trainer = isTrainerBattle ? 1.5f : 1f;
        var baseExp = fainted.Skeleton.expStat;
        return Mathf.FloorToInt((trainer * baseExp * fainted.Level * Mathf.Pow(2 * fainted.Level + 10f, 2.5f)) /
            (5f * candidates * Mathf.Pow(fainted.Level + candidate.Level + 10, 2.5f)) + 1);
    }

    /// <summary>
    /// Calculates the chance (0-100) of passing a catch check for a Pokemon (4 checks are required).
    /// </summary>
    public static float GetCatchChance(Pokemon target, float ballMultiplier)
    {
        float statusBonus;

        switch (target.Status)
        {
            case Status.Sleeping:
            case Status.Frozen:
                statusBonus = 2f;
                break;
            case Status.Paralyzed:
            case Status.Poisoned:
            case Status.Toxic:
            case Status.Burned:
                statusBonus = 1.5f;
                break;
            default:
                statusBonus = 1f;
                break;
        }

        var modifiedCatchRate = ((3f * target.MaxHealth - 2f * target.Health) * target.Skeleton.catchRate * ballMultiplier) /
            (3f * target.MaxHealth) * statusBonus;

        return 100f * 65536f / (Mathf.Pow(255f / Limit(1f, modifiedCatchRate, 255f), 0.1875f) * 65536f);
    }
}
