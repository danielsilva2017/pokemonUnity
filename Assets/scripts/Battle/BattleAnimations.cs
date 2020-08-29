using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static Utils;

public class BattleAnimations : MonoBehaviour
{
    public List<Unit> pokemonUnits;
    public SpriteRenderer thrownPokeball;

    private Sprite[] pokeballs;
    private Vector3 thrownStartingPos;
    private Vector3 originalUnitScale;

    private readonly int faintDropUnits = 80;
    private readonly float faintAnimationSpeed = 5f;
    private readonly int pokeballDropUnits = 110;
    private readonly float pokeballAnimationSpeed = 3f;
    private readonly int pokeballRotation = 25;
    private readonly float pokeballRotationSpeed = 2f;
    private readonly float failSpeed = 6f;

    // Start is called before the first frame update
    void Start()
    {
        thrownStartingPos = thrownPokeball.transform.localPosition;
        thrownPokeball.enabled = false;
        pokeballs = Resources.LoadAll<Sprite>("Images/pokeballs");
    }

    // Update is called once per frame
    void Update()
    {

    }

    public IEnumerator Faint(Pokemon fainted)
    {
        var unit = pokemonUnits.Find(u => u.Pokemon == fainted);
        if (unit == null) yield break;

        var renderer = unit.Renderer;
        var rect = unit.RectTransform;
        var originalY = rect.localPosition.y;

        var frames = faintDropUnits / faintAnimationSpeed;
        for (float i = 0; i <= frames; i++)
        {
            renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, 1f - i / frames);
            rect.localPosition = new Vector3(rect.localPosition.x, originalY - i * faintAnimationSpeed, rect.localPosition.z);
            yield return null;
        }

        renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, 0f);
        rect.position = new Vector3(rect.localPosition.x, originalY, rect.localPosition.z);
    }

    public IEnumerator ThrowPokeball(ItemLogic ballType, Pokemon target)
    {
        var unit = pokemonUnits.Find(u => u.Pokemon == target);
        if (unit == null) yield break;

        thrownPokeball.enabled = true;
        thrownPokeball.sprite = GetPokeballSprite(ballType, false);
        originalUnitScale = unit.RectTransform.localScale;
        var tf = thrownPokeball.transform;
        var originalY = tf.localPosition.y;
        var spriteModified = false;

        var frames = pokeballDropUnits / pokeballAnimationSpeed;
        for (float i = 0; i <= frames; i++)
        {
            tf.localPosition = new Vector3(tf.localPosition.x, originalY - i * pokeballAnimationSpeed, tf.localPosition.z);

            if (i >= frames * 0.5f)
            {
                if (!spriteModified)
                {
                    thrownPokeball.sprite = GetPokeballSprite(ballType, true);
                    spriteModified = true;
                }

                unit.RectTransform.localScale -= new Vector3(originalUnitScale.x / (frames * 0.5f), originalUnitScale.y / (frames * 0.5f), 0);
            }

            yield return null;
        }

        unit.RectTransform.localScale = new Vector3(0, 0, unit.RectTransform.localScale.z);
        thrownPokeball.sprite = GetPokeballSprite(ballType, false);
    }

    public IEnumerator ShakePokeball()
    {
        var tf = thrownPokeball.transform;

        var frames = pokeballRotation / pokeballRotationSpeed;
        var rotationPerFrame = pokeballRotation / frames;
        var movementPerFrame = 0.5f;

        for (float i = 0; i <= frames; i++)
        {
            tf.Rotate(new Vector3(0, 0, rotationPerFrame));
            tf.localPosition += new Vector3(-movementPerFrame, 0, 0);
            yield return null;
        }
        for (float i = 0; i <= frames * 2f; i++)
        {
            tf.Rotate(new Vector3(0, 0, -rotationPerFrame));
            tf.localPosition += new Vector3(movementPerFrame, 0, 0);
            yield return null;
        }
        for (float i = 0; i <= frames; i++)
        {
            tf.Rotate(new Vector3(0, 0, rotationPerFrame));
            tf.localPosition += new Vector3(-movementPerFrame, 0, 0);
            yield return null;
        }
    }

    public IEnumerator FailPokeball(ItemLogic ballType, Pokemon target)
    {
        var unit = pokemonUnits.Find(u => u.Pokemon == target);
        if (unit == null) yield break;

        var tf = thrownPokeball.transform;
        thrownPokeball.sprite = GetPokeballSprite(ballType, true);
        thrownPokeball.color = new Color(thrownPokeball.color.r, thrownPokeball.color.g, thrownPokeball.color.b, 0.5f);

        var frames = originalUnitScale.x / failSpeed;
        for (float i = 0; i <= frames; i++)
        {
            unit.RectTransform.localScale += new Vector3(originalUnitScale.x / frames, originalUnitScale.y / frames, 0);
            yield return null;
        }

        MakeVisible(thrownPokeball);
        tf.rotation = new Quaternion(0, 0, 0, 0);
        tf.localPosition = thrownStartingPos;
        thrownPokeball.enabled = false;
    }

    private Sprite GetPokeballSprite(ItemLogic ballType, bool open)
    {
        switch (ballType)
        {
            case ItemLogic.PokeBall:
                return open ? pokeballs[26] : pokeballs[14];
            default:
                return null;
        }
    }
}
