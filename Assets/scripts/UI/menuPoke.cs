using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static Utils;

public class MenuPoke: MonoBehaviour, ITransitionable
{
    public GameObject menu;
    public PlayerLogic playerLogic;
    public OverworldParty party;
    public SpriteRenderer transition;
    public AudioSource buttonPress;
    public Text pokemon;
    public Text bag;
    public Text save;

    private Text[] texts = new Text[3];
    private int selectionIndex;

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
        if (!IsBusy)
            OptionPicker();
    }

    public bool Toggle()
    {
        if (menu.activeInHierarchy) Hide();
        else Show();

        return menu.activeInHierarchy;
    }

    public void SetSelectionIndex(int index)
    {
        selectionIndex = index;
        for (var i = 0; i < texts.Length; i++)
        {
            if (i == index) AddHighlight(texts[i]);
            else RemoveHighlight(texts[i]);
        }
    }

    private void OptionPicker()
    {
        var oldIndex = selectionIndex;

        if (Input.GetKeyDown(KeyCode.UpArrow)) selectionIndex = selectionIndex == 0 ? 0 : selectionIndex - 1;
        if (Input.GetKeyDown(KeyCode.DownArrow)) selectionIndex = selectionIndex == 2 ? 2 : selectionIndex + 1;

        if (oldIndex != selectionIndex)
        {
            AddHighlight(texts[selectionIndex]);
            RemoveHighlight(texts[oldIndex]);
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            IsBusy = true;

            switch (selectionIndex)
            {
                case 0:
                    playerLogic.playerUI.MenuTransition(menu, party, party.gameObject);
                    break;
                case 1:
                    break;
                case 2:
                    break;
            }
        }
    }

    private void AddHighlight(Text text)
    {
        text.color = new Color(1, 0, 0, 1);
    }

    private void RemoveHighlight(Text text)
    {
        text.color = new Color(0, 0, 0, 1);
    }

    public void Show()
    {
        menu.SetActive(true);
    }

    public void Hide()
    {
        menu.SetActive(false);
    }

    public void Init()
    {
        IsBusy = false;
    }
}
