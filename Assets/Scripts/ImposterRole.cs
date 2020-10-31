using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class ImposterRole : Role
{
    public override string Name { get => "IMPOSTER"; }

    public override float SprintDuration => throw new System.NotImplementedException();

    public override float PrimaryActionCooldown { get => gameOptions.killIntervalStandard; }

    public override float SecondaryActionCooldown => throw new System.NotImplementedException();

    public override float TertiaryActionCooldown => throw new System.NotImplementedException();

    private bool primaryActionReady = true;
    public override bool PrimaryActionReady { get => primaryActionReady; set => primaryActionReady = value; }

    private GameOptions gameOptions;

    protected virtual float KillDistance { get => gameOptions.killDistanceStandard; }

    private Player killTarget;
    protected virtual Player KillTarget { get => killTarget; set => killTarget = value; }

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

        if (KillTarget != null)
        {
            Debug.Log("Closest player is " + KillTarget.nickname + ".");

            this.transform.SetPositionAndRotation(KillTarget.transform.position, this.transform.rotation);
            KillTarget.Kill(this.player);

            PrimaryActionLastUse = PrimaryActionCooldown;
            GameObject primaryActionButtonGameObject = playerUI.PrimaryActionButtonGameObject;
            primaryActionButtonGameObject.GetComponent<Image>().fillAmount = 0;
            primaryActionButtonGameObject.GetComponent<Button>().interactable = false;
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
            if (GameManager.IsImposterRole(player.Role.Name) || player.isDead)
                continue;

            Vector3 directionToTarget = player.GetComponent<Transform>().position - currentPosition;
            float dSqrToTarget = directionToTarget.sqrMagnitude;

            if (dSqrToTarget < closestDistanceSqr)
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

    void Update() {
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