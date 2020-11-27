using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerInventory : NetworkBehaviour
{
    /// <summary>
    /// The maximum number of primary weapons that the player is allowed to have in his or her inventory.
    /// </summary>
    public int MaximumPrimaryWeapons = 2;

    /// <summary>
    /// The maximum number of secondary weapons that the player is allowed to have in his or her inventory.
    /// </summary>
    public int MaximumSecondaryWeapons = 2;

    /// <summary>
    /// The maximum number of explosive weapons that the player is allowed to have in his or her inventory.
    /// </summary>
    public int MaximumExplosiveWeapons = 2;

    /// <summary>
    /// The number of primary weapons that the player currently has in his or her inventory.
    /// </summary>
    [HideInInspector] public int NumPrimariesHeld;

    /// <summary>
    /// The number of secondary weapons that the player currently has in his or her inventory.
    /// </summary>
    [HideInInspector] public int NumSecondariesHeld;

    /// <summary>
    /// The number of explosive weapons that the player currently has in his or her inventory.
    /// </summary>
    [HideInInspector] public int NumExplosiveWeaponsHeld;

    /// <summary>
    /// The player's weapons.
    /// </summary>
    [SerializeField] public SyncList<int> GunInventory = new SyncList<int>();

    /// <summary>
    /// The player's items (e.g., C4, body scanner, etc.).
    /// </summary>
    [SerializeField] public SyncList<int> ItemInventory = new SyncList<int>();

    private ItemDatabase itemDatabase;
    private ItemDatabase ItemDatabase
    {
        get
        {
            if (itemDatabase == null)
                itemDatabase = FindObjectOfType<ItemDatabase>();
            return itemDatabase;
        }
    }

    /// <summary>
    /// A mapping from gun ID to the associated game object of the weapon, if said weapon is in
    /// the player's inventory.
    /// </summary>
    private Dictionary<int, GameObject> WeaponGameObjects = new Dictionary<int, GameObject>();

    void Start()
    {
        itemDatabase = FindObjectOfType<ItemDatabase>();
    }

    /// <summary>
    /// Return the index of the weapon with the specified ID in the player's inventory.
    /// </summary>
    public int IndexOf(int weaponId)
    {
        return GunInventory.IndexOf(weaponId);
    }

    /// <summary>
    /// Return the index of the specified weapon in the player's inventory.
    /// </summary>
    public int IndexOf(Gun gun)
    {
        return IndexOf(gun.Id);
    }

    /// <summary>
    /// Return the ID of the gun stored at position <paramref name="index"/> of the player's inventory.
    /// 
    /// Returns -1 if the given index is out-of-bounds (negative or greater-than-or-equal-to the 
    /// length of the player's inventory).
    /// </summary>
    public int GetGunIdAtIndex(int index)
    {
        // Bounds check.
        if (index < 0 || index >= GunInventory.Count)
            return -1;

        return GunInventory[index];
    }

    /// <summary>
    /// Return the game object of the gun stored at index <paramref name="index"/> of the player's inventory.
    /// 
    /// Returns null if there if the index is out of bounds.
    /// </summary>
    public GameObject GetGunGameObjectAtIndex(int index)
    {
        // Bounds check.
        if (index < 0 || index >= GunInventory.Count)
            return null;

        WeaponGameObjects.TryGetValue(GunInventory[index], out GameObject gunGameObject);

        return gunGameObject;
    }

    /// <summary>
    /// Return the index of the next weapon of the given gun type relative to the currently-equipped weapon's index.
    /// 
    /// Note that the currently-held/currently-equipped weapon need not be of the specified gun type.
    /// </summary>
    /// <param name="currentWeaponID">The currently-held/currently-equipped weapon.</param>
    public int GetNextWeaponOfType(Gun.GunType gunType, int currentWeaponID)
    {
        int currentWeaponIndex;

        // If the current weapon ID is negative one, then the player just doesn't have a weapon equipped.
        if (currentWeaponID == -1)
            currentWeaponIndex = -1;
        else
        {
            // If the player's current weapon ID is not -1, then they have an actual weapon, 
            // and we should try to locate it in their inventory.
            currentWeaponIndex = IndexOf(currentWeaponID);

            if (currentWeaponIndex == -1)
            {
                Debug.LogError("Player does NOT actually have weapon with ID " + currentWeaponID + " in their inventory.");
                currentWeaponIndex = 0;
            }
        }

        Debug.Log("Cycling inventory. Current weapon ID = " + currentWeaponID + ", current weapon index = " + currentWeaponIndex);

        for (int i = currentWeaponIndex + 1; i < GunInventory.Count; i++)
        {
            int gunId = GunInventory[i];

            // If the gun we're currently looking at is of the desired type, return i, which is the index of that gun.
            if (ItemDatabase.GetGunByID(gunId)._GunType == gunType)
                return i;
        }

        // If currentWeaponIndex is not greater than zero, then we would've iterated thru the whole inventory already,
        // and there's no point in doing so again. So we'll just skip this for-loop and return -1.
        if (currentWeaponIndex > 0)
        {
            // Will return currentWeaponIndex if the current gun is the only gun of that type in the player's inventory.
            for (int i = 0; i <= currentWeaponIndex; i++)
            {
                int gunId = GunInventory[i];

                // If the gun we're currently looking at is of the desired type, return i, which is the index of that gun.
                if (ItemDatabase.GetGunByID(gunId)._GunType == gunType)
                    return i;
            }
        }

        // There is no weapon of the desired type in the player's inventory.
        return -1;
    }

    /// <summary>
    /// Returns a list of lists containing the names of the guns in the player's inventory, organized by gun type.
    /// 
    /// The list at index 0 contains the names of all primary weapons in the player's inventory.
    /// The list at index 1 contains the names of all secondary weapons in the player's inventory.
    /// The list at index 2 contains the names of all explosive weapons in the player's inventory.
    /// </summary>
    public Dictionary<Gun.GunType, IEnumerable<string>> GetGunsNamesOrganized()
    {
        Dictionary<Gun.GunType, IEnumerable<string>> gunNameLists = new Dictionary<Gun.GunType, IEnumerable<string>>();

        gunNameLists[Gun.GunType.PRIMARY] = new List<string>();
        gunNameLists[Gun.GunType.SECONDARY] = new List<string>();
        gunNameLists[Gun.GunType.EXPLOSIVE] = new List<string>();

        foreach (int gunId in GunInventory)
        {
            Gun gun = ItemDatabase.GetGunByID(gunId);
            (gunNameLists[gun._GunType] as List<string>).Add(gun.Name);
        }

        return gunNameLists;
    }

    #region Server Methods 

    /// <summary>
    /// Check if the player has the given gun with the given ID in their inventory.
    /// </summary>
    /// <param name="gun">The weapon in question.</param>
    /// <returns>True if the player's inventory contains the given weapon, otherwise false.</returns>
    [Server]
    public bool HasGun(Gun gun)
    {
        return HasGun(gun.Id);
    }

    /// <summary>
    /// Check if the player has the weapon with the given ID in their inventory.
    /// </summary>
    /// <param name="id">The ID of the weapon in question.</param>
    /// <returns>True if the player's inventory contains the weapon with the given ID, otherwise false.</returns>
    [Server]
    public bool HasGun(int id)
    {
        return GunInventory.Contains(id);
    }

    /// <summary>
    /// Attempt to add the weapon with the given ID to the player's inventory.
    /// </summary>
    /// <param name="weaponId">The ID of the weapon we're trying to add to our inventory.</param>
    /// <returns>True if the weapon was added, otherwise false.</returns>
    [Server]
    public bool AddWeaponToInventory(int weaponId, GameObject weaponGameObject)
    {
        // If the player already has this gun, return false.
        if (HasGun(weaponId)) return false;

        Gun gun = ItemDatabase.GetGunByID(weaponId);

        Gun.GunType gunType = gun._GunType;

        // Attempt to add the gun to the inventory. This will be unsuccessful if the player
        // already has the maximum number of weapons of the given type (i.e., primaries, secondaries, or explosives).
        switch(gunType)
        {
            case Gun.GunType.PRIMARY:
                if (NumPrimariesHeld < MaximumPrimaryWeapons)
                {
                    GunInventory.Add(gun.Id);
                    NumPrimariesHeld += 1;
                    WeaponGameObjects.Add(weaponId, weaponGameObject);
                    return true;
                }
                break;
            case Gun.GunType.SECONDARY:
                if (NumSecondariesHeld < MaximumSecondaryWeapons)
                {
                    GunInventory.Add(gun.Id);
                    NumSecondariesHeld += 1;
                    WeaponGameObjects.Add(weaponId, weaponGameObject);
                    return true;
                }
                break;
            case Gun.GunType.EXPLOSIVE:
                if (NumExplosiveWeaponsHeld < MaximumExplosiveWeapons)
                {
                    GunInventory.Add(gun.Id);
                    NumExplosiveWeaponsHeld += 1;
                    WeaponGameObjects.Add(weaponId, weaponGameObject);
                    return true;
                }
                break;
        }

        // Make sure our state is okay.
        SanityCheck();

        return false;
    }

    /// <summary>
    /// Attempt to remove the weapon with the given ID from the player's inventory.
    /// </summary>
    /// <param name="weaponId">The ID of the weapon to be removed.</param>
    /// <returns>Returns true if the gun was in the player's inventory, otherwise returns false.</returns>
    [Server]
    public bool RemoveWeaponFromInventory(int weaponId)
    {
        if (!GunInventory.Contains(weaponId))
            return false;

        Gun.GunType gunType = ItemDatabase.GetGunByID(weaponId)._GunType;

        if (gunType == Gun.GunType.PRIMARY)
            NumPrimariesHeld--;

        if (gunType == Gun.GunType.SECONDARY)
            NumSecondariesHeld--;

        if (gunType == Gun.GunType.EXPLOSIVE)
            NumExplosiveWeaponsHeld--;

        WeaponGameObjects.Remove(weaponId);
        GunInventory.Remove(weaponId);

        // Make sure our state is okay.
        SanityCheck();

        return true;
    }

    /// <summary>
    /// Return the GameObject of the weapon with the given ID from the player's inventory.
    /// 
    /// This will return null if the player does not have the weapon associated with the given ID in their inventory.
    /// </summary>
    [Server]
    public GameObject GetWeaponGameObjectFromInventory(int weaponId)
    {
        if (!HasGun(weaponId))
            return null;

        return WeaponGameObjects[weaponId];
    }

    /// <summary>
    /// Check the player's inventory for any funny business (i.e., state variables being negative).
    /// </summary>
    /// <returns>True if everything is fine, otherwise false.</returns>
    [Server]
    private bool SanityCheck()
    {
        if (NumPrimariesHeld < 0)
        {
            Debug.LogError("Player's inventory is in an error state. NumPrimariesHeld = " + NumPrimariesHeld);
            return false;
        }

        if (NumSecondariesHeld < 0)
        {
            Debug.LogError("Player's inventory is in an error state. NumSecondariesHeld = " + NumSecondariesHeld);
            return false;
        }

        if (NumExplosiveWeaponsHeld < 0)
        {
            Debug.LogError("Player's inventory is in an error state. NumExplosiveWeaponsHeld = " + NumExplosiveWeaponsHeld);
            return false;
        }

        if (NumPrimariesHeld > MaximumPrimaryWeapons)
        {
            Debug.LogError("Player's inventory is in an error state. NumPrimariesHeld = " 
                + NumPrimariesHeld + ", MaximumPrimaryWeapons = " + MaximumPrimaryWeapons);
            return false;
        }
        
        if (NumSecondariesHeld > MaximumSecondaryWeapons)
        {
            Debug.LogError("Player's inventory is in an error state. NumSecondariesHeld = "
                + NumSecondariesHeld + ", MaximumSecondaryWeapons = " + MaximumSecondaryWeapons);
            return false;
        }

        if (NumExplosiveWeaponsHeld > MaximumExplosiveWeapons)
        {
            Debug.LogError("Player's inventory is in an error state. NumExplosiveWeaponsHeld = "
                + NumExplosiveWeaponsHeld + ", MaximumExplosiveWeapons = " + MaximumExplosiveWeapons);
            return false;
        }

        return true;
    }

    #endregion 
}
