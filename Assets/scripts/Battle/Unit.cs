using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Unit : MonoBehaviour
{
    public GameObject animatable;

    public Animator Animator { get; set; }
    public SpriteRenderer Renderer { get; set; }
    public RectTransform RectTransform { get; set; }
    public Pokemon Pokemon { get; set; }

    public void Setup(Pokemon pokemon)
    {
        Pokemon = pokemon;
        Animator = animatable.GetComponent<Animator>();
        Renderer = animatable.GetComponent<SpriteRenderer>();
        RectTransform = animatable.GetComponent<RectTransform>();

        Animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(GetAnimationPath());
        Renderer.color = new Color(Renderer.color.r, Renderer.color.g, Renderer.color.b, 1f);
    }

    private string GetAnimationPath()
    {
        return string.Format("Images/{0}{1}/ctrl", Pokemon.Skeleton.dexNumber, Pokemon.IsAlly ? "b" : "");
    }
    
    public string Name { get { return Pokemon.Skeleton.pokemonName; } }
    public Move[] Moves { get { return Pokemon.Moves; } }
}
