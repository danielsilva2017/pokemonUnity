using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using static Utils;

public abstract class NPC : MonoBehaviour
{
    public OverworldDialog chatbox;
    public BoxCollider2D boxCollider2D;

    public string Name { get; set; }
    protected PlayerLogic PlayerLogic { get; set; }
    protected bool IsInteracting { get; set; }
    protected string[] Dialogue { get; set; }
    protected string[] PostDialogue { get; set; }
    protected int RoamRadius { get; set; }
    protected LayerMask SolidLayer { get; set; }
    protected LayerMask WaterLayer { get; set; }
    protected LayerMask JumpLayer { get; set; }
    protected Animator Animator { get; set; }
    protected Direction Direction { get; set; }
    protected Vector3 OriginalPosition { get; set; }

    private string next;
    private int dialogueIndex;
    private readonly Direction[] directions = { Direction.Up, Direction.Down, Direction.Left, Direction.Right };

    public int OverworldNpcID { get; set; }
    public Overworld Overworld { get; set; }
    public bool IsDefeated { get; set; }
    public bool IsBusy { get; set; }
    public bool IsMoving { get; set; }
    public bool IsIdling { get; set; }

    // Update is called once per frame
    void Update()
    {

        if (IsBusy || PlayerLogic.IsUsingMenu) return;

        if (!IsInteracting && !IsIdling)
        {
            StartCoroutine(IdleBehaviour());
            return;
        }

        if (IsInteracting && Input.GetKeyDown(KeyCode.Z))
        {
            // drop input
            if (chatbox.IsBusy) return;

            if ((next = NextDialogue()) != null)
            {
                chatbox.PrintWithSound(next);
            } 
            else
            {
                chatbox.Hide();
                IsInteracting = false;
                dialogueIndex = 0;
                if (!IsDefeated) StartCoroutine(ActionRunner());
                else PlayerLogic.EndInteraction();
            }
        }
    }

    public void FaceDirection(Direction direction)
    {
        if (Animator == null) return;

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

    protected IEnumerator Move(Vector3 target)
    {
        if (gameObject == PlayerLogic.Interactable) // drop the potential input
            PlayerLogic.Interactable = null;

        IsMoving = true;
        IsBusy = true;
        Animator.SetBool("isMoving", IsMoving);
        FaceDirection(Direction);

        var target2D = MakeVector2(target);
        var position2D = MakeVector2(transform.position);

        while ((target2D - position2D).sqrMagnitude > Mathf.Epsilon)
        {
            var step = Vector2.MoveTowards(
                position2D,
                target2D,
                PlayerLogic.moveSpeed / 2f * Time.deltaTime
            );

            transform.position = new Vector3(step.x, step.y, transform.position.z);
            position2D = MakeVector2(transform.position);

            if (gameObject == PlayerLogic.Interactable) // drop the potential input
                PlayerLogic.Interactable = null;

            yield return null;
        }

        transform.position = target;
        UpdateSpriteZIndex();
        IsMoving = false;
        IsBusy = false;
        Animator.SetBool("isMoving", IsMoving);
    }

    private string NextDialogue()
    {
        var dialogueList = IsDefeated ? PostDialogue : Dialogue;
        return dialogueIndex < dialogueList.Length ? dialogueList[dialogueIndex++] : null;
    }

    public void Interact(bool runOnInteractionStart = true)
    {
        if (runOnInteractionStart) OnInteractionStart();
        chatbox.Show();
        if (runOnInteractionStart) chatbox.PrintWithSound(NextDialogue());
        else chatbox.PrintSilent(NextDialogue());
        IsInteracting = true;
    }

    private IEnumerator ActionRunner()
    {
        IsBusy = true;
        yield return DoAction();
        IsBusy = false;
        PlayerLogic.EndInteraction();
    }

    private IEnumerator IdleBehaviour()
    {
        IsIdling = true;

        if (RoamRadius > 0 && !PlayerLogic.IsBusy && PlayerLogic.IsInteractionFinished)
        {
            IsBusy = true;
            var randomDirection = RandomElement(directions);
            var target = GetMovementTarget(transform.position, randomDirection);

            var xdiff = Math.Abs(OriginalPosition.x - target.x);
            var ydiff = Math.Abs(OriginalPosition.y - target.y);

            if (xdiff <= RoamRadius && ydiff <= RoamRadius && IsWalkable(target) && !Overworld.IsTileClaimed(target))
            {
                Direction = randomDirection;
                Overworld.ClaimTile(this, target);
                yield return Move(target);
            }

            IsBusy = false;
            OnIdle();
            yield return new WaitForSeconds(RandomFloat(0.5f, 4f));
        }

        IsIdling = false;
    }

    protected void UpdateSpriteZIndex()
    {
        var player = PlayerLogic.transform.position;
        transform.position = new Vector3(
            transform.position.x,
            transform.position.y,
            player.y > transform.position.y ? player.z - 1 : player.z + 1
        );
    }

    protected virtual bool IsWalkable(Vector3 target)
    {
        return !PositionIsLayer(target, SolidLayer) &&
               !PositionIsLayer(target, WaterLayer) &&
               !PositionIsLayer(target, JumpLayer);
    }

    public virtual void NotifyPlayerMoved() { }
    
    protected virtual void OnIdle() { }

    protected virtual void OnInteractionStart() { }

    protected virtual IEnumerator DoAction() { yield break; }
}
