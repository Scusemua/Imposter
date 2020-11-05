using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    private Player player;
    private PlayerController playerController;

    [Header("UI Elements")]
    public GameObject PrimaryActionButtonGameObject;
    public Image CrewmateVictoryImage;
    public Image ImposterVictoryImage;
    public Image PlayerImage;
    public GameObject ReturnToLobbyButton;
    public TextMeshProUGUI WaitingOnHostText;
    public TextMeshProUGUI nicknameText;
    public TextMeshProUGUI roleText;

    [Header("Misc.")]
    public float PlayerImageAlpha;

    private GameOptions gameOptions;
    private NetworkGameManager networkGameManager;

    void Alive()
    {
        gameOptions = GameOptions.singleton;
        networkGameManager = NetworkManager.singleton as NetworkGameManager;
    }

    // Start is called before the first frame update
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

    // Update is called once per frame
    void Update()
    {
        
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

    public void SetPlayer(Player player)
    {
        this.player = player;
        this.playerController = player.GetComponent<PlayerController>();
        this.PlayerImage.color = new Color(player.playerColor.r, player.playerColor.g, player.playerColor.b, PlayerImageAlpha);

        SetNickname(player.nickname);
    }

    public void SetNickname(string nickname)
    {
        nicknameText.text = nickname;
    }

    public void SetRole(string roleName)
    {
        roleText.text = roleName.ToUpper();
    }
}
