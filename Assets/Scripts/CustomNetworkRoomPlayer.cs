using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class CustomNetworkRoomPlayer : NetworkRoomPlayer
{
    public GameObject LobbyUIPrefab;

    private GameObject LobbyUI;

    private Button readyButton;
    private Button startButton;
    private Button leaveButton;

    private bool ready = false;

    private NetworkRoomManager manager;
    private NetworkRoomManager Manager
    {
        get
        {
            if (manager != null)
                return manager;

            manager = NetworkManager.singleton as NetworkRoomManager;
            return manager;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        LobbyUI = Instantiate(LobbyUIPrefab);

        Button[] buttons = LobbyUI.GetComponentsInChildren<Button>();

        foreach (Button button in buttons)
        {
            Debug.Log(button.name);

            if (button.name.Equals("ReadyButton"))
                button.onClick.AddListener(ReadyUp);
            else if (button.name.Equals("LeaveButton")) 
                button.onClick.AddListener(LeaveLobby)
            else if (button.name.Equals("StartButton"))
            {
                // If not host, disable button...
                button.onClick.AddListener(StartGame);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [Command]
    public void StartGame()
    {

    }

    [Command]
    public void LeaveLobby()
    {

    }

    [Command]
    public void ReadyUp()
    {
        this.ready = !this.ready;
        Debug.Log("Player ready: " + this.ready);
        this.CmdChangeReadyState(this.ready);
    }
}
