using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using System;
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
    public TrainerBase skeleton;
    public int playerDetectionRadius;
    public int roamRadius;
    public Direction startingDirection;
    [TextArea] public string[] dialogue;
    [TextArea] public string[] defeatDialogue;
    [TextArea] public string[] postDialogue;
    public Weather weather;
    public int difficulty;
    public int money;
    public TrainerPokemonInit[] pokemons;
    public PlayerLogic playerLogic;
    public LayerMask solidLayer;
    public LayerMask waterLayer;
    public LayerMask jumpLayer;
    public AudioSource detectedPlayer;

    private List<Pokemon> party;
    private readonly float maxIgnorableDistance = 0.5f; // used in NPC collision calculations
    private SpriteRenderer exclamation;

    void Start()
    {
        Name = trainerName;
        RoamRadius = roamRadius;
        Direction = startingDirection;
        Dialogue = dialogue;
        PostDialogue = postDialogue;
        PlayerLogic = playerLogic;
        SolidLayer = solidLayer;
        WaterLayer = waterLayer;
        JumpLayer = jumpLayer;
        OriginalPosition = transform.position;

        party = new List<Pokemon>(pokemons.Length);
        foreach (var init in pokemons)
            party.Add(CreatePokemon(init.speciesName, init.level));

        Animator = GetComponent<Animator>();
        Animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>($"Trainers/{skeleton.animationPrefix}_ctrl");
        exclamation = transform.GetChild(0).GetComponent<SpriteRenderer>();
        exclamation.gameObject.SetActive(false);
        FaceDirection(Direction);
    }

    protected override void OnIdle()
    {
        if (RoamRadius > 0 && !playerLogic.IsBusy)
            TryDetectPlayer();
    }

    public override void NotifyPlayerMoved()
    {
        UpdateSpriteZIndex();

        if (!playerLogic.IsBusy)
            TryDetectPlayer();
    }

    private void TryDetectPlayer()
    {
        if (playerDetectionRadius <= 0 || IsDefeated) return;

        var selfPos = transform.position;
        var playerPos = playerLogic.transform.position;

        if (!IsBusy && !playerLogic.IsBusy && HasDetectedPlayer(selfPos, playerPos))
            StartCoroutine(EngagePlayer());
    }

    private IEnumerator EngagePlayer()
    {
        playerLogic.IsBusy = true;
        playerLogic.IsMoving = false;
        playerLogic.IsRunning = false;
        playerLogic.Animator.SetBool("isMoving", false);
        playerLogic.Animator.SetBool("isRunning", false);
        IsMoving = false;

        exclamation.gameObject.SetActive(true);
        yield return FadeIn(exclamation, 5);
        detectedPlayer.Play();
        yield return Stall(30);
        yield return FadeOut(exclamation, 5);
        exclamation.gameObject.SetActive(false);

        OnInteractionStart();

        boxCollider2D.enabled = false; // ensure they don't get stuck on terrain while moving towards player
        var target = GetMovementTarget(transform.position, Direction);
        while (IsWalkable(target))
        {
            yield return Move(target);
            target = GetMovementTarget(transform.position, Direction);
        }

        playerLogic.Interactable = gameObject;
        playerLogic.FaceDirection(GetOppositeDirection(Direction));
        Interact(false);
    }

    private bool HasDetectedPlayer(Vector3 self, Vector3 player)
    {
        var xdiff = Math.Abs(self.x - player.x);
        var ydiff = Math.Abs(self.y - player.y);
        var maxdiff = playerDetectionRadius + 0.4f;

        switch (Direction)
        {
            case Direction.Up:
                return xdiff <= maxIgnorableDistance && self.y < player.y && ydiff <= maxdiff;
            case Direction.Down:
                return xdiff <= maxIgnorableDistance && self.y > player.y && ydiff <= maxdiff;
            case Direction.Left:
                return ydiff <= maxIgnorableDistance && self.x > player.x && xdiff <= maxdiff;
            case Direction.Right:
                return ydiff <= maxIgnorableDistance && self.x < player.x && xdiff <= maxdiff;
            default:
                return false;
        }
    }

    protected override void OnInteractionStart()
    {
        if (!IsDefeated)
        {
            PlayerLogic.overworld.locationMusic.Stop();
            PlayerLogic.Animator.speed = 0;
            PlayerLogic.playerUI.PlayMusicImmediate(PlayerLogic, skeleton.introMusic);
        }
    }

    protected override IEnumerator DoAction()
    {
        yield return PlayerLogic.playerUI.TrainerBattleTransition(PlayerLogic, skeleton.battleMusic);
        SceneInfo.BeginTrainerBattle(PlayerLogic, this, party, 1, weather);
    }
}
