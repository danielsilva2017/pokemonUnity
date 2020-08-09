using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move
{
    public MoveBase Skeleton { get; set; }
    public int Points { get; set; }

    public Move(MoveBase skeleton)
    {
        Skeleton = skeleton;
        Points = skeleton.points;
    }

    public Move(MoveBase skeleton, int points)
    {
        Skeleton = skeleton;
        Points = points;
    }

    public string Name { get { return Skeleton.moveName; } }
    public Type.Label Type { get { return Skeleton.type; } }
    public int MaxPoints { get { return Skeleton.points; } }
}
