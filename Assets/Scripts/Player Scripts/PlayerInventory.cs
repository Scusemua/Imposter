using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerInventory : NetworkBehaviour
{
    /// <summary>
    /// The player's primary weapons.
    /// </summary>
    [SerializeField] public SyncList<InventoryGun> PrimaryInventory = new SyncList<InventoryGun>();

    /// <summary>
    /// The player's secondary weapons.
    /// </summary>
    [SerializeField] public SyncList<InventoryGun> SecondaryInventory = new SyncList<InventoryGun>();

    /// <summary>
    /// The player's melee weapons.
    /// </summary>
    [SerializeField] public SyncList<InventoryGun> MeleeInventory = new SyncList<InventoryGun>();

    /// <summary>
    /// The player's explosive weapons.
    /// </summary>
    [SerializeField] public SyncList<InventoryGun> ExplosiveInventory = new SyncList<InventoryGun>();

    /// <summary>
    /// The player's grenades.
    /// </summary>
    [SerializeField] public SyncList<InventoryGun> GrenadeInventory = new SyncList<InventoryGun>();
}
