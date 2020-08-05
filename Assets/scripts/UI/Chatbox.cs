using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class Chatbox : MonoBehaviour
{
    public Font font;
    public AudioSource audioSource;

    private SpriteRenderer spriteRenderer;
    private Text[] textObjects; // chatbox can have up to 2 lines of text
    private GameObject canvasObject;
    private string[] lines;
    private readonly int maxCharsPerLine = 30;
    private readonly int framesPerChar = 10;
    private bool startShowingText;
    private bool isWritingText;

    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.enabled = false;
        textObjects = new Text[2];
        startShowingText = false;
        isWritingText = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (startShowingText)
        {
            StartCoroutine(PrintChars());
            startShowingText = false;
        }
    }

    private Vector3 ScreenPosition()
    {
        var pos = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width/2, 35));
        return new Vector3(pos.x, pos.y, 4);
    }

    private string[] GetTextLines(string text)
    { 
        var taglessText = Regex.Replace(text, "(<.*?>|</.*>)", "");

        if (text.Length <= maxCharsPerLine) return new string[] { text };

        //tags hopefully do not have spaces inside them
        string[] words = text.Split(' ');
        string[] taglessWords = taglessText.Split(' ');

        int newlineIndex = taglessWords[0].Length;
        for (var i=1; i<taglessWords.Length; i++)
        {
            if (newlineIndex + taglessWords[i].Length + 1 > maxCharsPerLine) break;
            else newlineIndex += words[i].Length + 1;
        }

        return new string[] { text.Substring(0, newlineIndex), text.Substring(newlineIndex).TrimStart(' ') };
    }

    public void Show()
    {
        spriteRenderer.enabled = true;

        transform.position = ScreenPosition();

        // Create Canvas GameObject.
        canvasObject = new GameObject
        {
            name = "Canvas"
        };
        canvasObject.AddComponent<Canvas>();
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // Get canvas from the GameObject.
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        for (var i=0; i < textObjects.Length; i++) {
            // Create the Text GameObject.
            GameObject textHolder = new GameObject();
            textHolder.transform.parent = canvasObject.transform;
            textObjects[i] = textHolder.AddComponent<Text>();

            // Set Text component properties.
            textObjects[i].font = font;
            textObjects[i].color = Color.black;
            textObjects[i].fontSize = 25;
            textObjects[i].alignment = TextAnchor.MiddleLeft;

            // Provide Text position and size using RectTransform.
            RectTransform rectTransform;
            rectTransform = textObjects[i].GetComponent<RectTransform>();
            // position relative to player (x,y,z)
            rectTransform.localPosition = new Vector3(0, -155 - 35*i, 0);
            rectTransform.sizeDelta = new Vector2(500, 200);
            // expected resolution: 457 x 257
        }
    }

    public void ShowTextSilent(string chatText)
    {
        lines = GetTextLines(chatText);
        startShowingText = true;
    }

    public void ShowText(string chatText)
    {
        audioSource.Play();
        lines = GetTextLines(chatText);
        startShowingText = true;
    }

    public void Hide()
    {
        Destroy(canvasObject);
        spriteRenderer.enabled = false;
    }

    IEnumerator PrintChars()
    {
        isWritingText = true;
        var lastCheckedIndex = 0;
        var tagOpenFound = false;
        var tagsClosedFound = 0;

        //clear this line
        textObjects[1].text = "";

        for (var line=0; line<lines.Length; line++)
        {
            Debug.Log(line);
            var maxLength = lines[line].Length * framesPerChar;
            for (var i=1; i<maxLength; i++)
            {
                var actualIndex = i / framesPerChar;

                //colored text "support"
                if (actualIndex != lastCheckedIndex && lines[line][actualIndex] == '<') tagOpenFound = true;
                else if (actualIndex != lastCheckedIndex && lines[line][actualIndex] == '>') tagsClosedFound++;

                lastCheckedIndex = actualIndex;

                if (tagOpenFound && tagsClosedFound < 2) continue;
                else
                {
                    tagOpenFound = false;
                    tagsClosedFound = 0;
                }

                if (i % framesPerChar == 0) textObjects[line].text = lines[line].Substring(0, actualIndex+1);
                yield return null;
            }          
        }

        isWritingText = false;
    }

    public bool IsBusy()
    {
        return isWritingText;
    }
}
