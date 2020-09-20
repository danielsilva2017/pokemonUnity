using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using static Utils;

public class HUD : MonoBehaviour
{
    public BattleAnimations anims;
    public GameObject introEffect;
    public Text allyName;
    public Text allyLevel;
    public Text allyHealth;
    public Text enemyName;
    public Text enemyLevel;
    public Image allyHUD;
    public Image enemyHUD;
    public Image allyStatus;
    public Image enemyStatus; 
    public GameObject allyHealthBar;
    public GameObject allyExpBar;
    public GameObject enemyHealthBar;
    public GameObject allyHealthBarFull;
    public GameObject enemyHealthBarFull;
    public SpriteRenderer transition;
    public AudioSource expUpSound;

    private Pokemon ally;
    private Pokemon enemy;
    private Sprite[] statuses; // psn, bpsn, slp, par, frz, brn, fnt
    private int lastAllyHealth;
    private float lastAllyHealthBar;
    private float lastAllyExpBar;
    private float lastEnemyHealthBar;

    private readonly float introSpeed = 0.8f;
    private readonly float updateSpeed = 120f; // amount of frames required to fill/empty a bar
    //private readonly float transitionSpeed = 10f;

    // Start is called before the first frame update
    void Start()
    {
        statuses = Resources.LoadAll<Sprite>("Images/status");
    }

    public void Init(Pokemon ally, Pokemon enemy)
    {
        transition.gameObject.SetActive(false);
        InitAlly(ally);
        InitEnemy(enemy);
    }

    /// <summary>
    /// Notify the HUD that a Pokemon was switched in.
    /// </summary>
    public void NotifySwitch(Pokemon switchedIn)
    {
        if (switchedIn.IsAlly)
            InitAlly(switchedIn);
        else
            InitEnemy(switchedIn);
    }

    /// <summary>
    /// Make all necessary preparations to display the ally's info.
    /// </summary>
    private void InitAlly(Pokemon ally)
    {
        this.ally = ally;
        allyName.text = Bold(GetGenderedName(ally));
        allyLevel.text = Bold(ally.Level.ToString());

        lastAllyHealth = ally.Health;
        lastAllyHealthBar = ((float)ally.Health) / ally.MaxHealth;
        lastAllyExpBar = ((float)ally.Experience - ally.CurLevelExp) / (ally.NextLevelExp - ally.CurLevelExp);

        StartCoroutine(UpdateAllyHealth(true));
        StartCoroutine(UpdateAllyExp(true));
        UpdateStatus(ally, allyStatus);
    }

    /// <summary>
    /// Make all necessary preparations to display the enemy's info.
    /// </summary>
    private void InitEnemy(Pokemon enemy)
    {
        this.enemy = enemy;
        enemyName.text = Bold(GetGenderedName(enemy));
        enemyLevel.text = Bold(enemy.Level.ToString());

        lastEnemyHealthBar = ((float)enemy.Health) / enemy.MaxHealth;

        StartCoroutine(UpdateEnemyHealth(true));
        UpdateStatus(enemy, enemyStatus);
    }

    private string GetGenderedName(Pokemon pokemon)
    {
        var genderChar = pokemon.Gender == Gender.Male ? "<color=blue>♂</color>" : pokemon.Gender == Gender.Female ? "<color=magenta>♀</color>" : "";
        return $"{pokemon.Name}{genderChar}";
    }

    public IEnumerator IntroEffect()
    {
        HideAllyHUD(); HideEnemyHUD();

        var frames = 1/introSpeed * 100;
        var introSprite = introEffect.GetComponent<SpriteRenderer>();

        for (var i=frames; i>=0; i--)
        {
            introSprite.color = new Color(introSprite.color.r, introSprite.color.g, introSprite.color.b, i / frames);
            yield return null;
        }

        Destroy(introEffect);
    }

    private IEnumerator UpdateBar(GameObject bar, int value, int max, float lastValue, bool immediate = false, bool displayDamageAnim = false)
    {
        if (immediate)
        {
            bar.transform.localScale = new Vector3(((float)value) / max, 1f, bar.transform.localScale.z);
            yield break;
        }

        var diff = ((float)value / max) - lastValue; // scale.x diff
        // stop when there's nothing to update - consider fp inaccuracies
        if (Math.Abs(diff) <= 0.0001f) yield break;

        if (displayDamageAnim && diff < 0) StartCoroutine(anims.TakeDamage(enemy));
        var frames = Math.Abs(diff) * updateSpeed;

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

    /// <summary>
    /// Updates the enemy's health bar.
    /// </summary>
    public IEnumerator UpdateEnemyHealth(bool immediate = false)
    {
        yield return UpdateBar(enemyHealthBar, enemy.Health, enemy.MaxHealth, lastEnemyHealthBar, immediate, true);
        lastEnemyHealthBar = ((float)enemy.Health) / enemy.MaxHealth;
        anims.UpdatePokemonAnimationSpeed(enemy);
    }

    /// <summary>
    /// Updates the ally's exp bar.
    /// </summary>
    public IEnumerator UpdateAllyExp(bool immediate = false)
    {
        expUpSound.Play();
        yield return UpdateBar(allyExpBar, ally.Experience - ally.CurLevelExp, ally.NextLevelExp - ally.CurLevelExp, lastAllyExpBar, immediate);
        if (!immediate) { for (var i = 0; i < 10; i++) yield return null; } // stall for extra sound duration
        expUpSound.Stop();
        lastAllyExpBar = ((float)ally.Experience - ally.CurLevelExp) / (ally.NextLevelExp - ally.CurLevelExp);
    }

    /// <summary>
    /// Fills the ally's exp bar. Uses dummy values to simulate a level up without doing exp calculations.
    /// </summary>
    public IEnumerator FillAllyExpBar(bool immediate = false)
    {
        expUpSound.Play();
        yield return UpdateBar(allyExpBar, 1, 1, lastAllyExpBar, immediate);
        expUpSound.Stop();
        allyLevel.text = Bold(ally.Level.ToString());
        yield return null;
        lastAllyExpBar = 0f;
    }

    /// <summary>
    /// Updates the ally's health bar and health display (e.g. 30/32).
    /// </summary>
    public IEnumerator UpdateAllyHealth(bool immediate = false)
    {
        if (immediate)
        {
            allyHealth.text = Bold($"{ally.Health}/{ally.MaxHealth}");
            lastAllyHealth = ally.Health;
            allyHealthBar.transform.localScale = new Vector3(((float)ally.Health) / ally.MaxHealth, 1f, allyHealthBar.transform.localScale.z);
            lastAllyHealthBar = ((float)ally.Health) / ally.MaxHealth;
            anims.UpdatePokemonAnimationSpeed(ally);
            yield break;
        }

        var numdiff = ally.Health - lastAllyHealth;
        // stop when there's nothing to update - consider fp inaccuracies
        if (Math.Abs(numdiff) <= 0.0001f) yield break;

        if (numdiff < 0) StartCoroutine(anims.TakeDamage(ally));
        var ratio = ((float)ally.Health) / ally.MaxHealth;
        var bardiff = ratio - lastAllyHealthBar; // scale.x diff
        var frames = Math.Abs(bardiff) * updateSpeed;

        for (var i = 0; i <= frames; i++)
        {
            allyHealth.text = Bold($"{Math.Max(0, Mathf.FloorToInt(lastAllyHealth + numdiff * i / frames))}/{ally.MaxHealth}");
            allyHealthBar.transform.localScale = new Vector3(lastAllyHealthBar + bardiff * i / frames, 1f, allyHealthBar.transform.localScale.z);
            yield return null;
        }

        // ensure correct numbers are shown at the end
        allyHealth.text = Bold($"{ally.Health}/{ally.MaxHealth}");
        allyHealthBar.transform.localScale = new Vector3(ratio, 1f, allyHealthBar.transform.localScale.z);

        lastAllyHealth = ally.Health;
        lastAllyHealthBar = ratio;
        anims.UpdatePokemonAnimationSpeed(ally);
    }

    /// <summary>
    /// Updates the ally and enemy's statuses.
    /// </summary>
    public void UpdateStatuses()
    {
        UpdateStatus(ally, allyStatus);
        UpdateStatus(enemy, enemyStatus);
    }

    private void UpdateStatus(Pokemon pokemon, Image statusHUD)
    {
        if (pokemon.Status == Status.None)
        {
            MakeInvisible(statusHUD);
            return;
        }

        MakeVisible(statusHUD);
        statusHUD.sprite = statuses[(int) pokemon.Status];
    }

    public IEnumerator ReturnToOverworld()
    {
        transition.gameObject.SetActive(true);
        MakeInvisible(transition);
        yield return FadeIn(transition, 20);
        SceneInfo.ReturnToOverworldFromBattle();
    }

    public IEnumerator FadeInTransition()
    {
        transition.gameObject.SetActive(true);
        yield return FadeIn(transition, 10);
        transition.gameObject.SetActive(false);
    }

    public IEnumerator FadeOutTransition()
    {
        transition.gameObject.SetActive(true);
        yield return FadeOut(transition, 10);
        transition.gameObject.SetActive(false);
    }

    public void ShowAllyHUD()
    {
        allyName.enabled = true;
        allyLevel.enabled = true;
        allyHealth.enabled = true;
        allyHUD.enabled = true;
        allyStatus.enabled = true;
        allyHealthBar.SetActive(true);
        allyExpBar.SetActive(true);
        allyHealthBarFull.SetActive(true);
    }

    public void ShowEnemyHUD()
    {
        enemyName.enabled = true;
        enemyLevel.enabled = true;
        enemyHUD.enabled = true;
        enemyStatus.enabled = true;
        enemyHealthBar.SetActive(true);
        enemyHealthBarFull.SetActive(true);
    }

    public void HideAllyHUD()
    {
        allyName.enabled = false;
        allyLevel.enabled = false;
        allyHealth.enabled = false;
        allyHUD.enabled = false;
        allyStatus.enabled = false;
        allyHealthBar.SetActive(false);
        allyExpBar.SetActive(false);
        allyHealthBarFull.SetActive(false);
    }

    public void HideEnemyHUD()
    {
        enemyName.enabled = false;
        enemyLevel.enabled = false;
        enemyHUD.enabled = false;
        enemyStatus.enabled = false;
        enemyHealthBar.SetActive(false);
        enemyHealthBarFull.SetActive(false);
    }
}
