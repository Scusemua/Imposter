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
        [GunClass.ASSAULT_RIFLE] = new Color32(255, 0, 251, 255),          // Pink            
        [GunClass.SHOTGUN] = new Color32(255, 242, 0, 255),                // Yellow
        [GunClass.SUBMACHINE_GUN] = new Color32(85, 0, 255, 255),          // Purple
        [GunClass.RIFLE] = new Color32(0, 157, 255, 255),                  // Blue
        [GunClass.PISTOL] = new Color32(43, 255, 0, 255),                  // Green
        [GunClass.LIGHT_MACHINE_GUN] = new Color32(255, 162, 0, 255),      // Orange
        [GunClass.EXPLOSIVE] = new Color32(255, 0, 0, 255)                 // Red
    };

    [Header("Stats")]
    public GunClass AssociatedGunClass;   // What ammo (gun) type will this refill?
    public int NumberBullets;             // How many bullets will the player get from picking up this ammo box?

    [Tooltip("If True, then this gives ammo when picked up. If False, then this gives health when picked up.")]
    public bool IsAmmoBox;

    // Start is called before the first frame update
    public override void OnStartClient()
    {
        Outline BoxOutline = GetComponent<Outline>();
        if (IsAmmoBox)
        {
            GunClass[] gunTypes = Enum.GetValues(typeof(GunClass)).Cast<GunClass>().ToArray();

            // Randomly assign an associated ammo type for this ammo box.
            AssociatedGunClass = gunTypes[UnityEngine.Random.Range(0, gunTypes.Length)];

            NumberBullets = ammoPickupAmounts[AssociatedGunClass];
            BoxOutline.OutlineWidth = 3;
            BoxOutline.OutlineColor = OutlineColors[AssociatedGunClass];
        }
        else
        {
            // In this case, NumberBullets functions has the health restored on pickup.
            BoxOutline.OutlineWidth = 3;
            BoxOutline.OutlineColor = new Color32(0, 120, 28, 255);  // Dark green.
        }
    }

    public override void OnStartServer()
    {
        Outline BoxOutline = GetComponent<Outline>();
        if (IsAmmoBox)
        {
            GunClass[] gunTypes = Enum.GetValues(typeof(GunClass)).Cast<GunClass>().ToArray();

            // Randomly assign an associated ammo type for this ammo box.
            AssociatedGunClass = gunTypes[UnityEngine.Random.Range(0, gunTypes.Length)];

            NumberBullets = ammoPickupAmounts[AssociatedGunClass];
            BoxOutline.enabled = false;
            BoxOutline.OutlineWidth = 3;
            BoxOutline.OutlineColor = OutlineColors[AssociatedGunClass];
            BoxOutline.enabled = true;
        }
        else
        {
            NumberBullets = 25; // In this case, NumberBullets functions has the health restored on pickup.
            BoxOutline.enabled = false;
            BoxOutline.OutlineWidth = 3;
            BoxOutline.OutlineColor = new Color32(0, 120, 28, 255);  // Dark green.
            BoxOutline.enabled = true;
        }
    }
}
