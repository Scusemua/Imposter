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
    public TMP_InputField KillCooldownStandardInput;
    public TMP_InputField KillCooldownAssassinInput;
    public TMP_InputField KillDistanceStandardInput;
    public TMP_InputField KillDistanceAssassinInput;

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

    #region Client 

    [Client]
    public void OnCommitChanges()
    {
        if (isClientOnly)
        {
            Debug.LogError("Non-host player somehow committed changes to game options...?");
            return;
        }

        Dictionary<string, float> settings = new Dictionary<string, float>();
        // Input fields.
        settings.Add("PlayerMovement", float.Parse(PlayerMovementInput.text));
        settings.Add("SprintBoost", float.Parse(SprintBoostInput.text));
        settings.Add("CrewmateStamina", float.Parse(CrewmateStaminaInput.text));
        settings.Add("ImposterStamina", float.Parse(ImposterStaminaInput.text));
        settings.Add("NumberRoundtables", float.Parse(NumberRoundtablesInput.text));
        settings.Add("RoundtableCooldown", float.Parse(RoundtableCooldownInput.text));
        settings.Add("DiscussionPeriodLength", float.Parse(DiscussionPeriodLengthInput.text));
        settings.Add("VotingPeriodLength", float.Parse(VotingPeriodLengthInput.text));
        settings.Add("PlayerLimit", float.Parse(PlayerLimitInput.text));
        settings.Add("NumberOfImposters", float.Parse(NumberOfImpostersInput.text));
        settings.Add("NumberOfSheriffs", float.Parse(NumberOfSheriffsInput.text));
        settings.Add("NumberOfAssassins", float.Parse(NumberOfAssassinsInput.text));
        settings.Add("NumberOfSaboteurs", float.Parse(NumberOfSaboteursInput.text));
        settings.Add("KillCooldownStandard", float.Parse(KillCooldownStandardInput.text));
        settings.Add("KillCooldownAssassin", float.Parse(KillCooldownAssassinInput.text));
        settings.Add("KillDistanceStandard", float.Parse(KillDistanceStandardInput.text));
        settings.Add("KillDistanceAssassin", float.Parse(KillDistanceAssassinInput.text));

        // Toggles.
        settings.Add("SprintEnabled", SprintEnabledToggle.enabled ? 1.0f : 0.0f);
        settings.Add("PlayersSpawnWithAllWeapons", PlayersSpawnWithAllWeaponsToggle.enabled ? 1.0f : 0.0f);
        settings.Add("SpawnWeaponsAroundMap", SpawnWeaponsAroundMapToggle.enabled ? 1.0f : 0.0f);
        settings.Add("MustKillAllCrewmates", MustKillAllCrewmatesToggle.enabled ? 1.0f : 0.0f);
        settings.Add("DarkMode", DarkModeToggle.enabled ? 1.0f : 0.0f);
        settings.Add("SheriffsEnabled", SheriffsEnabledToggle.enabled ? 1.0f : 0.0f);
        settings.Add("AssassinsEnabled", AssassinsEnabledToggle.enabled ? 1.0f : 0.0f);
        settings.Add("SaboteursEnabled", SaboteursEnabledToggle.enabled ? 1.0f : 0.0f);

        CmdApplyChanges(settings);
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
    public void CmdApplyChanges(Dictionary<string, float> settings)
    {
        // Iterate over the settings and update changes to GameOptions.
        foreach (KeyValuePair<string, float> setting in settings)
        {
            switch (setting.Key)
            {
                case "PlayerMovement":
                    GameOptions.singleton.PlayerSpeed = setting.Value;
                    break;
                case "SprintBoost":
                    GameOptions.singleton.SprintBoost = setting.Value;
                    break;
                case "CrewmateStamina":
                    GameOptions.singleton.CrewmateSprintDuration = setting.Value;
                    break;
                case "ImposterStamina":
                    GameOptions.singleton.ImposterSprintDuration = setting.Value;
                    break;
                case "NumberRoundtables":
                    GameOptions.singleton.NumEmergencyMeetings = (int)setting.Value;
                    break;
                case "RoundtableCooldown":
                    GameOptions.singleton.EmergencyMeetingCooldown = setting.Value;
                    break;
                case "DiscussionPeriodLength":
                    GameOptions.singleton.DiscussionPeriodLength = setting.Value;
                    break;
                case "VotingPeriodLength":
                    GameOptions.singleton.VotingPeriodLength = setting.Value;
                    break;
                case "PlayerLimit":
                    GameOptions.singleton.NumPlayers = (int)setting.Value;
                    break;
                case "NumberOfImposters":
                    GameOptions.singleton.NumberOfImposters = (int)setting.Value;
                    break;
                case "NumberOfSheriffs":
                    GameOptions.singleton.MaxSheriffs = (int)setting.Value;
                    break;
                case "NumberOfAssassins":
                    GameOptions.singleton.MaxAssassins = (int)setting.Value;
                    break;
                case "NumberOfSaboteurs":
                    GameOptions.singleton.MaxSaboteurs = (int)setting.Value;
                    break;
                case "KillCooldownStandard":
                    GameOptions.singleton.KillIntervalStandard = setting.Value;
                    break;
                case "KillCooldownAssassin":
                    GameOptions.singleton.KillIntervalAssassin = setting.Value;
                    break;
                case "KillDistanceStandard":
                    GameOptions.singleton.KillDistanceStandard = setting.Value;
                    break;
                case "KillDistanceAssassin":
                    GameOptions.singleton.KillDistanceAssassin = setting.Value;
                    break;
                // Booleans.
                case "SprintEnabled":
                    if (setting.Value == 1.0f)
                        GameOptions.singleton.SprintEnabled = true;
                    else
                        GameOptions.singleton.SprintEnabled = false;
                    break;
                case "PlayersSpawnWithAllWeapons":
                    if (setting.Value == 1.0f)
                        GameOptions.singleton.SpawnPlayersWithAllWeapons = true;
                    else
                        GameOptions.singleton.SpawnPlayersWithAllWeapons = false;
                    break;
                case "SpawnWeaponsAroundMap":
                    if (setting.Value == 1.0f)
                        GameOptions.singleton.SpawnWeaponsAroundMap = true;
                    else
                        GameOptions.singleton.SpawnWeaponsAroundMap = false;
                    break;
                case "MustKillAllCrewmates":
                    if (setting.Value == 1.0f)
                        GameOptions.singleton.ImpostersMustKillAllCrewmates = true;
                    else
                        GameOptions.singleton.ImpostersMustKillAllCrewmates = false;
                    break;
                case "DarkMode":
                    if (setting.Value == 1.0f)
                        GameOptions.singleton.DarkModeEnabled = true;
                    else
                        GameOptions.singleton.DarkModeEnabled = false;
                    break;
                case "SheriffsEnabled":
                    if (setting.Value == 1.0f)
                        GameOptions.singleton.SheriffEnabled = true;
                    else
                        GameOptions.singleton.SheriffEnabled = false;
                    break;
                case "AssassinsEnabled":
                    if (setting.Value == 1.0f)
                        GameOptions.singleton.AssassinEnabled = true;
                    else
                        GameOptions.singleton.AssassinEnabled = false;
                    break;
                case "SaboteursEnabled":
                    if (setting.Value == 1.0f)
                        GameOptions.singleton.SaboteurEnabled = true;
                    else
                        GameOptions.singleton.SaboteurEnabled = false;
                    break;
                default:
                    Debug.LogError("Received unknown setting.");
                    break;
            }
        }
    }

    #endregion 
}
