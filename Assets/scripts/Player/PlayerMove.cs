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
    public AudioSource audio;

    private bool isMoving;
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

    private void Awake(){
        animator = GetComponent<Animator>();
    }

    private void Update(){
        if (isInteracting && !isInteractionFinished)
        {
            return;
        }

        if(!isMoving){

            //interact with something if it exists
            if (Input.GetKeyDown(KeyCode.Z) && interactable != null)
            {
                if (isInteractionFinished && isInteracting)
                {
                    isInteracting = false;
                }
                else
                {
                    interact();
                    return;
                }  
            }

            input.x=Input.GetAxisRaw("Horizontal");
            input.y=Input.GetAxisRaw("Vertical");

            if (input.x != 0)
            {
                input.y = 0;
                direction = input.x < 0 ? Direction.LEFT : Direction.RIGHT;
            }
            else if (input.y != 0)
            {
                input.x = 0;
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

        //animator.SetBool("isMoving",isMoving);
    }
    //Inumerator is used to do something over a period of time -  move current to target pos over a period of time

    IEnumerator Move(Vector3 targetPos){
        isMoving=true;
        while((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(transform.position,targetPos,moveSpeed* Time.deltaTime);
            
            yield return null;
        }
        transform.position= targetPos;
        isMoving=false;
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
        this.audio.Play();
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
            if (CollisionFrom(new Vector3(xdiff, ydiff, 0), otherPos) == direction)
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
