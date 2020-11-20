using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using TMPro;
using UnityEngine.UI;
using Mirror;
using Lean.Gui;

public class GameOptionsUI : NetworkBehaviour
{
    [Header("UI Elements")]
    public GameObject WeaponScrollviewContent;

    [Header("UI Prefabs")]
    public GameObject WeaponGroupHeaderPrefab;
    public GameObject WeaponEntryPrefab;

    [Header("Data")]
    public Gun[] WeaponPrefabs;

    public Dictionary<Gun, GameObject> WeaponEntries = new Dictionary<Gun, GameObject>();

    public Dictionary<GunType, List<Gun>> GunsOrganizedByType = new Dictionary<GunType, List<Gun>>();

    public LeanWindow ExitConfirmationWindow;

    [Header("Input Fields")]
    public TMP_InputField PlayerMovementInput;
    public TMP_InputField SprintBoostInput;
    public TMP_InputField CrewmateStaminaInput;
    public TMP_InputField ImposterStaminaInput;
    public TMP_InputField NumberRoundtablesInput;
    public TMP_InputField RoundtableCooldownInput;
    public TMP_InputField DiscussionPeriodLengthInput;
    public TMP_InputField VotingPeriodLengthInput;
    public TMP_InputField PlayerLimitInput;
    public TMP_InputField NumberOfImpostersInput;
    public TMP_InputField NumberOfSheriffsInput;
    public TMP_InputField NumberOfAssassinsInput;
    public TMP_InputField NumberOfSaboteursInput;

    [Header("Toggles")]
    public LeanToggle SprintEnabledToggle;
    public LeanToggle PlayersSpawnWithAllWeaponsToggle;
    public LeanToggle SpawnWeaponsAroundMapToggle;
    public LeanToggle MustKillAllCrewmatesToggle;
    public LeanToggle DarkModeToggle;
    public LeanToggle SheriffsEnabledToggle;
    public LeanToggle AssassinsEnabledToggle;
    public LeanToggle SaboteursEnabledToggle;

    public override void OnStartLocalPlayer()
    {
        // Create a list for each type of gun.
        foreach (GunType gunType in Enum.GetValues(typeof(GunType)))
        {
            GunsOrganizedByType[gunType] = new List<Gun>();
        }

        // Store all of the guns in their respective lists.
        foreach (Gun gun in WeaponPrefabs)
        {
            Debug.Log("Initial processing on gun " + gun.Name);
            GunsOrganizedByType[gun.GunType].Add(gun);
        }

        foreach (KeyValuePair<GunType, List<Gun>> kvp in GunsOrganizedByType)
        {
            List<Gun> guns = kvp.Value;

            GameObject header = Instantiate(WeaponGroupHeaderPrefab, WeaponScrollviewContent.transform);
            header.GetComponentInChildren<TextMeshProUGUI>().text = kvp.Key.ToString();

            foreach (Gun gun in guns)
            {
                Debug.Log("Creating entry for gun " + gun.Name);
                GameObject entry = Instantiate(WeaponEntryPrefab, WeaponScrollviewContent.transform);
                entry.GetComponentInChildren<Text>().text = gun.Name;

                WeaponEntries[gun] = entry;
            }
        }

        if (isClientOnly)
        {

        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //// Create a list for each type of gun.
        //foreach (GunType gunType in Enum.GetValues(typeof(GunType)))
        //{
        //    GunsOrganizedByType[gunType] = new List<Gun>();
        //}

        //// Store all of the guns in their respective lists.
        //foreach (Gun gun in WeaponPrefabs)
        //{
        //    Debug.Log("Initial processing on gun " + gun.Name);
        //    GunsOrganizedByType[gun.GunType].Add(gun);
        //}

        //foreach (KeyValuePair<GunType, List<Gun>> kvp in GunsOrganizedByType)
        //{
        //    List<Gun> guns = kvp.Value;

        //    GameObject header = Instantiate(WeaponGroupHeaderPrefab, WeaponScrollviewContent.transform);
        //    header.GetComponentInChildren<TextMeshProUGUI>().text = kvp.Key.ToString();

        //    foreach (Gun gun in guns)
        //    {
        //        Debug.Log("Creating entry for gun " + gun.Name);
        //        GameObject entry = Instantiate(WeaponEntryPrefab, WeaponScrollviewContent.transform);
        //        entry.GetComponentInChildren<Text>().text = gun.Name;

        //        WeaponEntries[gun] = entry;
        //    }
        //}
    }

    #region Client 

    [Client]
    public void OnCommitChanges()
    {
        if (isClientOnly) return;
    }

    [Client]
    public void OnExitClicked()
    {
        // If they aren't host, then just exit. Don't show the exit confirmation window.
        if (isClientOnly)
            gameObject.SetActive(false);
        else
            ExitConfirmationWindow.TurnOn();
    }

    #endregion 

    #region Commands

    [Command]
    public void CmdApplyChanges(Dictionary<string, int> settings)
    {

    }

    #endregion 
}
