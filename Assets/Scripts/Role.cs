using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public abstract class Role : NetworkBehaviour, IRole
{
    protected float primaryActionLastUse;
    protected float secondaryActionLastUse;
    protected float tertiaryActionLastUse;

    public abstract string Name { get; }
    public float MovementSpeed { get => gameOptions.playerSpeed; }
    public abstract float SprintDuration { get; }
    public float SprintBoost { get => gameOptions.sprintBoost; }
    public abstract float PrimaryActionCooldown { get; }
    public abstract float SecondaryActionCooldown { get; }
    public abstract float TertiaryActionCooldown { get; }
    public abstract float PrimaryActionLastUse { get; set; }
    public abstract float SecondaryActionLastUse { get; set; }
    public abstract float TertiaryActionLastUse { get; set; }
    public abstract void PerformPrimaryAction();
    public abstract void PerformSecondaryAction();
    public abstract void PerformTertiaryAction();

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
        playerUI = player.playerUI;

        playerUI.PrimaryActionButton.onClick.AddListener(PerformPrimaryAction);
    }
}
