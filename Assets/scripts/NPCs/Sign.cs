using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sign : NPC
{
    [TextArea] public string[] dialogue;
    public PlayerLogic playerLogic;

    void Start()
    {
        Direction = Direction.Down;
        PlayerLogic = playerLogic;
        Dialogue = dialogue;
    }
}
