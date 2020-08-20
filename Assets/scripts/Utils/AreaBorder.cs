using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AreaBorder : MonoBehaviour
{
    public Overworld overworld;

    private bool isAnimating;

    public void ChangeArea(PlayerLogic playerLogic)
    {
        // manage old area
        playerLogic.overworld.locationMusic.Stop();
        SceneInfo.SetOverworldInfo(playerLogic.overworld);

        // manage new area
        playerLogic.overworld = overworld;
        overworld.locationMusic.Play();

        // end existing animation
        var abAnim = SceneInfo.GetAnimatedAreaBorder();
        if (abAnim != null)
        {
            abAnim.StopAnimation();
            SceneInfo.DeleteAnimatedAreaBorder();
        }

        // animate route header
        isAnimating = true;
        SceneInfo.SetAnimatedAreaBorder(this);
        playerLogic.routeName.text = overworld.locationName;
        StartCoroutine(DisplayAreaHeader(playerLogic.routeHeader.transform, playerLogic.routeShowPosition, playerLogic.routeHidePosition));
    }

    private IEnumerator DisplayAreaHeader(Transform header, RectTransform show, RectTransform hide)
    {
        var showPos = show.transform.localPosition;
        var hidePos = hide.transform.localPosition;

        while ((showPos - header.localPosition).sqrMagnitude > Mathf.Epsilon)
        {
            if (!isAnimating) yield break;
            header.localPosition = Vector3.MoveTowards(header.localPosition, showPos, 5);
            yield return null;
        }

        for (var i = 0; i < 40; i++)
        {
            if (!isAnimating) yield break;
            yield return null;
        }

        while ((hidePos - header.localPosition).sqrMagnitude > Mathf.Epsilon)
        {
            if (!isAnimating) yield break;
            header.localPosition = Vector3.MoveTowards(header.localPosition, hidePos, 5);
            yield return null;
        }
    }

    public void StopAnimation()
    {
        isAnimating = false;
    }
}
