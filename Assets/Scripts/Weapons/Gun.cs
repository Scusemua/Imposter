using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

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

    public enum WeaponType
    {
        Projectile,
        Beam,
        Raycast
    }

    public enum FiringMode
    {
        Burst,
        SemiAutomatic,
        Automatic
    }

    [Header("Weapon Statistics")]
    public int Id;                  // Unique identifier of the weapon.
    public GunClass GunClass;       // Defines maximum ammo and other properties.
    public GunType _GunType;        // We can only pick up a certian number of each type of weapon (primary, secondary, explosive, etc.).
    public int ClipSize;            // Number of times that the player can shoot before needing to reload.
    public float WeaponCooldown;    // Firerate.
    public float ReloadTime;        // How long it takes to reload.
    public float ScreenShakeAmount; // How much shooting the weapon shakes the screen.
    public int ProjectileCount;     // How many projectiles are created?
    public int AmmoPerShot = 1;     // How much ammo is consumed per shot?
    public bool UsesHitscan;        // Uses hitscan for hit detection (as opposed to projectiles).
    public float Damage;            // How much damage weapon does.
    public string Name;             // Name of the weapon.
    public float SwapTime;          // How long it takes to put this gun away or take it out.
    [SyncVar(hook = nameof(OnGroundStatusChanged))] public bool OnGround;
    [SyncVar] public int AmmoInClip;          // How much ammo is in the clip of this gun.
    [SyncVar] public bool Reloading;          // Currently reloading?
    public WeaponType _WeaponType = WeaponType.Raycast;
    public FiringMode _FiringMode = FiringMode.Automatic;

    public GameObject ProjectilePrefab;

    /// <summary>
    /// The player who is holding this weapon.
    /// </summary>
    [HideInInspector] public PlayerController HoldingPlayer;

    [Header("Accuracy")]
    [Tooltip("How accurate this weapon is on a scale of 0 to 100")]
    public float Accuracy = 80.0f;
    private float CurrentAccuracy;                      // Holds the current accuracy.  Used for varying accuracy based on speed, etc.
    [Tooltip("How much the accuracy will decrease on each shot")]
    public float AccuracyDropPerShot = 1.0f;
    [Tooltip("How quickly the accuracy recovers after each shot (value between 0 and 1)")]
    public float AccuracyRecoverRate = 0.1f;

    [Header("Speed Modifiers")]
    [Tooltip("If this is true, then you can specify an explicit speed modifier for the weapon. Otherwise it defaults to its class speed modifier.")]
    public bool UseCustomSpeedModifier;
    [Tooltip("Changing this value will not have an effect unless 'UseCustomSpeedModifier' is toggled (i.e., set to true).")]
    public float SpeedModifier;
    
    [Header("Weapon Audio")]
    public AudioClip ShootSound;    // Sound that gets played when shooting.
    public AudioClip ReloadSound;   // Sound that gets played when reloading.
    public AudioClip PickupSound;   // Sound that gets played when the player picks up the weapon.
    public AudioClip DryfireSound;  // Sound that plays when the player tries to shoot but is out of ammo.

    public event Action<float> OnReloadStarted;
    public event Action OnReloadCompleted;

    private float curCooldown;

    #region Client Functions 

    public void OnGroundStatusChanged(bool _, bool _New)
    {
        if (_New)
            HoldingPlayer = null;
    }

    /// <summary>
    /// Check if the player is shooting, depending on the firing mode.
    /// </summary>
    [Client]
    public bool DoShootTest()
    {
        if (_FiringMode == FiringMode.Automatic)
        {
            return Input.GetMouseButton(0);
        }
        else if (_FiringMode == FiringMode.Burst)
        {
            return Input.GetMouseButtonDown(0);
        }
        else if (_FiringMode == FiringMode.SemiAutomatic)
        {
            return Input.GetMouseButtonDown(0);
        }
        else
        {
            Debug.LogError("Unknown firing mode: " + _FiringMode.ToString());
            return false;
        }
    }

    #endregion

    #region Server Functions 
    
    [Server]
    public void InitReload()
    {
        if (Reloading) return;
        curCooldown = ReloadTime;
        StartCoroutine(DoReload());
    }

    [Server]
    IEnumerator DoReload()
    {
        HoldingPlayer.TargetPlayReloadSound();
        yield return new WaitForSeconds(ReloadTime);

        // Make sure we're being held for this part.
        if (HoldingPlayer == null)
        {
            Reloading = false;
            OnReloadCompleted?.Invoke();
            yield return null;
        }
        else
        {

            // Determine how much ammo the player is missing.
            // If we have enough ammo in reserve to top off the clip/magazine,
            // then do so. Otherwise, put back however much ammo we have left.
            int ammoMissing = ClipSize - AmmoInClip;

            // Remove from our reserve ammo whatever we loaded into the weapon.
            if (HoldingPlayer.AmmoCounts[GunClass] - ammoMissing >= 0)
            {
                AmmoInClip += ammoMissing;
                HoldingPlayer.AmmoCounts[GunClass] -= ammoMissing;
            }
            else
            {
                // We do not have enough ammo to completely top off the magazine.
                // Just add what we have.
                AmmoInClip += HoldingPlayer.AmmoCounts[GunClass];
                HoldingPlayer.AmmoCounts[GunClass] = 0;
            }

            Reloading = false;
            OnReloadCompleted?.Invoke();

            yield return null;
        }
    }

    [Server]
    public void Shoot(PlayerController shooter, Transform init)
    {
        if (curCooldown > 0) return;

        if (AmmoInClip <= 0)
        {
            HoldingPlayer.TargetPlayDryFire();
            return;
        }

        if (_WeaponType == WeaponType.Raycast)
            RaycastShoot(shooter, init);
        else if (_WeaponType == WeaponType.Projectile)
            ProjectileShoot(shooter, init);

        curCooldown = WeaponCooldown;
        AmmoInClip -= AmmoPerShot;
    }

    [Server]
    private void RaycastShoot(PlayerController shooter, Transform init)
    {
        Ray ray = new Ray(init.position, init.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f))
        {
            if (hit.collider.CompareTag("Player"))
            {
                shooter.RpcPlayerFiredEntity(shooter.GetComponent<NetworkIdentity>().netId, Id);
                if (hit.collider.GetComponent<NetworkIdentity>().netId != GetComponent<NetworkIdentity>().netId)
                    hit.collider.GetComponent<Player>().Damage(Damage);
            }
            else if (hit.collider.CompareTag("Enemy"))
                shooter.RpcPlayerFiredEntity(shooter.GetComponent<NetworkIdentity>().netId, Id);
            else
                shooter.RpcPlayerFired(shooter.GetComponent<NetworkIdentity>().netId, Id, hit.point, hit.normal);
        }
    }

    [Server]
    private void ProjectileShoot(PlayerController shooter, Transform init)
    {
        for (int i = 0; i < ProjectileCount; i++)
        {
            GameObject projectileGameObject = Instantiate(ProjectilePrefab, init.position + (init.forward * 3), init.rotation);
            NetworkServer.Spawn(projectileGameObject, shooter.Player.gameObject);

            shooter.RpcPlayerFiredProjectile(shooter.GetComponent<NetworkIdentity>().netId, Id);
        }
    }

    #endregion 

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

    void Update()
    {
        if (OnGround) return;

        CurrentAccuracy = Mathf.Lerp(CurrentAccuracy, Accuracy, AccuracyRecoverRate * Time.deltaTime);
        curCooldown -= Time.deltaTime;
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
