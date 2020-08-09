using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    public GameObject allyHealthBar;
    public GameObject allyExpBar;
    public GameObject enemyHealthBar;
    public GameObject allyHealthBarFull;
    public GameObject enemyHealthBarFull;

    private Pokemon ally;
    private Pokemon enemy;
    private int lastAllyHealth;
    private float lastAllyHealthBar;
    private float lastAllyExpBar;
    private float lastEnemyHealthBar;

    private readonly float introSpeed = 0.8f;
    private readonly float updateSpeed = 2f;

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
        this.ally = ally; ally.Health -= 5;
        this.enemy = enemy; enemy.Health -= 5;
        allyName.text = Bold(ally.Name);
        allyLevel.text = Bold(ally.Level.ToString());
        enemyName.text = Bold(enemy.Name);
        enemyLevel.text = Bold(enemy.Level.ToString());

        lastAllyHealth = ally.Health;
        lastAllyHealthBar = ((float) ally.Health) / ally.MaxHealth;
        lastAllyExpBar = ((float)2) / 3;
        lastEnemyHealthBar = ((float)enemy.Health) / enemy.MaxHealth;

        StartCoroutine(UpdateAllyHealth(true));
        StartCoroutine(UpdateAllyHealthBar(true));
        StartCoroutine(UpdateAllyExpBar(true));
        StartCoroutine(UpdateEnemyHealthBar(true));
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
        yield return UpdateBar(allyExpBar, 2, 3, lastAllyExpBar, immediate);
        lastAllyExpBar = ((float)2) / 3;
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
            allyHealth.text = Bold($"{Mathf.FloorToInt(lastAllyHealth + diff * i / frames)}/{ally.MaxHealth}");
            yield return null;
        }

        // ensure correct number is shown at the end
        allyHealth.text = Bold($"{ally.Health}/{ally.MaxHealth}");

        lastAllyHealth = ally.Health;
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
