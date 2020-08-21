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
    private static Dictionary<string, OverworldInfo> overworldInfo;
    private static PlayerInfo playerInfo;
    private static AreaBorder animatedAreaBorder;

    static SceneInfo()
    {
        overworldInfo = new Dictionary<string, OverworldInfo>();
    }

    /// <summary>
    /// Begins a trainer battle, switching to the appropriate scene and storing information to later return to the overworld.
    /// </summary>
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

    /// <summary>
    /// Begins a wild battle, switching to the appropriate scene and storing information to later return to the overworld.
    /// </summary>
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

    /// <summary>
    /// Returns to the overworld after a battle.
    /// </summary>
    public static void ReturnToOverworldFromBattle()
    {
        SceneManager.LoadScene(playerInfo.Scene);
    }

    /// <summary>
    /// Stores information about an overworld. Automatically done when starting a battle.
    /// </summary>
    public static void SetOverworldInfo(Overworld overworld)
    {
        overworldInfo[overworld.locationName] = new OverworldInfo
        {
            Characters = overworld.characters,
            Items = overworld.items
        };
    }

    public static void SetAnimatedAreaBorder(AreaBorder ab)
    {
        animatedAreaBorder = ab;
    }

    private static void SetOverworldInfo(PlayerLogic playerLogic)
    {
        overworldInfo[playerLogic.overworld.locationName] = new OverworldInfo
        {
            Characters = playerLogic.overworld.characters,
            Items = playerLogic.overworld.items
        };
    }

    private static void SetPlayerInfo(PlayerLogic playerLogic)
    {
        playerInfo = new PlayerInfo
        {
            Player = playerLogic.Player,
            Position = playerLogic.transform.position,
            Direction = playerLogic.Direction,
            OverworldKey = playerLogic.overworld.locationName,
            Scene = playerLogic.gameObject.scene.buildIndex
        };
    }

    public static BattleInfo GetBattleInfo()
    {
        return battleInfo;
    }

    public static OverworldInfo GetOverworldInfo(string name)
    {
        if (!overworldInfo.TryGetValue(name, out OverworldInfo value))
            return null;
        else
            return value;
    }

    public static AreaBorder GetAnimatedAreaBorder()
    {
        return animatedAreaBorder;
    }

    public static PlayerInfo GetPlayerInfo()
    {
        return playerInfo;
    }

    public static void DeleteAnimatedAreaBorder()
    {
        animatedAreaBorder = null;
    }

    public static void DeleteBattleInfo()
    {
        battleInfo = null;
    }

    public static void DeleteOverworldInfo(string name)
    {
        overworldInfo[name] = null;
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
    public AudioSource Music { get; set; } //unused for now
    public Image Background { get; set; } //unused for now
}

public class OverworldInfo
{
    public List<NPC> Characters { get; set; }
    public List<Item> Items { get; set; }
}

public class PlayerInfo
{
    public Player Player { get; set; }
    public Vector3 Position { get; set; }
    public Direction Direction { get; set; }
    public string OverworldKey { get; set; }
    public int Scene { get; set; }
}