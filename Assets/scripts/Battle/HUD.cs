using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class HUD : MonoBehaviour
{
    public GameObject introEffect;
    public Text allyName;
    public Text allyLevel;
    public Text allyHealth;
    public Text enemyName;
    public Text enemyLevel;
    public Image allyHUD;
    public Image enemyHUD;
    public SpriteRenderer allyStatus;
    public SpriteRenderer enemyStatus; 
    public GameObject allyHealthBar;
    public GameObject allyExpBar;
    public GameObject enemyHealthBar;
    public GameObject allyHealthBarFull;
    public GameObject enemyHealthBarFull;

    private Pokemon ally;
    private Pokemon enemy;
    private Sprite[] statuses; // psn, bpsn, slp, par, frz, brn, fnt
    private int lastAllyHealth;
    private float lastAllyHealthBar;
    private float lastAllyExpBar;
    private float lastEnemyHealthBar;

    private readonly float introSpeed = 0.8f;
    private readonly float updateSpeed = 2f;

    // Start is called before the first frame update
    void Start()
    {
        statuses = Resources.LoadAll<Sprite>("Images/status");
    }

    public void Init(Pokemon ally, Pokemon enemy)
    {
        this.ally = ally; ally.Health -= 5;
        this.enemy = enemy; enemy.Health -= 5;
        allyName.text = Bold(GetGenderedName(ally));
        allyLevel.text = Bold(ally.Level.ToString());
        enemyName.text = Bold(GetGenderedName(enemy));
        enemyLevel.text = Bold(enemy.Level.ToString());

        lastAllyHealth = ally.Health;
        lastAllyHealthBar = ((float) ally.Health) / ally.MaxHealth;
        lastAllyExpBar = ((float) ally.Experience - ally.CurLevelExp) / (ally.NextLevelExp - ally.CurLevelExp);
        lastEnemyHealthBar = ((float)enemy.Health) / enemy.MaxHealth;

        StartCoroutine(UpdateAllyHealth(true));
        StartCoroutine(UpdateAllyHealthBar(true));
        StartCoroutine(UpdateAllyExpBar(true));
        StartCoroutine(UpdateEnemyHealthBar(true));
        UpdateStatuses();
    }

    private string GetGenderedName(Pokemon pokemon)
    {
        var genderChar = pokemon.Gender == Gender.Male ? "<color=blue>♂</color>" : pokemon.Gender == Gender.Female ? "<color=magenta>♀</color>" : "";
        return $"{pokemon.Name}{genderChar}";
    }

    public IEnumerator IntroEffect()
    {
        HideAll();

        var frames = 1/introSpeed * 100;
        var introSprite = introEffect.GetComponent<SpriteRenderer>();

        for (var i=frames; i>=0; i--)
        {
            introSprite.color = new Color(introSprite.color.r, introSprite.color.g, introSprite.color.b, i / frames);
            yield return null;
        }

        Destroy(introEffect);

        ShowAll();
    }

    private IEnumerator UpdateBar(GameObject bar, int value, int max, float lastValue, bool immediate = false)
    {
        if (immediate)
        {
            bar.transform.localScale = new Vector3(((float)value) / max, 1f, bar.transform.localScale.z);
            yield break;
        }

        var frames = 1 / updateSpeed * 100;
        var diff = ((float)value / max) - lastValue; // scale.x diff

        for (var i = 0; i <= frames; i++)
        {
            bar.transform.localScale = new Vector3(lastValue + diff * i / frames, 1f, bar.transform.localScale.z);
            yield return null;
        }

        // ensure correct number is shown at the end
        bar.transform.localScale = new Vector3(((float)value) / max, 1f, bar.transform.localScale.z);
    }

    private string Bold(string text)
    {
        return $"<b>{text}</b>";
    }

    public IEnumerator UpdateAllyHealthBar(bool immediate = false)
    {
        yield return UpdateBar(allyHealthBar, ally.Health, ally.MaxHealth, lastAllyHealthBar, immediate);
        lastAllyHealthBar = ((float)ally.Health) / ally.MaxHealth;
    }

    public IEnumerator UpdateEnemyHealthBar(bool immediate = false)
    {
        yield return UpdateBar(enemyHealthBar, enemy.Health, enemy.MaxHealth, lastEnemyHealthBar, immediate);
        lastEnemyHealthBar = ((float)enemy.Health) / enemy.MaxHealth;
    }

    public IEnumerator UpdateAllyExpBar(bool immediate = false)
    {
        yield return UpdateBar(allyExpBar, ally.Experience - ally.CurLevelExp, ally.NextLevelExp - ally.CurLevelExp, lastAllyExpBar, immediate);
        lastAllyExpBar = ((float)ally.Experience - ally.CurLevelExp) / (ally.NextLevelExp - ally.CurLevelExp);
    }

    // uses dummy values to simulate a level up without doing exp calculations
    public IEnumerator FillAllyExpBar(bool immediate = false)
    {
        yield return UpdateBar(allyExpBar, 1, 1, lastAllyExpBar, immediate);
        allyLevel.text = Bold(ally.Level.ToString());
        yield return null;
        lastAllyExpBar = 0f;
    }

    public IEnumerator UpdateAllyHealth(bool immediate = false)
    {
        if (immediate)
        {
            allyHealth.text = Bold($"{ally.Health}/{ally.MaxHealth}");
            lastAllyHealth = ally.Health;
            yield break;
        }

        var frames = 1 / updateSpeed * 100;
        var diff = ally.Health - lastAllyHealth;

        for (var i=0; i<=frames; i++)
        {
            allyHealth.text = Bold($"{Math.Max(0, Mathf.FloorToInt(lastAllyHealth + diff * i / frames))}/{ally.MaxHealth}");
            yield return null;
        }

        // ensure correct number is shown at the end
        allyHealth.text = Bold($"{ally.Health}/{ally.MaxHealth}");

        lastAllyHealth = ally.Health;
    }

    public void UpdateStatuses()
    {
        UpdateStatus(ally, allyStatus);
        UpdateStatus(enemy, enemyStatus);
    }

    private void UpdateStatus(Pokemon pokemon, SpriteRenderer statusHUD)
    {
        if (pokemon.Status == Status.None)
        {
            statusHUD.sprite = null;
            return;
        }

        statusHUD.sprite = statuses[(int) pokemon.Status];
    }

    private void ShowAll()
    {
        allyName.enabled = true;
        allyLevel.enabled = true;
        allyHealth.enabled = true;
        enemyName.enabled = true;
        enemyLevel.enabled = true;
        allyHUD.enabled = true;
        enemyHUD.enabled = true;
        allyHealthBar.SetActive(true);
        allyExpBar.SetActive(true);
        enemyHealthBar.SetActive(true);
        allyHealthBarFull.SetActive(true);
        enemyHealthBarFull.SetActive(true);
    }

    private void HideAll()
    {
        allyName.enabled = false;
        allyLevel.enabled = false;
        allyHealth.enabled = false;
        enemyName.enabled = false;
        enemyLevel.enabled = false;
        allyHUD.enabled = false;
        enemyHUD.enabled = false;
        allyHealthBar.SetActive(false);
        allyExpBar.SetActive(false);
        enemyHealthBar.SetActive(false);
        allyHealthBarFull.SetActive(false);
        enemyHealthBarFull.SetActive(false);
    }
}
