using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using TMPro;
using UnityEngine.UI;
using Mirror;
using Lean.Gui;
using System.Linq;

public class GameOptionsUI : NetworkBehaviour
{
    // TODO: Implement enabling and disablign weapons.

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

    public GameObject PrimaryLobbyUIGameObject;

    [Header("Buttons")]
    public GameObject SaveButton;
    public GameObject DiscardButton;
    public GameObject YesButton;
    public GameObject NoButton;

    [Header("Text")]
    public Text ExitDialogText;

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
        PopulateValues();
    }

    #region Client 

    [Client]
    public void PopulateValues()
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

        PlayerMovementInput.text = GameOptions.singleton.PlayerSpeed.ToString();
        SprintBoostInput.text = GameOptions.singleton.SprintBoost.ToString();
        CrewmateStaminaInput.text = GameOptions.singleton.CrewmateSprintDuration.ToString();
        ImposterStaminaInput.text = GameOptions.singleton.ImposterSprintDuration.ToString();
        NumberRoundtablesInput.text = GameOptions.singleton.NumEmergencyMeetings.ToString();
        RoundtableCooldownInput.text = GameOptions.singleton.EmergencyMeetingCooldown.ToString();
        DiscussionPeriodLengthInput.text = GameOptions.singleton.DiscussionPeriodLength.ToString();
        VotingPeriodLengthInput.text = GameOptions.singleton.VotingPeriodLength.ToString();
        PlayerLimitInput.text = GameOptions.singleton.NumPlayers.ToString();
        NumberOfImpostersInput.text = GameOptions.singleton.NumberOfImposters.ToString();
        NumberOfSheriffsInput.text = GameOptions.singleton.MaxSheriffs.ToString();
        NumberOfAssassinsInput.text = GameOptions.singleton.MaxAssassins.ToString();
        NumberOfSaboteursInput.text = GameOptions.singleton.MaxSaboteurs.ToString();
        KillCooldownStandardInput.text = GameOptions.singleton.KillIntervalStandard.ToString();
        KillCooldownAssassinInput.text = GameOptions.singleton.KillIntervalAssassin.ToString();
        KillDistanceStandardInput.text = GameOptions.singleton.KillDistanceStandard.ToString();
        KillDistanceAssassinInput.text = GameOptions.singleton.KillDistanceAssassin.ToString();
        
        // Toggles.
        SprintEnabledToggle.On = GameOptions.singleton.SprintEnabled;
        PlayersSpawnWithAllWeaponsToggle.On = GameOptions.singleton.SpawnPlayersWithAllWeapons;
        SpawnWeaponsAroundMapToggle.On = GameOptions.singleton.SpawnWeaponsAroundMap;
        MustKillAllCrewmatesToggle.On = GameOptions.singleton.ImpostersMustKillAllCrewmates;
        DarkModeToggle.On = GameOptions.singleton.DarkModeEnabled;
        SheriffsEnabledToggle.On = GameOptions.singleton.SheriffEnabled;
        AssassinsEnabledToggle.On = GameOptions.singleton.AssassinEnabled;
        SaboteursEnabledToggle.On = GameOptions.singleton.SaboteurEnabled;
    }

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

        CmdApplyChanges(settings.Keys.ToArray(), settings.Values.ToArray());
    }

    [Client]
    public void OnExitClicked()
    {
        // If they aren't host, then just exit. Don't show the exit confirmation window.
        if (isClientOnly)
        {
            gameObject.SetActive(false);
            return;
        }

        // Configure the window to show the correct buttons and the correct text.
        SaveButton.SetActive(true);
        DiscardButton.SetActive(true);
        YesButton.SetActive(false);
        NoButton.SetActive(false);
        ExitDialogText.text = "Save changes?";

        ExitConfirmationWindow.TurnOn();
    }

    #endregion 

    #region Commands

    [Command]
    public void CmdApplyChanges(string[] keys, float[] values)
    {
        // Iterate over the settings and update changes to GameOptions.
        //foreach (KeyValuePair<string, float> setting in settings)
        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            float value = values[i];
            switch (key)
            {
                case "PlayerMovement":
                    GameOptions.singleton.PlayerSpeed = value;
                    break;
                case "SprintBoost":
                    GameOptions.singleton.SprintBoost = value;
                    break;
                case "CrewmateStamina":
                    GameOptions.singleton.CrewmateSprintDuration = value;
                    break;
                case "ImposterStamina":
                    GameOptions.singleton.ImposterSprintDuration = value;
                    break;
                case "NumberRoundtables":
                    GameOptions.singleton.NumEmergencyMeetings = (int)value;
                    break;
                case "RoundtableCooldown":
                    GameOptions.singleton.EmergencyMeetingCooldown = value;
                    break;
                case "DiscussionPeriodLength":
                    GameOptions.singleton.DiscussionPeriodLength = value;
                    break;
                case "VotingPeriodLength":
                    GameOptions.singleton.VotingPeriodLength = value;
                    break;
                case "PlayerLimit":
                    GameOptions.singleton.NumPlayers = (int)value;
                    break;
                case "NumberOfImposters":
                    GameOptions.singleton.NumberOfImposters = (int)value;
                    break;
                case "NumberOfSheriffs":
                    GameOptions.singleton.MaxSheriffs = (int)value;
                    break;
                case "NumberOfAssassins":
                    GameOptions.singleton.MaxAssassins = (int)value;
                    break;
                case "NumberOfSaboteurs":
                    GameOptions.singleton.MaxSaboteurs = (int)value;
                    break;
                case "KillCooldownStandard":
                    GameOptions.singleton.KillIntervalStandard = value;
                    break;
                case "KillCooldownAssassin":
                    GameOptions.singleton.KillIntervalAssassin = value;
                    break;
                case "KillDistanceStandard":
                    GameOptions.singleton.KillDistanceStandard = value;
                    break;
                case "KillDistanceAssassin":
                    GameOptions.singleton.KillDistanceAssassin = value;
                    break;
                // Booleans.
                case "SprintEnabled":
                    if (value == 1.0f)
                        GameOptions.singleton.SprintEnabled = true;
                    else
                        GameOptions.singleton.SprintEnabled = false;
                    break;
                case "PlayersSpawnWithAllWeapons":
                    if (value == 1.0f)
                        GameOptions.singleton.SpawnPlayersWithAllWeapons = true;
                    else
                        GameOptions.singleton.SpawnPlayersWithAllWeapons = false;
                    break;
                case "SpawnWeaponsAroundMap":
                    if (value == 1.0f)
                        GameOptions.singleton.SpawnWeaponsAroundMap = true;
                    else
                        GameOptions.singleton.SpawnWeaponsAroundMap = false;
                    break;
                case "MustKillAllCrewmates":
                    if (value == 1.0f)
                        GameOptions.singleton.ImpostersMustKillAllCrewmates = true;
                    else
                        GameOptions.singleton.ImpostersMustKillAllCrewmates = false;
                    break;
                case "DarkMode":
                    if (value == 1.0f)
                        GameOptions.singleton.DarkModeEnabled = true;
                    else
                        GameOptions.singleton.DarkModeEnabled = false;
                    break;
                case "SheriffsEnabled":
                    if (value == 1.0f)
                        GameOptions.singleton.SheriffEnabled = true;
                    else
                        GameOptions.singleton.SheriffEnabled = false;
                    break;
                case "AssassinsEnabled":
                    if (value == 1.0f)
                        GameOptions.singleton.AssassinEnabled = true;
                    else
                        GameOptions.singleton.AssassinEnabled = false;
                    break;
                case "SaboteursEnabled":
                    if (value == 1.0f)
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
