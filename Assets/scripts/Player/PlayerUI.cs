using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Utils;

public interface ITransitionable
{
    void Init();
    bool IsBusy { get; set; }
    GameObject GameObject { get; }
}

public class PlayerUI : MonoBehaviour
{
    public Camera mainCamera;
    public MenuPoke menu;
    public GameObject routeHeaders;
    public SpriteRenderer introEffect;
    public SpriteRenderer transition;
    public AudioSource wildBattleMusic;
    public AudioSource areaExitSound;
    public OverworldDialog chatbox;

    public Image RouteHeader { get; set; }
    public Text RouteName { get; set; }
    public RectTransform RouteShowPosition { get; set; }
    public RectTransform RouteHidePosition { get; set; }  

    // Start is called before the first frame update
    void Start()
    {
        RouteHeader = routeHeaders.transform.GetChild(0).GetComponent<Image>();
        RouteName = routeHeaders.transform.GetChild(0).GetChild(0).GetComponent<Text>();
        RouteShowPosition = routeHeaders.transform.GetChild(1).GetComponent<RectTransform>();
        RouteHidePosition = routeHeaders.transform.GetChild(2).GetComponent<RectTransform>();
        introEffect.gameObject.SetActive(false);
        //transition.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator EnterSceneTransition(Overworld overworld)
    {
        transition.gameObject.SetActive(true);
        transition.transform.position = mainCamera.ScreenToWorldPoint(new Vector3(
            transform.position.x + Screen.width / 2,
            transform.position.y + Screen.height / 2,
            transform.position.z
        ));
        MakeVisible(transition);
        yield return FadeOut(transition, 20);
        transition.gameObject.SetActive(false);
        EnterAreaOnSpawn(overworld);
    }

    public IEnumerator ExitAreaTransition()
    {
        areaExitSound.Play();
        transition.gameObject.SetActive(true);
        transition.transform.position = transform.position;
        yield return FadeIn(transition, 10);
        yield return Stall(10);
    }

    public IEnumerator TrainerBattleTransition(PlayerLogic playerLogic, AudioClip music)
    {
        playerLogic.battleMusicPlayer.clip = music;
        SceneInfo.PlayBattleMusic(playerLogic.battleMusicPlayer);
        yield return BattleTransition();
    }

    public void PlayMusicImmediate(PlayerLogic playerLogic, AudioClip music)
    {
        playerLogic.battleMusicPlayer.clip = music;
        SceneInfo.PlayMusicImmediate(playerLogic.battleMusicPlayer);
    }

    public IEnumerator WildBattleTransition()
    {
        SceneInfo.PlayBattleMusic(wildBattleMusic);
        yield return BattleTransition();
    }

    private IEnumerator BattleTransition()
    {
        introEffect.gameObject.SetActive(true);
        MakeInvisible(introEffect);
        introEffect.transform.position = transform.position;

        for (var i = mainCamera.orthographicSize; i <= 5f; i += 0.01f)
        {
            mainCamera.orthographicSize = i;
            yield return null;
        }

        for (var i = mainCamera.orthographicSize; i >= 0.5f;)
        {
            if (i >= 2.5f) i -= 0.125f;
            else if (i >= 1.75f) i -= 0.095f;
            else if (i >= 1.25f) i -= 0.07f;
            else if (i >= 0.75f) i -= 0.05f;
            else i -= 0.03f;

            mainCamera.orthographicSize = i;
            introEffect.color = new Color(introEffect.color.r, introEffect.color.g, introEffect.color.b, introEffect.color.a + 0.025f);
            yield return null;
        }
    }

    /// <summary>
    /// Switches active menus while also showing a fade in and out animation.
    /// </summary>
    public void MenuTransition(ITransitionable oldScreenScript, ITransitionable newScreenScript)
    {
        StartCoroutine(PerformScreenTransition(oldScreenScript, newScreenScript));
    }

    private IEnumerator PerformScreenTransition(ITransitionable oldScreenScript, ITransitionable newScreenScript)
    {
        oldScreenScript.IsBusy = true;
        transition.gameObject.SetActive(true);
        transition.transform.position = transform.position;
        yield return FadeIn(transition, 10);

        oldScreenScript.GameObject.SetActive(false);
        newScreenScript.GameObject.SetActive(true);
        newScreenScript.IsBusy = true;
        newScreenScript.Init();
        yield return FadeOut(transition, 10);

        newScreenScript.IsBusy = false;
        transition.gameObject.SetActive(false);
    }

    /// <summary>
    /// Display an area border upon entering the new scene.
    /// </summary>
    private void EnterAreaOnSpawn(Overworld overworld)
    {
        overworld.locationMusic.Play();

        if (!SceneInfo.DisplayAreaHeaderOnSpawn) return;

        var border = new FakeAreaBorder
        {
            Overworld = overworld,
            IsAnimating = true
        };

        SceneInfo.SetAnimatedAreaBorder(border);
        RouteName.text = overworld.locationName;
        StartCoroutine(DisplayAreaHeader(border));
    }

    /// <summary>
    /// Transition from one overworld to another in the same scene.
    /// </summary>
    public void PassAreaBorder(IAreaBorder border, Overworld oldOverworld)
    {
        // manage old area
        oldOverworld.locationMusic.Stop();
        SceneInfo.SetOverworldInfo(oldOverworld);

        // manage new area
        var newOverworld = border.Overworld;
        newOverworld.locationMusic.Play();

        // end existing animation
        var borderAnim = SceneInfo.GetAnimatedAreaBorder();
        if (borderAnim != null)
        {
            borderAnim.IsAnimating = false; // stop its animation
            SceneInfo.DeleteAnimatedAreaBorder();
        }

        // animate route header
        border.IsAnimating = true;
        SceneInfo.SetAnimatedAreaBorder(border);
        RouteName.text = newOverworld.locationName;
        StartCoroutine(DisplayAreaHeader(border));
    }

    private IEnumerator DisplayAreaHeader(IAreaBorder border)
    {
        var header = RouteHeader.transform;
        var showPos = RouteShowPosition.transform.localPosition;
        var hidePos = RouteHidePosition.transform.localPosition;

        while ((showPos - header.localPosition).sqrMagnitude > Mathf.Epsilon)
        {
            if (!border.IsAnimating) yield break;
            header.localPosition = Vector3.MoveTowards(header.localPosition, showPos, 5);
            yield return null;
        }

        for (var i = 0; i < 40; i++)
        {
            if (!border.IsAnimating) yield break;
            yield return null;
        }

        while ((hidePos - header.localPosition).sqrMagnitude > Mathf.Epsilon)
        {
            if (!border.IsAnimating) yield break;
            header.localPosition = Vector3.MoveTowards(header.localPosition, hidePos, 5);
            yield return null;
        }
    }
}
