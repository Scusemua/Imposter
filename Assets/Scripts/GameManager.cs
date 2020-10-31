﻿using System.Collections;
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

    [SerializeField]
    private GameObject sceneCamera;

    private static System.Random RNG = new System.Random();

    #region Player Tracking 
    private const string PLAYER_ID_PREFIX = "Player ";

    private static Dictionary<string, Player> players = new Dictionary<string, Player>();

    /// <summary>
    /// This is called on the server when all the players in the room are ready.
    /// <para>The default implementation of this function uses ServerChangeScene() to switch to the game player scene. By implementing this callback you can customize what happens when all the players in the room are ready, such as adding a countdown or a confirmation for a group leader.</para>
    /// </summary>
    public override void OnRoomServerPlayersReady()
    {
        AssignRoles();

        // All players are readyToBegin, start the game.
        base.ServerChangeScene(GameplayScene);
    }

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
        Player player = null;
        try
        {
            player = players[_playerID];
        }
        catch (Exception ex)
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

        foreach (Player p in imposters)
            p.AssignRole("imposter");

        foreach (Player p in crewmates)
            p.AssignRole("crewmate");
    }

    void StartGame()
    {
        currentGameState = GameState.STARTING;
        AssignRoles();

        currentGameState = GameState.IN_PROGRESS;
    }

    void StopGame()
    {
        currentGameState = GameState.LOBBY;
    }

    public void SetSceneCameraActive(bool isActive)
    {
        if (sceneCamera == null)
            return;

        sceneCamera.SetActive(isActive);
    }

    void Awake()
    {
        base.Awake();
    }

    // Start is called before the first frame update
    void Start()
    {
        currentGameState = GameState.LOBBY;
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {

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
