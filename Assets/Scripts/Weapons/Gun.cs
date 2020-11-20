using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    #region Static Constants
    public static int MAX_PISTOL_AMMO = 120;
    public static int MAX_ASSAULT_RIFLE_AMMO = 180;
    public static int MAX_SHOTGUN_AMMO = 75;
    public static int MAX_RIFLE_AMMO = 90;
    public static int MAX_SUBMACHINE_GUN_AMMO = 300;
    public static int MAX_LMG_AMMO = 500;
    public static int MAX_EXPLOSIVE_AMMO = 30;
    #endregion 

    [Header("Weapon Statistics")]
    public GunType GunType;         // Defines maximum ammo.
    public int ClipSize;            // Number of times that the player can shoot before needing to reload.
    public float WeaponCooldown;    // Firerate.
    public float ReloadTime;        // How long it takes to reload.
    public float Accuracy;          // How accurate the weapon is.
    public float ScreenShakeAmount; // How much shooting the weapon shakes the screen.
    public int ProjectileCount;     // How many projectiles are created?
    public bool UsesHitscan;        // Uses hitscan for hit detection (as opposed to projectiles).
    public float Damage;            // How much damage weapon does.
    public string Name;             // Name of the weapon.
    public float SwapTime;          // How long it takes to put this gun away or take it out.
    
    [Header("Weapon Audio")]
    public AudioClip ShootSound;    // Sound that gets played when shooting.
    public AudioClip ReloadSound;   // Sound that gets played when reloading.
}
