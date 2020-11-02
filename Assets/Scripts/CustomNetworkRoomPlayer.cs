using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Lean.Gui;

public class CustomNetworkRoomPlayer : NetworkRoomPlayer
{
    public GameObject LobbyUIPrefab;

    private GameObject LobbyUI;

    private LobbyPlayerList LobbyPlayerList;

    private LeanButton startButton;

    [SyncVar(hook = nameof(UpdateReadyDisplay))]
    public bool ready = false;

    [SyncVar(hook = nameof(HandleDisplayNameChanged))]
    public string DisplayName = "Loading...";

    private NetworkGameManager NetworkGameManagerInstance
    {
        get
        {
            return NetworkGameManager.singleton as NetworkGameManager;
        }
    }

    private void UpdateReadyDisplay(bool _Old, bool _New)
    {
        Debug.Log("_Old = " + _Old + ", _New = " + _New);
    }

    public void HandleReadyStatusChanged(bool oldValue, bool newValue) => UpdateDisplay();
    public void HandleDisplayNameChanged(string oldValue, string newValue) => UpdateDisplay();

    private void UpdateDisplay()
    {
        Debug.Log("Updating display...");

        if (!hasAuthority)
        {

        }
    }

    // Start is called before the first frame update
    void Start()
    {
        base.Start();

        //GameObject LobbyUI = GameObject.FindWithTag("LobbyUI");

        //DisplayName = PlayerPrefs.GetString("nickname");

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
        //            button.onClick.AddListener(NetworkGameManagerInstance.OnStartButtonClicked);
        //        else
        //            startButton.interactable = false;
        //    }
        //}
    }

    public override void OnStartAuthority()
    {
        GameObject LobbyUI = GameObject.FindWithTag("LobbyUI");

        if (LobbyPlayerList == null)
            LobbyPlayerList = GameObject.FindWithTag("LobbyPlayerListContent").GetComponent<LobbyPlayerList>();

        DisplayName = PlayerPrefs.GetString("nickname");

        LeanButton[] buttons = LobbyUI.GetComponentsInChildren<LeanButton>();

        foreach (LeanButton button in buttons)
        {
            Debug.Log(button.name);

            if (button.name.Equals("ReadyButton"))
                button.OnClick.AddListener(ReadyUp);
            else if (button.name.Equals("LeaveButton"))
                button.OnClick.AddListener(LeaveLobby);
            else if (button.name.Equals("StartButton"))
            {
                startButton = button;

                if (!isClientOnly)
                    // This feels like a dirty hack...
                    button.OnClick.AddListener(NetworkGameManagerInstance.OnStartButtonClicked);
                else
                    startButton.interactable = false;
            }
        }

        CmdSetDisplayName(DisplayName);
    }


    [Command]
    private void CmdSetDisplayName(string displayName)
    {
        DisplayName = displayName;
    }

    public override void OnClientEnterRoom()
    {
        Debug.Log("Player " + DisplayName + " joined the lobby.");

        if (LobbyPlayerList == null)
            LobbyPlayerList = GameObject.FindWithTag("LobbyPlayerListContent").GetComponent<LobbyPlayerList>();

        LobbyPlayerList.AddEntry(DisplayName, false);
    }

    public override void OnClientExitRoom()
    {
        Debug.Log("Player " + DisplayName + " joined the lobby.");

        if (LobbyPlayerList == null)
            LobbyPlayerList = GameObject.FindWithTag("LobbyPlayerListContent").GetComponent<LobbyPlayerList>();

        LobbyPlayerList.RemoveEntry(DisplayName, false);
    }

    public override void ReadyStateChanged(bool _, bool newReadyState)
    {
        if (LobbyPlayerList == null)
            LobbyPlayerList = GameObject.FindWithTag("LobbyPlayerListContent").GetComponent<LobbyPlayerList>();
        
        LobbyPlayerList.ModifyReadyStatus(DisplayName, ready);
    }

    public void LeaveLobby()
    {
        Debug.Log("Leave button clicked.");
        if (NetworkServer.active && NetworkClient.isConnected) // Stop host if host mode.
            NetworkGameManagerInstance.StopHost();
        else if (NetworkClient.isConnected)     // Stop client only.
            NetworkGameManagerInstance.StopClient();
        else if (NetworkServer.active)          // Stop server only.
            NetworkGameManagerInstance.StopServer();
    }

    public void ReadyUp()
    {
        if (!isLocalPlayer) return;

        Debug.Log("Ready button clicked.");
        ready = !ready;
        Debug.Log("Player ready: " + ready);
        CmdChangeReadyState(ready);
    }
}
