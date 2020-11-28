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
    [Tooltip("The UI for the detective scanner item.")]
    public DetectiveScannerUI DetectiveScannerUI;
    [Tooltip("The UI for identifying a dead player's body.")]
    public PlayerIdentificationUI PlayerIdentificationUI;

    [Tooltip("The tab menu UI.")]
    public GameObject TabMenuUI;
    [Tooltip("The content portion of the tab menu UI scrollview.")]
    public GameObject TabMenuUIContent;
    public GameObject TabMenuEntryPrefab;

    private List<TabMenuEntry> tabMenuEntries = new List<TabMenuEntry>();

    private List<GameObject> weaponUiEntries = new List<GameObject>();

    private Coroutine WeaponUiFadeRoutine;

    public GameObject RoleAnimator;
    public Text RoleAnimationText;

    [Header("Misc.")]
    public float PlayerImageAlpha;

    private GameOptions gameOptions;
    private NetworkGameManager networkGameManager;

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

        if (Input.GetKeyDown(KeyCode.Tab))
            CreateTabMenu();

        if (Input.GetKeyUp(KeyCode.Tab))
        {
            TabMenuUI.SetActive(false);

            foreach (TabMenuEntry entry in tabMenuEntries)
                Destroy(entry.gameObject);

            tabMenuEntries.Clear();
        }
    }

    #region UI Handlers 

    /// <summary>
    /// Create and setup the tab menu.
    /// </summary>
    public void CreateTabMenu()
    {
        TabMenuUI.SetActive(true);

        Debug.Log("player.Role.ToString() = " + player.Role.ToString());

        // We build this menu differently depending on whether or not the local player is an Imposter of some sort.
        if (NetworkGameManager.IsImposterRole(player.Role.ToString()))
        {
            Debug.Log("Creating imposter tab menu.");
            // Imposter setup.
            foreach (Player gamePlayer in networkGameManager.GamePlayers)
            {
                TabMenuEntry entry = Instantiate(TabMenuEntryPrefab, TabMenuUIContent.transform).GetComponent<TabMenuEntry>();
                tabMenuEntries.Add(entry);
                entry.NameText.text = gamePlayer.Nickname;
                entry.NameText.color = gamePlayer.PlayerColor;
                entry.RoleText.text = gamePlayer.Role.ToString();

                Debug.Log("gamePlayer " + gamePlayer.Nickname + ", netId = " + gamePlayer.netId + ", has role " + gamePlayer.Role.ToString());

                if (NetworkGameManager.IsImposterRole(gamePlayer.Role.ToString()))
                {
                    entry.BackgroundColor = TabMenuEntry.ImposterColor;
                }
                else if (gamePlayer.Role.ToString() == "SHERIFF")
                {
                    entry.BackgroundColor = TabMenuEntry.SheriffColor;
                }
                else
                {
                    entry.BackgroundColor = TabMenuEntry.CrewmateColor;
                }

                // Since the local player is an imposter, we'll designate dead-but-unidentified players as such.
                if (gamePlayer.IsDead)
                {
                    if (gamePlayer.Identified)
                        entry.StatusText.text = "Unidentified";
                    else
                        entry.StatusText.text = "Dead";
                }
                else
                {
                    entry.StatusText.text = "Alive";
                }
            }
        }
        else
        {
            Debug.Log("Creating crewmate tab menu.");

            // Crewmate setup.
            foreach (Player gamePlayer in networkGameManager.GamePlayers)
            {
                TabMenuEntry entry = Instantiate(TabMenuEntryPrefab, TabMenuUIContent.transform).GetComponent<TabMenuEntry>();
                tabMenuEntries.Add(entry);
                entry.NameText.text = gamePlayer.Nickname;
                entry.NameText.color = gamePlayer.PlayerColor;

                // Crewmates can see the sheriff. Otherwise we just designate everyone as Crewmate, unless they're a dead imposter.
                if (gamePlayer.Role.ToString() == "SHERIFF")
                {
                    entry.RoleText.text = "Sheriff";
                    entry.BackgroundColor = TabMenuEntry.SheriffColor;
                }
                // Only indicate that they're an imposter if they're dead and identified.
                else if (NetworkGameManager.IsImposterRole(gamePlayer.Role.ToString()) && gamePlayer.Identified)
                {
                    entry.RoleText.text = "Imposter";
                    entry.BackgroundColor = TabMenuEntry.ImposterColor;
                }
                else
                {
                    entry.RoleText.text = "Crewmate";
                    entry.BackgroundColor = TabMenuEntry.CrewmateColor;
                }

                // If the player for which we're creating an entry is identified as dead, then we'll display that. 
                // Likewise, if WE are dead, then we'll show any other dead players as dead. But if we're alive,
                // then we'll only show players as dead if they've been identified as such.
                if (gamePlayer.IsDead && gamePlayer.Identified) // dead & identified
                {
                    // Only indicate that they're dead if they've been identified.
                    entry.StatusText.text = "Dead";
                }
                else if (gamePlayer.IsDead && player.IsDead) // dead & unidentified
                {
                    // If we're also dead, then we'll show them as unidentified, just as an imposter would see.
                    entry.StatusText.text = "Unidentified";
                }
                else
                {
                    entry.StatusText.text = "Alive";
                }
            }
        }
    }
    
    /// <summary>
    /// This function corresponds to the generic Inspect/Interact/Use button on the player's UI overlay.
    /// </summary>
    public void OnInteractButtonClicked()
    {
        if (!player.isLocalPlayer) return;

        playerController.InteractableInput();
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

    void OnDestroy()
    {
        Cursor.visible = true;

        if (PlayerCrosshair != null)
            PlayerCrosshair.DisableCrosshair();
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
        InteractableButton.enabled = true; // Just make sure this is enabled for now.

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

    private void GiveWeapon(int playerId, int weaponId)
    {
        uint netId = (uint)playerId;

        networkGameManager.NetIdMap[netId].GetComponent<PlayerController>().CmdGivePlayerWeapon(weaponId, false);
    }

    private void ListWeapons()
    {
        ItemDatabase itemDatabase = FindObjectOfType<ItemDatabase>();
        int maxId = itemDatabase.MaxWeaponId;
        Debug.Log("== Listing Weapons ==");
        for (int i = 0; i <= maxId; i++)
        {
            Debug.Log(String.Format("{0}\t{1}", i, itemDatabase.GetGunByID(i).Name));
        }
    }

    private void InfiniteAmmo(int playerId)
    {
        uint netId = (uint)playerId;

        if (networkGameManager.NetIdMap.ContainsKey(netId))
            networkGameManager.NetIdMap[netId].GetComponent<PlayerController>().CmdInfiniteAmmo();
        else
            Debug.LogWarning("There is no player with netId " + netId);
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
        DebugLogConsole.AddCommand("listguns", "listguns, Lists all guns and their IDs.", ListWeapons);
        DebugLogConsole.AddCommand<int>("ammo", "ammo <netId> - gives the specified player 999999 of each ammo type.", InfiniteAmmo);
        DebugLogConsole.AddCommand<int, int>("give", "give <netId> <gunId>", GiveWeapon);
        DebugLogConsole.AddCommand<int, Vector3>("tppos", "tppos <netId> [<x> <y> <z>]", TeleportPosition);
        DebugLogConsole.AddCommand<int, int>("tp", "tp <netIdSrc> <netIdDst>", TeleportToOtherPlayer);
        DebugLogConsole.AddCommand<int, float>("sethealth", "sethealth <netId> <health>", SetHealth);
    }

    #endregion 
}
