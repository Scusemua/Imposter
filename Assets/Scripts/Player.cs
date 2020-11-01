using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections;
using System;

[RequireComponent(typeof(PlayerController))]
public class Player : NetworkBehaviour 
{
    [SyncVar]
    private bool _isDead = false;

    [SyncVar(hook = nameof(OnNameChanged))]
    public string nickname = "Loading...";

    [SerializeField]
    private Behaviour[] disableOnDeath;
    private bool[] wasEnabled;

    [SerializeField]
    private GameObject[] disableGameObjectsOnDeath;

    [SerializeField]
    private GameObject deathEffect;

    [SerializeField]
    private GameObject spawnEffect;

    [SerializeField]
    Behaviour[] componentsToDisable;

    [SerializeField]
    string remoteLayerName = "RemotePlayer";

    [SerializeField]
    string dontDrawLayerName = "DontDraw";

    [SerializeField]
    GameObject playerGraphics;

    [SerializeField]
    GameObject playerUIPrefab;

    [HideInInspector]
    public GameObject playerUIInstance;

    [HideInInspector]
    public PlayerUI playerUI;

    private bool firstSetup = true;

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

    public TextMesh playerNameText;

    public bool isDead
    {
        get { return _isDead; }
        protected set { _isDead = value; }
    }

    void OnNameChanged(string _Old, string _New)
    {
        Debug.Log("OnNameChanged called. Old = " + _Old + ", New = " + _New);
        playerNameText.text = nickname;
    }

    public IRole Role { get; set; }

    public void DisplayEndOfGameUI(bool crewmateVictory)
    {
        playerUI.DisplayEndOfGameUI(crewmateVictory);
    }

    [Command]
    public void CmdSetupPlayer(string _name)
    {
        isDead = false;

        Debug.Log("CmdSetupPlayer --> _name = nickname = " + _name);

        // player info sent to server, then server updates sync vars which handles it on all clients
        nickname = _name;
    }

    public override void OnStartLocalPlayer()
    {
        if (!isLocalPlayer) return;

        Debug.Log("OnStartLocalPlayer() called...");

        // Create PlayerUI
        playerUIInstance = Instantiate(playerUIPrefab);

        // Configure PlayerUI
        PlayerUI ui = playerUIInstance.GetComponent<PlayerUI>();
        if (ui == null)
            Debug.LogError("No PlayerUI component on PlayerUI prefab.");
        playerUI = ui;

        ui.SetPlayer(GetComponent<Player>());

        playerUIInstance.SetActive(true);

        CmdRegisterPlayer();
    }
    
    [TargetRpc]
    public void TargetAssignRole(string role)
    {
        CmdAssignedRole(role);
    }

    [Command]
    public void CmdRegisterPlayer()
    {
        string _netID = GetComponent<NetworkIdentity>().netId.ToString();
        NetworkGameManager.RegisterPlayer(_netID, this);
    }


    [Command]
    public void CmdAssignedRole(string role)
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

        Debug.Log("Role assigned: " + role);

        Role.AssignPlayer(this);

        if (isLocalPlayer)
            playerUI.SetRole(role);
    }

    public void Kill(Player killer, bool serverKilled = false)
    {
        if (killer == null && serverKilled)
        {
            Debug.Log("The server has killed player " + this.nickname);
        }
        else if (killer != null)
            Debug.Log("Player " + nickname + " has been killed by player " + killer.nickname + ", who is a/an " + killer.Role.Name);
        else
            Debug.LogError("Player " + nickname + "(" + this.netId + ") was killed by NULL, and the server didn't kill the player...");

        _isDead = true;
    }

    public override void OnStartClient()
    {
        //DontDestroyOnLoad(gameObject);

        if (!isLocalPlayer) return;

        nickname = PlayerPrefs.GetString("nickname", default_nicknames[RNG.Next(default_nicknames.Length)]);

        Debug.Log("Player nickname: " + nickname);

        CmdSetupPlayer(nickname);
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    // When we are destroyed
    void OnDisable()
    {
        Destroy(playerUIInstance);

        if (isLocalPlayer && hasAuthority)
            NetworkGameManager.GamePlayers.Remove(this);
    }
}