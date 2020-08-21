using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Utils;

public class PlayerUI : MonoBehaviour
{
    public Camera mainCamera;
    public MenuPoke menu;
    public GameObject routeHeaders;
    public SpriteRenderer introEffect;
    public AudioSource battleIntroSound;

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
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator WildBattleTransition()
    {
        battleIntroSound.Play();
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
            battleIntroSound.volume -= 0.004f;
            yield return null;
        }
    }

    public void PassAreaBorder(AreaBorder border, Overworld oldOverworld)
    {
        // manage old area
        oldOverworld.locationMusic.Stop();
        SceneInfo.SetOverworldInfo(oldOverworld);

        // manage new area
        var newOverworld = border.overworld;
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

    private IEnumerator DisplayAreaHeader(AreaBorder border)
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
