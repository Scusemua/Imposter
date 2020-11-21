using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

public class AmmoBox : NetworkBehaviour
{
    private static Dictionary<GunClass, int> ammoPickupAmounts = new Dictionary<GunClass, int>
    {
        [GunClass.ASSAULT_RIFLE] = 60,
        [GunClass.SHOTGUN] = 20,
        [GunClass.SUBMACHINE_GUN] = 90,
        [GunClass.RIFLE] = 25,
        [GunClass.PISTOL] = 50,
        [GunClass.LIGHT_MACHINE_GUN] = 100,
        [GunClass.EXPLOSIVE] = 10
    };

    private static Dictionary<GunClass, Color> OutlineColors = new Dictionary<GunClass, Color>
    {
        [GunClass.ASSAULT_RIFLE] = new Color32(255, 0, 251, 0),          // Pink            
        [GunClass.SHOTGUN] = new Color32(255, 242, 0, 0),                // Yellow
        [GunClass.SUBMACHINE_GUN] = new Color32(85, 0, 255, 0),          // Purple
        [GunClass.RIFLE] = new Color32(0, 157, 255, 0),                  // Blue
        [GunClass.PISTOL] = new Color32(43, 255, 0, 0),                  // Green
        [GunClass.LIGHT_MACHINE_GUN] = new Color32(255, 162, 0, 0),      // Orange
        [GunClass.EXPLOSIVE] = new Color32(255, 0, 0, 0)                 // Red
    };

    [Header("Stats")]
    public GunClass AssociatedGunType;   // What ammo (gun) type will this refill?
    public int NumberBullets;           // How many bullets will the player get from picking up this ammo box?

    [Tooltip("If True, then this gives ammo when picked up. If False, then this gives health when picked up.")]
    public bool IsAmmoBox;

    // Start is called before the first frame update
    void Start()
    {
        if (IsAmmoBox)
        {
            GunClass[] gunTypes = Enum.GetValues(typeof(GunClass)).Cast<GunClass>().ToArray();

            // Randomly assign an associated ammo type for this ammo box.
            AssociatedGunType = gunTypes[UnityEngine.Random.Range(0, gunTypes.Length)];

            NumberBullets = ammoPickupAmounts[AssociatedGunType];
            GetComponent<Outline>().OutlineColor = OutlineColors[AssociatedGunType];
        }
        else
        {
            NumberBullets = 25; // In this case, NumberBullets functions has the health restored on pickup.
            GetComponent<Outline>().OutlineColor = new Color32(0, 120, 28, 0);  // Dark green.
        }
    }
}
