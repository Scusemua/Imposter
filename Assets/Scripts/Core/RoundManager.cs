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
        if (Room.roomSlots.Count != Room.GamePlayers.Count) { return; }

        Debug.Log("Round can now start. Performing pre-round tasks (e.g., spawning weapons, assigning roles, etc.).");

        if (GameOptions.singleton.SpawnWeaponsAroundMap)
            Room.SpawnItemsAroundMap();
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
