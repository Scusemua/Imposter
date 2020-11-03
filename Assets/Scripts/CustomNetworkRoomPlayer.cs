﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Lean.Gui;

public class CustomNetworkRoomPlayer : NetworkRoomPlayer
{
    private GameObject LobbyUI;

    private LobbyPlayerList lobbyPlayerList;
    private LobbyPlayerList LobbyPlayerList
    {
        get => lobbyPlayerList;
        set
        {
            Debug.Log("Modifying value of LobbyPlayerList for Player " + DisplayName + ", netId = " + netId + ".");
            Debug.Log("Old value: " + (lobbyPlayerList == null ? "null" : "non-null") + ", New Value: " + (value == null ? "null" : "non-null"));
            lobbyPlayerList = value;
        }
    }

    private LeanButton startButton;

    [SyncVar(hook = nameof(HandleDisplayNameChanged))]
    public string DisplayName = "Loading...";

    private NetworkGameManager NetworkGameManagerInstance
    {
        get
        {
            return NetworkGameManager.singleton as NetworkGameManager;
        }
    }

    public void HandleDisplayNameChanged(string oldValue, string newValue) => UpdateDisplay();

    private void UpdateDisplay()
    {
        Debug.Log("UpdateDisplay() called for Player " + DisplayName + ", netId = " + netId + ". isLocalPlayer = " + isLocalPlayer + ", hasAuthority = " + hasAuthority + ".");

        if (!hasAuthority)
        {
            Debug.Log("Size of roomSlots during UpdateDisplay(): " + NetworkGameManagerInstance.roomSlots.Count + "!");

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

        Debug.Log("Size of roomSlots during UpdateDisplay(): " + NetworkGameManagerInstance.roomSlots.Count + ".");

        foreach (NetworkRoomPlayer player in NetworkGameManagerInstance.roomSlots)
        {
            CustomNetworkRoomPlayer customPlayer = player as CustomNetworkRoomPlayer;

            LobbyPlayerList.AddOrUpdateEntry(customPlayer.netId, customPlayer.DisplayName, customPlayer.readyToBegin);
        }
    }

    public override void OnStopClient()
    {
        UpdateDisplay();
    }

    public override void OnStartAuthority()
    {
        Debug.Log("OnStartAuthority() called for RoomPlayer " + netId + ". isLocalPlayer = " + isLocalPlayer + ".");

        DisplayName = PlayerPrefs.GetString("nickname");
        Debug.Log("Player " + DisplayName + " joined the lobby.");
    }


    [Command]
    private void CmdSetDisplayName(string displayName)
    {
        Debug.Log("CmdSetDisplayName called for RoomPlayer " + netId + ". isLocalPlayer = " + isLocalPlayer + ", hasAuthority = " + hasAuthority + ".");
        DisplayName = displayName;

        //if (LobbyPlayerList == null)
        //{
        //    GameObject gameObject = GameObject.FindWithTag("LobbyPlayerListContent");
        //    if (gameObject == null)
        //    {
        //        Debug.LogWarning("Could not find GameObject with tag \"LobbyPlayerListContent\" in OnClientEnterRoom...");
        //        return;
        //    }
        //    LobbyPlayerList = gameObject.GetComponent<LobbyPlayerList>();
        //}

        LobbyPlayerList.AddEntry(netId, DisplayName, false);
    }

    public override void OnClientEnterRoom()
    {
        Debug.Log("OnClientEnterRoom() called for RoomPlayer " + netId + ". isLocalPlayer = " + isLocalPlayer + ", hasAuthority = " + hasAuthority + ".");
        if (isLocalPlayer)
        {
            Debug.Log("RoomPlayer " + netId + " is a local player, so creating UI hooks now.");
            CreateUIHooks();

            if (hasAuthority)
            {
                Debug.Log("RoomPlayer " + netId + " has authority, so calling CmdSetDisplayName now...");
                CmdSetDisplayName(DisplayName);
            }
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
        Debug.Log("OnClientExitRoom() called for player " + DisplayName + ", netId = " + netId + ".");

        if (LobbyPlayerList == null)
        {
            Debug.LogWarning("LobbyPlayerList is null for Player " + DisplayName + ", netId = " + netId + ", cannot remove entry from list...");
            return;
        }

        Debug.Log("Removing entry for player " + DisplayName + ", netId = " + netId + ", from lobby player list now.");

        LobbyPlayerList.RemoveEntry(netId, DisplayName, false);
    }

    public override void ReadyStateChanged(bool _, bool newReadyState)
    {
        Debug.Log("ReadyStateChanged() called for RoomPlayer " + netId + ". (LobbyPlayerList == null) = " + (LobbyPlayerList == null));

        if (LobbyPlayerList == null) return;
        UpdateDisplay();
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
