using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Lean.Gui;

public class CustomNetworkRoomPlayer : NetworkRoomPlayer
{
    public GameObject LobbyPlayerModel;

    public float ModelRotationSpeed;

    [SyncVar(hook = nameof(OnPlayerModelColorChanged))]
    public Color PlayerModelColor = GameColors.START_COLOR;

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

    private bool uiHooksCreated = false;

    private LeanButton startButton;

    private ChatHandler chatHandler;

    [SyncVar(hook = nameof(HandleDisplayNameChanged))]
    public string DisplayName = "Loading...";

    private ColorSelector colorSelector;

    private NetworkGameManager NetworkGameManagerInstance
    {
        get
        {
            return NetworkGameManager.singleton as NetworkGameManager;
        }
    }

    public static Vector3 yAxis = new Vector3(0, 1, 0);

    public void HandleDisplayNameChanged(string oldValue, string newValue) => UpdateDisplay();

    private void UpdateDisplay()
    {
        Debug.Log("UpdateDisplay() called for Player " + DisplayName + ", netId = " + netId + ". isLocalPlayer = " + isLocalPlayer + ", hasAuthority = " + hasAuthority + ".");

        if (!NetworkManager.IsSceneActive(NetworkGameManagerInstance.RoomScene))
        {
            Debug.Log("Current game scene is NOT the room/lobby scene. Returning from UpdateDisplay() immediately.");
            return;
        }

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

    public void OnPlayerModelColorChanged(Color _Old, Color _New)
    {
        Debug.Log("OnPlayerModelColorChanged() called for player " + netId + ". _Old = " + _Old + ", _New = " + _New + ".");
        if (LobbyPlayerModel != null && isLocalPlayer)
        {
            Debug.Log("Modifying lobby player renderer component now. New color = " + _New + " (" + GameColors.COLOR_NAMES[_New] + ").");
            LobbyPlayerModel.GetComponentInChildren<Renderer>().sharedMaterials[1].color = _New;
        }
    }

    public override void OnStopClient()
    { 

    }

    public override void OnStartLocalPlayer()
    {
        // We actually only care about the ColorSelector on the MainMenu...
        GameObject.FindGameObjectWithTag("ColorSelector").GetComponent<ColorSelector>().PopulateButtons(this);
    }

    public override void OnStartAuthority()
    {
        Debug.Log("OnStartAuthority() called for RoomPlayer " + netId + ". isLocalPlayer = " + isLocalPlayer + ".");
    }

    [Command]
    public void CmdAssignInitialColor()
    {
        PlayerModelColor = GameObject.FindGameObjectWithTag("ColorSelector").GetComponent<ColorSelector>().ClaimRandomAvailableColor();
    }

    [Command]
    public void CmdAttemptClaimColor(Color desiredColor)
    {
        if (GameObject.FindGameObjectWithTag("ColorSelector").GetComponent<ColorSelector>().AttemptClaimColor(PlayerModelColor, desiredColor))
            PlayerModelColor = desiredColor;
    }

    [Client]
    public void OnClickColorButton(Color clickedColor)
    {
        if (!isLocalPlayer) return;

        CmdAttemptClaimColor(clickedColor);
    }


    [Command]
    private void CmdSetDisplayName(string displayName)
    {
        Debug.Log("CmdSetDisplayName called for RoomPlayer " + netId + ". isLocalPlayer = " + isLocalPlayer + ", hasAuthority = " + hasAuthority + ".");
        DisplayName = displayName;
    }

    public override void OnClientEnterRoom()
    {
        Debug.Log("OnClientEnterRoom() called for RoomPlayer " + netId + ". isLocalPlayer = " + isLocalPlayer + ", hasAuthority = " + hasAuthority + ".");
        if (isLocalPlayer)
        {
            //DisplayName = PlayerPrefs.GetString("nickname");
            Debug.Log("Player " + DisplayName + " joined the lobby.");

            Debug.Log("RoomPlayer " + netId + " is a local player, so creating UI hooks now.");
            CreateUIHooks();

            if (hasAuthority)
            {
                Debug.Log("RoomPlayer " + netId + " has authority, so calling CmdSetDisplayName now...");
                CmdSetDisplayName(PlayerPrefs.GetString("nickname"));
            }
        }
    }

    /// <summary>
    /// Create references to buttons, onClick listeners, and the lobby player list.
    /// </summary>
    [Client]
    public void CreateUIHooks()
    {
        Debug.Log("CreateUIHooks called for Player " + DisplayName + ", netId = " + netId + ", isLocalPlayer = " + isLocalPlayer + ", hasAuthority = " + hasAuthority + ".");

        if (uiHooksCreated)
        {
            Debug.Log("UI Hooks already created. Returning.");
            return;
        }

        if (LobbyPlayerModel == null)
        {
            LobbyPlayerModel = GameObject.FindWithTag("LobbyPlayerModel");
        }
        
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
            {
                if (hasAuthority)
                    button.OnClick.AddListener(ReadyUp);
                else
                    button.enabled = false;
            }
            else if (button.name.Equals("LeaveButton"))
            {
                
                if (hasAuthority)
                    button.OnClick.AddListener(LeaveLobby);
                else
                    button.enabled = false;
            }
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

        chatHandler = GetComponent<ChatHandler>();

        if (chatHandler != null)
        {
            Debug.Log("Calling CreateUIHooks() on chatHandler now.");
            chatHandler.CreateUIHooks();
        }

        //Debug.Log("Player " + netId + " about to get assigned initial color. colorSelector is null? " + (colorSelector == null) + ", colorSelector.roomPlayer is null? " + (colorSelector.RoomPlayer == null));
        CmdAssignInitialColor();

        uiHooksCreated = true;
    }

    public override void OnClientExitRoom()
    {
        Debug.Log("OnClientExitRoom() called for player " + DisplayName + ", netId = " + netId + ", isLocalPlayer = " + isLocalPlayer + ", hasAuthority = " + hasAuthority + ".");

        if (isLocalPlayer) uiHooksCreated = false;

        if (LobbyPlayerList == null)
        {
            Debug.LogWarning("LobbyPlayerList is null for Player " + DisplayName + ", netId = " + netId + ", cannot remove entry from list...");
            return;
        }

        Debug.Log("Removing entry for player " + DisplayName + ", netId = " + netId + ", from lobby player list now.");

        LobbyPlayerList.RemoveEntry(netId, DisplayName, false);
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // Rotate the player model.
        if (LobbyPlayerModel != null)
        {
            LobbyPlayerModel.transform.Rotate(yAxis, ModelRotationSpeed * Time.deltaTime);
        }
    }

    public override void ReadyStateChanged(bool _, bool newReadyState)
    {
        Debug.Log("ReadyStateChanged() called for RoomPlayer " + DisplayName + ", netId = " + netId + ", isLocalPlayer = " + isLocalPlayer + ", hasAuthority = " + hasAuthority + ", (LobbyPlayerList == null) = " + (LobbyPlayerList == null));

        // if (LobbyPlayerList == null) return;
        UpdateDisplay();
    }

    public void LeaveLobby()
    {
        if (!isLocalPlayer) return;

        Debug.Log("Player " + netId + " clicked Leave button. isLocalPlayer = " + isLocalPlayer + ", hasAuthority = " + hasAuthority);

        if (NetworkServer.active && NetworkClient.isConnected) // Stop host if host mode.
            NetworkGameManagerInstance.StopHost();
        else if (NetworkClient.isConnected)     // Stop client only.
            NetworkGameManagerInstance.StopClient();
        else if (NetworkServer.active)          // Stop server only.
            NetworkGameManagerInstance.StopServer();
    }
    
    [Client]
    public void ReadyUp()
    {
        Debug.Log("Player " + netId + " clicked Ready button. isLocalPlayer = " + isLocalPlayer + ", hasAuthority = " + hasAuthority + ", readyToBegin = " + readyToBegin);

        //if (!isLocalPlayer) return;

        if (hasAuthority) CmdChangeReadyState(!readyToBegin);
    }
}
