using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    // Start is called before the first frame update
    public float moveSpeed;
    public LayerMask  solidObjectsLayer;
    private bool isMoving;

    private Vector2 input;

    private Animator animator;

    private void Awake(){
        Debug.Log("here");
        animator = GetComponent<Animator>();
    }
    private void Update(){
        if(!isMoving){
            input.x=Input.GetAxisRaw("Horizontal");
            input.y=Input.GetAxisRaw("Vertical");
            Debug.Log(Input.GetAxisRaw("Horizontal"));

            if(input.x !=0)input.y=0;

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
    }


    // checks if the target Pos contains objects with SolidObjectsLayer, 0.2f is the offset radius
    private bool isWalkable(Vector3 targetPos){
        if(Physics2D.OverlapCircle(targetPos,0.2f,solidObjectsLayer)!=null){
            return false;
        }
        return true;
    }
}
