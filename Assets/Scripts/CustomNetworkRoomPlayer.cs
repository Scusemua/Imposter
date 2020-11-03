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

    //[SyncVar(hook = nameof(UpdateReadyDisplay))]
    //public bool ready = false;

    [SyncVar(hook = nameof(HandleDisplayNameChanged))]
    public string DisplayName = "Loading...";

    private NetworkGameManager NetworkGameManagerInstance
    {
        get
        {
            return NetworkGameManager.singleton as NetworkGameManager;
        }
    }

    // public void HandleReadyStatusChanged(bool oldValue, bool newValue) => UpdateDisplay();
    public void HandleDisplayNameChanged(string oldValue, string newValue) => UpdateDisplay();

    private void UpdateDisplay()
    {
        Debug.Log("UpdateDisplay() called for RoomPlayer " + netId);

        if (!hasAuthority)
        {
            foreach (NetworkRoomPlayer player in NetworkGameManagerInstance.roomSlots)
            {
                CustomNetworkRoomPlayer customPlayer = player as CustomNetworkRoomPlayer;

                if (customPlayer.hasAuthority) {
                    customPlayer.UpdateDisplay();
                    break;
                }
            }

            return;
        }

        foreach (NetworkRoomPlayer player in NetworkGameManagerInstance.roomSlots)
        {
            CustomNetworkRoomPlayer customPlayer = player as CustomNetworkRoomPlayer;

            LobbyPlayerList.AddOrUpdateEntry(customPlayer.netId, customPlayer.DisplayName, customPlayer.readyToBegin);
        }
    }

    public override void OnStartAuthority()
    {
        Debug.Log("OnStartAuthority() called for RoomPlayer " + netId);

        DisplayName = PlayerPrefs.GetString("nickname");
        Debug.Log("Player " + DisplayName + " joined the lobby.");

        CmdSetDisplayName(DisplayName);
    }


    [Command]
    private void CmdSetDisplayName(string displayName)
    {
        DisplayName = displayName;

        if (LobbyPlayerList == null)
        {
            GameObject gameObject = GameObject.FindWithTag("LobbyPlayerListContent");
            if (gameObject == null)
            {
                Debug.LogWarning("Could not find GameObject with tag \"LobbyPlayerListContent\" in OnClientEnterRoom...");
                return;
            }
            LobbyPlayerList = gameObject.GetComponent<LobbyPlayerList>();
        }

        LobbyPlayerList.AddEntry(netId, DisplayName, false);
    }

    public override void OnClientEnterRoom()
    {
        Debug.Log("OnClientEnterRoom() called for RoomPlayer " + netId);
        if (isLocalPlayer)
        {
            Debug.Log("RoomPlayer " + netId + " is a local player, so creating UI hooks now.");
            CreateUIHooks();
        }
    }

    /// <summary>
    /// Create references to buttons, onClick listeners, and the lobby player list.
    /// </summary>
    public void CreateUIHooks()
    {
        if (LobbyPlayerList == null)
        {
            GameObject gameObject = GameObject.FindWithTag("LobbyPlayerListContent");
            if (gameObject == null)
            {
                Debug.LogWarning("Could not find GameObject with tag \"LobbyPlayerListContent\" in OnClientEnterRoom...");
                return;
            }
            LobbyPlayerList = gameObject.GetComponent<LobbyPlayerList>();
        }

        LobbyPlayerList.AddEntry(netId, DisplayName, false);

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
        Debug.Log("OnClientExitRoom() called. Player " + DisplayName + ", netId = " + netId + ", has left the room.");

        if (LobbyPlayerList == null)
        {
            Debug.LogWarning("LobbyPlayerList is null; cannot remove entry from list...");
            return;
        }

        Debug.Log("Removing entry for player " + DisplayName + ", netId = " + netId + ", from lobby player list now.");

        LobbyPlayerList.RemoveEntry(netId, DisplayName, false);
    }

    public override void ReadyStateChanged(bool _, bool newReadyState)
    {
        Debug.Log("ReadyStateChanged() called for RoomPlayer " + netId);
        UpdateDisplay();
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

        LobbyPlayerList.ModifyReadyStatus(netId, DisplayName, readyToBegin);
    }

    public void LeaveLobby()
    {
        Debug.Log("Player " + netId + " clicked Leave button. isLocalPlayer = " + isLocalPlayer + ", hasAuthority = " + hasAuthority);

        if (NetworkServer.active && NetworkClient.isConnected) // Stop host if host mode.
            NetworkGameManagerInstance.StopHost();
        else if (NetworkClient.isConnected)     // Stop client only.
            NetworkGameManagerInstance.StopClient();
        else if (NetworkServer.active)          // Stop server only.
            NetworkGameManagerInstance.StopServer();
    }

    public void ReadyUp()
    {
        Debug.Log("Player " + netId + " clicked Ready button. isLocalPlayer = " + isLocalPlayer + ", hasAuthority = " + hasAuthority);

        if (hasAuthority) CmdChangeReadyState(!readyToBegin);
    }
}
