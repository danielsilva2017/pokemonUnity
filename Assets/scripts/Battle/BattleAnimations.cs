using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using static Utils;

public class BattleAnimations : MonoBehaviour
{
    public List<Unit> pokemonUnits;
    public SpriteRenderer thrownPokeball;
    public Animator npcAnimator;
    public RectTransform npcPosition;
    public RectTransform npcEndPosition;
    public AudioSource audioPlayer;
    public AudioClip exitPokeball;
    public AudioClip faint;

    private Sprite[] pokeballs;
    private Vector3 thrownStartingPos;
    private Vector3 npcStartingPos;
    private Vector3 originalUnitScale;

    private readonly int faintDropUnits = 80;
    private readonly float faintAnimationSpeed = 5f;
    private readonly int pokeballDropUnits = 110;
    private readonly float pokeballAnimationSpeed = 3f;
    private readonly int pokeballRotation = 25;
    private readonly float pokeballRotationSpeed = 2f;
    private readonly float failSpeed = 6f;
    private readonly float takeDamageSpeed = 24f;
    private readonly float switchSpeed = 4f;
    private readonly float npcIntroSpeed = 5f;

    // Start is called before the first frame update
    void Start()
    {
        thrownStartingPos = thrownPokeball.transform.localPosition;
        npcStartingPos = npcPosition.localPosition;
        thrownPokeball.enabled = false;
        pokeballs = Resources.LoadAll<Sprite>("Images/pokeballs");
    }

    public Unit GetUnit(Pokemon pokemon)
    {
        return pokemonUnits.Find(u => u.Pokemon == pokemon);
    }

    public IEnumerator Faint(Pokemon fainted)
    {
        var unit = pokemonUnits.Find(u => u.Pokemon == fainted);
        if (unit == null) yield break;

        var renderer = unit.Renderer;
        var rect = unit.RectTransform;
        var originalY = rect.localPosition.y;

        audioPlayer.clip = faint;
        audioPlayer.Play();

        var frames = faintDropUnits / faintAnimationSpeed;
        for (float i = 0; i <= frames; i++)
        {
            renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, Math.Max(0f, 1f - i * 1.5f / frames));
            rect.localPosition = new Vector3(rect.localPosition.x, originalY - i * faintAnimationSpeed, rect.localPosition.z);
            yield return null;
        }

        renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, 0f);
        rect.localPosition = new Vector3(rect.localPosition.x, originalY, rect.localPosition.z);
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
            case ItemLogic.GreatBall:
                return open ? pokeballs[27] : pokeballs[15];
            default:
                return null;
        }
    }

    public IEnumerator TakeDamage(Pokemon target)
    {
        var unit = pokemonUnits.Find(u => u.Pokemon == target);
        if (unit == null) yield break;

        var targetGreyShade = 80;
        var frames = (255 - targetGreyShade) / takeDamageSpeed;
        var colorChangePerFrame = ((255 - targetGreyShade) / frames) / 255f;

        for (var grad = 0; grad < 4; grad++)
        {
            var change = grad % 2 == 0 ? -colorChangePerFrame : colorChangePerFrame;
            for (float i = 0; i <= frames; i++)
            {
                unit.Renderer.color += new Color(change, change, change);
                yield return null;
            }
        }

        unit.Renderer.color = new Color(1, 1, 1);
    }

    public void UpdatePokemonAnimationSpeed(Pokemon target)
    {
        var unit = pokemonUnits.Find(u => u.Pokemon == target);
        if (unit == null) return;

        var ratio = 1f * target.Health / target.MaxHealth;
        if (ratio >= 0.5f) unit.Animator.speed = 1f;
        else if (ratio >= 0.25f) unit.Animator.speed = 0.75f;
        else if (ratio > 0f) unit.Animator.speed = 0.6f;
        else unit.Animator.speed = 0.35f;
    }

    public IEnumerator SwitchInPokemon(Pokemon target)
    {
        var unit = pokemonUnits.Find(u => u.Pokemon == target);
        if (unit == null) yield break;

        var ball = unit.switchPokeball;
        var tf = ball.transform;
        var originalScale = unit.RectTransform.localScale;

        ball.enabled = true;
        ball.sprite = GetPokeballSprite(target.Pokeball, false);
        unit.RectTransform.localScale = new Vector3(0, 0, unit.RectTransform.localScale.z);

        yield return new WaitForSeconds(0.5f);
        ball.color = new Color(ball.color.r, ball.color.g, ball.color.b, 0.5f);
        ball.sprite = GetPokeballSprite(target.Pokeball, true);
        audioPlayer.clip = exitPokeball;
        audioPlayer.Play();

        var frames = unit.OriginalScale.x / switchSpeed;
        for (float i = 0; i <= frames; i++)
        {
            unit.RectTransform.localScale += new Vector3(unit.OriginalScale.x / frames, unit.OriginalScale.y / frames, 0);
            yield return null;
        }

        ball.enabled = false;
    }

    public void SetupNPCIntro(Trainer trainer)
    {
        npcAnimator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>($"Trainers/{trainer.skeleton.animationPrefix}_intro");
        npcAnimator.speed = 0;
    }

    public void DisableNPCIntro()
    {
        npcAnimator.gameObject.SetActive(false);
    }

    public IEnumerator PlayNPCIntro()
    {
        npcAnimator.speed = 1;
        while (npcAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1 < 0.99f)
            yield return null;
    }

    public IEnumerator PlayNPCSlideOut()
    {
        var frames = (npcEndPosition.localPosition.x - npcStartingPos.x) / npcIntroSpeed;
        for (float i = 0; i <= frames; i++)
        {
            npcPosition.localPosition += new Vector3(
                (npcEndPosition.localPosition.x - npcStartingPos.x) / frames,
                (npcEndPosition.localPosition.y - npcStartingPos.y) / frames,
                0);
            yield return null;
        }
    }

    public IEnumerator PlayNPCSlideIn()
    {
        var frames = (npcEndPosition.localPosition.x - npcStartingPos.x) / npcIntroSpeed;
        for (float i = 0; i <= frames; i++)
        {
            npcPosition.localPosition -= new Vector3(
                (npcEndPosition.localPosition.x - npcStartingPos.x) / frames,
                (npcEndPosition.localPosition.y - npcStartingPos.y) / frames,
                0);
            yield return null;
        }
    }
}
