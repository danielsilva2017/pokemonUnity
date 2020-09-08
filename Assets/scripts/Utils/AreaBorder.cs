using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAreaBorder
{
    Overworld Overworld { get; set; }
    bool IsAnimating { get; set; }
}

/// <summary>
/// Class related to special tiles that trigger the entry into an overworld.
/// </summary>
public class AreaBorder : MonoBehaviour, IAreaBorder
{
    public Overworld overworld;

    public Overworld Overworld
    {
        get { return overworld; }
        set { overworld = value; }
    }
    public bool IsAnimating { get; set; }
}

/// <summary>
/// Used for scene entrances.
/// </summary>
public class FakeAreaBorder : IAreaBorder
{
    public Overworld Overworld { get; set; }
    public bool IsAnimating { get; set; }
}
