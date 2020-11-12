using Lean.Gui;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VotingEntry : MonoBehaviour
{
    public Image PlayerAliveIcon;

    public Image PlayerDeadIcon;

    public Text ButtonText;

    /// <summary>
    /// Clicking this displays the confirm/deny buttons.
    /// </summary>
    public LeanButton PrimaryVoteButton;

    /// <summary>
    /// Submits the user's vote.
    /// </summary>
    public LeanButton ConfirmVoteButton;

    /// <summary>
    /// Does not submit the user's vote.
    /// </summary>
    public LeanButton DenyVoteButton;
}
