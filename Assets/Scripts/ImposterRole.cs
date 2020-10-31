using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class ImposterRole : Role
{
    public override string Name { get => "IMPOSTER"; }

    public override float SprintDuration => throw new System.NotImplementedException();

    public override float PrimaryActionLastUse { get { return primaryActionLastUse; } set { primaryActionLastUse = value; } }

    public override float SecondaryActionLastUse { get { return secondaryActionLastUse; } set { secondaryActionLastUse = value; } }

    public override float TertiaryActionLastUse { get { return tertiaryActionLastUse; } set { tertiaryActionLastUse = value; } }

    public override float PrimaryActionCooldown { get => gameOptions.killIntervalStandard; }

    public override float SecondaryActionCooldown => throw new System.NotImplementedException();

    public override float TertiaryActionCooldown => throw new System.NotImplementedException();

    private GameOptions gameOptions;

    void Start()
    {
        gameOptions = GameOptions.singleton;
    }

    public override void PerformPrimaryAction()
    {
        if (PrimaryActionLastUse > 0)
        {
            Debug.Log("User tried to perform primary action, but cooldown is " + PrimaryActionLastUse);
            return;
        }

        Debug.Log("Performing primary action now.");

        Player[] allPlayers = FindObjectsOfType<Player>();

        foreach (Player player in allPlayers)
        {

        }

        PrimaryActionLastUse = PrimaryActionCooldown;
    }

    public override void PerformSecondaryAction()
    {
        throw new System.NotImplementedException();
    }

    public override void PerformTertiaryAction()
    {
        throw new System.NotImplementedException();
    }

    void Update() {
        if (PrimaryActionLastUse > 0)
            PrimaryActionLastUse -= Time.deltaTime;

            if (PrimaryActionLastUse < 0)
            PrimaryActionLastUse = 0;
    }
}