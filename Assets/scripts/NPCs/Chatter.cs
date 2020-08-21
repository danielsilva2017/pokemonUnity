using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chatter : NPC
{
    [TextArea] public string[] dialogue;

    void Start()
    {
        Dialogue = dialogue;
    }

    protected override void DoAction() { }
}
