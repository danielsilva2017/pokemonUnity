using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MenuPoke: MonoBehaviour
{
    public GameObject chatbox;
    public Text pokemon;
    public Text bag;
    public Text save;
    public AudioSource buttonPress;

    private Text[] texts= new Text[3];
    private int selectionIndex=0;
        
    // Start is called before the first frame update
    void Start()
    {   
        texts[0]=pokemon;
        texts[1]=bag;
        texts[2]=save;
        //Debug.Log(texts[0].text);
        AddHighlight(pokemon);
        Hide();
        
    }

    // Update is called once per frame
    void Update()
    {
        SwitchPicker();
    }

    public bool Toggle()
    {
        if (chatbox.activeInHierarchy) Hide();
        else Show();

        return chatbox.activeInHierarchy;
    }

    private void SwitchPicker()
    {
        var oldIndex = selectionIndex;

        if (Input.GetKeyDown(KeyCode.UpArrow)) selectionIndex = selectionIndex == 0 ? 0 : selectionIndex-1;
        if (Input.GetKeyDown(KeyCode.DownArrow)) selectionIndex =  selectionIndex == 2 ? 2 : selectionIndex+1;

        if (oldIndex != selectionIndex)
        {
            AddHighlight(texts[selectionIndex]);
            RemoveHighlight(texts[oldIndex]);
        }
    }

   private void AddHighlight(Text text)
    {
        text.color= new Color(1, 0, 0, 1);
    }

    private void RemoveHighlight(Text text)
    {
        text.color= new Color(0, 0, 0, 1);
    }

    private void Show()
    {
        chatbox.SetActive(true);
    }

    private void Hide()
    {
        chatbox.SetActive(false);
    }

    
}
