using System.Collections;
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

    [Header("Footprints")]
    /* Footprint related. */
    [SyncVar] public bool BloodyFootprintsEnabled;    // Are there bloody footprints after killing?
    [SyncVar] public bool BloodyFootprintsOnKillOnly; // If False, any player will have bloody footprints after walking over a dead body.
    [SyncVar] public float FootprintDistance;         // For how long will you have bloody footprints?         
    [SyncVar] public float FootprintOpacity;          // How dark are the footprints on the ground?
    [SyncVar] public bool FootprintsDisappear;        // Do the footprints eventually disappear?       
    [SyncVar] public float FootprintDuration;         // How long do footprints stick around before disappearing?

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
    [SyncVar] public bool GunsEnabled;
    [SyncVar] public bool CrewmatesHaveGuns;
    [SyncVar] public bool SheriffHasGun;              // If crewmatesHaveGUns is true, then this is ignored.
    [SyncVar] public bool Instakill;                  // If true, all guns insta-kill.
    [SyncVar] public float PlayerHealth;              // If instakill is false, then bullets do damage.
    [SyncVar] public float ImposterHealthMultiplier;  // PlayerHealth is multiplied by this to determine base health for imposters.
    [SyncVar] public int NumBulletsImposter;
    [SyncVar] public int NumBulletsAssassin;
    [SyncVar] public int NumBulletsSheriff;
    [SyncVar] public int NumBulletsSaboteur;
    [SyncVar] public int NumBulletsCrewmate;
    [SyncVar] public float GunshotVolume;
    [SyncVar] public float GunshotBrightness;
    [SyncVar] public float Firerate;
    [SyncVar] public bool ReloadsRequired;    // If false, then players can shoot infinitely without reloading.
    [SyncVar] public float ReloadTime;
    [SyncVar] public float MagazineSize;
    [SyncVar] public float Accuracy;
    [SyncVar] public float NumProjectiles;
    [SyncVar] public float Range;
    [SyncVar] public float ProjectileDamage;  // How much damage each individual projectile does.

    [Header("Debug")]
    /* Debug */
    public bool disableWinChecking;

    public enum ProjectileType
    {
        BULLET, // Regular bullets
        ROCKET  // AoE
    }

    // Presets.
    public enum GunType
    {
        PISTOL,
        RIFLE,
        SHOTGUN,
        SMG,
        ASSAULT_RIFLE,
        LMG,
        RPG
    }
    
    /* Player Visuals */
    [SyncVar] public float playerScale;   // Multiplier for visual size of players.
    [SyncVar] public bool trailEnabled;   // Should players leave a trail (of particles or some shit)
    [SyncVar] public float trailDuration; // How far back does the trail go?

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
        Debug.Log("GameOptions created singleton (DontDestroyOnLoad)");
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