using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Class used to pass information between scenes.
/// </summary>
public static class SceneInfo
{
    private static BattleInfo battleInfo;
    private static OverworldInfo overworldInfo;

    public static void SetTrainerBattleInfo(PlayerLogic player, Trainer trainer, List<Pokemon> enemies, int battleSize = 1, Weather weather = Weather.None)
    {
        battleInfo = new BattleInfo
        {
            Trainer = trainer,
            Allies = player.Player.Pokemons,
            Enemies = enemies,
            BattleSize = battleSize,
            Weather = weather,
            IsTrainerBattle = true
        };

        player.overworldManager.CreateSnapshot();
    }

    public static void SetWildBattleInfo(PlayerLogic player, Pokemon enemy, Weather weather = Weather.None)
    {
        battleInfo = new BattleInfo
        {
            Allies = player.Player.Pokemons,
            Enemies = new List<Pokemon>() { enemy },
            BattleSize = 1,
            Weather = weather,
            IsTrainerBattle = false
        };
        player.overworldManager.CreateSnapshot();
    }

    public static void SetOverworldInfo(PlayerLogic player, List<NPC> characters, List<Item> items, int scene)
    {
        overworldInfo = new OverworldInfo
        {
            PlayerPosition = player.transform.position,
            PlayerDirection = player.Direction,
            Characters = characters,
            Items = items,
            Scene = scene
        };
    }

    public static BattleInfo GetBattleInfo()
    {
        return battleInfo;
    }

    public static OverworldInfo GetOverworldInfo()
    {
        return overworldInfo;
    }

    public static void DeleteBattleInfo()
    {
        battleInfo = null;
    }

    public static void DeleteOverworldInfo()
    {
        overworldInfo = null;
    }
}

public class BattleInfo
{
    public Trainer Trainer { get; set; }
    public List<Pokemon> Allies { get; set; }
    public List<Pokemon> Enemies { get; set; }
    public int BattleSize { get; set; }
    public Weather Weather { get; set; }
    public bool IsTrainerBattle { get; set; }
    public int Difficulty { get; set; } //unused for now
    public int Money { get; set; } //unused for now
    public string TrainerClass { get; set; } //unused for now
    public string TrainerName { get; set; } //unused for now
    public AudioSource Music { get; set; } //unused for now
    public Image Background { get; set; } //unused for now
}

public class OverworldInfo
{
    public Vector3 PlayerPosition { get; set; }
    public Direction PlayerDirection { get; set; }
    public List<NPC> Characters { get; set; }
    public List<Item> Items { get; set; }
    public int Scene { get; set; }
}