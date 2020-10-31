using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using Mirror;

public class GameManager : NetworkRoomManager
{
    public enum GameState
    {
        LOBBY,
        STARTING,
        IN_PROGRESS,
        ENDING
    }

    private GameState currentGameState;
    private GameOptions gameOptions;

    private static System.Random RNG = new System.Random();

    #region Player Tracking 
    private const string PLAYER_ID_PREFIX = "Player ";

    private Dictionary<string, Player> players = new Dictionary<string, Player>();

    public GameObject startButton;

    private int numCrewmatesAlive;
    private int numImpostersAlive;

    /// <summary>
    /// Have the roles been assigned yet? Used to prevent win-condition checking before the game has really started.
    /// </summary>
    private bool rolesAssigned = false;

    public Player[] GetAllPlayers()
    {
        return players.Values.ToArray();
    }

    public void RegisterPlayer(string _netID, Player _player)
    {
        string _playerID = PLAYER_ID_PREFIX + _netID;
        Debug.Log("Registering player with PlayerID " + _playerID);
        players.Add(_playerID, _player);
        _player.transform.name = _playerID;

        if (players.Count == roomSlots.Count)
        {
            AssignRoles();
        }
    }

    public void UnRegisterPlayer(string _playerID)
    {
        players.Remove(_playerID);
    }

    public void VictoryCheck()
    {
        bool imposterVictory = false;
        bool crewmateVictory = false;

        // Win condition for imposters is simply equal number of crewmates and imposters alive.
        if (numImpostersAlive == numCrewmatesAlive && !gameOptions.impostersMustKillAllCrewmates)
        {
            // Imposters win.
            Debug.Log("Imposters have won!");
            imposterVictory = true;
        }
        // Imposters must kill all crewmates, all crewmates are dead, and at least one imposter is alive.
        else if (gameOptions.impostersMustKillAllCrewmates && numCrewmatesAlive == 0 && numImpostersAlive > 0)
        {
            // Imposters win.
            Debug.Log("Imposters have won!");
            imposterVictory = true;
        }
        // There are no imposters remaining and at least one crewmate lives.
        else if (numImpostersAlive == 0 && numCrewmatesAlive > 0)
        {
            Debug.Log("Crewmates have won!");
            crewmateVictory = true;
        }
        else if (numImpostersAlive == 0 && numCrewmatesAlive == 0)
        {
            Debug.Log("It's a draw... for now, draw goes to the crewmates.");
            crewmateVictory = true;
        }

        if (crewmateVictory || imposterVictory)
        {
            Player[] players = GetAllPlayers();

            // Kill the remaining players.
            foreach (Player p in players)
            {
                if (!p.isDead)
                    p.Kill(null, serverKilled: true);

                p.DisplayEndOfGameUI(crewmateVictory);
            }

            currentGameState = GameState.ENDING;
        }
    }

    public void PlayerDied(string _playerID)
    {
        Player deceasedPlayer = GetPlayer(_playerID);

        if (IsImposterRole(deceasedPlayer.Role.Name))
        {
            Debug.Log("Server has noted that player " + _playerID + ", who was an imposter, has died.");
            numImpostersAlive--;
        }
        else
        {
            Debug.Log("Server has noted that player " + _playerID + ", who was a crewmate, has died.");
            numCrewmatesAlive--;
        }
    }

    public Player GetPlayer(string _playerID)
    {
        Player player = null;
        try
        {
            player = players[_playerID];
        }
        catch (Exception)
        {
            Debug.LogError("Error: there is no player with ID \"" + _playerID + "\".");
            Debug.Log("Valid player IDs: " + players.Keys.ToArray());
        }

        return player;
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

    public void AssignRoles()
    {
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

        Debug.Log("Number of Players: " + allPlayers.Count);
        Debug.Log("Number of Imposters: " + numImposters);

        // Assign imposter roles randomly.
        for (int i = 0; i < numImposters; i++)
        {
            int imposterIndex = RNG.Next(players.Count);
            Player imposter = allPlayers[imposterIndex];
            allPlayers.RemoveAt(imposterIndex);
            imposter.Role = new ImposterRole();
            imposters.Add(imposter);
        }

        // The remaining players are crewmates.
        crewmates.AddRange(allPlayers);

        if (imposters.Count > 0)
        {
            foreach (Player p in imposters)
                p.AssignRole("imposter");
        }

        if (crewmates.Count > 0)
        {
            foreach (Player p in crewmates)
                p.AssignRole("crewmate");
        }

        numCrewmatesAlive = crewmates.Count;
        numImpostersAlive = imposters.Count;

        rolesAssigned = true;

        // Game has officially started.
        currentGameState = GameState.IN_PROGRESS;
    }

    public override void OnRoomServerPlayersReady()
    {
        // base.OnRoomServerPlayersReady();
    }

    public override void OnRoomServerSceneChanged(string sceneName)
    {
        base.OnRoomServerSceneChanged(sceneName);

        if (sceneName == RoomScene)
            currentGameState = GameState.LOBBY;
    }

    public bool AreAllPlayersReady()
    {
        return allPlayersReady;
    }

    public void OnStartButtonClicked()
    {
        if (allPlayersReady)
        {
            currentGameState = GameState.STARTING;
            //AssignRoles();

            // All players are readyToBegin, start the game.
            base.ServerChangeScene(GameplayScene);
        }
    }

    void StopGame()
    {
        currentGameState = GameState.LOBBY;
    }

    // Start is called before the first frame update
    void Start()
    {
        base.Start();
        currentGameState = GameState.LOBBY;
        gameOptions = GameOptions.singleton;
    }

    // Update is called once per frame
    void Update()
    {
        if (rolesAssigned && currentGameState == GameState.IN_PROGRESS)
            // Do a victory check.
            VictoryCheck();

        if (gameOptions == null)
            gameOptions = GameOptions.singleton;
    }

    public static bool IsImposterRole(string roleName)
    {
        string roleNameLower = roleName.ToLower();
        switch (roleNameLower)
        {
            case "imposter":
            case "saboteur":
            case "assassin":
                return true;
        }
        return false;
    }
}
