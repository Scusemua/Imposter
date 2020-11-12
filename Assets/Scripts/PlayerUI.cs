﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using Lean.Gui;
using System;

public class PlayerUI : MonoBehaviour
{
    private Player player;
    private PlayerController playerController;

    [Header("UI Elements")]
    public GameObject PrimaryActionButtonGameObject;
    public LeanButton InteractableButton;
    public Image CrewmateVictoryImage;
    public Image ImposterVictoryImage;
    public Image PlayerImage;
    public GameObject ReturnToLobbyButton;
    public TextMeshProUGUI WaitingOnHostText;
    public TextMeshProUGUI NicknameText;
    public TextMeshProUGUI RoleText;
    public GameObject VotingUIPrefab;
    public GameObject PlayerUICanvas;
    public Healthbar HpBar;
    public Healthbar StaminaBar;

    public GameObject RoleAnimator;
    public Text RoleAnimationText;

    [Header("Misc.")]
    public float PlayerImageAlpha;

    private GameOptions gameOptions;
    private NetworkGameManager networkGameManager;
    private bool canInteractWithEmergencyButton;

    public event Action<uint> OnPlayerVoted;

    void Alive()
    {
        gameOptions = GameOptions.singleton;
        networkGameManager = NetworkManager.singleton as NetworkGameManager;
    }

    // Start is called before the first frame update.
    void Start()
    {
        // Hide end-of-game UI.
        CrewmateVictoryImage.enabled = false;
        ImposterVictoryImage.enabled = false;
        ReturnToLobbyButton.SetActive(false);
        WaitingOnHostText.enabled = false;

        gameOptions = GameOptions.singleton;
        networkGameManager = NetworkManager.singleton as NetworkGameManager;
    }

    // Update is called once per frame.
    void Update()
    {
        if (!player.isLocalPlayer) return;

        float distToEmergencyButton = playerController.GetSquaredDistanceToEmergencyButton();

        bool interactableWithinRange = false;

        if (distToEmergencyButton <= 25)
        {
            interactableWithinRange = true;
            canInteractWithEmergencyButton = true;
        }
        else
            canInteractWithEmergencyButton = false;

        if (interactableWithinRange)
            InteractableButton.enabled = true;
        else
            InteractableButton.enabled = false;

        if (Input.GetKey(KeyCode.E) && interactableWithinRange && canInteractWithEmergencyButton)
            OnInteractWithEmergencyButton();
    }

    #region UI Handlers 

    [Client]
    /// <summary>
    /// This function corresponds to the generic Inspect/Interact/Use button on the player's UI overlay.
    /// </summary>
    public void OnInteractButtonClicked()
    {
        if (canInteractWithEmergencyButton)
            OnInteractWithEmergencyButton();
    }

    /// <summary>
    /// Corresponds to the "Return to Lobby" button that gets displayed on the host's UI at the end of the game.
    /// </summary>
    public void OnReturnToLobbyPressed()
    {
        if (player.isClientOnly)
            Debug.LogError("Client-only player clicked the 'Return to Lobby' button.");

        // Go back to the room.
        networkGameManager.ServerChangeScene(networkGameManager.RoomScene);
    }

    [Client]
    public void OnInteractWithBody()
    {
        Debug.Log("Player interacted with body.");
    }

    [Client]
    public void OnInteractWithEmergencyButton()
    {
        Debug.Log("Player interacted with emergency button.");

        networkGameManager.CmdStartVote();
    }

    #endregion

    #region Display UI

    [Client]
    public void DisplayEndOfGameUI(bool crewmateVictory)
    {
        if (crewmateVictory)
            CrewmateVictoryImage.enabled = true;
        else
            ImposterVictoryImage.enabled = true;

        if (player.isClientOnly)
            WaitingOnHostText.enabled = true;
        else
            ReturnToLobbyButton.SetActive(true);
    }

    [Client]
    public void CreateAndDisplayVotingUI()
    {
        PlayerUICanvas.SetActive(false);

        GameObject votingUIGameObject = Instantiate(VotingUIPrefab, transform);
        VotingUI votingUI = votingUIGameObject.GetComponent<VotingUI>();
        votingUI.PlayerUI = PlayerUICanvas;
        votingUI.PlayerController = playerController;

        OnPlayerVoted += votingUI.PlayerVoted;
    }

    /// <summary>
    /// This will cause the I VOTED icon to display on the VotingUI, if it exists.
    [Client]
    public void PlayerVoted(uint voterId)
    {
        try
        {
            OnPlayerVoted?.Invoke(voterId);
        }
        catch (Exception ex)
        {
            // Do nothing, it's fine.
            Debug.LogWarning("Caught exception when attempting to fire OnPlayerVoted event.");
            Debug.LogException(ex);
        }
    }

    #endregion

    #region Player Management 
    public void AnimateRole()
    {
        RoleAnimator.SetActive(true);
        RoleAnimator.GetComponent<Animator>().Play("Expand");
    }

    public void SetPlayer(Player player)
    {
        this.player = player;
        playerController = player.GetComponent<PlayerController>();
        PlayerImage.color = new Color(player.PlayerColor.r, player.PlayerColor.g, player.PlayerColor.b, PlayerImageAlpha);

        SetNickname(player.Nickname);
    }

    public void SetNickname(string nickname)
    {
        NicknameText.text = nickname;
    }

    public void SetRole(string roleName)
    {
        RoleText.text = roleName.ToUpper();
        RoleAnimationText.text = roleName.ToUpper();

        AnimateRole();
    }
    #endregion
}
