using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Chatbox : MonoBehaviour
{
    public Font font;
    public AudioSource audioSource;

    private SpriteRenderer spriteRenderer;
    private Text[] textObjects; // chatbox can have up to 2 lines of text
    private GameObject canvas;
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

    private Vector3 screenPosition()
    {
        var pos = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width/2, 35));
        return new Vector3(pos.x, pos.y, 4);
    }

    private string[] getTextLines(string text)
    {
        if (text.Length <= maxCharsPerLine) return new string[] { text };

        string[] words = text.Split(' ');
        int newlineIndex = words[0].Length;
        for (var i=1; i<words.Length; i++)
        {
            var toAdd = words[i].Length + 1;
            if (newlineIndex + toAdd > maxCharsPerLine) break;
            else newlineIndex += toAdd;
        }

        return new string[] { text.Substring(0, newlineIndex), text.Substring(newlineIndex).TrimStart(' ') };
    }

    public void Show()
    {
        spriteRenderer.enabled = true;

        transform.position = screenPosition();

        // Create Canvas GameObject.
        this.canvas = new GameObject
        {
            name = "Canvas"
        };
        this.canvas.AddComponent<Canvas>();
        this.canvas.AddComponent<CanvasScaler>();
        this.canvas.AddComponent<GraphicRaycaster>();

        // Get canvas from the GameObject.
        Canvas canvas;
        canvas = this.canvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        for (var i=0; i < textObjects.Length; i++) {
            // Create the Text GameObject.
            GameObject textHolder = new GameObject();
            textHolder.transform.parent = this.canvas.transform;
            textObjects[i] = textHolder.AddComponent<Text>();

            // Set Text component properties.
            textObjects[i].font = font;
            textObjects[i].color = Color.black;
            textObjects[i].fontSize = 15;
            textObjects[i].alignment = TextAnchor.MiddleLeft;

            // Provide Text position and size using RectTransform.
            RectTransform rectTransform;
            rectTransform = textObjects[i].GetComponent<RectTransform>();
            // position relative to player (x,y,z)
            rectTransform.localPosition = new Vector3(108, -82 - 20 * i, 0);
            rectTransform.sizeDelta = new Vector2(500, 200);
            // expected resolution: 457 x 257
        }
    }

    public void ShowTextSilent(string chatText)
    {
        lines = getTextLines(chatText);
        startShowingText = true;
    }

    public void ShowText(string chatText)
    {
        audioSource.Play();
        lines = getTextLines(chatText);
        startShowingText = true;
    }

    public void Hide()
    {
        Destroy(canvas);
        spriteRenderer.enabled = false;
    }

    IEnumerator PrintChars()
    {
        isWritingText = true;

        //clear this line
        textObjects[1].text = "";

        for (var line=0; line<lines.Length; line++)
        {
            var maxLength = lines[line].Length * framesPerChar;
            for (var i=1; i<=maxLength; i++)
            {
                if (i % framesPerChar == 0) textObjects[line].text = lines[line].Substring(0, i/framesPerChar);
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
