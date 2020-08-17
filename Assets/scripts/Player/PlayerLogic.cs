using System;
using System.Collections;
using UnityEngine;

public enum Direction
{
    Up, Down, Left, Right
}

public class PlayerLogic : MonoBehaviour
{
    // Start is called before the first frame update
    public float moveSpeed;
    public LayerMask solidObjectsLayer;
    public LayerMask grass;
    public OverworldManager overworldManager;
    public AudioSource audioSource;

    private bool isMoving;
    private bool isRunning;
    private bool isInteractionFinished; // used by npc to signal dialogue is over
    private GameObject interactable;
    private Vector2 input;
    private Animator animator;

    public Direction Direction { get; set; }
    public Player Player { get; set; }

    void Start()
    {
        Application.targetFrameRate = 60;
        Direction = Direction.Down;
        isInteractionFinished = true;
        Player = new Player();
    }

    void Awake(){
        animator = GetComponent<Animator>();
    }

    void Update(){
        if (!isInteractionFinished) return; //drop input

        if(!isMoving){
            //interact with something if it exists
            if (Input.GetKeyDown(KeyCode.Z) && interactable != null)
            {
                Interact();
                return;
            }

            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");

            if (input.x != 0)
            {
                input.y = 0;
                interactable = null; //clear interaction; will be set again by trigger if one is available
                Direction = input.x < 0 ? Direction.Left : Direction.Right;
            }
            else if (input.y != 0)
            {
                input.x = 0;
                interactable = null; //clear interaction; will be set again by trigger if one is available
                Direction = input.y < 0 ? Direction.Down : Direction.Up;
            }

            if(input != Vector2.zero){
                animator.SetFloat("moveX",input.x);
                animator.SetFloat("moveY",input.y);

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

        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isMoving", isMoving);
    }

    // IEnumerator is used to do something over a period of time -  move current to target pos over a period of time
    private IEnumerator Move(Vector3 targetPos){
        isMoving = true;
        while ((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, 
                targetPos,
                moveSpeed * (isRunning ? 2 : 1) * Time.deltaTime
            ); 
            yield return null;
        }
        transform.position = targetPos;
        isMoving = false;
        CheckForPokemons();
    }

    public void FaceDirection(Direction direction)
    {
        switch (direction)
        {
            case Direction.Down:
                animator.SetFloat("moveY", -1f);
                break;
            case Direction.Up:
                animator.SetFloat("moveY", 1f);
                break;
            case Direction.Left:
                animator.SetFloat("moveX", -1f);
                break;
            case Direction.Right:
                animator.SetFloat("moveX", 1f);
                break;
        }

        Direction = direction;
    }

    // checks if the target Pos contains objects with SolidObjectsLayer, 0.2f is the offset radius
    private bool IsWalkable(Vector3 targetPos){
        if(Physics2D.OverlapCircle(targetPos,0.2f,solidObjectsLayer)!=null){
            return false;
        }
        return true;
    }

    private void CheckForPokemons(){
        if(Physics2D.OverlapCircle(transform.position,0.2f,grass)!=null){
            if(UnityEngine.Random.Range(1,101)<=10){
                    Debug.Log("Pokemon Found");
            }
        }
    }

    private void Interact()
    {
        isInteractionFinished = false;

        switch (interactable.tag)
        {
            case "NPC":
                interactable.GetComponent<NPC>().Interact(this);
                break;
            case "Item":
                interactable.GetComponent<Item>().Collect(this);
                break;
        }
        
    }

    public void EndInteraction()
    {
        if (!audioSource.isPlaying) audioSource.Play();
        isInteractionFinished = true;
        // required due to the order in which update() functions are run
        if (interactable.tag == "Item") interactable = null;
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
