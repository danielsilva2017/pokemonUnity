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
    private int maxCharsPerLine = 30;
    private int framesPerChar = 10;
    private bool startShowingText;
    private bool isWritingText;

    // Start is called before the first frame update
    void Start()
    {
        this.spriteRenderer = this.GetComponent<SpriteRenderer>();
        this.spriteRenderer.enabled = false;
        this.textObjects = new Text[2];
        this.startShowingText = false;
        this.isWritingText = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (startShowingText)
        {
            StartCoroutine(PrintChars());
            this.startShowingText = false;
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
        this.spriteRenderer.enabled = true;

        transform.position = screenPosition();

        // Create Canvas GameObject.
        this.canvas = new GameObject();
        this.canvas.name = "Canvas";
        this.canvas.AddComponent<Canvas>();
        this.canvas.AddComponent<CanvasScaler>();
        this.canvas.AddComponent<GraphicRaycaster>();

        // Get canvas from the GameObject.
        Canvas canvas;
        canvas = this.canvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        for (var i=0; i < this.textObjects.Length; i++) {
            // Create the Text GameObject.
            GameObject textHolder = new GameObject();
            textHolder.transform.parent = this.canvas.transform;
            this.textObjects[i] = textHolder.AddComponent<Text>();

            // Set Text component properties.
            this.textObjects[i].font = font;
            this.textObjects[i].color = Color.black;
            this.textObjects[i].fontSize = 15;
            this.textObjects[i].alignment = TextAnchor.MiddleLeft;

            // Provide Text position and size using RectTransform.
            RectTransform rectTransform;
            rectTransform = this.textObjects[i].GetComponent<RectTransform>();
            // position relative to player (x,y,z)
            rectTransform.localPosition = new Vector3(108, -82 - 20*i, 0);
            rectTransform.sizeDelta = new Vector2(500, 200);
        }
    }

    public void ShowText(string chatText)
    {
        this.audioSource.Play();
        this.lines = getTextLines(chatText);
        this.startShowingText = true;
    }

    public void Hide()
    {
        Destroy(canvas);
        this.spriteRenderer.enabled = false;
    }

    IEnumerator PrintChars()
    {
        this.isWritingText = true;

        //clear this line
        this.textObjects[1].text = "";

        for (var line=0; line<this.lines.Length; line++)
        {
            var maxLength = this.lines[line].Length * this.framesPerChar;
            for (var i=1; i<=maxLength; i++)
            {
                if (i % this.framesPerChar == 0) this.textObjects[line].text = this.lines[line].Substring(0, i/this.framesPerChar);
                yield return null;
            }          
        }

        this.isWritingText = false;
    }

    public bool isBusy()
    {
        return this.isWritingText;
    }
}
