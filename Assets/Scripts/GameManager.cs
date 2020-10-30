using System.Collections;
using System.Collections.Generic;
using System;

using UnityEngine;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public enum GameState
    {
        LOBBY,
        STARTING,
        IN_PROGRESS,
        ENDING
    }

    private GameState currentGameState;

    [SerializeField]
    private GameObject sceneCamera;

    private static System.Random RNG = new System.Random();

    #region Player Tracking 
    private const string PLAYER_ID_PREFIX = "Player ";

    private static Dictionary<string, Player> players = new Dictionary<string, Player>();

    public static Player[] GetAllPlayers()
    {
        return players.Values.ToArray();
    }

    public static void RegisterPlayer(string _netID, Player _player)
    {
        string _playerID = PLAYER_ID_PREFIX + _netID;
        Debug.Log("Registering player with PlayerID " + _playerID);
        players.Add(_playerID, _player);
        _player.transform.name = _playerID;
    }

    public static void UnRegisterPlayer(string _playerID)
    {
        players.Remove(_playerID);
    }

    public static Player GetPlayer(string _playerID)
    {
        return players[_playerID];
    }

    #endregion

    // Players can either be an imposter or a crewmate.
    public enum PlayerRole
    {
        Crewmate,
        Imposter
    }

    // Crewmates may be a regular, standard crewmate, or a different role that has special abilities.
    public enum CrewmateRoles
    {
        StandardCrewmate,
        Sheriff 
    }

    // Imposters may be a regular, standard crewmate, or a different role that has special abilities.
    public enum ImposterRoles 
    {
        StandardImposter,
        Assassin,
        Saboteur
    }

    private void AssignRoles() {
        Debug.Log("Assigning roles now...");

        // We need to first determine the configuration of the game. We can get this information from the GameOptions object.
        GameOptions gameOptions = FindObjectOfType<GameOptions>();

        // First, get the constraints on roles.
        int numImposters = gameOptions.numberOfImposters;
        int maxAssassins = gameOptions.maxAssassins;
        int maxSheriffs = gameOptions.maxSheriffs;
        int maxSaboteurs = gameOptions.maxSaboteurs;

        bool sheriffsEnabled = gameOptions.sheriffEnabled;
        bool saboteurEnabled = gameOptions.saboteurEnabled;
        bool assassinEnabled = gameOptions.assassinEnabled;

        // First, determine who is a crewmate and who is an imposter. 
        // Then, assign however many specialized roles as specified by the game options.
        List<Player> imposters = new List<Player>();
        List<Player> crewmates = new List<Player>();

        List<Player> allPlayers = new List<Player>(GetAllPlayers());

        // Assign imposter roles randomly.
        for (int i = 0; i < numImposters; i++) {
            int imposterIndex = RNG.Next(players.Count);
            Player imposter = allPlayers[imposterIndex];
            allPlayers.RemoveAt(imposterIndex);
            imposter.Role = new ImposterRole();
            imposters.Add(imposter);
        }
        
        // The remaining players are crewmates.
        crewmates.AddRange(allPlayers);

        foreach (Player p in imposters)
            p.AssignRole("imposter");

        foreach (Player p in crewmates)
            p.AssignRole("crewmate");
    }

    void StartGame()
    {
        AssignRoles();
    }

   public void SetSceneCameraActive(bool isActive)
   {
      if (sceneCamera == null)
         return;

      sceneCamera.SetActive(isActive);
   }

    void Awake() {
        if (instance != null)
        {
            Debug.LogError("More than one GameManager in scene.");
        }
        else
        {
            instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (players.Count > 0)
            StartGame();
    }
}
