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

    //private LobbyPlayerList lobbyPlayerList
    private LobbyPlayerList LobbyPlayerList;
    //{
    //    get
    //    {
    //        if (lobbyPlayerList == null)
    //        {
    //            GameObject gameObject = GameObject.FindWithTag("LobbyPlayerListContent");
    //            if (gameObject == null)
    //                return null;
    //            lobbyPlayerList = gameObject.GetComponent<LobbyPlayerList>();
    //        }

    //        return lobbyPlayerList;
    //    }
    //}

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
    }

    public override void OnStartAuthority()
    {

    }


    [Command]
    private void CmdSetDisplayName(string displayName)
    {
        DisplayName = displayName;
    }

    public override void OnClientEnterRoom()
    {
        DisplayName = PlayerPrefs.GetString("nickname");

        Debug.Log("Player " + DisplayName + " joined the lobby.");

        if (LobbyPlayerList == null) {
            GameObject gameObject = GameObject.FindWithTag("LobbyPlayerListContent");
            if (gameObject == null)
            {
                Debug.LogWarning("Could not find GameObject with tag \"LobbyPlayerListContent\" in OnClientEnterRoom...");
                return;
            }
            LobbyPlayerList = gameObject.GetComponent<LobbyPlayerList>();
        }

        LobbyPlayerList.AddEntry(DisplayName, false);

        CmdSetDisplayName(DisplayName);

        if (!hasAuthority) return;

        GameObject LobbyUI = GameObject.FindWithTag("LobbyUI");

        if (LobbyPlayerList == null)
            LobbyPlayerList = GameObject.FindWithTag("LobbyPlayerListContent").GetComponent<LobbyPlayerList>();

        LeanButton[] buttons = LobbyUI.GetComponentsInChildren<LeanButton>();

        foreach (LeanButton button in buttons)
        {
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
    }

    public override void OnClientExitRoom()
    {
        Debug.Log("Player " + DisplayName + " joined the lobby.");

        if (LobbyPlayerList == null)
            return;

        LobbyPlayerList.RemoveEntry(DisplayName, false);
    }

    public override void ReadyStateChanged(bool _, bool newReadyState)
    {
        if (LobbyPlayerList == null)
        {
            GameObject gameObject = GameObject.FindWithTag("LobbyPlayerListContent");
            if (gameObject == null)
            {
                Debug.LogWarning("Could not find GameObject with tag \"LobbyPlayerListContent\" in ReadyStateChanged...");
                return;
            }
                
            LobbyPlayerList = gameObject.GetComponent<LobbyPlayerList>();
        }

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

    [ClientRpc]
    public void ReadyUpClicked()
    {

    }


    public void ReadyUp()
    {
        Debug.Log("Ready button clicked.");

        ready = !ready;
        Debug.Log("Player ready: " + ready);
        CmdChangeReadyState(ready);
    }
}
