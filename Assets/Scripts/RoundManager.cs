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
            return room = NetworkManager.singleton as NetworkGameManager;
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
        Debug.Log("Room.GetAllPlayersAsList().Count(x => x.connectionToClient.isReady) = " + Room.GetAllPlayersAsList().Count(x => x.connectionToClient.isReady));
        Debug.Log("Number of registered players: " + Room.GetAllPlayersAsList().Count);
        if (Room.roomSlots.Count != Room.GetAllPlayersAsList().Count) { return; }

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
