using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class CrewmateRole : Role
{
    public override string Name { get => "Crewmate"; }
    public override float SprintDuration { get => gameOptions.CrewmateSprintDuration;  }

    public override float PrimaryActionCooldown => throw new System.NotImplementedException();

    public override float SecondaryActionCooldown => throw new System.NotImplementedException();

    public override float TertiaryActionCooldown => throw new System.NotImplementedException();

    public override bool PrimaryActionReady { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    private GameOptions gameOptions;


    void Start()
    {
        gameOptions = GameOptions.singleton;
    }

    void Update()
    {

    }

    public override void PerformPrimaryAction()
    {
        throw new System.NotImplementedException();
    }

    public override void PerformSecondaryAction()
    {
        throw new System.NotImplementedException();
    }

    public override void PerformTertiaryAction()
    {
        throw new System.NotImplementedException();
    }
}