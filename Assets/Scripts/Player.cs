using UnityEngine;
using Mirror;
using System.Collections;

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

    public bool isDead
    {
        get { return _isDead; }
        protected set { _isDead = value; }
    }

    public IRole Role { get; set; }

    public string Nickname { get; set; }

    void Start()
    {
        if (!isLocalPlayer)
        {
            DisableComponents();
            AssignRemoteLayer();
        }
        else
        {
            // Disable player graphics for local player
            //SetLayerRecursively(playerGraphics, LayerMask.NameToLayer(dontDrawLayerName));

            // Create PlayerUI
            playerUIInstance = Instantiate(playerUIPrefab);

            // Configure PlayerUI
            PlayerUI ui = playerUIInstance.GetComponent<PlayerUI>();
            if (ui == null)
                Debug.LogError("No PlayerUI component on PlayerUI prefab.");
            playerUI = ui;

            ui.SetPlayer(GetComponent<Player>());
            GetComponent<Player>().SetupPlayer();

            string _nickname = "Loading...";
            if (UserAccountManager.IsLoggedIn)
                _nickname = UserAccountManager.PlayerUsername;
            else
                _nickname = transform.name;

            CmdSetUsername(transform.name, _nickname);
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
                Role = new CrewmateRole();
                break;
            case "imposter":
                Role = new ImposterRole();
                break;
            case "assassin":
                Role = new AssassinRole();
                break;
            case "saboteur":
                Role = new SaboteurRole();
                break;
            case "sheriff":
                Role = new SheriffRole();
                break;
            default:
                Debug.LogError("Unknown role assigned to player " + nickname + ": " + role);
                break;
        }

        playerUI.SetRole(role);
    }

    public void SetupPlayer()
    {
        if (isLocalPlayer)
        {
            //Switch cameras
            GameManager.instance.SetSceneCameraActive(false);
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

    [Command]
    void CmdSetNickname(string playerID, string nickname)
    {
        Player player = GameManager.GetPlayer(playerID);
        if (player != null)
        {
            Debug.Log(nickname + " has joined!");
            player.Nickname = nickname;
        }
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

        // Enable the collider.
        Collider _col = GetComponent<Collider>();
        if (_col != null)
            _col.enabled = true;

        // Create spawn effect.
        // GameObject _gfxIns = (GameObject)Instantiate(spawnEffect, transform.position, Quaternion.identity);
        // Destroy(_gfxIns, 3f);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        string _netID = GetComponent<NetworkIdentity>().netId.ToString();
        Player _player = GetComponent<Player>();

        GameManager.RegisterPlayer(_netID, _player);
    }

    [Command]
    void CmdSetUsername(string playerID, string nickname)
    {
        Player player = GameManager.GetPlayer(playerID);
        if (player != null)
        {
            Debug.Log(nickname + " has joined!");
            player.nickname = nickname;
        }
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

        if (isLocalPlayer)
            GameManager.instance.SetSceneCameraActive(true);

        GameManager.UnRegisterPlayer(transform.name);
    }
}