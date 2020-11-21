using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Gun : NetworkBehaviour
{
    #region Static Constants
    public static int MAX_PISTOL_AMMO = 175;
    public static int MAX_ASSAULT_RIFLE_AMMO = 360;
    public static int MAX_SHOTGUN_AMMO = 100;
    public static int MAX_RIFLE_AMMO = 125;
    public static int MAX_SUBMACHINE_GUN_AMMO = 450;
    public static int MAX_LMG_AMMO = 1500;
    public static int MAX_EXPLOSIVE_AMMO = 30;

    public static Dictionary<GunClass, int> AmmoMaxCounts = new Dictionary<GunClass, int>
    {
        [GunClass.ASSAULT_RIFLE] = MAX_ASSAULT_RIFLE_AMMO,
        [GunClass.SHOTGUN] = MAX_SHOTGUN_AMMO,
        [GunClass.SUBMACHINE_GUN] = MAX_SUBMACHINE_GUN_AMMO,
        [GunClass.RIFLE] = MAX_RIFLE_AMMO,
        [GunClass.PISTOL] = MAX_PISTOL_AMMO,
        [GunClass.LIGHT_MACHINE_GUN] = MAX_LMG_AMMO,
        [GunClass.EXPLOSIVE] = MAX_EXPLOSIVE_AMMO
    };
    #endregion 

    public enum GunType
    {
        PRIMARY,
        SECONDARY,
        EXPLOSIVE
    }

    [Header("Weapon Statistics")]
    public int Id;                  // Unique identifier of the weapon.
    public GunClass GunClass;       // Defines maximum ammo and other properties.
    public GunType _GunType;        // We can only pick up a certian number of each type of weapon (primary, secondary, explosive, etc.).
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
    public bool OnGround;
    public int AmmoInClip;          // How much ammo is in the clip of this gun.

    [Tooltip("If this is true, then you can specify an explicit speed modifier for the weapon. Otherwise it defaults to its class speed modifier.")]
    public bool UseCustomSpeedModifier;
    [Tooltip("Changing this value will not have an effect unless 'UseCustomSpeedModifier' is toggled (i.e., set to true).")]
    public float SpeedModifier;
    
    [Header("Weapon Audio")]
    public AudioClip ShootSound;    // Sound that gets played when shooting.
    public AudioClip ReloadSound;   // Sound that gets played when reloading.
    public AudioClip PickupSound;   // Sound that gets played when the player picks up the weapon.

    void Alive()
    {
        // If this weapon isn't configured to use a particular speed modifier, then use the default class speed modifier.
        if (!UseCustomSpeedModifier)
            SpeedModifier = GameOptions.GunClassSpeedModifiers[GunClass];

        AmmoInClip = ClipSize; // This will be changed by the player if necessary.
    }

    void Awake()
    {
        if (!UseCustomSpeedModifier)
            SpeedModifier = GameOptions.GunClassSpeedModifiers[GunClass];

        AmmoInClip = ClipSize; // This will be changed by the player if necessary.
    }

    public override int GetHashCode()
    {
        int hash = 13;
        hash = (hash * 7) + Name.GetHashCode();
        return hash;
    }

    public override bool Equals(object other)
    {
        Gun _other = other as Gun;

        if (_other == null)
            return false;

        return this.Name.Equals(_other.Name);
    }
}
