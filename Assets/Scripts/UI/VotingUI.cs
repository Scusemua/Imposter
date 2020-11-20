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
            if (!CountdownTimer.IsVotingPhase())
            {
                Debug.Log("We are not in the voting phase. Returning from primary skip button clicked now.");
                return;
            }

            Debug.Log("Skip vote button clicked.");

            ConfirmSkipVoteButton.gameObject.SetActive(true);
            DenySkipVoteButton.gameObject.SetActive(true);
        });

        ConfirmSkipVoteButton.OnClick.AddListener(() =>
        {
            if (!CountdownTimer.IsVotingPhase()) return;
            Debug.Log("Skipping vote.");
            
            ConfirmSkipVoteButton.gameObject.SetActive(false);
            DenySkipVoteButton.gameObject.SetActive(false);

            castVote(NetworkGameManager.SKIPPED_VOTE_NET_ID);
        });

        DenySkipVoteButton.OnClick.AddListener(() =>
        {
            ConfirmSkipVoteButton.gameObject.SetActive(false);
            DenySkipVoteButton.gameObject.SetActive(false);
        });

        foreach (Player gamePlayer in instance.GamePlayers)
        {
            GameObject votingEntryGameObject = Instantiate(VotingEntryPrefab, ScrollViewContent.transform);
            VotingEntry votingEntry = votingEntryGameObject.GetComponent<VotingEntry>();
            NetIdToVotingEntryMap[gamePlayer.netId] = votingEntry;
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
                    if (!CountdownTimer.IsVotingPhase())
                    {
                        Debug.Log("We are not in the voting phase. Returning from primary vote button clicked now.");
                        return;
                    }

                    if (VoteCasted) AlreadyVotedNotification.Pulse();

                    Debug.Log("Primary vote button clicked for player " + gamePlayer.Nickname + ", netId = " + gamePlayer.netId);

                    votingEntry.ConfirmVoteButton.gameObject.SetActive(true);
                    votingEntry.DenyVoteButton.gameObject.SetActive(true);
                });

                // Add an OnClick listener to the "Confirm Vote" button, which 
                // actually submits the player's vote.
                votingEntry.ConfirmVoteButton.OnClick.AddListener(() =>
                {
                    if (!CountdownTimer.IsVotingPhase()) return;

                    votingEntry.ConfirmVoteButton.gameObject.SetActive(false);
                    votingEntry.DenyVoteButton.gameObject.SetActive(false);

                    if (VoteCasted) return;

                    Debug.Log("Casting vote for player " + gamePlayer.netId + " now...");

                    castVote(gamePlayer.netId);
                });

                // Add an OnClick listener to the "Deny/Redact Vote" button, which 
                // just hides the confirm/deny buttons.
                votingEntry.DenyVoteButton.OnClick.AddListener(() =>
                {
                    Debug.Log("Deny vote button clicked for player " + gamePlayer.Nickname + ", netId = " + gamePlayer.netId);

                    votingEntry.ConfirmVoteButton.gameObject.SetActive(false);
                    votingEntry.DenyVoteButton.gameObject.SetActive(false);
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

        Debug.Log("Casted vote: " + VoteCasted);
    }

    public void OnTimerCompleted(int arg0)
    {
        Debug.Log("Timer has completed.");

        PlayerUI.SetActive(true);
        PlayerController.MovementEnabled = true;
        
        // Make all buttons NOT interactable. 
        SkipVoteButton.interactable = false;
        ConfirmSkipVoteButton.interactable = false;
        DenySkipVoteButton.interactable = false;
        ConfirmSkipVoteButton.gameObject.SetActive(false);
        DenySkipVoteButton.gameObject.SetActive(false);

        // Make all voting entry buttons not interactable. Hide confirm/deny buttons.
        foreach (KeyValuePair<uint, VotingEntry> kvp in NetIdToVotingEntryMap)
        {
            VotingEntry entry = kvp.Value;
            entry.PrimaryVoteButton.interactable = false;
            entry.ConfirmVoteButton.interactable = false;
            entry.DenyVoteButton.interactable = false;

            entry.ConfirmVoteButton.gameObject.SetActive(false);
            entry.DenyVoteButton.gameObject.SetActive(false);
        }
        
        // TODO: Display all the votes, play animation for booting kicked player (or doing nothing if nobody got kicked).
        Destroy(gameObject, 1.0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
        CountdownTimer.OnTimerCompleted -= OnTimerCompleted;

        if (PlayerUI != null)
        {
            PlayerUI playerUI = PlayerUI.GetComponent<PlayerUI>();
            
            if (playerUI != null)
            {
                playerUI.OnPlayerVoted -= PlayerVoted; // Clean up this event handler.
            }
        }
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
