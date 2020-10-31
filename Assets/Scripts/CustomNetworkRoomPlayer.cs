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
    new void Start()
    {
        base.Start();

        LobbyUI = Instantiate(LobbyUIPrefab);

        Button[] buttons = LobbyUI.GetComponentsInChildren<Button>();

        foreach (Button button in buttons)
        {
            Debug.Log(button.name);

            if (button.name.Equals("ReadyButton"))
            {
                if (!isClientOnly)
                    // If host, disable button...
                    button.gameObject.SetActive(false);
                else
                    button.onClick.AddListener(ReadyUp);

            }
            else if (button.name.Equals("LeaveButton"))
                button.onClick.AddListener(LeaveLobby);
            else if (button.name.Equals("HostReadyButton"))
            {
                if (isClientOnly)
                    // If not host, disable button...
                    button.gameObject.SetActive(false);
                else
                    button.onClick.AddListener(HostReadyUp);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void HostReadyUp()
    {
        Debug.Log("Ready button clicked.");
        ready = !ready;
        Debug.Log("Player ready: " + ready);
        CmdChangeReadyState(ready);
    }

    public void LeaveLobby()
    {
        Debug.Log("Leave button clicked.");
        if (NetworkServer.active && NetworkClient.isConnected) // Stop host if host mode.
            Manager.StopHost();
        else if (NetworkClient.isConnected)     // Stop client only.
            Manager.StopClient();
        else if (NetworkServer.active)          // Stop server only.
            Manager.StopServer();
    }

    public void ReadyUp()
    {
        Debug.Log("Ready button clicked.");
        ready = !ready;
        Debug.Log("Player ready: " + ready);
        CmdChangeReadyState(ready);
    }
}
