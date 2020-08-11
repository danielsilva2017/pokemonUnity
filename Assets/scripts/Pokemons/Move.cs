using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using static Utils;

/// <summary>
/// An instance of a move, based on a skeleton.
/// </summary>
public class Move
{
    public MoveBase Skeleton { get; set; }
    public MoveFunctions Functions { get; set; }
    public int Points {
        get { return points; }
        set { points = Limit(0, value, MaxPoints); }
    }

    private int points;

    public Move(MoveBase skeleton)
    {
        Skeleton = skeleton;
        Points = skeleton.points;
        Functions = GetFunctions(skeleton.logic);
    }

    public Move(MoveBase skeleton, int points)
    {
        Skeleton = skeleton;
        Points = points;
    }

    // some getters for convenience
    public string Name { get { return Skeleton.moveName; } }
    public Type Type { get { return Skeleton.type; } }
    public Category Category { get { return Skeleton.category; } }
    public Targeting Targeting { get { return Skeleton.targeting; } }
    public int MaxPoints { get { return Skeleton.points; } }
    public int Accuracy { get { return Skeleton.accuracy; } }
    public int Power { get { return Skeleton.power; } }

    // use reflection to find the correct move logic
    private MoveFunctions GetFunctions(MoveLogic logic)
    {
        var _class = System.Type.GetType(Enum.GetName(typeof(MoveLogic), logic));
        return (MoveFunctions) Activator.CreateInstance(_class);
    }
}