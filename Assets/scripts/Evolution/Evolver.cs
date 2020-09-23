using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class Evolver : MonoBehaviour
{
    public EvolutionDialog chatbox;
    public GameObject oldPokemon;
    public GameObject newPokemon;
    public SpriteRenderer oldPokemonSprite;
    public SpriteRenderer newPokemonSprite;
    public Animator oldPokemonAnim;
    public Animator newPokemonAnim;
    public SpriteRenderer transition;
    public AudioSource chatSound;
    public AudioSource cryPlayer;
    public AudioSource musicPlayer;
    public AudioClip evolutionMusic;
    public AudioClip sparkleSound;
    public AudioClip successMusic;
    //public AudioClip fillerMusic;

    private List<PendingEvolution> pendingEvolutions;
    private bool cancelledEvolution;
    private int moveIndex;
    private int confirmationIndex;
    private bool? confirmation;
    private int? moveLearningSelection;
    private InputRequest inputRequestMode;
    private Pokemon moveLearner;

    public bool IsBusy { get; set; }

    private enum InputRequest
    {
        Cancel, Confirmation, MoveSelection
    }

    private IEnumerator Print(string message, bool delay = true)
    {
        yield return chatbox.Print(message);
        if (delay) yield return new WaitForSeconds(1f);
    }

    void Start()
    {
        pendingEvolutions = SceneInfo.GetPendingEvolutions();
        /*pendingEvolutions = new List<PendingEvolution>
        {
            new PendingEvolution
            {
                Subject = CreatePokemon("Bulbasaur", 16),
                TargetSkeleton = CreatePokemon("Ivysaur", 16).Skeleton
            }
        };*/
        chatbox.confirmationObject.SetActive(false);
        chatbox.SetState(ChatState.ChatOnly);
        IsBusy = true;
        StartCoroutine(HandleAllEvolutions());
    }

    void Update()
    {
        if (IsBusy) return;

        switch (inputRequestMode)
        {
            case InputRequest.Cancel:
                if (Input.GetKeyDown(KeyCode.X))
                    cancelledEvolution = true;
                break;
            case InputRequest.Confirmation:
                ConfirmationPicker();
                break;
            case InputRequest.MoveSelection:
                MovePicker();
                break;
        }
    }

    private IEnumerator HandleAllEvolutions()
    {
        foreach (var evolution in pendingEvolutions)
        {
            // intro
            oldPokemonAnim.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(GetAnimationPath(evolution.Subject.Skeleton));
            newPokemonAnim.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(GetAnimationPath(evolution.TargetSkeleton));
            newPokemon.SetActive(false);
            transition.gameObject.SetActive(true);
            musicPlayer.clip = evolutionMusic;
            musicPlayer.Play();
            MakeVisible(transition);
            yield return FadeOut(transition, 20);

            // actually perform the evolution
            yield return HandleEvolution(evolution);

            // outro
            transition.gameObject.SetActive(true);
            MakeInvisible(transition);
            yield return FadeIn(transition, 20);
            musicPlayer.Stop();
        }

        SceneInfo.ClearPendingEvolutions();
        SceneInfo.ReturnToOverworld();
    }

    private IEnumerator HandleEvolution(PendingEvolution evolution)
    {
        // set initial flags
        IsBusy = true;
        cancelledEvolution = false;
        var originalName = evolution.Subject.Name;

        yield return Print($"Oh! {originalName} is evolving!");
        inputRequestMode = InputRequest.Cancel;
        IsBusy = false;

        // run a partially cancellable animation
        var anim = AnimateEvolution();
        do
        {
            if (cancelledEvolution) break;
            yield return anim.Current;
        } while (anim.MoveNext());

        if (cancelledEvolution)
        {
            oldPokemonSprite.color = new Color(1, 1, 1, 1);
            yield return Print("Huh?");
            musicPlayer.Stop();
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            oldPokemon.SetActive(false);
            newPokemon.SetActive(true);
            evolution.Subject.Evolve(evolution.TargetSkeleton);
            musicPlayer.clip = sparkleSound;
            musicPlayer.Play();
            yield return new WaitForSeconds(1.7f);
            cryPlayer.clip = evolution.Subject.Skeleton.cry;
            cryPlayer.Play();
            yield return new WaitForSeconds(0.7f);
            musicPlayer.clip = successMusic;
            musicPlayer.Play();
            yield return Print($"{originalName} evolved into {evolution.Subject.Name}!");
            yield return new WaitForSeconds(3f);
            yield return HandleMoveLearning(evolution.Subject);
        }
    }

    private IEnumerator AnimateEvolution()
    {
        newPokemonSprite.color = new Color(0, 0, 0);
        var scale = newPokemonSprite.transform.localScale;
        newPokemonSprite.transform.localScale = new Vector3(0, 0, scale.z);
        newPokemon.SetActive(true);

        var colorStep = 1f / 15f;
        for (var frame = 0; frame < 15; frame++)
        {
            oldPokemonSprite.color -= new Color(colorStep, colorStep, colorStep, 0);
            yield return null;
        }

        var cycles = 10;
        for (int i = 0, duration = 30; i < cycles; i++, duration = Math.Max(10, duration - 5))
        {
            var scaleStep = 1f / duration * scale.x;
            for (var frame = 0; frame < duration; frame++)
            {
                oldPokemonSprite.transform.localScale -= new Vector3(scaleStep, scaleStep, 0);
                newPokemonSprite.transform.localScale += new Vector3(scaleStep, scaleStep, 0);
                yield return null;
            }
            for (var frame = 0; frame < duration; frame++)
            {
                oldPokemonSprite.transform.localScale += new Vector3(scaleStep, scaleStep, 0);
                newPokemonSprite.transform.localScale -= new Vector3(scaleStep, scaleStep, 0);
                yield return null;
            }

            // restore new sprite's scale
            if (i >= cycles - 1)
            {
                IsBusy = true;
                for (var frame = 0; frame < duration; frame++)
                {
                    oldPokemonSprite.transform.localScale -= new Vector3(scaleStep, scaleStep, 0);
                    newPokemonSprite.transform.localScale += new Vector3(scaleStep, scaleStep, 0);
                    yield return null;
                }
            }
        }

        for (var frame = 0; frame < 15; frame++)
        {
            newPokemonSprite.color += new Color(colorStep, colorStep, colorStep);
            yield return null;
        }
    }

    private IEnumerator HandleMoveLearning(Pokemon learner)
    {
        var newMoves = learner.NewMovesFromLevelUp();

        if (newMoves.Count == 0)
            yield break;

        //musicPlayer.clip = fillerMusic;
        //musicPlayer.Play();

        foreach (var moveSkeleton in newMoves)
        {
            // clear the flags
            confirmation = null;
            moveLearningSelection = null;

            // there's room, automatically learn
            var usedSlots = learner.GetFilledMoveSlots();
            if (usedSlots < 4)
            {
                learner.Moves[usedSlots] = new Move(moveSkeleton);
                yield return Print($"{learner.Name} learned {moveSkeleton.moveName}!");
                continue;
            }

            // no room, replace a move
            moveLearner = learner;
            yield return Print($"{learner.Name} is trying to learn {moveSkeleton.moveName}.");
            yield return Print("Should it do so?", false);

            chatbox.confirmationObject.SetActive(true);
            chatbox.ConfirmationBox.CursorYes();
            confirmationIndex = 0;
            inputRequestMode = InputRequest.Confirmation;
            IsBusy = false;
            yield return Await(() => confirmation != null);
            IsBusy = true;
            chatbox.confirmationObject.SetActive(false);

            if (confirmation == false) // rejected learning move
                continue;

            yield return Print("Which move should be replaced?");
            chatbox.SetState(ChatState.SelectMove);
            chatbox.RefreshMoves(learner);
            chatbox.moves[moveIndex].color = Color.black;
            moveIndex = 0;
            inputRequestMode = InputRequest.MoveSelection;
            IsBusy = false;
            yield return Await(() => moveLearningSelection != null);
            IsBusy = true;
            chatbox.SetState(ChatState.ChatOnly);

            if (moveLearningSelection < 0) // rejected learning move
            {
                yield return Print($"{learner.Name} did not learn {moveSkeleton.moveName}.");
                continue;
            }

            learner.Moves[moveLearningSelection.Value] = new Move(moveSkeleton);
            yield return Print($"{learner.Name} learned {moveSkeleton.moveName}!");
        }
    }

    private void ConfirmationPicker()
    {
        var oldConfirmationIndex = confirmationIndex;

        if (Input.GetKeyDown(KeyCode.UpArrow)) confirmationIndex = confirmationIndex == 1 ? 0 : 1;
        if (Input.GetKeyDown(KeyCode.DownArrow)) confirmationIndex = confirmationIndex == 0 ? 1 : 0;

        if (oldConfirmationIndex != confirmationIndex)
        {
            chatSound.Play();
            if (confirmationIndex == 0) chatbox.ConfirmationBox.CursorYes();
            else chatbox.ConfirmationBox.CursorNo();
        }

        // don't learn move
        if (Input.GetKeyDown(KeyCode.X))
        {
            confirmation = false;
            chatSound.Play();
            return;
        }

        // use or cancel
        if (Input.GetKeyDown(KeyCode.Z))
        {
            confirmation = confirmationIndex == 0; // 0 = yes
            chatSound.Play();
        }
    }

    private void MovePicker()
    {
        var oldIndex = moveIndex;
        chatbox.moves[moveIndex].color = Color.black;

        if (Input.GetKeyDown(KeyCode.UpArrow)) moveIndex = moveIndex < 2 ? moveIndex + 2 : moveIndex - 2;
        if (Input.GetKeyDown(KeyCode.DownArrow)) moveIndex = (moveIndex + 2) % 4;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) moveIndex = moveIndex % 2 == 0 ? moveIndex + 1 : moveIndex - 1;
        if (Input.GetKeyDown(KeyCode.RightArrow)) moveIndex = moveIndex % 2 != 0 ? moveIndex - 1 : moveIndex + 1;

        // reset selection
        if (chatbox.moves[moveIndex].text == "-") moveIndex = oldIndex;

        chatbox.moves[moveIndex].color = Color.blue;
        chatbox.ShowMoveInfo(moveLearner.Moves[moveIndex]);

        if (oldIndex != moveIndex) chatSound.Play();

        // back to actions
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (chatbox.IsBusy) return;

            chatSound.Play();
            moveLearningSelection = -1;
            return;
        }

        // perform move
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (chatbox.IsBusy) return;

            chatSound.Play();
            moveLearningSelection = moveIndex;
        }
    }

    private string GetAnimationPath(PokemonBase skeleton)
    {
        return string.Format("Images/{0}/ctrl", skeleton.dexNumber);
    }
}