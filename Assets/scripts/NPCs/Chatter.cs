using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chatter : NPC
{
    public int roamRadius;
    public Direction startingDirection;
    public string animationPrefix;
    [TextArea] public string[] dialogue;
    public LayerMask solidLayer;
    public LayerMask waterLayer;
    public LayerMask jumpLayer;
    public PlayerLogic playerLogic;

    void Start()
    {
        RoamRadius = roamRadius;
        Direction = startingDirection;
        PlayerLogic = playerLogic;
        Dialogue = dialogue;
        SolidLayer = solidLayer;
        WaterLayer = waterLayer;
        JumpLayer = jumpLayer;
        OriginalPosition = transform.position;

        Animator = GetComponent<Animator>();
        Animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>($"Trainers/{animationPrefix}_ctrl");
        FaceDirection(Direction);
    }
}
