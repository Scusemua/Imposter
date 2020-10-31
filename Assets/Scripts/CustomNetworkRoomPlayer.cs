using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class CustomNetworkRoomPlayer : NetworkRoomPlayer
{
    public GameObject LobbyUIPrefab;

    private GameObject LobbyUI;

    private Button startButton;

    private bool ready = false;

    private GameManager GameManagerInstance
    {
        get
        {
            return GameManager.singleton as GameManager;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        base.Start();

        //LobbyUI = Instantiate(LobbyUIPrefab);

        GameObject LobbyUI = GameObject.FindWithTag("LobbyUI");

        Button[] buttons = LobbyUI.GetComponentsInChildren<Button>();

        foreach (Button button in buttons)
        {
            Debug.Log(button.name);

            if (button.name.Equals("ReadyButton"))
                button.onClick.AddListener(ReadyUp);
            else if (button.name.Equals("LeaveButton"))
                button.onClick.AddListener(LeaveLobby);
            else if (button.name.Equals("StartButton"))
            {
                startButton = button;

                if (!isClientOnly)
                    // This feels like a dirty hack...
                    button.onClick.AddListener(GameManagerInstance.OnStartButtonClicked);
            }
        }
    }

    private void Awake()
    {
        //base.Start();

        //LobbyUI = Instantiate(LobbyUIPrefab);

        ////GameObject LobbyUI = GameObject.FindWithTag("LobbyUI");

        //Button[] buttons = LobbyUI.GetComponentsInChildren<Button>();

        //foreach (Button button in buttons)
        //{
        //    Debug.Log(button.name);

        //    if (button.name.Equals("ReadyButton"))
        //        button.onClick.AddListener(ReadyUp);
        //    else if (button.name.Equals("LeaveButton"))
        //        button.onClick.AddListener(LeaveLobby);
        //    else if (button.name.Equals("StartButton"))
        //    {
        //        startButton = button;

        //        if (!isClientOnly)
        //            // This feels like a dirty hack...
        //            button.onClick.AddListener(GameManagerInstance.OnStartButtonClicked);
        //    }
        //}
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void LeaveLobby()
    {
        Debug.Log("Leave button clicked.");
        if (NetworkServer.active && NetworkClient.isConnected) // Stop host if host mode.
            GameManagerInstance.StopHost();
        else if (NetworkClient.isConnected)     // Stop client only.
            GameManagerInstance.StopClient();
        else if (NetworkServer.active)          // Stop server only.
            GameManagerInstance.StopServer();
    }

    public void ReadyUp()
    {
        Debug.Log("Ready button clicked.");
        ready = !ready;
        Debug.Log("Player ready: " + ready);
        CmdChangeReadyState(ready);
    }
}
