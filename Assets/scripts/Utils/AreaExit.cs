using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class related to special tiles that trigger the entry into an overworld from a different scene.
/// </summary>
public class AreaExit : MonoBehaviour
{
    public SceneID scene;
    public string targetOverworldName;
    public Vector2 targetCoordinates;
}
