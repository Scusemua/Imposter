using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class PlayerUI : MonoBehaviour
{
    [SerializeField]
    Text nicknameText;

    [SerializeField]
    Text roleText;

    private Player player;
    private PlayerController playerController;

    [Header("UI Elements")]
    public GameObject PrimaryActionButtonGameObject;
    public Text PrimaryActionLabel;
    public Image CrewmateVictoryImage;
    public Image ImposterVictoryImage;
    public GameObject ReturnToLobbyButton;
    public Text WaitingOnHostText;

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

        SetNickname(player.nickname);
    }

    void AssignTextComponents()
    {
        Text[] textComponents = GetComponentsInChildren<Text>();
        nicknameText = textComponents[0];
        roleText = textComponents[1];
    }

    public void SetNickname(string nickname)
    {
        if (nicknameText == null)
        {
            AssignTextComponents();
        }
        nicknameText.text = nickname;
    }

    public void SetRole(string roleName)
    {
        roleText.text = roleName.ToUpper();
    }
}
