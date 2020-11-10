using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using Lean.Gui;

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

    public void OnReturnToLobbyPressed()
    {
        // Go back to the room.
        networkGameManager.ServerChangeScene(networkGameManager.RoomScene);
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

    public void OnInteractButtonClicked()
    {
        if (canInteractWithEmergencyButton)
            OnInteractWithEmergencyButton();
    }

    public void OnInteractWithBody()
    {
        Debug.Log("Player interacted with body.");
    }

    public void OnInteractWithEmergencyButton()
    {
        Debug.Log("Player interacted with emergency button.");

        PlayerUICanvas.SetActive(false);

        GameObject votingUI = Instantiate(VotingUIPrefab, transform);
        votingUI.GetComponent<VotingUI>().PlayerUI = PlayerUICanvas;
        votingUI.GetComponent<VotingUI>().PlayerController = playerController;
    }

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

        SetNickname(player.nickname);
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
}
