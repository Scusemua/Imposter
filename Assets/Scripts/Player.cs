using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections;
using System;

[RequireComponent(typeof(PlayerController))]
public class Player : NetworkBehaviour 
{
    [Header("UI Related")]
    public GameObject playerUIPrefab;

    [HideInInspector]
    public GameObject playerUIInstance;

    [HideInInspector]
    public PlayerUI playerUI;

    public TextMesh PlayerNameText;

    [SyncVar(hook = nameof(OnNameChanged))]
    public string nickname = "Loading...";

    [Header("In-Game Related")]

    [SyncVar(hook = nameof(OnPlayerColorChanged))]
    public Color PlayerColor;

    public IRole Role { get; set; }

    public bool isDead
    {
        get { return _isDead; }
        protected set { _isDead = value; }
    }

    [SyncVar(hook = nameof(OnAliveStatusChanged))]
    private bool _isDead = false;

    private static string[] default_nicknames = { "Sally", "Betty", "Charlie", "Anne", "Bob" };

    private System.Random RNG = new System.Random();

    private NetworkGameManager networkGameManager;
    private NetworkGameManager NetworkGameManager
    {
        get
        {
            if (networkGameManager == null)
                networkGameManager = NetworkManager.singleton as NetworkGameManager;
            return networkGameManager;
        }
    }

    #region SyncVar Hooks
    void OnAliveStatusChanged(bool _Old, bool _New)
    {
        if (isDead)
        {
            CmdKill("Someone", false);
            Debug.Log("Player " + nickname + " is now dead.");
            GetComponent<PlayerController>().Die();
        }
        else
            Debug.Log("Player " + nickname + " is now alive.");
    }

    void OnNameChanged(string _Old, string _New)
    {
        Debug.Log("OnNameChanged called. Old = " + _Old + ", New = " + _New);
        PlayerNameText.text = nickname;
    }
    #endregion

    #region Commands 

    [Command]
    public void CmdSetupPlayer(string _name)
    {
        isDead = false;

        Debug.Log("CmdSetupPlayer --> _name = nickname = " + _name);

        // player info sent to server, then server updates sync vars which handles it on all clients
        nickname = _name;
    }

    [Command]
    public void CmdRegisterPlayer()
    {
        string _netID = GetComponent<NetworkIdentity>().netId.ToString();
        NetworkGameManager.RegisterPlayer(_netID, this);
    }

    [Command(ignoreAuthority = true)]
    public void CmdKill(string killerNickname, bool serverKilled)
    {
        Debug.Log("The server has been informed that player " + nickname + " has been killed by " + killerNickname);
        RpcKill(killerNickname, serverKilled);
    }

    [Command]
    public void CmdSuicide()
    {
        RpcKill("SUICIDE", false);
    }

    #endregion

    #region ClientRPC

    [ClientRpc]
    public void RpcAssignRole(string role)
    {
        switch (role)
        {
            case "crewmate":
                Role = gameObject.AddComponent<CrewmateRole>() as CrewmateRole;
                break;
            case "imposter":
                Role = gameObject.AddComponent<ImposterRole>() as ImposterRole;
                break;
            case "assassin":
                Role = gameObject.AddComponent<AssassinRole>() as AssassinRole;
                break;
            case "saboteur":
                Role = gameObject.AddComponent<SaboteurRole>() as SaboteurRole;
                break;
            case "sheriff":
                Role = gameObject.AddComponent<SheriffRole>() as SheriffRole;
                break;
            default:
                Debug.LogError("Unknown role assigned to player " + nickname + ": " + role);
                break;
        }

        Debug.Log("Player " + nickname + ", netId = " + netId + "assigned role " + role);

        Role.AssignPlayer(this);

        if (isLocalPlayer)
        {
            playerUI.SetRole(role);

            if (NetworkGameManager.IsImposterRole(role))
                GetComponent<PlayerController>().PlayImposterStart();
            else
                GetComponent<PlayerController>().PlayCrewmateStart();
        }
    }

    [ClientRpc]
    public void RpcKill(string killerNickname, bool serverKilled)
    {
        if (serverKilled)
            Debug.Log("The server has killed player " + this.nickname);
        else
            Debug.Log("Player " + nickname + " has been killed by player " + killerNickname + ".");

        _isDead = true;
        GetComponent<PlayerController>().Die();
    }

    #endregion

    #region Client Commands

    public override void OnStartLocalPlayer()
    {
        Debug.Log("OnStartLocalPlayer() called...");

        // Create PlayerUI
        playerUIInstance = Instantiate(playerUIPrefab);

        // Configure PlayerUI
        PlayerUI ui = playerUIInstance.GetComponent<PlayerUI>();
        if (ui == null)
            Debug.LogError("No PlayerUI component on PlayerUI prefab.");
        playerUI = ui;

        nickname = PlayerPrefs.GetString("nickname", default_nicknames[RNG.Next(default_nicknames.Length)]);
        PlayerNameText.text = nickname;

        ui.SetPlayer(GetComponent<Player>());

        playerUIInstance.SetActive(true);

        Debug.Log("Player nickname: " + nickname);

        CmdSetupPlayer(nickname);   // This is required for nicknames to be properly synchronized.
        CmdRegisterPlayer();
    }

    [Client]
    public void OnPlayerColorChanged(Color _, Color _New)
    {
        Debug.Log("OnPlayerModelColorChanged() called for player " + netId);
        GetComponentInChildren<Renderer>().material.color = _New;
    }

    [Client]
    /// <summary>
    /// Display the end-of-game screen for whichever outcome (i.e., Imposter victory or Crewmate victory).
    /// </summary>
    /// <param name="crewmateVictory"></param>
    public void DisplayEndOfGameUI(bool crewmateVictory)
    {
        playerUI.DisplayEndOfGameUI(crewmateVictory);
    }

    #endregion

    #region Server Commands 

    [Server]
    public void Kill(string killerNickname, bool serverKilled)
    {
        if (serverKilled)
            Debug.Log("The server has killed player " + this.nickname);
        else
            Debug.Log("Player " + nickname + " has been killed by player " + killerNickname + ".");

        _isDead = true;
        GetComponent<PlayerController>().Die();
    }

    #endregion

    // When we are destroyed
    void OnDisable()
    {
        Destroy(playerUIInstance);

        if (isLocalPlayer && hasAuthority)
            NetworkGameManager.GamePlayers.Remove(this);
    }

    public override void OnStopClient()
    {
        Destroy(playerUIInstance);

        if (isLocalPlayer && hasAuthority)
            NetworkGameManager.GamePlayers.Remove(this);
    }
}