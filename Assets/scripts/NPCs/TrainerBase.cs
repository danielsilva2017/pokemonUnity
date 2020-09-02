using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Trainer", menuName = "Trainer")]

public class TrainerBase : ScriptableObject
{
    public string className;
    public string animationPrefix;
    public AudioClip introMusic;
    public AudioClip battleMusic;
    public AudioClip victoryMusic;
}
