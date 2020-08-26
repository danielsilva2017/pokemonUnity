using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Utils;

[System.Serializable]
public class TrainerPokemonInit
{
    public string speciesName;
    public int level;
}

public class Trainer : NPC
{
    public string trainerName;
    public string trainerClass;
    public AudioSource music;
    [TextArea] public string[] dialogue;
    [TextArea] public string[] defeatDialogue;
    [TextArea] public string[] postDialogue;
    public Weather weather;
    public int difficulty;
    public int money;
    public TrainerPokemonInit[] pokemons;

    private List<Pokemon> party;

    void Start()
    {
        Name = trainerName;
        Class = trainerClass;
        Dialogue = dialogue;
        PostDialogue = postDialogue;
        party = new List<Pokemon>(pokemons.Length);

        foreach (var init in pokemons)
            party.Add(CreatePokemon(init.speciesName, init.level));
    }

    protected override void DoAction()
    {
        SceneInfo.BeginTrainerBattle(PlayerLogic, this, party, 1, weather);
    }
}
