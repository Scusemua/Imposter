using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Used to configure all of the various game options.
public class GameOptions : MonoBehaviour
{
    /* Movement related. */
    public float playerSpeed;
    public bool springEnabled;              // Is the sprint mechanic enabled?
    public float sprintBoost;               // Base speed is multiplied by this value when sprinting.
    public float crewmateSprintDuration;    // How long can crewmates sprint?
    public float imposterSprintDuration;    // How long can imposters sprint?

    /* Footprint related. */
    public bool bloodyFootprintsEnabled;    // Are there bloody footprints after killing?
    public bool bloodyFootprintsOnKillOnly; // If False, any player will have bloody footprints after walking over a dead body.
    public float footprintDistance;         // For how long will you have bloody footprints?         
    public float footprintOpacity;          // How dark are the footprints on the ground?
    public bool footprintsDisappear;        // Do the footprints eventually disappear?       
    public float footprintDuration;         // How long do footprints stick around before disappearing?

    /* Killing related. */
    public float killIntervalStandard;      // How long inbetween kills by standard imposter?
    public float killIntervalAssassin;      // How long do assassins wait inbetween kills?
    public float killDistanceStandard;      // Minimum distance required between crewmate and standard imposter for kill to be available.
    public float killDistanceAssassin;      // Minimum distance required between crewmate and assassin imposter for kill to be available.

    /* Task-related settings. */
    public int numCommonTasks;              // How many tasks do ALL players have?
    public int numShortTasks;               // How many short-length tasks does each player get?
    public int numLongTasks;                // How many long-length tasks does each player get?

    /* Voting and discussion related. */
    public int numEmergencyMeetings;        // How many emergency players may each player call?
    public float meetingCooldown;           // How long must players wait inbetween emergency meetings?
    public float discussionPeriod;          // How long do players have to discuss before voting begins?
    public float votingPeriod;              // How long do players have to cast their votes once voting begins?
    
    /* Emergency related. */
    public float emergencyCooldownStandard; // How long must regular imposters wait inbetween causing emergencies?
    public float emergencyCooldownSaboteur; // How long must Saboteurs wait inbetween causing emergencies?

    /* Role related */
    public bool saboteurEnabled;            // Is the "Saboteur" imposter role enabled?
    public bool assassinEnabled;            // Is the "Assassin" imposter role enabled?
    public bool sheriffEnabled;             // Is the "Sheriff" crewmate role enabed?
    public int maxSaboteurs;                // What is the maximum number of Saboteur imposters allowed?
    public int maxAssassins;                // What is the maximum number of Assassin imposters allowed?
    public int maxSheriffs;                 // What is the maximum number of sheriffs allowed?
    public int numberOfImposters;           // How many imposters are there?
    public float sheriffScannerCooldown;    // How long must the sheriff wait inbetween uses of his/her scanner?

    /* Vision related. */
    public int crewmateVision;              // How far can crewmates see?
    public int imposterVision;              // How far can imposters see?

    /* Visual */
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

    void Start() {

    }

    void Update() {

    }
}