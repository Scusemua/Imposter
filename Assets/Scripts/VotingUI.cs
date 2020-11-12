using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Lean.Gui;

public class VotingUI : MonoBehaviour
{
    public Timer CountdownTimer;
    public PlayerController PlayerController;
    public GameObject PlayerUI;
    public GameObject VotingEntryPrefab;
    public GridLayoutGroup ScrollViewContent;
    public LeanPulse AlreadyVotedNotification;
    public LeanButton SkipVoteButton;
    public LeanButton ConfirmSkipVoteButton;
    public LeanButton DenySkipVoteButton;

    /// <summary>
    /// Used to update to target UI updates to specific entries based on the player they're associated with.
    /// </summary>
    public Dictionary<uint, VotingEntry> NetIdToVotingEntryMap = new Dictionary<uint, VotingEntry>();

    /// <summary>
    /// Indicates whether or not this player has casted a vote yet.
    /// 
    /// The server validates votes so if a player managed to send multiple votes, it wouldn't matter.
    /// This is just use by the UI.
    /// </summary>
    public bool VoteCasted;

    // Start is called before the first frame update
    void Start()
    {
        CountdownTimer.OnTimerCompleted += OnTimerCompleted;

        NetworkGameManager instance = NetworkManager.singleton as NetworkGameManager;

        SkipVoteButton.OnClick.AddListener(() =>
        {
            ConfirmSkipVoteButton.enabled = true;
            DenySkipVoteButton.enabled = true;
        });

        ConfirmSkipVoteButton.OnClick.AddListener(() =>
        {
            Debug.Log("Skipping vote.");
            
            ConfirmSkipVoteButton.enabled = false;
            DenySkipVoteButton.enabled = false;

            castVote(NetworkGameManager.SKIPPED_VOTE_NET_ID);
        });

        DenySkipVoteButton.OnClick.AddListener(() =>
        {
            ConfirmSkipVoteButton.enabled = false;
            DenySkipVoteButton.enabled = false;
        });

        foreach (Player gamePlayer in instance.GamePlayers)
        {
            GameObject votingEntryGameObject = Instantiate(VotingEntryPrefab, ScrollViewContent.transform);
            VotingEntry votingEntry = votingEntryGameObject.GetComponent<VotingEntry>();
            votingEntry.PlayerAliveIcon.color = gamePlayer.PlayerColor;
            votingEntry.ButtonText.text = gamePlayer.Nickname;
            votingEntry.PlayerDeadIcon.color = gamePlayer.PlayerColor;

            // If the player is dead, then hide their "alive" icon and display the "dead" icon.
            // We also avoid adding the OnClick listener to the button in this case.
            if (gamePlayer.IsDead)
            {
                votingEntry.PlayerAliveIcon.enabled = false;    // Disable "alive" icon.
                votingEntry.PlayerDeadIcon.enabled = true;      // Enable "dead" icon.
                votingEntry.PrimaryVoteButton.interactable = false;        // Disable the button.
            } 
            else
            {
                // Add the OnClick listener to the button.
                // This is just the main button, so it shows a confirmation dialog. 
                votingEntry.PrimaryVoteButton.OnClick.AddListener(() =>
                {
                    if (VoteCasted) AlreadyVotedNotification.Pulse();

                    votingEntry.ConfirmVoteButton.enabled = true;
                    votingEntry.DenyVoteButton.enabled = true;
                });

                // Add an OnClick listener to the "Confirm Vote" button, which 
                // actually submits the player's vote.
                votingEntry.ConfirmVoteButton.OnClick.AddListener(() =>
                {
                    votingEntry.ConfirmVoteButton.enabled = false;
                    votingEntry.DenyVoteButton.enabled = false;

                    if (VoteCasted) return;

                    Debug.Log("Casting vote for player " + gamePlayer.netId + " now...");

                    castVote(gamePlayer.netId);
                });

                // Add an OnClick listener to the "Deny/Redact Vote" button, which 
                // just hides the confirm/deny buttons.
                votingEntry.DenyVoteButton.OnClick.AddListener(() =>
                {
                    votingEntry.ConfirmVoteButton.enabled = false;
                    votingEntry.DenyVoteButton.enabled = false;
                });
            }
        }
    }

    /// <summary>
    /// Send our vote to the server.
    /// </summary>
    [Client]
    private void castVote(uint recipientNetId)
    {
        PlayerController.Player.CmdCastVote(recipientNetId);

        VoteCasted = true;
    }

    public void OnTimerCompleted(int arg0)
    {
        Debug.Log("Timer has completed.");

        PlayerUI.SetActive(true);
        PlayerController.MovementEnabled = true;

        Destroy(this, 1.0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
        CountdownTimer.OnTimerCompleted -= OnTimerCompleted;
        PlayerUI.GetComponent<PlayerUI>().OnPlayerVoted -= PlayerVoted; // Clean up this event handler.
    }

    /// <summary>
    /// Display the "I VOTED" icon on the voting entry for the player who cast the vote.
    /// 
    /// This is only displayed when the server has received the vote.
    /// </summary>
    /// <param name="voterId"></param>
    [Client]
    public void PlayerVoted(uint voterId)
    {
        // Get the voting entry associated with the given player and toggle the voting icon.
        VotingEntry votingEntry = NetIdToVotingEntryMap[voterId];
        votingEntry.PlayerVotedIcon.SetActive(true);
    }
}
