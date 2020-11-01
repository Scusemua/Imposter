using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

public class RoundManager : NetworkBehaviour
{
    private NetworkGameManager room;
    private NetworkGameManager Room
    {
        get
        {
            if (room != null) { return room; }
            room = NetworkManager.singleton as NetworkGameManager;
            return room;
        }
    }

    #region Server 

    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkGameManager.OnServerStopped += CleanUpServer;
        NetworkGameManager.OnPlayerRegistered += CheckToStartRound;
    }

    [ServerCallback]
    private void OnDestroy() => CleanUpServer();

    [Server]
    private void CleanUpServer()
    {
        NetworkGameManager.OnServerStopped -= CleanUpServer;
        NetworkGameManager.OnPlayerRegistered -= CheckToStartRound;
    }

    [ServerCallback]
    public void StartRound()
    {
        RpcStartRound();
    }

    [Server]
    private void CheckToStartRound()
    {
        if (isClientOnly)
        {
            Debug.Log("I am a client. Returning from CheckToStartRound() immediately.");
            return;
        }

        Debug.Log("Room.GamePlayers.Count(x => x.connectionToClient.isReady) = " + Room.GamePlayers.Count(x => x.connectionToClient.isReady));
        Debug.Log("Number of registered players: " + Room.GamePlayers.Count);
        if (Room.roomSlots.Count != Room.GamePlayers.Count) { return; }

        Room.AssignRoles();
    }

    #endregion

    #region Client 

    [ClientRpc]
    private void RpcStartRound()
    {
        Debug.Log("Round starting!");
    }

    #endregion 
}
