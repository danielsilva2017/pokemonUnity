using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public interface IDialog
{
    IEnumerator Print(string message, bool immediate = false);
}

public class ConfirmationBox
{
    public Text[] options;
    public Image[] arrows;

    public ConfirmationBox(GameObject gameObject)
    {
        options = new Text[]{ gameObject.transform.GetChild(0).GetComponent<Text>(),
                              gameObject.transform.GetChild(1).GetComponent<Text>() };
        arrows = new Image[]{ gameObject.transform.GetChild(0).GetChild(0).GetComponent<Image>(),
                              gameObject.transform.GetChild(1).GetChild(0).GetComponent<Image>() };
    }

    public void CursorYes()
    {
        options[0].color = Color.blue;
        options[1].color = Color.black;
        arrows[0].enabled = true;
        arrows[1].enabled = false;
    }

    public void CursorNo()
    {
        options[1].color = Color.blue;
        options[0].color = Color.black;
        arrows[1].enabled = true;
        arrows[0].enabled = false;
    }
}

public class OverworldDialog : MonoBehaviour, IDialog
{
    public GameObject chatbox;
    public GameObject confirmationObject;
    public Text chatText;
    public AudioSource buttonPress;

    private readonly int framesPerChar = 2;

    public bool IsBusy { get; set; }
    public ConfirmationBox ConfirmationBox { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        ConfirmationBox = new ConfirmationBox(confirmationObject);
        Hide();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void PrintSilent(string message, bool immediate = false)
    {
        StartCoroutine(Print(message, immediate));
    }

    public void PrintWithSound(string message, bool immediate = false)
    {
        buttonPress.Play();
        StartCoroutine(Print(message, immediate));
    }

    public void Show()
    {
        chatbox.SetActive(true);
    }

    public void Hide()
    {
        chatbox.SetActive(false);
    }

    public IEnumerator Print(string message, bool immediate = false)
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
