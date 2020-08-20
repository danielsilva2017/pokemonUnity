using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class menuPoke: MonoBehaviour
{
    public Image chatbox;
    public Text pokemon;
    public Text bag;
    public Text save;
    public Text[] texts= new Text[3];
    public int selectionIndex=0;
    public AudioSource buttonPress;

    private readonly int framesPerChar = 2;

    public bool IsBusy { get; set; }

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

    private void RemoveHighlight(Text text){
        text.color= new Color(0, 0, 0, 1);
    }
    public void Show()
    {
        chatbox.enabled = true;
      
    }

    public void Hide()
    {
        chatbox.enabled = false;
        pokemon.enabled = false;
        bag.enabled = false;
        save.enabled = false;
    }

    
}
