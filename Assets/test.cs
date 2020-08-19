using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
public class test : MonoBehaviour
{
    // Start is called before the first frame update
    public Text PokeName;
    public Text Lvl; 
    public Text totalexp;
    public Text nextLvl;
    public Text currentHealth;
    public Text maxHealth;
    public Text attack;
    public Text defense; 
    public Text spAttack;
    public Text spDefense;
    public Text speed;
    public Text move1Name;
    public Text move1currentPP;
    public Text move1maxPP;
    public Text move2Name;
    public Text move2currentPP;
    public Text move2maxPP;
    public Text move3Name;
    public Text move3currentPP;
    public Text move3maxPP;
    public Text move4Name;
    public Text move4currentPP;
    public Text move4maxPP;
    void Start()
    {
        PokeName.text = "bulbasaur";
        Lvl.text = "5";
        totalexp.text = "100000";
        nextLvl.text = "20000";
        currentHealth.text="150";
        maxHealth.text="200";
        attack.text="200";
        defense.text="400";
        spAttack.text="60";
        spDefense.text="70";
        speed.text="80";
        //Move 1 stats
        move1Name.text="Ember";
        move1currentPP.text="1";
        move1maxPP.text="1";
        //Move 2 stats
        move2Name.text="Bubble";
        move2currentPP.text="2";
        move2maxPP.text="2";
        //Move 3 stats
        move3Name.text="Fly";
        move3currentPP.text="3";
        move3maxPP.text="3";
        //Move 4 stats
        move4Name.text="Surf";
        move4currentPP.text="4";
        move4maxPP.text="4";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
