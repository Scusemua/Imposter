using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using Mirror;

public class NetworkGameManager : NetworkRoomManager
{
    /// <summary>
    /// This is the netId submitted by players when they're skipping their vote.
    /// </summary>
    public static uint SKIPPED_VOTE_NET_ID = 9999;

    private ItemDatabase itemDatabase;
    
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

    /// <summary>
    /// We can spawn ammo and weapons at these locations when the match starts.
    /// </summary>
    [HideInInspector]
    public static List<Transform> ItemSpawnLocations = new List<Transform>();

    private GameState currentGameState;
    private GameOptions gameOptions;

    private static System.Random RNG = new System.Random();

    /// <summary>
    /// As long as this is greater than zero, the emergency meeting button is still cooling down.
    /// </summary>
    private float emergencyMeetingCooldown;

    /// <summary>
    /// This is set to false immediately after an emergency meeting occurs.
    /// 
    /// It automatically gets set back to true after enough time has passed.
    /// </summary>
    public bool CanHoldEmergencyMeeting;
    
    private const string PLAYER_ID_PREFIX = "Player ";

    /// <summary>
    /// Keep track of players in the game.
    /// </summary>
    public List<Player> GamePlayers = new List<Player>();

    /// <summary>
    /// Mapping between netId's and the players associated with the given netId.
    /// </summary>
    public Dictionary<uint, Player> NetIdMap = new Dictionary<uint, Player>();

    private int numCrewmatesAlive;
    private int numImpostersAlive;

    /// <summary>
    /// The number of votes we need to receive during a voting period. 
    /// Dead players don't get any votes, so they aren't counted.
    /// </summary>
    private int numVotesExpected;

    /// <summary>
    /// The number of votes we've received so far.
    /// </summary>
    private int numVotesReceived;

    /// <summary>
    /// Map from Player netId to the votes cast for that player.
    /// </summary>
    private Dictionary<uint, List<Vote>> votes = new Dictionary<uint, List<Vote>>();

    #region Player State Tracking 

    /// <summary>
    /// Have the roles been assigned yet? Used to prevent win-condition checking before the game has really started.
    /// </summary>
    private bool rolesAssigned = false;

    public void RegisterPlayer(uint _netID, Player _player)
    {
        string _playerID = PLAYER_ID_PREFIX + _netID.ToString();
        Debug.Log("Registering player with PlayerID " + _playerID);
        GamePlayers.Add(_player);
        NetIdMap.Add(_netID, _player);
        _player.transform.name = _playerID;

        Debug.Log("Players registered: " + GamePlayers.Count + ", Room Slots: " + roomSlots.Count);
        OnPlayerRegistered?.Invoke();
    }

    public void PlayerDied(Player deceasedPlayer)
    {
        if (IsImposterRole(deceasedPlayer.Role.Name))
        {
            Debug.Log("Server has noted that player " + deceasedPlayer.Nickname + ", who was an imposter, has died.");
            numImpostersAlive--;
        }
        else
        {
            Debug.Log("Server has noted that player " + deceasedPlayer.Nickname + ", who was a crewmate, has died.");
            numCrewmatesAlive--;
        }
    }

    #endregion

    #region Role Related

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
        int numImposters = gameOptions.NumberOfImposters;
        int maxAssassins = gameOptions.MaxAssassins;
        int maxSheriffs = gameOptions.MaxSheriffs;
        int maxSaboteurs = gameOptions.MaxSaboteurs;

        bool sheriffsEnabled = gameOptions.SheriffEnabled;
        bool saboteurEnabled = gameOptions.SaboteurEnabled;
        bool assassinEnabled = gameOptions.AssassinEnabled;

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

    #endregion

    #region Voting 

    /// <summary>
    /// This is called whenever a player either:
    /// (a) interacts with the emergency button, or
    /// (b) finds a previously unidentified body.
    /// 
    /// This method causes the voting UI to be displayed on all of the players' screens.
    /// </summary>
    [Server]
    public void StartVote()
    {
        if (!CanHoldEmergencyMeeting)
            return;

        foreach (Player player in GamePlayers)
        {
            player.RpcBeginVoting();

            // If the player is still alive, then take note that we need to receive their vote.
            if (!player.IsDead)
                numVotesExpected += 1;
        }

        CanHoldEmergencyMeeting = false;

        // The next meeting will be available after the voting period, discussion 
        // period, transition period, and cooldown have elapsed. 
        emergencyMeetingCooldown = gameOptions.DiscussionPeriodLength 
                                   + gameOptions.VotingPeriodLength 
                                   + gameOptions.EmergencyMeetingCooldown
                                   + 1.0f; // Transition period between discussion and voting phase.
    }

    /// <summary>
    /// Handle a vote from a player.
    /// </summary>
    /// <param name="voter">The player whose vote is being casted.</param>
    /// <param name="candidateId">The player for which the voter casted their vote.</param>
    [Server]
    public void CastVote(Player voter, uint candidateId)
    {
        numVotesReceived += 1;
        Debug.Log("Received vote " + numVotesReceived + "/" + numVotesExpected + " from player " + voter.Nickname + ", netId = " + voter.netId);

        Player voteRecipient = NetIdMap[candidateId];

        if (voteRecipient.IsDead)
        {
            Debug.LogError("Player " + voter.Nickname + ", netId = " + voter.netId + " has cast a vote for a dead player...");
            throw new InvalidOperationException("Player " + voter.netId + " cast vote for dead player " + voteRecipient.netId);
        }

        // Get the current number of votes for the candidate. This returns 0 if there are no votes yet.
        votes.TryGetValue(candidateId, out List<Vote> currentVotes);

        Vote vote = new Vote(candidateId, voter.netId);

        // If there are no existing votes yet for this player, we'll create the list of votes.
        if (currentVotes == null)
        {
            currentVotes = new List<Vote>();
            currentVotes.Add(vote);
            votes[candidateId] = currentVotes;
        }
        else
            currentVotes.Add(vote);

        if (numVotesReceived == numVotesExpected)
        {
            // TODO: Remove this as we don't wanna bother when we're just talling the votes.
            foreach (Player gamePlayer in GamePlayers)
                gamePlayer.RpcPlayerVoted(voter.netId);

            // We skip displaying the "I VOTED" icon in this case. Just tally votes.
            Debug.Log("Successfully received all " + numVotesExpected + " votes. Tallying results now...");
            TallyVotes();
        }
        else if (numVotesReceived > numVotesExpected)
            Debug.LogError("Received " + numVotesReceived + " even though we're only expecting " + numVotesExpected + " votes...");
        else
        {
            // Inform all of the players that somebody voted so that the "I VOTED" icon can be displayed.
            foreach (Player gamePlayer in GamePlayers)
                gamePlayer.RpcPlayerVoted(voter.netId);
        }
    }

    [Server]
    private void TallyVotes()
    {
        // Iterate through votes map. For each key value pair, count the number of votes
        // and check who casted each vote. We will definitely inform players of the number of
        // votes each player received.
        //
        // Depending on how the game is configured, we will also tell players who cast which vote.
        throw new NotImplementedException("Have not written logic for TallyVotes() yet.");
    }

    /// <summary>
    /// Clear all state related to voting so that voting can seamlessly occur again.
    /// </summary>
    [Server]
    public void VotingEnded()
    {
        votes.Clear();
        numVotesReceived = 0;
        numVotesExpected = 0;
    }

    #endregion

    #region Game Management

    /// <summary>
    /// Register a Transform as a location at which weapons and ammo can be spawned by the server.
    /// </summary>
    public static void RegisterItemSpawnLocation(Transform spawnLocation)
    {
        Debug.Log("Registering item spawn location at " + spawnLocation.position);
        ItemSpawnLocations.Add(spawnLocation);
    }

    /// <summary>
    /// UnRegister a Transform as a location at which weapons and ammo can be spawned by the server.
    /// </summary>
    public static void UnRegisterItemSpawnLocation(Transform spawnLocation)
    {
        Debug.Log("Unregistering item spawn location at " + spawnLocation.position);
        ItemSpawnLocations.Remove(spawnLocation);
    }

    /// <summary>
    /// Perform the necessary clean-up once the game has ended and players are returning back to the lobby.
    /// </summary>
    void GameEnded()
    {
        GamePlayers.Clear();    // This effectively unregisters all players.
        NetIdMap.Clear();       // Clear this mapping as well, since we've unregistered all the players.
        numCrewmatesAlive = 0;  // The game is not active so reset these variables.
        numImpostersAlive = 0;  // The game is not active so reset these variables.
        rolesAssigned = false;  // Flip this back to false since roles have not been assigned for next game.

        currentGameState = GameState.LOBBY;
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
        if (numImpostersAlive == numCrewmatesAlive && !gameOptions.ImpostersMustKillAllCrewmates)
        {
            // Imposters win.
            Debug.Log("Imposters have won!");
            imposterVictory = true;
        }
        // Imposters must kill all crewmates, all crewmates are dead, and at least one imposter is alive.
        else if (gameOptions.ImpostersMustKillAllCrewmates && numCrewmatesAlive == 0 && numImpostersAlive > 0)
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
                if (!p.IsDead)
                    p.Kill("Server", serverKilled: true);

                p.DisplayEndOfGameUI(crewmateVictory);
            }

            currentGameState = GameState.ENDING;
        }
    }

    public bool AreAllPlayersReady()
    {
        return allPlayersReady;
    }

    #endregion 

    /// <summary>
    /// This allows customization of the creation of the GamePlayer object on the server.
    /// <para>This is only called for subsequent GamePlay scenes after the first one.</para>
    /// <para>See <see cref="OnRoomServerCreateGamePlayer(NetworkConnection, GameObject)">OnRoomServerCreateGamePlayer(NetworkConnection, GameObject)</see> to customize the player object for the initial GamePlay scene.</para>
    /// </summary>
    /// <param name="conn">The connection the player object is for.</param>
    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        if (IsSceneActive(RoomScene))
        {
            if (roomSlots.Count == maxConnections)
                return;

            allPlayersReady = false;

            Debug.Log("NetworkRoomManager.OnServerAddPlayer playerPrefab:{0}" + roomPlayerPrefab.name);

            GameObject newRoomGameObject = OnRoomServerCreateRoomPlayer(conn);
            if (newRoomGameObject == null)
                newRoomGameObject = Instantiate(roomPlayerPrefab.gameObject, Vector3.zero, Quaternion.identity);

            NetworkServer.AddPlayerForConnection(conn, newRoomGameObject);
        }
        else
        {
            Transform startPos = GetStartPosition();
            GameObject player = startPos != null
                ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
                : Instantiate(playerPrefab);

            NetworkServer.AddPlayerForConnection(conn, player);

            uint networkID = conn.identity.GetComponent<NetworkIdentity>().netId;

            foreach (NetworkRoomPlayer netPlayer in roomSlots)
            {
                if (netPlayer.netId == networkID)
                {
                    Debug.Log("Found match. Assigning color " + (netPlayer as CustomNetworkRoomPlayer).PlayerModelColor);
                    player.GetComponent<Player>().PlayerColor = (netPlayer as CustomNetworkRoomPlayer).PlayerModelColor;
                    break;
                }
            }
            Debug.LogError("Failed to find matching player for networkID " + networkID);
        }
    }

    public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnection conn, GameObject roomPlayer, GameObject gamePlayer)
    {
        gamePlayer.GetComponent<Player>().PlayerColor = roomPlayer.GetComponent<CustomNetworkRoomPlayer>().PlayerModelColor;

        //return base.OnRoomServerSceneLoadedForPlayer(conn, roomPlayer, gamePlayer);
        return true;
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        if (sceneName == GameplayScene)
            itemDatabase = GameObject.FindGameObjectWithTag("ItemDatabase").GetComponent<ItemDatabase>();

        if (gameOptions.SpawnWeaponsAroundMap)
        {
            Debug.Log("Spawning items now. There are " + ItemSpawnLocations.Count + " item spawn locations.");
            System.Random rng = new System.Random();
            foreach (Transform transform in ItemSpawnLocations)
            {
                Vector3 position = transform.position;
                Quaternion rotation = transform.rotation;

                if (rng.NextDouble() <= GameOptions.WeaponSpawnChance)
                {
                    // Instantiate the scene object on the server.
                    int gunId = rng.Next(0, itemDatabase.MaxWeaponId + 1); // We add 1 bc this is exclusive.
                    Gun gun = Instantiate(itemDatabase.GetGunByID(gunId), position, rotation);

                    // set the RigidBody as non-kinematic on the server only (isKinematic = true in prefab).
                    gun.GetComponent<Rigidbody>().isKinematic = false;

                    NetworkServer.Spawn(gun.gameObject);
                    Debug.Log("Spawned weapon " + gunId + " -- \"" + gun.Name + "\" -- at position " + position + ".");
                }
                else
                {
                    // Spawn an ammo box instead -- with the possibility that it is a medkit.
                    if (rng.NextDouble() <= GameOptions.MedkitSpawnChance)
                    {
                        // Instantiate the scene object on the server.
                        int medkitPrefabVariantIndex = rng.Next(0, itemDatabase.NumMedkitVariants);
                        AmmoBox medkit = Instantiate(itemDatabase.GetMedkitByIndex(medkitPrefabVariantIndex), position, rotation);

                        // set the RigidBody as non-kinematic on the server only (isKinematic = true in prefab).
                        medkit.GetComponent<Rigidbody>().isKinematic = false;

                        NetworkServer.Spawn(medkit.gameObject);
                        Debug.Log("Spawned medkit variant " + medkitPrefabVariantIndex + " at position " + position + ".");
                    }
                    else
                    {
                        // Instantiate the scene object on the server.
                        int ammoBoxPrefabVariantIndex = rng.Next(0, itemDatabase.NumMedkitVariants);
                        AmmoBox ammoBox = Instantiate(itemDatabase.GetMedkitByIndex(ammoBoxPrefabVariantIndex), position, rotation);

                        // set the RigidBody as non-kinematic on the server only (isKinematic = true in prefab).
                        ammoBox.GetComponent<Rigidbody>().isKinematic = false;

                        NetworkServer.Spawn(ammoBox.gameObject);
                        Debug.Log("Spawned ammo box variant " + ammoBoxPrefabVariantIndex + " at position " + position + ".");
                    }
                }
            }
        }
    }

    private void SpawnItemsAroundMap()
    {

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
        
        if (!CanHoldEmergencyMeeting)
        {
            emergencyMeetingCooldown -= Time.deltaTime;

            // Check if cooldown has ended.
            if (emergencyMeetingCooldown <= 0)
                CanHoldEmergencyMeeting = true;
        }
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

    #region Utility 

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

    private class Vote
    {
        /// <summary>
        /// The netId of the player for which the vote was casted.
        /// </summary>
        private uint recipientNetId;

        /// <summary>
        /// The netId of the player who cast the vote.
        /// </summary>
        private uint voterNetId;

        public Vote(uint recipientNetId, uint voterNetId)
        {
            this.recipientNetId = recipientNetId;
            this.voterNetId = voterNetId;
        }
    }

    #endregion 
}
