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
    public LayerMask solidLayer;  // solid objects
    public LayerMask grassLayer;  // grass
    public LayerMask waterLayer;  // water
    public LayerMask jumpLayer;   // can jump over
    public LayerMask noJumpLayer; // walkable, cannot jump over something from here
    public Overworld overworld;
    public PlayerUI playerUI; // transitions and animations other than moving

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
    private Animator animator;

    public Direction Direction { get; set; }
    public Player Player { get; set; }

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

        overworld.locationMusic.Play();
        isInteractionFinished = true;
    }

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!isInteractionFinished) return; // drop inputs

        if (interactionForbiddenFrames > 0) // stop user from beginning interaction on the same keypress that ended one
            interactionForbiddenFrames--;

        if (Input.GetKeyUp(KeyCode.Return)) // open/close side menu
            isUsingMenu = playerUI.menu.Toggle();

        if (!isMoving && !isJumping && !isUsingMenu && !isBusy) // process movement if not busy
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

            if (input != Vector2.zero)
            {
                animator.SetFloat("moveX", input.x);
                animator.SetFloat("moveY", input.y);

                var targetPos = transform.position;
                targetPos.x += input.x;
                targetPos.y += input.y;

                if(IsWalkable(targetPos))
                    StartCoroutine(Move(targetPos));
            }
        }

        //only run while pressing key
        if (Input.GetKeyDown(KeyCode.X))
            isRunning = true;
        else if (Input.GetKeyUp(KeyCode.X))
            isRunning = false;

        animator.SetBool("isMoving", isMoving);
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isJumping", isJumping);
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
                animator.SetFloat("moveX", 0f);
                animator.SetFloat("moveY", -1f);
                break;
            case Direction.Up:
                animator.SetFloat("moveX", 0f);
                animator.SetFloat("moveY", 1f);
                break;
            case Direction.Left:
                animator.SetFloat("moveX", -1f);
                animator.SetFloat("moveY", 0f);
                break;
            case Direction.Right:
                animator.SetFloat("moveX", 1f);
                animator.SetFloat("moveY", 0f);
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
        yield return playerUI.WildBattleTransition();
        SceneInfo.BeginWildBattle(this, overworld.GenerateGrassEncounter(), overworld.weather);
    }

    private void Interact()
    {
        if (interactionForbiddenFrames > 0) return;
        isInteractionFinished = false;

        switch (interactable.tag)
        {
            case "NPC":
                interactable.GetComponent<NPC>().Interact(this);
                break;
            case "Item":
                interactable.GetComponent<Item>().Collect(this);
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
        
        var selfPos = transform.position;
        var otherPos = other.gameObject.transform.position;

        // correct errors
        var xdiff = (Math.Abs(selfPos.x - otherPos.x) >= 1) ? selfPos.x : otherPos.x;
        var ydiff = (Math.Abs(selfPos.y - otherPos.y) >= 1) ? selfPos.y : otherPos.y;

        var location = CollisionFrom(new Vector3(xdiff, ydiff, 0), otherPos);

        // use z-index for correct sprite priority
        other.gameObject.transform.position = new Vector3(
            otherPos.x,
            otherPos.y,
            location == Direction.Down ? 1 : 3
        );

        // directly facing the NPC
        if (location == Direction)
            interactable = other.gameObject;
    }

    private Direction? CollisionFrom(Vector3 self, Vector3 other)
    {
        if (self.y < other.y) return Direction.Up;
        else if (self.y > other.y) return Direction.Down;
        else if (self.x > other.x) return Direction.Left;
        else if (self.x < other.x) return Direction.Right;
        else return null;
    }
}
