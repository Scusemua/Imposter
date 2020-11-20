using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class ImposterRole : Role
{
    public override string Name { get => "IMPOSTER"; }

    public override float SprintDuration => throw new System.NotImplementedException();

    public override float PrimaryActionCooldown { get => gameOptions.KillIntervalStandard; }

    public override float SecondaryActionCooldown => throw new System.NotImplementedException();

    public override float TertiaryActionCooldown => throw new System.NotImplementedException();

    private bool primaryActionReady = true;
    public override bool PrimaryActionReady { get => primaryActionReady; set => primaryActionReady = value; }

    private GameOptions gameOptions;

    protected virtual float KillDistance { get => gameOptions.KillDistanceStandard; }

    private Player killTarget;
    protected virtual Player KillTarget { get => killTarget; set => killTarget = value; }

    void Start()
    {
        gameOptions = GameOptions.singleton;

        if (isLocalPlayer) enabled = true;
    }

    public override void PerformPrimaryAction()
    {
        if (PrimaryActionLastUse > 0)
        {
            Debug.Log("User tried to perform primary action, but cooldown is " + PrimaryActionLastUse);
            return;
        }

        if (player.IsDead)
        {
            Debug.Log("User tried to perform primary action, but they are dead.");
            return;
        }

        Debug.Log("Performing primary action now.");

        if (KillTarget != null)
        {
            Debug.Log("Closest player is " + KillTarget.Nickname + ".");

            this.transform.SetPositionAndRotation(KillTarget.transform.position, this.transform.rotation);

            KillTarget.CmdKill(this.player.Nickname, false);

            PrimaryActionLastUse = PrimaryActionCooldown;
            GameObject primaryActionButtonGameObject = playerUI.PrimaryActionButtonGameObject;
            primaryActionButtonGameObject.GetComponent<Image>().fillAmount = 0;
            primaryActionButtonGameObject.GetComponent<Button>().interactable = false;
            PrimaryActionReady = false;

            GetComponent<PlayerController>().PlayImpactSound();
        }
        else
        {
            Debug.Log("There are no players within kill radius.");
        }
    }

    /// <summary>
    /// Find and return the closest killable (i.e., non-Imposter) player. 
    /// </summary>
    /// <returns>The closest killable player, or null if there aren't any killable players within range.</returns>
    protected virtual Player GetClosestKillablePlayer()
    {
        Player[] allPlayers = FindObjectsOfType<Player>();

        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;
        Player closestPlayer = null;

        foreach (Player player in allPlayers)
        {
            // Don't check distance between us and other imposters or dead players.
            if (player.Role == null || NetworkGameManager.IsImposterRole(player.Role.Name) || player.IsDead)
                continue;

            Vector3 directionToTarget = player.GetComponent<Transform>().position - currentPosition;
            float dSqrToTarget = directionToTarget.sqrMagnitude;

            if (dSqrToTarget < closestDistanceSqr && dSqrToTarget < Mathf.Pow(KillDistance, 2))
            {
                closestDistanceSqr = dSqrToTarget;
                closestPlayer = player;
            }
        }

        return closestPlayer;
    }

    public override void PerformSecondaryAction()
    {
        throw new System.NotImplementedException();
    }

    public override void PerformTertiaryAction()
    {
        throw new System.NotImplementedException();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        KillTarget = GetClosestKillablePlayer();

        if (!PrimaryActionReady)
        {
            PrimaryActionLastUse -= Time.deltaTime;

            playerUI.PrimaryActionButtonGameObject.GetComponent<Image>().fillAmount = 1 - (PrimaryActionLastUse / PrimaryActionCooldown);

            if (PrimaryActionLastUse <= 0)
            {
                PrimaryActionReady = true;
                PrimaryActionLastUse = 0;
            }
        }

        if (PrimaryActionReady && KillTarget != null)
            playerUI.PrimaryActionButtonGameObject.GetComponent<Button>().interactable = true;
        else
            playerUI.PrimaryActionButtonGameObject.GetComponent<Button>().interactable = false;
    }
}