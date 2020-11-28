using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public class Gun : UsableItem
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

    [Header("Prefabs")]
    public GameObject MuzzleFlashPrefab;
    [Tooltip("Where the bullets get instantiated.")]
    public Transform WeaponMuzzle;  // Where the bullets get instantiated.

    [Header("Weapon Classification")]
    public GunClass GunClass;       // Defines maximum ammo and other properties.
    public GunType _GunType;        // We can only pick up a certian number of each type of weapon (primary, secondary, explosive, etc.).
    public WeaponType _WeaponType = WeaponType.Raycast;
    public FiringMode _FiringMode = FiringMode.Automatic;

    [Header("Weapon Statistics")]
    [Tooltip("Number of times that the player can shoot before needing to reload.")]
    public int ClipSize;            // Number of times that the player can shoot before needing to reload.
    [Tooltip("Firerate.")]
    public float WeaponCooldown;    // Firerate.
    [Tooltip("How long it takes to reload.")]
    public float ReloadTime;        // How long it takes to reload.
    [Tooltip("How many projectiles are created?")]
    public int ProjectileCount;     // How many projectiles are created?
    [Tooltip("How much ammo is consumed per shot?")]
    public int AmmoPerShot = 1;     // How much ammo is consumed per shot?
    [Tooltip("How much the bullet pushes things when it shoots them.")]
    public float BulletForce = 5.0f;
    [Tooltip("Uses hitscan for hit detection (as opposed to projectiles).")]
    public bool UsesHitscan;        // Uses hitscan for hit detection (as opposed to projectiles).
    [Tooltip("How much damage weapon does.")]
    public float Damage;            // How much damage weapon does.
    [Tooltip("How long it takes to put this gun away or take it out.")]
    public float SwapTime;          // How long it takes to put this gun away or take it out.

    
    [Tooltip("How much ammo is in the clip of this gun.")] [HideInInspector]
    [SyncVar] public int AmmoInClip;          // How much ammo is in the clip of this gun.
    [Tooltip("Currently reloading?")] [HideInInspector]
    [SyncVar] public bool Reloading;          // Currently reloading?
    [Tooltip("This affects how commonly this weapon spawns around the map.")]
    public float Rarity = 1.0f;

    public GameObject ProjectilePrefab;

    [Header("Camera Shake")]
    [Tooltip("Give camera shaking effects to nearby cameras that have the vibration component")]
    public bool ShakeCamera = true;             // Should this explosion shake the camera?
    [Tooltip("Affects the smoothness and speed of the shake. Lower roughness values are slower and smoother. Higher values are faster and noisier.")]
    public float ShakeRoughness = 0.1f;         // Lower roughness values are slower and smoother. Higher values are faster and noisier.
    [Tooltip("The intensity / magnitude of the shake.")]
    public float ShakeMagnitude = 0.1f;         // The intensity / magnitude of the shake.
    [Tooltip("The time, in seconds, for the shake to fade in.")]
    public float ShakeFadeIn = 0.05f;           // The time, in seconds, for the shake to fade in.
    [Tooltip("The time, in seconds, for the shake to fade out.")]
    public float ShakeFadeOut = 0.2f;           // The time, in seconds, for the shake to fade out.

    [Header("Accuracy")]
    [Tooltip("How accurate this weapon is on a scale of 0 to 100")]
    public float Accuracy = 80.0f;
    private float CurrentAccuracy;                      // Holds the current accuracy.  Used for varying accuracy based on speed, etc.
    [Tooltip("How much the accuracy will decrease on each shot")]
    public float AccuracyDropPerShot = 1.0f;
    [Tooltip("How quickly the accuracy recovers after each shot (value between 0 and 1)")]
    public float AccuracyRecoverRate = 0.1f;
    
    [Header("Weapon Audio")]
    public AudioClip ShootSound;    // Sound that gets played when shooting.
    public AudioClip ReloadSound;   // Sound that gets played when reloading.
    public AudioClip PickupSound;   // Sound that gets played when the player picks up the weapon.
    public AudioClip DryfireSound;  // Sound that plays when the player tries to shoot but is out of ammo.

    public event Action<float> OnReloadStarted;
    public event Action OnReloadCompleted;

    private float curCooldown = 0f;
    private float dryFireCooldown = 0f; // Prevents us from spamming the dry fire sound on automatic weapons.
    private float dryFireInterval = 0.3f; // How often we can play the dry fire sound effect.

    #region Client Functions 

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

    #region Client Functions 

    [Client]
    public void MuzzleFlash()
    {
        // Random rotation.
        Instantiate(MuzzleFlashPrefab, WeaponMuzzle.position, Quaternion.Euler(UnityEngine.Random.Range(0, 360), transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z), WeaponMuzzle);
    }

    #endregion 

    #region Server Functions 

    [Server]
    public void InitReload()
    {
        if (Reloading)
        {
            Debug.Log("Already reloading. Returning.");
            return;
        }
        Debug.Log("Starting reload coroutine.");
        curCooldown = ReloadTime;
        StartCoroutine(DoReload());
    }

    [Server]
    IEnumerator DoReload()
    {
        Reloading = true;
        HoldingPlayer.TargetShowReloadBar(ReloadTime);
        HoldingPlayer.TargetPlayReloadSound();
        yield return new WaitForSeconds(ReloadTime);

        // Make sure we're being held for this part.
        if (HoldingPlayer == null)
        {
            Debug.Log("Player dropped us during reload.");
            Reloading = false;
            OnReloadCompleted?.Invoke();
            yield return null;
        }
        else
        {
            Debug.Log("Reload completed. Updating ammo counts now.");
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
            HoldingPlayer.TargetReload();
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
            // If it's been long enough that we can play the dry fire sound effect again, then do so.
            if (dryFireCooldown <= 0)
            {
                HoldingPlayer.TargetPlayDryFire();
                dryFireCooldown = dryFireInterval;
            }
            return;
        }

        shooter.RpcPlayerShotGun(shooter.GetComponent<NetworkIdentity>().netId, Id);
        if (_WeaponType == WeaponType.Raycast)
            RaycastShoot(shooter, init);
        else if (_WeaponType == WeaponType.Projectile)
            ProjectileShoot(shooter, init);

        HoldingPlayer.TargetShoot();
        //HoldingPlayer.Player.TargetDoCameraShake(ShakeMagnitude, ShakeRoughness, ShakeFadeIn, ShakeFadeOut);
        HoldingPlayer.Player.TargetDoCameraShake(false);

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
            if (hit.rigidbody != null)
            {
                // Calculate Angle Between the collision point and the player
                Vector3 dir = hit.point - shooter.transform.position;

                // We then get the opposite (-Vector3) and normalize it
                dir = -dir.normalized;

                // And finally we add force in the direction of dir and multiply it by force. 
                hit.rigidbody.AddForce(dir * BulletForce);
            }

            // Did we hit another player?
            if (hit.collider.CompareTag("Player") && hit.collider.GetComponent<NetworkIdentity>().netId != GetComponent<NetworkIdentity>().netId)
            {
                hit.collider.GetComponent<Player>().Damage(
                    Damage, 
                    HoldingPlayer.Player.netId, 
                    DamageSource.Bullet,
                    hit.point,
                    BulletForce,
                    0f);
                shooter.RpcGunshotHitEntity(hit.point, hit.normal);
            }
            // Did we hit an enemy? (Used for debugging)
            else if (hit.collider.CompareTag("Enemy"))
                shooter.RpcGunshotHitEntity(hit.point, hit.normal);
            else // We just hit the environment.
                shooter.RpcGunshotHitEnvironment(shooter.GetComponent<NetworkIdentity>().netId, Id, hit.point, hit.normal);
        }
    }

    [Server]
    private void ProjectileShoot(PlayerController shooter, Transform init)
    {
        for (int i = 0; i < ProjectileCount; i++)
        {
            // Slightly randomize the position for the projectiles.
            Vector3 pos = new Vector3(init.position.x + UnityEngine.Random.Range(0.0f, 0.25f),
                                      init.position.y + UnityEngine.Random.Range(0.0f, 0.25f),
                                      init.position.z + UnityEngine.Random.Range(0.0f, 0.25f));
            GameObject projectileGameObject = Instantiate(ProjectilePrefab, pos + (init.forward * 3), init.rotation);
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
        dryFireCooldown -= Time.deltaTime;
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
