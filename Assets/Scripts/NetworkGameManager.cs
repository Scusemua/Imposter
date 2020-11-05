using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using Mirror;

public class NetworkGameManager : NetworkRoomManager
{
    public enum GameState
    {
        LOBBY,
        STARTING,
        IN_PROGRESS,
        ENDING
    }

    [SerializeField] private GameObject roundManager = null;

    public static event Action OnPlayerRegistered;
    public static event Action OnClientConnected;
    public static event Action OnClientDisconnected;
    public static event Action<NetworkConnection> OnServerReadied;
    public static event Action OnServerStopped;

    private GameState currentGameState;
    private GameOptions gameOptions;

    private static System.Random RNG = new System.Random();

    #region Player Tracking 
    private const string PLAYER_ID_PREFIX = "Player ";

    public List<Player> GamePlayers = new List<Player>();

    private int numCrewmatesAlive;
    private int numImpostersAlive;

    /// <summary>
    /// Have the roles been assigned yet? Used to prevent win-condition checking before the game has really started.
    /// </summary>
    private bool rolesAssigned = false;

    public void RegisterPlayer(string _netID, Player _player)
    {
        string _playerID = PLAYER_ID_PREFIX + _netID;
        Debug.Log("Registering player with PlayerID " + _playerID);
        GamePlayers.Add(_player);
        _player.transform.name = _playerID;

        Debug.Log("Players registered: " + GamePlayers.Count + ", Room Slots: " + roomSlots.Count);
        OnPlayerRegistered?.Invoke();
    }

    /// <summary>
    /// Check various win conditions for crewmates and imposters.
    /// </summary>
    public void VictoryCheck()
    {
        if (gameOptions.disableWinChecking) return;

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
            // Kill the remaining players.
            foreach (Player p in GamePlayers)
            {
                if (!p.isDead)
                    p.Kill("Server", serverKilled: true);

                p.DisplayEndOfGameUI(crewmateVictory);
            }

            currentGameState = GameState.ENDING;
        }
    }

    public void PlayerDied(Player deceasedPlayer)
    {
        if (IsImposterRole(deceasedPlayer.Role.Name))
        {
            Debug.Log("Server has noted that player " + deceasedPlayer.nickname + ", who was an imposter, has died.");
            numImpostersAlive--;
        }
        else
        {
            Debug.Log("Server has noted that player " + deceasedPlayer.nickname + ", who was a crewmate, has died.");
            numCrewmatesAlive--;
        }
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

    [Server]
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

        List<Player> allPlayers = new List<Player>(GamePlayers);

        Player[] shuffledPlayers = GamePlayers.ToArray<Player>();
        Debug.Log("Shuffling players now.");
        Shuffle<Player>(shuffledPlayers);

        Debug.Log("Number of Players: " + allPlayers.Count);
        Debug.Log("Number of Imposters: " + numImposters);

        // GameOptions validates the settings so that this will run without errors. We do not need to be careful here.
        int currentPlayerIndex;

        // Assign imposter roles randomly.
        for (currentPlayerIndex = 0; currentPlayerIndex < numImposters; currentPlayerIndex++)
            imposters.Add(shuffledPlayers[currentPlayerIndex]);

        // Add the remaining players to the crewmates list.
        for (; currentPlayerIndex < shuffledPlayers.Length; currentPlayerIndex++)
            crewmates.Add(shuffledPlayers[currentPlayerIndex]);

        if (imposters.Count > 0)
            foreach (Player p in imposters)
                p.RpcAssignRole("imposter");

        if (crewmates.Count > 0)
            foreach (Player p in crewmates)
                p.RpcAssignRole("crewmate");

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
        if (sceneName == RoomScene)
        {
            GameEnded();
        }

        if (sceneName == GameplayScene)
        {
            GameObject roundManagerInstance = Instantiate(roundManager);
            NetworkServer.Spawn(roundManagerInstance);
        }
    }

    public bool AreAllPlayersReady()
    {
        return allPlayersReady;
    }

    public void OnStartButtonClicked()
    {
        if (allPlayersReady)
        {
            Debug.Log("Start button clicked and all players are ready. Changing scene now...");

            currentGameState = GameState.STARTING;

            // All players are readyToBegin, start the game.
            base.ServerChangeScene(GameplayScene);
        }
        else
            Debug.Log("Start button clicked and not all players are ready. Doing nothing... ");
    }

    /// <summary>
    /// Perform the necessary clean-up once the game has ended and players are returning back to the lobby.
    /// </summary>
    void GameEnded()
    {
        GamePlayers.Clear();    // This effectively unregisters all players.
        numCrewmatesAlive = 0;  // The game is not active so reset these variables.
        numImpostersAlive = 0;  // The game is not active so reset these variables.
        rolesAssigned = false;  // Flip this back to false since roles have not been assigned for next game.

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

    public override void OnServerReady(NetworkConnection conn)
    {
        base.OnServerReady(conn);

        OnServerReadied?.Invoke(conn);
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);

        OnClientConnected?.Invoke();
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);

        OnClientDisconnected?.Invoke();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        OnServerStopped?.Invoke();

        GamePlayers.Clear();
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

    /// <summary>
    /// Shuffle the array.
    /// </summary>
    /// <typeparam name="T">Array element type.</typeparam>
    /// <param name="array">Array to shuffle.</param>
    public void Shuffle<T>(T[] array)
    {
        int n = array.Length;
        for (int i = 0; i < (n - 1); i++)
        {
            // Use Next on random instance with an argument.
            // ... The argument is an exclusive bound.
            //     So we will not go past the end of the array.
            int r = i + RNG.Next(n - i);
            T t = array[r];
            array[r] = array[i];
            array[i] = t;
        }
    }
}
