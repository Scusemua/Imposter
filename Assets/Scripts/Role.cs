using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public abstract class Role : NetworkBehaviour, IRole
{
    protected float primaryActionLastUse = 0.0f;
    protected float secondaryActionLastUse = 0.0f;
    protected float tertiaryActionLastUse = 0.0f;

    public abstract string Name { get; }
    public abstract float SprintDuration { get; }
    public abstract float PrimaryActionCooldown { get; }
    public abstract float SecondaryActionCooldown { get; }
    public abstract float TertiaryActionCooldown { get; }
    public abstract void PerformPrimaryAction();
    public abstract void PerformSecondaryAction();
    public abstract void PerformTertiaryAction();

    public float MovementSpeed { get => gameOptions.PlayerSpeed; }
    public float SprintBoost { get => gameOptions.SprintBoost; }

    public float PrimaryActionLastUse { get { return primaryActionLastUse; } set { primaryActionLastUse = value; } }
    public float SecondaryActionLastUse { get { return secondaryActionLastUse; } set { secondaryActionLastUse = value; } }
    public float TertiaryActionLastUse { get { return tertiaryActionLastUse; } set { tertiaryActionLastUse = value; } }

    public abstract bool PrimaryActionReady { get; set; }

    [HideInInspector]
    public Player player;

    protected PlayerUI playerUI;

    protected Button primaryActionButton;
    protected Button secondaryActionButton;
    protected Button tertiaryActionButton;

    private GameOptions gameOptions;

    void Start()
    {
        gameOptions = GameOptions.singleton;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public virtual void AssignPlayer(Player _player)
    {
        player = _player;

        if (player == null)
            Debug.LogError("Player is null during Role.AssignPlayer()");

        playerUI = player.playerUI;

        if (_player.isLocalPlayer)
        {
            if (playerUI == null)
                Debug.LogError("PlayerUI is null during Role.AssignPlayer()");

            playerUI.PrimaryActionButtonGameObject.GetComponent<Button>().onClick.AddListener(PerformPrimaryAction);
        }
    }
}
