using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public Text allyName;
    public Text allyLevel;
    public GameObject allyHealth;
    public GameObject allyExp;
    public Text enemyName;
    public Text enemyLevel;
    public GameObject enemyHealth;

    private Pokemon ally;
    private Pokemon enemy;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Init(Pokemon ally, Pokemon enemy)
    {
        this.ally = ally;
        this.enemy = enemy;
        allyName.text = ally.Skeleton.pokemonName;
        allyLevel.text = ally.Level.ToString();
        enemyName.text = enemy.Skeleton.pokemonName;
        enemyLevel.text = enemy.Level.ToString();
        //set hp and exp bars too
    }

}
