using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static Utils;

public class PlayerLogic : MonoBehaviour
{
    public float moveSpeed;
    public Rigidbody2D rb2d;
    public LayerMask solidLayer;  // solid objects
    public LayerMask grassLayer;  // grass
    public LayerMask waterLayer;  // water
    public LayerMask jumpLayer;   // can jump over
    public LayerMask noJumpLayer; // walkable, cannot jump over something from here
    public Overworld overworld;
    public PlayerUI playerUI; // transitions and animations other than moving
    public AudioSource battleMusicPlayer;

    private int interactionForbiddenFrames; // do not allow interaction for this amount of frames
    private Vector2 input;
    private readonly float maxIgnorableDistance = 0.5f; // used in NPC collision calculations

    public GameObject Interactable { get; set; }
    public Animator Animator { get; set; }
    public Direction Direction { get; set; }
    public Player Player { get; set; }
    public bool IsMoving { get; set; }
    public bool IsRunning { get; set; }
    public bool IsJumping { get; set; }
    public bool IsSurfing { get; set; }
    public bool IsUsingMenu { get; set; }
    public bool IsBusy { get; set; } // general boolean for dropping inputs
    public bool IsInteractionFinished { get; set; } // used by interactable to signal dialogue is over
    public ItemBase b;
    void Start()
    {
        Application.targetFrameRate = 60;
        var playerInfo = SceneInfo.GetPlayerInfo();

        if (playerInfo == null) // create new
        {
            FaceDirection(Direction.Down);
            Player = new Player();
        }
        else // load state
        {
            FaceDirection(playerInfo.Direction);
            Player = new Player(playerInfo.Player);
            overworld = GameObject.Find(playerInfo.OverworldKey).GetComponent<Overworld>();

            var targetCoordinates = SceneInfo.GetTargetCoordinates();
            transform.position = new Vector3(
                targetCoordinates?.x ?? playerInfo.Position.x, 
                targetCoordinates?.y ?? playerInfo.Position.y, 
                playerInfo.Position.z
            );

            SceneInfo.DeletePlayerInfo();
            SceneInfo.DeleteTargetCoordinates();
        }
        Player.Bag.AddItem(new Item(b), 20);
        IsInteractionFinished = true;
        StartCoroutine(PlayEnterSceneTransition());
    }

    void Awake()
    {
        Animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!IsInteractionFinished || IsBusy) return; // drop inputs

        if (interactionForbiddenFrames > 0) // stop user from beginning interaction on the same keypress that ended one
            interactionForbiddenFrames--;

        if (Input.GetKeyUp(KeyCode.Return) && !playerUI.menu.IsBusy) // open/close side menu
            IsUsingMenu = playerUI.menu.Toggle();

        if (Input.GetKeyUp(KeyCode.X) && IsUsingMenu) // x also closes menu
            IsUsingMenu = playerUI.menu.Toggle();

        if (!IsMoving && !IsJumping && !IsUsingMenu) // process movement if not busy
        {
            // interact with something if it exists
            if (Input.GetKeyDown(KeyCode.Z) && Interactable != null)
            {
                Interact();
                return;
            }

            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");
            var lastDirection = Direction;

            if (input.x != 0)
            {
                input.y = 0;
                Interactable = null; // clear interaction; will be set again by trigger if one is available
                Direction = input.x < 0 ? Direction.Left : Direction.Right;
            }
            else if (input.y != 0)
            {
                input.x = 0;
                Interactable = null; // clear interaction; will be set again by trigger if one is available
                Direction = input.y < 0 ? Direction.Down : Direction.Up;
            }

            if (input != Vector2.zero) // will attempt to move
            {
                rb2d.WakeUp(); // force it to recompute physics
                Animator.SetFloat("moveX", input.x);
                Animator.SetFloat("moveY", input.y);

                var targetPos = transform.position;
                targetPos.x += input.x;
                targetPos.y += input.y;

                if(IsWalkable(targetPos))
                    StartCoroutine(Move(targetPos));
            }
        }

        //only run while pressing key
        IsRunning = Input.GetKey(KeyCode.X);

        Animator.SetBool("isMoving", IsMoving);
        Animator.SetBool("isRunning", IsRunning);
        Animator.SetBool("isJumping", IsJumping);
    }

    private IEnumerator PlayEnterSceneTransition()
    {
        IsBusy = true;
        yield return playerUI.EnterSceneTransition(overworld);
        IsBusy = false;
    }

    // IEnumerator is used to do something over a period of time -  move current to target pos over a period of time
    private IEnumerator Move(Vector3 target)
    {
        IsMoving = true;

        if (PositionIsLayer(target, jumpLayer))
        {
            IsJumping = true;
            target = GetJumpTarget(target);
        }

        while ((target - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, 
                target,
                moveSpeed * (IsRunning && !IsJumping ? 2 : 1) * Time.deltaTime
            ); 
            yield return null;
        }

        transform.position = target;

        IsMoving = false;
        IsJumping = false;

        foreach (var npc in overworld.characters)
            npc.NotifyPlayerMoved();

        CheckForPokemons();
    }

    private Vector3 GetJumpTarget(Vector3 originalTarget)
    {
        switch (Direction)
        {
            case Direction.Down:
                originalTarget.y--;
                return originalTarget;
            case Direction.Up:
                originalTarget.y++;
                return originalTarget;
            case Direction.Left:
                originalTarget.x--;
                return originalTarget;
            case Direction.Right:
                originalTarget.x++;
                return originalTarget;
            default:
                return originalTarget;
        }
    }

    public void FaceDirection(Direction direction)
    {
        switch (direction)
        {
            case Direction.Down:
                Animator.SetFloat("moveX", 0f);
                Animator.SetFloat("moveY", -1f);
                break;
            case Direction.Up:
                Animator.SetFloat("moveX", 0f);
                Animator.SetFloat("moveY", 1f);
                break;
            case Direction.Left:
                Animator.SetFloat("moveX", -1f);
                Animator.SetFloat("moveY", 0f);
                break;
            case Direction.Right:
                Animator.SetFloat("moveX", 1f);
                Animator.SetFloat("moveY", 0f);
                break;
        }

        Direction = direction;
    }

    private bool IsWalkable(Vector3 target)
    {
        return !PositionIsLayer(target, solidLayer) && // solid block
            !(PositionIsLayer(target, waterLayer) && !IsSurfing) && // water when on foot
            !(PositionIsLayer(target, jumpLayer) && PositionIsLayer(transform.position, noJumpLayer)); // trying to jump when not allowed
    }

    private void CheckForPokemons()
    {
        if(PositionIsLayer(transform.position, grassLayer)){
            if (Chance(overworld.wildPokemonChance))
                StartCoroutine(BeginWildBattle());
        }
    }
    
    private IEnumerator BeginWildBattle()
    {
        IsBusy = true; // stop inputs
        overworld.locationMusic.Stop();
        Animator.speed = 0;
        yield return playerUI.WildBattleTransition();
        SceneInfo.BeginWildBattle(this, overworld.GenerateGrassEncounter(), overworld.weather);
    }

    private IEnumerator OnAreaExit(AreaExit exit)
    {
        IsBusy = true;
        yield return playerUI.ExitAreaTransition();
        SceneInfo.FollowAreaExit(exit, this);
    }

    private void Interact()
    {
        if (interactionForbiddenFrames > 0) return;
        IsInteractionFinished = false;
        IsMoving = false;

        switch (Interactable.tag)
        {
            case "NPC":
                var npc = Interactable.GetComponent<NPC>();
                if (!npc.IsBusy)
                {
                    npc.FaceDirection(GetOppositeDirection(Direction));
                    npc.Interact();
                }
                else // abort interaction attempt
                {
                    IsInteractionFinished = true;
                }
                break;
            case "Item":
                Interactable.GetComponent<OverworldItem>().Collect(this);
                break;
            default:
                IsInteractionFinished = true;
                break;
        } 
    }

    public void EndInteraction()
    {
        IsInteractionFinished = true;

        // required due to the order in which update() functions are run
        interactionForbiddenFrames = 2;
        if (Interactable.tag == "Item") Interactable = null;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        switch (other.tag)
        {
            case "Area Border":
                var border = other.gameObject.GetComponent<AreaBorder>();
                if (border.overworld.locationName != overworld.locationName) // player is now in a different area
                {
                    playerUI.PassAreaBorder(border, overworld);
                    overworld = border.overworld;
                }
                break;
            case "Area Exit":
                var exit = other.gameObject.GetComponent<AreaExit>();
                StartCoroutine(OnAreaExit(exit));
                break;
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.tag != "NPC" && other.gameObject.tag != "Item") return;

        var selfPos = transform.position;
        var otherPos = other.gameObject.transform.position;

        if (!IsValidCollision(selfPos, otherPos)) return;

        // use z-index for correct sprite priority
        other.gameObject.transform.position = new Vector3(
            otherPos.x,
            otherPos.y,
            Direction == Direction.Down ? 1 : 3
        );

        // at this point we know the player is directly facing the interactable thing
        Interactable = other.gameObject;
    }

    private bool IsValidCollision(Vector3 self, Vector3 other)
    {
        var xdiff = Math.Abs(self.x - other.x);
        var ydiff = Math.Abs(self.y - other.y);

        switch (Direction)
        {
            case Direction.Up:
                return xdiff <= maxIgnorableDistance && self.y < other.y;
            case Direction.Down:
                return xdiff <= maxIgnorableDistance && self.y > other.y;
            case Direction.Left:
                return ydiff <= maxIgnorableDistance && self.x > other.x;
            case Direction.Right:
                return ydiff <= maxIgnorableDistance && self.x < other.x;
            default:
                return false;
        }
    }
}
