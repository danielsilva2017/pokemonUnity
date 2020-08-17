using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarterSelection : MonoBehaviour
{
    public OverworldDialog chatbox;

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;
        chatbox.Show();
        chatbox.PrintSilent("Which Pokemon will you choose?");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
