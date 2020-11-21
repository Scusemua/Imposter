using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

// Used to configure all of the various game options.
public class GameOptions : NetworkBehaviour
{
    [SyncVar] public int NumPlayers;

    [Header("Movement")]
    /* Movement related. */
    [SyncVar] public float PlayerSpeed = 5;
    [SyncVar] public bool SprintEnabled = true;               // Is the sprint mechanic enabled?
    [SyncVar] public float SprintBoost = 2;                   // Base speed is multiplied by this value when sprinting.
    [SyncVar] public float CrewmateSprintDuration = 10;       // How long can crewmates sprint?
    [SyncVar] public float ImposterSprintDuration = 15;       // How long can imposters sprint?

    [Header("Killing")]
    /* Killing related. */
    [SyncVar] public float KillIntervalStandard;              // How long inbetween kills by standard imposter?
    [SyncVar] public float KillIntervalAssassin;              // How long do assassins wait inbetween kills?
    [SyncVar] public float KillDistanceStandard = 0.5f;       // Minimum distance required between crewmate and standard imposter for kill to be available.
    [SyncVar] public float KillDistanceAssassin;              // Minimum distance required between crewmate and assassin imposter for kill to be available.

    [Header("Tasks")]
    /* Task-related settings. */
    [SyncVar] public int NumCommonTasks;              // How many tasks do ALL players have?
    [SyncVar] public int NumShortTasks;               // How many short-length tasks does each player get?
    [SyncVar] public int NumLongTasks;                // How many long-length tasks does each player get?

    [Header("Voting & Discussion")]
    /* Voting and discussion related. */
    [SyncVar] public int NumEmergencyMeetings;        // How many emergency players may each player call?
    [SyncVar] public float MeetingCooldown;           // How long must players wait inbetween emergency meetings?
    [SyncVar] public float DiscussionPeriodLength;    // How long do players have to discuss before voting begins?
    [SyncVar] public float VotingPeriodLength;        // How long do players have to cast their votes once voting begins?

    [Header("Emergencies")]
    /* Emergency related. */
    [SyncVar] public float EmergencyCooldownStandard; // How long must regular imposters wait inbetween causing emergencies?
    [SyncVar] public float EmergencyCooldownSaboteur; // How long must Saboteurs wait inbetween causing emergencies?
    [SyncVar] public float EmergencyMeetingCooldown;  // How much time must pass before the next emergency meeting can be called?

    [Header("Win Conditions")]
    /* Win Conditions */
    [SyncVar] public bool ImpostersMustKillAllCrewmates;

    [Header("Roles")]
    /* Role related */
    [SyncVar] public bool SaboteurEnabled;                // Is the "Saboteur" imposter role enabled?
    [SyncVar] public bool AssassinEnabled;                // Is the "Assassin" imposter role enabled?
    [SyncVar] public bool SheriffEnabled;                 // Is the "Sheriff" crewmate role enabed?
    [SyncVar] public int MaxSaboteurs;                    // What is the maximum number of Saboteur imposters allowed?
    [SyncVar] public int MaxAssassins;                    // What is the maximum number of Assassin imposters allowed?
    [SyncVar] public int MaxSheriffs;                     // What is the maximum number of sheriffs allowed?
    [SyncVar] public int NumberOfImposters;               // How many imposters are there?
    [SyncVar] public float SheriffScannerCooldown;        // How long must the sheriff wait inbetween uses of his/her scanner?

    [Header("Vision")]
    /* Vision related. */
    [SyncVar] public int CrewmateVision;              // How far can crewmates see?
    [SyncVar] public int ImposterVision;              // How far can imposters see?

    /* Dark-mode Related */
    [SyncVar] public bool DarkModeEnabled;

    [Header("Weapons")]
    /* Gun Related */
    [SyncVar] public float PlayerHealth;              // If instakill is false, then bullets do damage.
    [SyncVar] public float ImposterHealthMultiplier;  // PlayerHealth is multiplied by this to determine base health for imposters.
    [SyncVar] public bool SpawnPlayersWithAllWeapons;
    [SyncVar] public bool SpawnWeaponsAroundMap;

    [Header("Debug")]
    /* Debug */
    public bool disableWinChecking;

    #region Static Properties (not user configurable)

    // TODO: Make these modifiable.
    public static Dictionary<Gun.GunType, int> GunTypeInventoryLimits = new Dictionary<Gun.GunType, int>()
    {
        [Gun.GunType.PRIMARY] = 2,
        [Gun.GunType.SECONDARY] = 2,
        [Gun.GunType.EXPLOSIVE] = 2
    };

    // Holding a different weapon type impacts your speed.
    public static float PistolSpeedModifier = 0.98f;
    public static float ShotgunSpeedModifier = 0.95f;
    public static float SMGSpeedModifier = 0.96f;
    public static float ARSpeedModifier = 0.93f;
    public static float ExplosiveSpeedModifier = 0.90f;
    public static float LMGSpeedModifier = 0.87f;
    public static float RifleSpeedModifier = 0.91f;

    /// <summary>
    /// Map from each gun class to its speed modifier.
    /// </summary>
    public static Dictionary<GunClass, float> GunClassSpeedModifiers = new Dictionary<GunClass, float>()
    {
        [GunClass.ASSAULT_RIFLE] = ARSpeedModifier,
        [GunClass.SHOTGUN] = ShotgunSpeedModifier,
        [GunClass.SUBMACHINE_GUN] = SMGSpeedModifier,
        [GunClass.RIFLE] = RifleSpeedModifier,
        [GunClass.PISTOL] = PistolSpeedModifier,
        [GunClass.LIGHT_MACHINE_GUN] = LMGSpeedModifier,
        [GunClass.EXPLOSIVE] = ExplosiveSpeedModifier
    };

    /// <summary>
    /// This is the chance that a weapon spawns (rather than an ammo box) at a given item spawn position.
    /// </summary>
    public static float WeaponSpawnChance = 0.85f;

    /// <summary>
    /// This is the chance that, when an ammo box spawns, it spawns as a medkit rather than an actual ammo box.
    /// </summary>
    public static float MedkitSpawnChance = 0.15f;
    
    #endregion 

    public enum ProjectileType
    {
        BULLET, // Regular bullets
        ROCKET  // AoE
    }
    
    /* Player Visuals */
    [SyncVar] public float playerScale;   // Multiplier for visual size of players.

    /// <summary>
    /// NetworkManager singleton
    /// </summary>
    public static GameOptions singleton { get; private set; }

    bool InitializeSingleton()
    {
        if (singleton != null && singleton == this) return true;

        // do this early

        if (singleton != null)
        {
            Debug.LogWarning("Multiple GameOptions detected in the scene. Only one GameOptions can exist at a time. The duplicate GameOptions will be destroyed.");
            Destroy(gameObject);

            // Return false to not allow collision-destroyed second instance to continue.
            return false;
        }
        //Debug.Log("GameOptions created singleton (DontDestroyOnLoad)");
        singleton = this;
        if (Application.isPlaying) DontDestroyOnLoad(gameObject);

        return true;
    }

    void Awake()
    {
        // Don't allow collision-destroyed second instance to continue.
        if (!InitializeSingleton()) return;
    }
}

/// <summary>
/// Extension class/methods to send settings dictionary over the network.
/// </summary>
public static class SettingsDictionaryReaderWriter
{
    public static void WriteDictionary(this NetworkWriter writer, Dictionary<string, float> dict)
    {
        writer.WriteArray(dict.Keys.ToArray());
        writer.WriteArray(dict.Values.ToArray());
    }

    public static Dictionary<string, float> ReadDictionary(this NetworkReader reader)
    {
        Dictionary<string, float> settingsDictionary = new Dictionary<string, float>();
        string[] keys = reader.ReadArray<string>();
        float[] values = reader.ReadArray<float>();

        for (int i = 0; i < keys.Length; i++)
            settingsDictionary.Add(keys[i], values[i]);

        return settingsDictionary;
    }
}