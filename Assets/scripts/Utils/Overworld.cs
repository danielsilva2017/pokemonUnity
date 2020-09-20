using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

[System.Serializable]
public class WildPokemon
{
    public PokemonBase species;
    public int minLevel;
    public int maxLevel;
    public int weight;
}

/// <summary>
/// Stores and loads the overworld state.
/// </summary>
public class Overworld : MonoBehaviour
{
    public string locationName;
    public AudioSource locationMusic;
    public int wildPokemonChance; // 0-100 chance to roll a wild encounter per step on grass
    public Weather weather;
    public PlayerLogic player;
    public List<NPC> characters;
    public List<OverworldItem> items;
    public WildPokemon[] grassWildPokemon;
    public WildPokemon[] surfWildPokemon;
    public WildPokemon[] fishingWildPokemon;

    private WildEncounterGenerator grassEncounter;
    private WildEncounterGenerator surfEncounter;
    private WildEncounterGenerator fishingEncounter;
    private Vector2[] npcWalkingPositions; // to prevent multiple npcs from walking onto the same tile

    // Start is called before the first frame update
    void Start()
    {
        var overworldInfo = SceneInfo.GetOverworldInfo(locationName);
        npcWalkingPositions = new Vector2[characters.Count];

        // load a saved state if any exists
        if (overworldInfo != null)
            LoadState(overworldInfo);

        for (var i = 0; i < characters.Count; i++)
        {
            characters[i].OverworldNpcID = i;
            characters[i].Overworld = this;
        }

        // set up wild pokemon chances
        if (grassWildPokemon != null) grassEncounter = new WildEncounterGenerator(grassWildPokemon);
        if (surfWildPokemon != null) surfEncounter = new WildEncounterGenerator(surfWildPokemon);
        if (fishingWildPokemon != null) fishingEncounter = new WildEncounterGenerator(fishingWildPokemon);
    }

    private void LoadState(OverworldInfo overworldInfo)
    {
        // set all defeated trainers
        for (var i = 0; i < overworldInfo.Characters.Count; i++)
        {
            var npc = overworldInfo.Characters[i];
            if (npc.IsDefeated)
                characters[i].IsDefeated = true;
        }

        // destroy all collected items
        for (var i = 0; i < overworldInfo.Items.Count; i++)
        {
            var item = overworldInfo.Items[i];
            if (item.IsCollected)
                Destroy(items[i].gameObject);
        }

        SceneInfo.DeleteBattleInfo();
        SceneInfo.DeleteOverworldInfo(locationName);
    }

    public void ClaimTile(NPC npc, Vector2 position)
    {
        npcWalkingPositions[npc.OverworldNpcID] = position;
    }

    public bool IsTileClaimed(Vector2 position)
    {
        foreach (var tile in npcWalkingPositions)
        {
            if (CompareFloats(tile.x, position.x, 0.02f) && CompareFloats(tile.y, position.y, 0.02f))
                return true;
        }

        return false;
    }

    public Pokemon GenerateGrassEncounter() { return grassEncounter.Generate(); }
    public Pokemon GenerateSurfEncounter() { return surfEncounter.Generate(); }
    public Pokemon GenerateFishingEncounter() { return fishingEncounter.Generate(); }

    public class WildEncounterGenerator
    {
        private readonly WildPokemon[] entries;
        private readonly int[] thresholds;
        private readonly int total;

        public WildEncounterGenerator(WildPokemon[] wildPokemons)
        {
            entries = wildPokemons;
            thresholds = new int[wildPokemons.Length];
            var threshold = 0;

            for (var i = 0; i < wildPokemons.Length; i++)
            {
                thresholds[i] = threshold;
                threshold += wildPokemons[i].weight;
            }

            total = threshold;
        }

        public Pokemon Generate()
        {
            var roll = RandomInt(1, total);
            var index = 0;
            while (index + 1 < thresholds.Length && roll > thresholds[index + 1])
                index++;
            return new Pokemon(
                entries[index].species,
                RandomInt(entries[index].minLevel, entries[index].maxLevel)
            );
        }
    }
}
