using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public GameObject animatable;
    public PokemonBase skeleton;
    public int level;
    public bool isAlly;

    private Animator animator;

    public Pokemon Pokemon { get; set; }

    public void Setup()
    {
        Pokemon = new Pokemon(skeleton, level);
        animator = animatable.GetComponent<Animator>();

        animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(GetAnimationPath());
    }

    private string GetAnimationPath()
    {
        return string.Format("Images/{0}{1}/ctrl", skeleton.dexNumber, isAlly ? "b" : "");
    }
    
    public string Name { get { return skeleton.pokemonName; } }
    public Move[] Moves { get { return Pokemon.Moves; } }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
