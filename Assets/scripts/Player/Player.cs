using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class Player
{
    public string Name { get; set; }
    public List<Pokemon> Pokemons { get; set; }
    public int Money { get; set; }

    public Player()
    {
        Name = "the boss man";
        Pokemons = new List<Pokemon>()
        {
            CreatePokemon("Bulbasaur", 9, Gender.Male),
            CreatePokemon("Snorlax", 12, Gender.Male)
        };
        Pokemons[0].Health -= 6;
    }

    public Player(Player playerInfo)
    {
        Name = playerInfo.Name;
        Pokemons = playerInfo.Pokemons;
        Money = playerInfo.Money;
    }
}
