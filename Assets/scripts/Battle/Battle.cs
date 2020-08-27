using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public enum BattleState
{
    Intro, TurnHappening, SelectingAction, SelectingMove, Idle
}

/// <summary>
/// Interface for Battle UIs.
/// </summary>
public interface IBattle
{
    /// <summary>
    /// Prints to the battle's chatbox, optionally pausing for a period of time after the message is printed.
    /// </summary>
    IEnumerator Print(string message, bool delay = true);
    /// <summary>
    /// Notifies the UI that the turn has ended.
    /// </summary>
    void NotifyTurnFinished();
    /// <summary>
    /// Notifies the UI that an item was used.
    /// </summary>
    void NotifyItemUsed(Item item);
    /// <summary>
    /// Notifies the UI that a switch was performed.
    /// </summary>
    void NotifySwitchPerformed(Pokemon selection);
    /// <summary>
    /// Updates the ally and enemy's health bars and statuses either smoothly or instantly.
    /// </summary>
    IEnumerator NotifyUpdateHealth(bool immediate = false);
    /// <summary>
    /// Updates the ally's exp bar, either filling it fully or based on a percentage.
    /// </summary>
    IEnumerator NotifyUpdateExp(bool fill);
    /// <summary>
    /// Moves directed at the Pokemon switched out will now be directed at the Pokemon switched in.
    /// </summary>
    void UpdateMoveTargets(SwitchCommand cmd);
    /// <summary>
    /// Update battle graphics to represent the Pokemon switched in.
    /// </summary>
    void RegisterSwitch(Pokemon switchedIn);

    BattleLogic Logic { get; }
    IDialog Chatbox { get; }
    BattleInfo BattleInfo { get; }
    PlayerInfo PlayerInfo { get; }
    void PlayHitSound();
    void PlayNotVeryEffectiveHitSound();
    void PlaySuperEffectiveHitSound();
    void PlayLevelUpSound();
}