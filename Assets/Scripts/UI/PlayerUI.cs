using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using Lean.Gui;
using System;
using IngameDebugConsole;

public class PlayerUI : MonoBehaviour
{
    private Player player;
    private PlayerController playerController;

    [Header("UI Elements")]
    public GameObject PrimaryActionButtonGameObject;
    public LeanButton InteractableButton;
    public Image CrewmateVictoryImage;
    public Image ImposterVictoryImage;
    public Image PlayerImage;
    public GameObject ReturnToLobbyButton;
    public TextMeshProUGUI WaitingOnHostText;
    public TextMeshProUGUI NicknameText;
    public TextMeshProUGUI RoleText;
    public TextMeshProUGUI AmmoReserveText;
    public TextMeshProUGUI AmmoClipText;
    public GameObject VotingUIPrefab;
    public GameObject PlayerUICanvas;
    public Healthbar HpBar;
    public TextMeshProUGUI HealthText;
    public Healthbar StaminaBar;
    public TextMeshProUGUI StaminaText;
    public Healthbar ReloadingProgressBar;
    public GameObject AmmoUI;
    public GameObject WeaponUI;
    public GameObject PrimaryInventoryPanel;
    public GameObject SecondaryInventoryPanel;
    public GameObject ExplosiveInventoryPanel;
    public GameObject WeaponUiEntryPrefab;
    public Crosshair PlayerCrosshair;

    private List<GameObject> weaponUiEntries = new List<GameObject>();

    private Coroutine WeaponUiFadeRoutine;

    public GameObject RoleAnimator;
    public Text RoleAnimationText;

    [Header("Misc.")]
    public float PlayerImageAlpha;

    private GameOptions gameOptions;
    private NetworkGameManager networkGameManager;
    private bool canInteractWithEmergencyButton;

    public event Action<uint> OnPlayerVoted;

    void Alive()
    {
        gameOptions = GameOptions.singleton;
        networkGameManager = NetworkManager.singleton as NetworkGameManager;

        // Make sure the debug image is disabled.
        GetComponentInChildren<Canvas>().GetComponent<Image>().enabled = false;
    }

    // Start is called before the first frame update.
    void Start()
    {
        // Hide end-of-game UI.
        CrewmateVictoryImage.enabled = false;
        ImposterVictoryImage.enabled = false;
        ReturnToLobbyButton.SetActive(false);
        WaitingOnHostText.enabled = false;

        gameOptions = GameOptions.singleton;
        networkGameManager = NetworkManager.singleton as NetworkGameManager;

        // Make sure the debug image is disabled.
        GetComponentInChildren<Canvas>().GetComponent<Image>().enabled = false;
    }

    // Update is called once per frame.
    void Update()
    {
        if (!player.isLocalPlayer) return;

        float distToEmergencyButton = playerController.GetSquaredDistanceToEmergencyButton();

        bool interactableWithinRange = false;

        if (distToEmergencyButton <= 25)
        {
            interactableWithinRange = true;
            canInteractWithEmergencyButton = true;
        }
        else
            canInteractWithEmergencyButton = false;

        if (interactableWithinRange)
            InteractableButton.enabled = true;
        else
            InteractableButton.enabled = false;

        if (Input.GetKey(KeyCode.E) && interactableWithinRange && canInteractWithEmergencyButton)
            OnInteractWithEmergencyButton();
    }

    #region UI Handlers 

    
    /// <summary>
    /// This function corresponds to the generic Inspect/Interact/Use button on the player's UI overlay.
    /// </summary>
    public void OnInteractButtonClicked()
    {
        if (!player.isLocalPlayer) return;

        if (canInteractWithEmergencyButton)
            OnInteractWithEmergencyButton();
    }

    /// <summary>
    /// Corresponds to the "Return to Lobby" button that gets displayed on the host's UI at the end of the game.
    /// </summary>
    public void OnReturnToLobbyPressed()
    {
        if (player.isClientOnly)
            Debug.LogError("Client-only player clicked the 'Return to Lobby' button.");

        // Go back to the room.
        networkGameManager.ServerChangeScene(networkGameManager.RoomScene);
    }

    
    public void OnInteractWithBody()
    {
        if (!player.isLocalPlayer) return;

        Debug.Log("Player interacted with body.");
    }

    
    public void OnInteractWithEmergencyButton()
    {
        if (!player.isLocalPlayer) return;

        Debug.Log("Player interacted with emergency button.");

        player.CmdStartVote();
    }

    #endregion

    #region Display UI

    
    public void DisplayEndOfGameUI(bool crewmateVictory)
    {
        if (crewmateVictory)
            CrewmateVictoryImage.enabled = true;
        else
            ImposterVictoryImage.enabled = true;

        if (player.isClientOnly)
            WaitingOnHostText.enabled = true;
        else
            ReturnToLobbyButton.SetActive(true);
    }

    
    public void CreateAndDisplayVotingUI()
    {
        PlayerUICanvas.SetActive(false);
        PlayerCrosshair.DisableCrosshair();

        GameObject votingUIGameObject = Instantiate(VotingUIPrefab, transform);
        VotingUI votingUI = votingUIGameObject.GetComponent<VotingUI>();
        votingUI.PlayerUIGameObject = PlayerUICanvas;
        votingUI.PlayerController = playerController;

        OnPlayerVoted += votingUI.PlayerVoted;
    }

    /// <summary>
    /// This will cause the I VOTED icon to display on the VotingUI, if it exists.
    
    public void PlayerVoted(uint voterId)
    {
        try
        {
            OnPlayerVoted?.Invoke(voterId);
        }
        catch (Exception ex)
        {
            // Do nothing, it's fine.
            Debug.LogWarning("Caught exception when attempting to fire OnPlayerVoted event.");
            Debug.LogException(ex);
        }
    }
    
    public void UpdateHealth(float health)
    {
        //HpBar.TakeDamage((int)health);
        HpBar.health = health;
        HpBar.UpdateHealth();
        HealthText.text = ((int)health).ToString();
    }

    
    public void ShowWeaponUI(IEnumerable<string> primaryWeapons, IEnumerable<string> secondaryWeapons, IEnumerable<string> explosiveWeapons)
    {
        foreach (GameObject gameObject in weaponUiEntries)
        {
            Destroy(gameObject);
        }

        foreach (string name in primaryWeapons)
        {
            GameObject entry = Instantiate(WeaponUiEntryPrefab, PrimaryInventoryPanel.transform);
            TextMeshProUGUI weaponName = entry.GetComponentInChildren<TextMeshProUGUI>();
            weaponName.text = name;
            weaponUiEntries.Add(entry);
        }

        foreach (string name in secondaryWeapons)
        {
            GameObject entry = Instantiate(WeaponUiEntryPrefab, SecondaryInventoryPanel.transform);
            TextMeshProUGUI weaponName = entry.GetComponentInChildren<TextMeshProUGUI>(); 
            weaponName.text = name;
            weaponUiEntries.Add(entry);
        }

        foreach (string name in explosiveWeapons)
        {
            GameObject entry = Instantiate(WeaponUiEntryPrefab, ExplosiveInventoryPanel.transform);
            TextMeshProUGUI weaponName = entry.GetComponentInChildren<TextMeshProUGUI>();
            weaponName.text = name;
            weaponUiEntries.Add(entry);
        }

        WeaponUI.SetActive(true);
        InitWeaponUIFade();
    }

    
    public void InitWeaponUIFade()
    {
        CanvasGroup canvasGroup = WeaponUI.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1.0f;
        if (WeaponUiFadeRoutine != null)
        {
            StopCoroutine(WeaponUiFadeRoutine);
        }
        WeaponUiFadeRoutine = StartCoroutine(DoFade());
    }

    IEnumerator DoFade()
    {
        CanvasGroup canvasGroup = WeaponUI.GetComponent<CanvasGroup>();
        yield return new WaitForSeconds(3.0f);
        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime / 2;
            yield return null;
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;
        WeaponUI.SetActive(false);
        yield return null;
    }

    
    public void SetUpHpBar(float maxHealth)
    {
        HpBar.maximumHealth = maxHealth;
    }

    #endregion

    #region Player Management 

    
    public void AnimateRole()
    {
        RoleAnimator.SetActive(true);
        RoleAnimator.GetComponent<Animator>().Play("Expand");
    }

    
    public void SetPlayer(Player player)
    {
        this.player = player;
        playerController = player.GetComponent<PlayerController>();
        PlayerImage.color = new Color(player.PlayerColor.r, player.PlayerColor.g, player.PlayerColor.b, PlayerImageAlpha);

        SetNickname(player.Nickname);
        RegisterConsoleCommands();
    }

    
    public void SetNickname(string nickname)
    {
        NicknameText.text = nickname;
    }

    
    public void SetRole(string roleName)
    {
        RoleText.text = roleName.ToUpper();
        RoleAnimationText.text = roleName.ToUpper();

        AnimateRole();
    }
    #endregion

    #region Console Commands

    /// <summary>
    /// Returns True if the player associated with this UI has the authority to execute console commands.
    /// 
    /// This is based on whether or not the player is the host.
    /// </summary>
    private bool hasAuthority()
    {
        return !this.player.isClientOnly;
    }

    private void ListPlayers()
    {
        Player[] players = networkGameManager.GamePlayers.ToArray();
        Array.Sort(players, (x, y) => x.netId.CompareTo(y.netId));
        Debug.Log("== Listing Players ==");
        foreach (Player player in players)
        {
            Debug.Log(String.Format("{0}\t{1}", player.netId, player.Nickname));
        }
    }

    private void GiveWeapon(int weaponId, int playerId)
    {
        uint netId = (uint)playerId;

        networkGameManager.NetIdMap[netId].GetComponent<PlayerController>().CmdGivePlayerWeapon(weaponId, false);
    }

    private void InfiniteAmmo(int playerId)
    {
        uint netId = (uint)playerId;

        networkGameManager.NetIdMap[netId].GetComponent<PlayerController>().CmdInfiniteAmmo();
    }

    private void TeleportPosition(int playerId, Vector3 newPosition)
    {
        uint netId = (uint)playerId;

        networkGameManager.NetIdMap[netId].GetComponent<PlayerController>().CmdSetPosition(newPosition);
    }

    private void TeleportToOtherPlayer(int playerIdSrc, int playerIdDst)
    {
        uint netIdSrc = (uint)playerIdSrc;
        uint netIdDst = (uint)playerIdDst;

        networkGameManager.NetIdMap[netIdSrc].GetComponent<PlayerController>().CmdMoveToPlayer(netIdDst);
    }

    private void SetHealth(int playerId, float healthValue)
    {
        uint netId = (uint)playerId;

        networkGameManager.NetIdMap[netId].GetComponent<Player>().CmdSetHealth(healthValue);
    }

    /// <summary>
    /// Register all of our console commands with the debug console.
    /// </summary>
    public void RegisterConsoleCommands()
    {
        if (!hasAuthority()) return;

        DebugLogConsole.AddCommand("listplayers", "listplayers, Lists the players in the game.", ListPlayers);
        DebugLogConsole.AddCommand<int>("ammo", "ammo <netId> - gives the specified player 999999 of each ammo type.", InfiniteAmmo);
        DebugLogConsole.AddCommand<int, int>("give", "give <netId> <gunId>", GiveWeapon);
        DebugLogConsole.AddCommand<int, Vector3>("tppos", "tppos <netId> [<x> <y> <z>]", TeleportPosition);
        DebugLogConsole.AddCommand<int, int>("tp", "tp <netIdSrc> <netIdDst>", TeleportToOtherPlayer);
        DebugLogConsole.AddCommand<int, float>("sethealth", "sethealth <netId> <health>", SetHealth);
    }

    #endregion 
}
