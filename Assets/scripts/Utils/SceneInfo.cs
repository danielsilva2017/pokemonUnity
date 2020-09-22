using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using static Utils;

public enum SceneID
{
    Title, Initial, StarterSelection, SingleBattle, PokeCenter, Forest1, Evolution
}

/// <summary>
/// Class used to pass information between scenes.
/// </summary>
public static class SceneInfo
{
    private static BattleInfo battleInfo;
    private static Dictionary<string, OverworldInfo> overworldInfo;
    private static PlayerInfo playerInfo;
    private static Vector2? targetCoordinates;
    private static IAreaBorder animatedAreaBorder;
    private static AudioSource battleMusic;
    private static Outcome? forcedOutcome;
    private static List<PendingEvolution> pendingEvolutions;

    public static bool DisplayAreaHeaderOnSpawn { get; set; }

    static SceneInfo()
    {
        overworldInfo = new Dictionary<string, OverworldInfo>();
        pendingEvolutions = new List<PendingEvolution>();
        DisplayAreaHeaderOnSpawn = true;
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
        DisplayAreaHeaderOnSpawn = false;
        SceneManager.LoadScene((int) SceneID.SingleBattle);
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
        DisplayAreaHeaderOnSpawn = false;
        SceneManager.LoadScene((int) SceneID.SingleBattle);
    }

    /// <summary>
    /// Returns to the overworld after a battle or handles pending evolutions if there are any.
    /// </summary>
    public static void ReturnToOverworldFromBattle()
    {
        StopBattleMusic();
        SceneManager.LoadScene(pendingEvolutions.Count > 0 ? (int) SceneID.Evolution : playerInfo.Scene);
    }

    /// <summary>
    /// Returns to the overworld after performing evolutions.
    /// </summary>
    public static void ReturnToOverworldFromEvolutions()
    {
        SceneManager.LoadScene(playerInfo.Scene);
    }

    /// <summary>
    /// Transitions to an overworld in a different scene.
    /// </summary>
    public static void FollowAreaExit(AreaExit exit, PlayerLogic playerLogic)
    {
        SetOverworldInfo(playerLogic);
        SetPlayerInfo(playerLogic);
        SetTargetCoordinates(exit);
        playerInfo.OverworldKey = exit.targetOverworldName;
        DisplayAreaHeaderOnSpawn = true;
        SceneManager.LoadScene((int) exit.scene);
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

    public static void SetAnimatedAreaBorder(IAreaBorder border)
    {
        animatedAreaBorder = border;
    }

    private static void SetOverworldInfo(PlayerLogic playerLogic)
    {
        overworldInfo[playerLogic.overworld.locationName] = new OverworldInfo
        {
            Characters = playerLogic.overworld.characters,
            Items = playerLogic.overworld.items
        };
    }

    private static void SetTargetCoordinates(AreaExit exit)
    {
        targetCoordinates = exit.targetCoordinates;
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

    public static void AddPendingEvolution(Pokemon subject, PokemonBase targetSkeleton)
    {
        pendingEvolutions.Add(new PendingEvolution
        {
            Subject = subject,
            TargetSkeleton = targetSkeleton
        });
    }

    public static void SetForcedOutcome(Outcome outcome)
    {
        forcedOutcome = outcome;
    }

    public static Outcome ConsumeForcedOutcome()
    {
        var o = forcedOutcome;
        forcedOutcome = null;
        return o ?? Outcome.Undecided;
    }

    public static void PlayMusicImmediate(AudioSource music)
    {
        battleMusic = music;
        battleMusic.volume = 0.6f;
        battleMusic.Play();
    }

    public static void PlayBattleMusic(AudioSource music)
    {
        battleMusic = music;
        battleMusic.volume = 0.4f;
        battleMusic.Play();
        Object.DontDestroyOnLoad(battleMusic.gameObject);
    }

    public static void StopBattleMusic()
    {
        if (battleMusic == null) return;

        battleMusic.Stop();
        Object.Destroy(battleMusic.gameObject);
        battleMusic = null;
    }

    public static BattleInfo GetBattleInfo()
    {
        return battleInfo;
    }

    public static OverworldInfo GetOverworldInfo(string name)
    {
        return GetValue(overworldInfo, name);
    }

    public static List<PendingEvolution> GetPendingEvolutions()
    {
        return pendingEvolutions;
    }

    public static Vector2? GetTargetCoordinates()
    {
        return targetCoordinates;
    }

    public static IAreaBorder GetAnimatedAreaBorder()
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

    public static void DeleteTargetCoordinates()
    {
        targetCoordinates = null;
    }

    public static void DeletePlayerInfo()
    {
        playerInfo = null;
    }

    public static void ClearPendingEvolutions()
    {
        pendingEvolutions = new List<PendingEvolution>();
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
    public List<OverworldItem> Items { get; set; }
}

public class PlayerInfo
{
    public Player Player { get; set; }
    public Vector3 Position { get; set; }
    public Direction Direction { get; set; }
    public string OverworldKey { get; set; }
    public int Scene { get; set; }
}

public class OverworldSceneInfo
{
    public int EntryID { get; set; }
}

public class PendingEvolution
{
    public Pokemon Subject { get; set; }
    public PokemonBase TargetSkeleton { get; set; }
}