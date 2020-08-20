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
    private static PlayerInfo playerInfo;

    public static void BeginTrainerBattle(PlayerLogic playerLogic, Trainer trainer, List<Pokemon> enemies, int battleSize = 1, Weather weather = Weather.None)
    {
        battleInfo = new BattleInfo
        {
            Trainer = trainer,
            Allies = playerLogic.Player.Pokemons,
            Enemies = enemies,
            BattleSize = battleSize,
            Weather = weather,
            IsTrainerBattle = true
        };

        SetOverworldInfo(playerLogic);
        SetPlayerInfo(playerLogic);
        SceneManager.LoadScene(3);
    }

    public static void BeginWildBattle(PlayerLogic playerLogic, Pokemon enemy, Weather weather = Weather.None)
    {
        battleInfo = new BattleInfo
        {
            Allies = playerLogic.Player.Pokemons,
            Enemies = new List<Pokemon>() { enemy },
            BattleSize = 1,
            Weather = weather,
            IsTrainerBattle = false
        };

        SetOverworldInfo(playerLogic);
        SetPlayerInfo(playerLogic);
        SceneManager.LoadScene(3);
    }

    private static void SetOverworldInfo(PlayerLogic playerLogic)
    {
        overworldInfo = new OverworldInfo
        {
            Characters = playerLogic.overworld.characters,
            Items = playerLogic.overworld.items,
            Scene = playerLogic.gameObject.scene.buildIndex
        };
    }

    private static void SetPlayerInfo(PlayerLogic playerLogic)
    {
        playerInfo = new PlayerInfo
        {
            Player = playerLogic.Player,
            Position = playerLogic.transform.position,
            Direction = playerLogic.Direction
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

    public static PlayerInfo GetPlayerInfo()
    {
        return playerInfo;
    }

    public static void DeleteBattleInfo()
    {
        battleInfo = null;
    }

    public static void DeleteOverworldInfo()
    {
        overworldInfo = null;
    }

    public static void DeletePlayerInfo()
    {
        playerInfo = null;
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
    public List<NPC> Characters { get; set; }
    public List<Item> Items { get; set; }
    public int Scene { get; set; }
}

public class PlayerInfo
{
    public Player Player { get; set; }
    public Vector3 Position { get; set; }
    public Direction Direction { get; set; }
}