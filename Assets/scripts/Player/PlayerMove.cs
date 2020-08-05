using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    // Start is called before the first frame update
    public float moveSpeed;
    public LayerMask solidObjectsLayer;
    public LayerMask grass;
    public AudioSource audioSource;

    private bool isMoving;
    private bool isRunning;
    private bool isInteracting;
    private bool isInteractionFinished; // used by npc to signal dialogue is over
    private Direction direction;
    private GameObject interactable;

    private Vector2 input;

    private Animator animator;

    private enum Direction
    {
        UP, DOWN, LEFT, RIGHT
    }

    void Start()
    {
        this.direction = Direction.DOWN;
        this.isInteracting = false;
        this.isInteractionFinished = true;
    }

    private void Awake(){
        animator = GetComponent<Animator>();
    }

    private void Update(){
        if (!isInteractionFinished) return; //drop input
        else isInteracting = false;

        if(!isMoving){

            //interact with something if it exists
            if (Input.GetKeyDown(KeyCode.Z) && interactable != null)
            {
                interact();
                return;
            }

            input.x=Input.GetAxisRaw("Horizontal");
            input.y=Input.GetAxisRaw("Vertical");

            if (input.x != 0)
            {
                input.y = 0;
                interactable = null; //clear interaction; will be set again by trigger if one is available
                direction = input.x < 0 ? Direction.LEFT : Direction.RIGHT;
            }
            else if (input.y != 0)
            {
                input.x = 0;
                interactable = null; //clear interaction; will be set again by trigger if one is available
                direction = input.y < 0 ? Direction.DOWN : Direction.UP;
            }

            if(input != Vector2.zero){
               
                animator.SetFloat("moveX",input.x);
                animator.SetFloat("moveY",input.y);
                var targetPos = transform.position;
                targetPos.x += input.x;
                targetPos.y += input.y;
                if(isWalkable(targetPos)){
                    StartCoroutine(Move(targetPos));
                }
                
            }
        }

        //only run while pressing key
        if (Input.GetKeyDown(KeyCode.X))
            isRunning = true;
        else if (Input.GetKeyUp(KeyCode.X))
            isRunning = false;

        Debug.Log(">"+ isRunning);
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isMoving", isMoving);
    }
    //Inumerator is used to do something over a period of time -  move current to target pos over a period of time

    IEnumerator Move(Vector3 targetPos){
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


    // checks if the target Pos contains objects with SolidObjectsLayer, 0.2f is the offset radius
    private bool isWalkable(Vector3 targetPos){
        if(Physics2D.OverlapCircle(targetPos,0.2f,solidObjectsLayer)!=null){
            return false;
        }
        return true;
    }

    private void CheckForPokemons(){
        if(Physics2D.OverlapCircle(transform.position,0.2f,grass)!=null){
            Debug.Log("xdds");
            if(UnityEngine.Random.Range(1,101)<=10){
                    Debug.Log("Pokemon Found");
            }
        }
    }

    private void interact()
    {
        isInteracting = true;
        isInteractionFinished = false;

        switch (interactable.tag)
        {
            case "NPC":
                interactable.GetComponent<NPC>().Interact(this);
                break;
        }
        
    }

    public void endInteraction()
    {
        if (!this.audioSource.isPlaying) this.audioSource.Play();
        this.isInteractionFinished = true;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.tag == "NPC")
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
                location == Direction.DOWN ? 1 : 3
            );

            // directly facing the NPC
            if (location == direction)
                interactable = other.gameObject;
        }
    }

    private Direction? CollisionFrom(Vector3 self, Vector3 other)
    {
        if (self.y < other.y) return Direction.UP;
        else if (self.y > other.y) return Direction.DOWN;
        else if (self.x > other.x) return Direction.LEFT;
        else if (self.x < other.x) return Direction.RIGHT;
        else return null;
    }
}
