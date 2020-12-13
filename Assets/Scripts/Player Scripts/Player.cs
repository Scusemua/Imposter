using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections;
using System;
using MilkShake;

[RequireComponent(typeof(PlayerController))]
public class Player : NetworkBehaviour 
{
    public ShakePreset ExplosionShakePreset;
    public ShakePreset GunshotShakePreset;

    [Header("UI Related")]
    public GameObject PlayerUIPrefab;

    [HideInInspector]
    public GameObject PlayerUIGameObject;

    [HideInInspector]
    public PlayerUI PlayerUI;

    public GameObject FloatingInfo;
    public Healthbar FloatingHealthBar;
    public TextMesh PlayerNameText;
    [SerializeField] GameObject muzzleFlashPrefab;
    [SerializeField] public Transform WeaponMuzzle;

    [SyncVar(hook = nameof(OnHealthChanged))] public float Health = 100;
    [SyncVar] public float HealthMax = 100;

    [SyncVar(hook = nameof(OnNameChanged))]
    public string Nickname = "Loading...";

    [Header("In-Game Related")]

    [SyncVar(hook = nameof(OnPlayerColorChanged))]
    public Color PlayerColor;

    /// <summary>
    /// The netId of the player who killed this player.
    /// </summary>
    [HideInInspector] [SyncVar] public uint KillerId;
    [HideInInspector] [SyncVar] public int CauseOfDeath;

    public IRole Role { get; set; }

    [SyncVar]
    private bool _isDead = false;
    public bool IsDead
    {
        get { return _isDead; }
        protected set { _isDead = value; }
    }

    /// <summary>
    /// Marks whether or not the player has been identified.
    /// </summary>
    [SyncVar]
    public bool Identified = false;

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

    void OnNameChanged(string _Old, string _New)
    {
        //Debug.Log("OnNameChanged called. Old = " + _Old + ", New = " + _New);
        PlayerNameText.text = Nickname;
    }
    #endregion

    #region Commands 

    [Command]
    public void CmdStartVote()
    {
        networkGameManager.StartVote();
    }

    [Command]
    public void CmdCastVote(uint netId)
    {
        Debug.Log("Player " + Nickname + ", netId = " + netId + " has voted for player <TBD>");

        this.networkGameManager.CastVote(this, netId);
    }

    [Command]
    public void CmdSetupPlayer(string _name)
    {
        IsDead = false;

        //Debug.Log("CmdSetupPlayer --> _name = nickname = " + _name);

        // player info sent to server, then server updates sync vars which handles it on all clients
        Nickname = _name;
    }

    [Command]
    public void CmdRegisterPlayer()
    {
        uint _netID = GetComponent<NetworkIdentity>().netId;
        NetworkGameManager.RegisterPlayer(_netID, this);
    }

    [Command(ignoreAuthority = true)]
    public void CmdKill(uint killerId)
    {
        this.KillerId = killerId;
        Die(1f, 0f, NetworkGameManager.NetIdMap[killerId].transform.position);
    }

    [Command]
    public void CmdSuicide()
    {
        Die(1f, 0f, transform.position);
    }

    [Command(ignoreAuthority = true)]
    public void CmdDoDamage(float amount, float force, uint damageSrcPlayerId, int damageType, Vector3 damagePosition)
    {
        Damage(amount, damageSrcPlayerId, damageType, damagePosition, force, 0f);
    }

    [Command(ignoreAuthority = true)]
    public void CmdSetHealth(float newHealthValue)
    {
        Health = newHealthValue;
        TargetGotDamaged(); // Just updates UI.

        if (Health <= 0)
            Die(0f, 0f, transform.position);
    }

    [Command(ignoreAuthority = true)]
    public void CmdIdentify(uint inspectorId)
    {
        if (!Identified)
        {
            Identified = true;

            // TODO: Do something now that they're identified?
        }
        else
        {
            Player inspector = NetworkGameManager.NetIdMap[inspectorId];
            inspector.TargetShowIdentificationUI(netId);
        }

    }

    #endregion

    #region ClientRPC

    [ClientRpc]
    public void RpcPlayerVoted(uint voterId)
    {
        // Pass this event to the UI.
        PlayerUI.PlayerVoted(voterId);
    }

    /// <summary>
    /// Called by Server (NetworkGameManager, specifically) on each player object (including dead players).
    /// </summary>
    [ClientRpc]
    public void RpcBeginVoting()
    {
        Debug.Log("Voting has started.");
        GetComponent<PlayerController>().MovementEnabled = false;

        PlayerUI.CreateAndDisplayVotingUI();
    }

    [ClientRpc]
    public void RpcEndVoting()
    {
        Debug.Log("Voting has ended.");
    }

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
                Debug.LogError("Unknown role assigned to player " + Nickname + ": " + role);
                break;
        }

        Debug.Log("Player " + Nickname + ", netId = " + netId + ", assigned role " + role);

        Role.AssignPlayer(this);

        if (isLocalPlayer)
        {
            PlayerUI.SetRole(role);

            if (NetworkGameManager.IsImposterRole(role))
                GetComponent<PlayerController>().PlayImposterStart();
            else
                GetComponent<PlayerController>().PlayCrewmateStart();
        }
    }

    [ClientRpc]
    public void RpcKill(float damageForce, float extraArg, Vector3 damagePosition, int causeOfDeath)
    {
        // Disable the name and healthbar.
        FloatingInfo.SetActive(false);

        if (causeOfDeath == DamageSource.Explosion)
            GetComponent<PlayerController>().DieFromExplosion(damageForce, extraArg, damagePosition);
        else
            GetComponent<PlayerController>().Die();
    }

    #endregion

    #region Client Functions

    public override void OnStartLocalPlayer()
    {
        if (!isLocalPlayer) return;

        // Create PlayerUI
        PlayerUIGameObject = Instantiate(PlayerUIPrefab);

        // Configure PlayerUI
        PlayerUI ui = PlayerUIGameObject.GetComponent<PlayerUI>();
        if (ui == null)
            Debug.LogError("No PlayerUI component on PlayerUI prefab.");
        PlayerUI = ui;

        Nickname = PlayerPrefs.GetString("nickname", default_nicknames[RNG.Next(default_nicknames.Length)]);
        PlayerNameText.text = Nickname;

        ui.SetPlayer(GetComponent<Player>());

        PlayerUIGameObject.SetActive(true);

        GetComponent<PlayerController>().UpdateAmmoDisplay();

        ui.SetUpHpBar(HealthMax);

        //Debug.Log("Player nickname: " + Nickname);

        CmdSetupPlayer(Nickname);   // This is required for nicknames to be properly synchronized.
        CmdRegisterPlayer();
    }

    [Client]
    public void DoCameraShake(bool explosion)
    {
        if (explosion)
            GetComponent<PlayerController>().Camera.GetComponent<Shaker>().Shake(ExplosionShakePreset);
        else
            GetComponent<PlayerController>().Camera.GetComponent<Shaker>().Shake(GunshotShakePreset);
    }

    [Client]
    public void PlayAudioClip(AudioClip clip)
    {
        GetComponent<AudioSource>().PlayOneShot(clip);
    }

    [Client]
    public void OnPlayerColorChanged(Color _, Color _New)
    {
        //Debug.Log("OnPlayerModelColorChanged() called for player " + netId);
        GetComponentInChildren<Renderer>().material.color = _New;
    }

    [Client]
    public void OnHealthChanged(float _Old, float _New)
    {
        FloatingHealthBar.health = _New;
        FloatingHealthBar.UpdateHealth();
    }

    [Client]
    /// <summary>
    /// Display the end-of-game screen for whichever outcome (i.e., Imposter victory or Crewmate victory).
    /// </summary>
    /// <param name="crewmateVictory"></param>
    public void DisplayEndOfGameUI(bool crewmateVictory)
    {
        PlayerUI.DisplayEndOfGameUI(crewmateVictory);
    }

    #endregion

    #region TargetRPC

    [TargetRpc]
    public void TargetGotDamaged()
    {
        PlayerUI.UpdateHealth(Health);
    }

    [TargetRpc]
    public void TargetShowIdentificationUI(uint targetId)
    {
        Player target = NetworkGameManager.NetIdMap[targetId];
        PlayerUI.PlayerIdentificationUI.DisplayUI(target);
    }

    [TargetRpc]
    public void TargetDoCameraShake(bool explosion)
    {
        DoCameraShake(explosion);
    }

    #endregion

    #region Server Functions 

    /// <summary>
    /// Deal damage to this player.
    /// </summary>
    /// <param name="amount">Amount of damage dealt.</param>
    /// <param name="damageSrcPlayerId">netId of the player responsible for damaging this player.</param>
    /// <param name="damageSource">The type of damage being done to this player.</param>
    /// <param name="damagePosition">The position/direction that this damage came from.</param>
    /// <param name="damageForce">Any force associated with the damage (e.g., how much a bullet should push, or an explosive force).</param>
    /// <param name="extraArg">Some extra argument that is relevant depending on the damage type.</param>
    [Server]
    public void Damage(
        float amount, 
        uint damageSrcPlayerId, 
        int damageSource,
        Vector3 damagePosition,
        float damageForce,
        float extraArg)
    {
        Health -= amount;
        TargetGotDamaged();

        if (Health <= 0)
        {
            KillerId = damageSrcPlayerId;
            CauseOfDeath = damageSource;
            Die(damageForce, extraArg, damagePosition);
        }
    }

    /// <summary>
    /// Handle death.
    /// </summary>
    /// <param name="damageForce">The force associated with whatever killed us (e.g., a bullet, a projectile, an explosion).</param>
    /// <param name="extraArg">Some extra argument relevant to the damage source.</param>
    /// <param name="damagePosition">The position/direction that the damage was inflicted from.</param>
    [Server]
    public void Die(float damageForce, float extraArg, Vector3 damagePosition)
    {
        if (KillerId == netId)
            CauseOfDeath = DamageSource.Suicide;

        _isDead = true;

        RpcKill(damageForce, extraArg, damagePosition, CauseOfDeath);
    }

    #endregion

    // When we are destroyed
    void OnDisable()
    {
        Destroy(PlayerUIGameObject);

        if (isLocalPlayer && hasAuthority)
            NetworkGameManager.GamePlayers.Remove(this);
    }

    public override void OnStopClient()
    {
        Destroy(PlayerUIGameObject);

        if (isLocalPlayer && hasAuthority)
            NetworkGameManager.GamePlayers.Remove(this);
    }
}