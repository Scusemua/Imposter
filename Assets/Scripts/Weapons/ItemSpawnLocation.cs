using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawnLocation : MonoBehaviour
{
    public void Awake()
    {
        Transform[] spawnLocations = GetComponentsInChildren<Transform>();
        foreach (Transform spawnLoc in spawnLocations)
            NetworkGameManager.RegisterItemSpawnLocation(spawnLoc);
    }

    public void OnDestroy()
    {
        Transform[] spawnLocations = GetComponentsInChildren<Transform>();
        foreach (Transform spawnLoc in spawnLocations)
            NetworkGameManager.UnRegisterItemSpawnLocation(spawnLoc);
    }
}
