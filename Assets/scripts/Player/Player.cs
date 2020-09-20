using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class Player
{
    public string Name { get; set; }
    public List<Pokemon> Pokemons { get; set; }
    public Bag Bag { get; set; }
    public int Money { get; set; }

    public Player()
    {
        Name = "Player";
        Pokemons = new List<Pokemon>()
        {
            CreatePokemon("Bulbasaur", 5),
            CreatePokemon("Snorlax", 12)
        };
        Pokemons[0].Experience = Pokemons[0].NextLevelExp - 1;
        Pokemons[1].Health = 44;
        Bag = new Bag();
    }

    public Player(Player player)
    {
        Name = player.Name;
        Pokemons = player.Pokemons;
        Money = player.Money;
        Bag = player.Bag;
    }
}
