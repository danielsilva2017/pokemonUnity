using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarterSelection : MonoBehaviour
{
    private Chatbox chatbox;

    // Start is called before the first frame update
    void Start()
    {
        chatbox = GameObject.Find("Chatbox").GetComponent<Chatbox>();
        chatbox.Show();
        chatbox.ShowTextSilent("Which Pokemon will you choose?");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
