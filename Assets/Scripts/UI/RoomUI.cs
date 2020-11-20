using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class RoomUI : NetworkBehaviour
{
    public GameObject playersList;

    public GameObject gameChat;

    public GameObject gameOptions;

    public GameObject readyButton;

    public GameObject startButton;

    public GameObject leaveButton;

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
        // Disable the 'start' button for clients.
        if (isClientOnly)
        {
            startButton.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnStartClicked()
    {
        Debug.Log("Start button clicked.");
    }

    [Command]
    public void CmdReadyUp()
    {
        Debug.Log("Ready button clicked.");

        // Client ready
        if (NetworkClient.active && isLocalPlayer)
        {
            ClientScene.Ready(NetworkClient.connection);

            if (ClientScene.localPlayer == null)
            {
                Debug.Log("Adding player to ClientScene now...");
                ClientScene.AddPlayer(NetworkClient.connection);
                readyButton.GetComponent<Image>().color = new Color(155, 255, 91);
            }
        }
    }

    public void OnLeaveClicked()
    {
        Debug.Log("Leave button clicked.");

        if (NetworkServer.active && NetworkClient.isConnected) // Stop host if host mode.
            Manager.StopHost();
        else if (NetworkClient.isConnected)     // Stop client only.
            Manager.StopClient();
        else if (NetworkServer.active)          // Stop server only.
            Manager.StopServer();
            
    }
}
