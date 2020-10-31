using UnityEngine;
using Mirror;
using System.Collections;
using System;

[RequireComponent(typeof(PlayerController))]
public class Player : NetworkBehaviour 
{
    [SyncVar]
    private bool _isDead = false;

    [SyncVar]
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

    private GameManager gameManager;

    public bool isDead
    {
        get { return _isDead; }
        protected set { _isDead = value; }
    }

    public IRole Role { get; set; }

    [Command]
    public void DisplayEndOfGameUI(bool crewmateVictory)
    {
        playerUI.DisplayEndOfGameUI(crewmateVictory);
    }

    void Start()
    {
        gameManager = GameManager.singleton as GameManager;
        if (!isLocalPlayer)
        {
            DisableComponents();
            AssignRemoteLayer();
        }
        else
        {
            // Create PlayerUI
            playerUIInstance = Instantiate(playerUIPrefab);

            // Configure PlayerUI
            PlayerUI ui = playerUIInstance.GetComponent<PlayerUI>();
            if (ui == null)
                Debug.LogError("No PlayerUI component on PlayerUI prefab.");
            playerUI = ui;

            nickname = PlayerPrefs.GetString("nickname", default_nicknames[RNG.Next(default_nicknames.Length)]);

            ui.SetPlayer(GetComponent<Player>());
            GetComponent<Player>().SetupPlayer();
        }
    }

    void Update()
    {

    }

    public void AssignRole(string role)
    {
        switch(role)
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

    public void SetupPlayer()
    {
        if (isLocalPlayer)
        {
            //Switch cameras
            playerUIInstance.SetActive(true);
        }

        CmdBroadCastNewPlayerSetup();
    }

    [Command]
    private void CmdBroadCastNewPlayerSetup()
    {
        RpcSetupPlayerOnAllClients();
    }


    [ClientRpc]
    private void RpcSetupPlayerOnAllClients()
    {
        if (firstSetup)
        {
            wasEnabled = new bool[disableOnDeath.Length];
            for (int i = 0; i < wasEnabled.Length; i++)
            {
            wasEnabled[i] = disableOnDeath[i].enabled;
            }

            firstSetup = false;
        }

        SetDefaults();
    }

    public void SetDefaults()
    {
        isDead = false;

        // Enable the components.
        for (int i = 0; i < disableOnDeath.Length; i++)
        {
            disableOnDeath[i].enabled = wasEnabled[i];
        }

        // Enable the gameobjects.
        for (int i = 0; i < disableGameObjectsOnDeath.Length; i++)
        {
            disableGameObjectsOnDeath[i].SetActive(true);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        string _netID = GetComponent<NetworkIdentity>().netId.ToString();
        Player _player = GetComponent<Player>();

        _player.nickname = nickname;

        gameManager.RegisterPlayer(_netID, _player);
    }

    void AssignRemoteLayer()
    {
        gameObject.layer = LayerMask.NameToLayer(remoteLayerName);
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    void DisableComponents()
    {
        for (int i = 0; i < componentsToDisable.Length; i++)
        {
            componentsToDisable[i].enabled = false;
        }
    }

    // When we are destroyed
    void OnDisable()
    {
        Destroy(playerUIInstance);

        //if (isLocalPlayer)
        //    GameManager.singleton.SetSceneCameraActive(true);

        gameManager.UnRegisterPlayer(transform.name);
    }
}