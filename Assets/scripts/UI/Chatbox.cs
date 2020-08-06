using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Chatbox : MonoBehaviour
{
    public Font font;
    public AudioSource audioSource;

    private SpriteRenderer spriteRenderer;
    private Text textObject;
    private GameObject canvasObject;
    private string textToDisplay;
    private readonly int maxCharsPerLine = 30;
    private readonly int framesPerChar = 2;
    private bool startShowingText;
    private bool isWritingText;

    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.enabled = false;
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
        canvasObject.transform.position = new Vector3(0, 0, 0);

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        //scaler.referenceResolution = new Vector2(457, 257);

        // Get canvas from the GameObject.
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Create the Text GameObject.
        GameObject textHolder = new GameObject();
        textHolder.transform.parent = canvasObject.transform;
        textObject = textHolder.AddComponent<Text>();

        // Set Text component properties.
        textObject.font = font;
        textObject.color = Color.black;
        textObject.fontSize = 25;
        textObject.alignment = TextAnchor.UpperLeft;
        textObject.lineSpacing = 1.1f;

        // Provide Text position and size using RectTransform.
        RectTransform rectTransform;
        rectTransform = textObject.GetComponent<RectTransform>();
        // i dont care anymore
        rectTransform.localPosition = new Vector3(0, (float) (-77 * Math.Log10(1.2 * (Screen.height - 117))), 0);
        rectTransform.sizeDelta = new Vector2(480,80);
    }

    public void ShowTextSilent(string chatText)
    {
        textToDisplay = chatText;
        startShowingText = true;
    }

    public void ShowText(string chatText)
    {
        audioSource.Play();
        textToDisplay = chatText;
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

        //clear
        textObject.text = "";

        var maxLength = textToDisplay.Length * framesPerChar;
        for (var i=1; i<maxLength; i++)
        {
            var actualIndex = i / framesPerChar;

            //colored text "support"
            if (actualIndex != lastCheckedIndex && textToDisplay[actualIndex] == '<') tagOpenFound = true;
            else if (actualIndex != lastCheckedIndex && textToDisplay[actualIndex] == '>') tagsClosedFound++;

            lastCheckedIndex = actualIndex;

            if (tagOpenFound && tagsClosedFound < 2) continue;
            else
            {
                tagOpenFound = false;
                tagsClosedFound = 0;
            }

            if (i % framesPerChar == 0) textObject.text = textToDisplay.Substring(0, actualIndex+1);
            yield return null;
        }          

        isWritingText = false;
    }

    public bool IsBusy()
    {
        return isWritingText;
    }
}
