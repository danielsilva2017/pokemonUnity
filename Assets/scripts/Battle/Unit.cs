using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Unit : MonoBehaviour
{
    public GameObject animatable;

    private Animator animator;

    public Pokemon Pokemon { get; set; }

    public void Setup(Pokemon pokemon)
    {
        Pokemon = pokemon;
        animator = animatable.GetComponent<Animator>();

        animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(GetAnimationPath());
    }

    private string GetAnimationPath()
    {
        return string.Format("Images/{0}{1}/ctrl", Pokemon.Skeleton.dexNumber, Pokemon.IsAlly ? "b" : "");
    }
    
    public string Name { get { return Pokemon.Skeleton.pokemonName; } }
    public Move[] Moves { get { return Pokemon.Moves; } }
}
