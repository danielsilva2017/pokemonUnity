using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static Utils;

public enum Direction
{
    Up, Down, Left, Right
}

public class PlayerLogic : MonoBehaviour
{
    public float moveSpeed;
    public Rigidbody2D rigidbody2D;
    public LayerMask solidLayer;  // solid objects
    public LayerMask grassLayer;  // grass
    public LayerMask waterLayer;  // water
    public LayerMask jumpLayer;   // can jump over
    public LayerMask noJumpLayer; // walkable, cannot jump over something from here
    public Overworld overworld;
    public PlayerUI playerUI; // transitions and animations other than moving
    public AudioSource battleMusicPlayer;

    private bool isMoving;
    private bool isRunning;
    private bool isJumping;
    private bool isSurfing;
    private bool isUsingMenu;
    private bool isBusy; // general boolean for dropping inputs
    private bool isInteractionFinished; // used by interactable to signal dialogue is over
    private int interactionForbiddenFrames; // do not allow interaction for this amount of frames
    private GameObject interactable;
    private Vector2 input;
    private readonly float maxIgnorableDistance = 0.5f; // used in NPC collision calculations

    public Animator Animator { get; set; }
    public Direction Direction { get; set; }
    public Player Player { get; set; }
    public ItemBase b;
    void Start()
    {
        Application.targetFrameRate = 60;
        var playerInfo = SceneInfo.GetPlayerInfo();

        if (playerInfo == null) // create new
        {
            Direction = Direction.Down;
            Player = new Player();
        }
        else // load state
        {
            FaceDirection(playerInfo.Direction);
            Player = new Player(playerInfo.Player);
            transform.position = playerInfo.Position;
            overworld = GameObject.Find(playerInfo.OverworldKey).GetComponent<Overworld>();
            SceneInfo.DeletePlayerInfo();
        }
        Player.Bag.AddItem(new Item(b), 20);
        overworld.locationMusic.Play();
        isInteractionFinished = true;
        StartCoroutine(PlayEnterSceneTransition());
    }

    void Awake()
    {
        Animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!isInteractionFinished || isBusy) return; // drop inputs

        if (interactionForbiddenFrames > 0) // stop user from beginning interaction on the same keypress that ended one
            interactionForbiddenFrames--;

        if (Input.GetKeyUp(KeyCode.Return) && !playerUI.menu.IsBusy) // open/close side menu
            isUsingMenu = playerUI.menu.Toggle();

        if (Input.GetKeyUp(KeyCode.X) && isUsingMenu) // x also closes menu
            isUsingMenu = playerUI.menu.Toggle();

        if (!isMoving && !isJumping && !isUsingMenu) // process movement if not busy
        {
            // interact with something if it exists
            if (Input.GetKeyDown(KeyCode.Z) && interactable != null)
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
                interactable = null; // clear interaction; will be set again by trigger if one is available
                Direction = input.x < 0 ? Direction.Left : Direction.Right;
            }
            else if (input.y != 0)
            {
                input.x = 0;
                interactable = null; // clear interaction; will be set again by trigger if one is available
                Direction = input.y < 0 ? Direction.Down : Direction.Up;
            }

            if (input != Vector2.zero) // will attempt to move
            {
                rigidbody2D.WakeUp(); // force it to recompute physics
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
        isRunning = Input.GetKey(KeyCode.X);

        Animator.SetBool("isMoving", isMoving);
        Animator.SetBool("isRunning", isRunning);
        Animator.SetBool("isJumping", isJumping);
    }

    private IEnumerator PlayEnterSceneTransition()
    {
        isBusy = true;
        yield return playerUI.EnterSceneTransition();
        isBusy = false;
    }

    // IEnumerator is used to do something over a period of time -  move current to target pos over a period of time
    private IEnumerator Move(Vector3 target)
    {
        isMoving = true;

        if (PositionIsLayer(target, jumpLayer))
        {
            isJumping = true;
            target = GetJumpTarget(target);
        }

        while ((target - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, 
                target,
                moveSpeed * (isRunning && !isJumping ? 2 : 1) * Time.deltaTime
            ); 
            yield return null;
        }

        transform.position = target;
        isMoving = false;
        isJumping = false;

        //var xx = Physics2D.Raycast(transform.position, Vector2.right);
        //Debug.Log(xx.collider);
        
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

    // checks if the target Pos contains objects with SolidObjectsLayer, 0.2f is the offset radius
    private bool IsWalkable(Vector3 target)
    {
        return !PositionIsLayer(target, solidLayer) && // solid block
            !(PositionIsLayer(target, waterLayer) && !isSurfing) && // water when on foot
            !(PositionIsLayer(target, jumpLayer) && PositionIsLayer(transform.position, noJumpLayer)); // trying to jump when not allowed
    }

    private void CheckForPokemons()
    {
        if(PositionIsLayer(transform.position, grassLayer)){
            if (Chance(overworld.wildPokemonChance))
                StartCoroutine(BeginWildBattle());
        }
    }

    private bool PositionIsLayer(Vector3 position, LayerMask layer)
    {
        return Physics2D.OverlapCircle(position, 0.2f, layer) != null;
    }
    
    private IEnumerator BeginWildBattle()
    {
        isBusy = true; // stop inputs
        overworld.locationMusic.Stop();
        Animator.speed = 0;
        yield return playerUI.WildBattleTransition();
        SceneInfo.BeginWildBattle(this, overworld.GenerateGrassEncounter(), overworld.weather);
    }

    private void Interact()
    {
        if (interactionForbiddenFrames > 0) return;
        isInteractionFinished = false;
        isMoving = false;

        switch (interactable.tag)
        {
            case "NPC":
                interactable.GetComponent<NPC>().Interact(this);
                break;
            case "Item":
                interactable.GetComponent<OverworldItem>().Collect(this);
                break;
            default:
                isInteractionFinished = true;
                break;
        } 
    }

    public void EndInteraction()
    {
        isInteractionFinished = true;

        // required due to the order in which update() functions are run
        interactionForbiddenFrames = 2;
        if (interactable.tag == "Item") interactable = null;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Area Border")
        {
            var border = other.gameObject.GetComponent<AreaBorder>();
            if (border.overworld.locationName != overworld.locationName) // change area
            {
                playerUI.PassAreaBorder(border, overworld);
                overworld = border.overworld;
            }
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
        interactable = other.gameObject;
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
