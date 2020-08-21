using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class OverworldDialog : MonoBehaviour
{
    public GameObject chatbox;
    public Text chatText;
    public AudioSource buttonPress;

    private readonly int framesPerChar = 2;

    public bool IsBusy { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        Hide();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void PrintSilent(string message, bool immediate = false)
    {
        StartCoroutine(PrintRoutine(message, immediate));
    }

    public void Print(string message, bool immediate = false)
    {
        buttonPress.Play();
        StartCoroutine(PrintRoutine(message, immediate));
    }

    public void Show()
    {
        chatbox.SetActive(true);
    }

    public void Hide()
    {
        chatbox.SetActive(false);
    }

    private IEnumerator PrintRoutine(string message, bool immediate)
    {
        if (immediate)
        {
            chatText.text = message;
            yield break;
        }

        IsBusy = true;

        var lastCheckedIndex = 0;
        var tagOpenFound = false;
        var tagsClosedFound = 0;

        //clear
        chatText.text = "";

        var maxLength = message.Length * framesPerChar;
        for (var i = 1; i < maxLength; i++)
        {
            var actualIndex = i / framesPerChar;

            //colored text "support"
            if (actualIndex != lastCheckedIndex && message[actualIndex] == '<') tagOpenFound = true;
            else if (actualIndex != lastCheckedIndex && message[actualIndex] == '>') tagsClosedFound++;

            lastCheckedIndex = actualIndex;

            if (tagOpenFound && tagsClosedFound < 2) continue;
            else
            {
                tagOpenFound = false;
                tagsClosedFound = 0;
            }

            if (i % framesPerChar == 0) chatText.text = message.Substring(0, actualIndex + 1);
            yield return null;
        }

        IsBusy = false;
    }
}
