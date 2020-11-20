using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Used to configure all of the various game options.
public class GameOptions : MonoBehaviour
{
    public int NumPlayers;

    [Header("Movement")]
    /* Movement related. */
    public float PlayerSpeed = 5;
    public bool SprintEnabled = true;               // Is the sprint mechanic enabled?
    public float SprintBoost = 2;                   // Base speed is multiplied by this value when sprinting.
    public float CrewmateSprintDuration = 10;       // How long can crewmates sprint?
    public float ImposterSprintDuration = 15;       // How long can imposters sprint?

    [Header("Footprints")]
    /* Footprint related. */
    public bool BloodyFootprintsEnabled;    // Are there bloody footprints after killing?
    public bool BloodyFootprintsOnKillOnly; // If False, any player will have bloody footprints after walking over a dead body.
    public float FootprintDistance;         // For how long will you have bloody footprints?         
    public float FootprintOpacity;          // How dark are the footprints on the ground?
    public bool FootprintsDisappear;        // Do the footprints eventually disappear?       
    public float FootprintDuration;         // How long do footprints stick around before disappearing?

    [Header("Killing")]
    /* Killing related. */
    public float KillIntervalStandard;              // How long inbetween kills by standard imposter?
    public float KillIntervalAssassin;              // How long do assassins wait inbetween kills?
    public float KillDistanceStandard = 0.5f;       // Minimum distance required between crewmate and standard imposter for kill to be available.
    public float KillDistanceAssassin;              // Minimum distance required between crewmate and assassin imposter for kill to be available.

    [Header("Tasks")]
    /* Task-related settings. */
    public int NumCommonTasks;              // How many tasks do ALL players have?
    public int NumShortTasks;               // How many short-length tasks does each player get?
    public int NumLongTasks;                // How many long-length tasks does each player get?

    [Header("Voting & Discussion")]
    /* Voting and discussion related. */
    public int NumEmergencyMeetings;        // How many emergency players may each player call?
    public float MeetingCooldown;           // How long must players wait inbetween emergency meetings?
    public float DiscussionPeriodLength;    // How long do players have to discuss before voting begins?
    public float VotingPeriodLength;        // How long do players have to cast their votes once voting begins?

    [Header("Emergencies")]
    /* Emergency related. */
    public float EmergencyCooldownStandard; // How long must regular imposters wait inbetween causing emergencies?
    public float EmergencyCooldownSaboteur; // How long must Saboteurs wait inbetween causing emergencies?
    public float EmergencyMeetingCooldown;  // How much time must pass before the next emergency meeting can be called?

    [Header("Win Conditions")]
    /* Win Conditions */
    public bool ImpostersMustKillAllCrewmates;

    [Header("Roles")]
    /* Role related */
    public bool SaboteurEnabled;                // Is the "Saboteur" imposter role enabled?
    public bool AssassinEnabled;                // Is the "Assassin" imposter role enabled?
    public bool SheriffEnabled;                 // Is the "Sheriff" crewmate role enabed?
    public int MaxSaboteurs;                    // What is the maximum number of Saboteur imposters allowed?
    public int MaxAssassins;                    // What is the maximum number of Assassin imposters allowed?
    public int MaxSheriffs;                     // What is the maximum number of sheriffs allowed?
    public int NumberOfImposters;               // How many imposters are there?
    public float SheriffScannerCooldown;        // How long must the sheriff wait inbetween uses of his/her scanner?

    [Header("Vision")]
    /* Vision related. */
    public int CrewmateVision;              // How far can crewmates see?
    public int ImposterVision;              // How far can imposters see?

    /* Dark-mode Related */
    public bool DarkModeEnabled;

    [Header("Weapons")]
    /* Gun Related */
    public bool GunsEnabled;
    public bool CrewmatesHaveGuns;
    public bool SheriffHasGun;              // If crewmatesHaveGUns is true, then this is ignored.
    public bool Instakill;                  // If true, all guns insta-kill.
    public float PlayerHealth;              // If instakill is false, then bullets do damage.
    public float ImposterHealthMultiplier;  // PlayerHealth is multiplied by this to determine base health for imposters.
    public int NumBulletsImposter;
    public int NumBulletsAssassin;
    public int NumBulletsSheriff;
    public int NumBulletsSaboteur;
    public int NumBulletsCrewmate;
    public float GunshotVolume;
    public float GunshotBrightness;
    public float Firerate;
    public bool ReloadsRequired;    // If false, then players can shoot infinitely without reloading.
    public float ReloadTime;
    public float MagazineSize;
    public float Accuracy;
    public float NumProjectiles;
    public float Range;
    public float ProjectileDamage;  // How much damage each individual projectile does.

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
    public float playerScale;   // Multiplier for visual size of players.
    public bool trailEnabled;   // Should players leave a trail (of particles or some shit)
    public float trailDuration; // How far back does the trail go?

    // TODO: Set minimum and maximum values for all of these.

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