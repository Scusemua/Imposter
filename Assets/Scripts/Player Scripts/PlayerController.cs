using UnityEngine;
using Mirror;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System;

public class PlayerController : NetworkBehaviour
{
    public Player Player;
    private Rigidbody rigidbody;
    private PlayerInventory inventory;

    [Header("Audio")]
    public AudioClip DeathSound;
    public AudioClip ImposterStart;
    public AudioClip CrewmateStart;
    public AudioClip ImpactSound;
    public AudioClip DefaultGunshotSound;
    public AudioClip DefaultReloadSound;
    public AudioClip DefaultDryfireSound;
    public AudioClip InvalidAction;
    public AudioClip PickupAmmoSound;
    public AudioClip PickupHealthSound;
    public AudioClip PickupWeaponSound;
    public AudioSource AudioSource;

    [Header("Visual")]
    public GameObject DeathEffectPrefab;
    public GameObject BloodPoolPrefab;
    public GameObject CameraPrefab;
    public Camera Camera;
    public Animator Animator;

    [Header("Game-Related")]
    public GameObject EmergencyButton;
    public bool MovementEnabled;

    [Header("Weapon")]
    [SerializeField] Transform weaponContainer; // This is where the weapon goes.
    [SerializeField] GameObject bulletHolePrefab;
    [SerializeField] GameObject bulletFXPrefab;
    [SerializeField] GameObject bulletBloodFXPrefab;
    public int StartingWeaponId = -1;

    [SyncVar] public int CurrentWeaponID = -1;
    public Gun CurrentWeapon;
    
    public Dictionary<GunClass, int> AmmoCounts = new Dictionary<GunClass, int>
    {
        [GunClass.ASSAULT_RIFLE] = 100,
        [GunClass.SHOTGUN] = 100,
        [GunClass.SUBMACHINE_GUN] = 100,
        [GunClass.RIFLE] = 100,
        [GunClass.PISTOL] = 100,
        [GunClass.LIGHT_MACHINE_GUN] = 100,
        [GunClass.EXPLOSIVE] = 100
    };

    /// <summary>
    /// The player's primary weapons.
    /// </summary>
    //[SerializeField] private SyncList<InventoryGun> PrimaryInventory = new SyncList<InventoryGun>();

    /// <summary>
    /// The player's secondary weapons.
    /// </summary>
    //[SerializeField] private SyncList<InventoryGun> SecondaryInventory = new SyncList<InventoryGun>();

    /// <summary>
    /// The player's melee weapons.
    /// </summary>
    //[SerializeField] private SyncList<InventoryGun> MeleeInventory = new SyncList<InventoryGun>();

    /// <summary>
    /// The player's explosive weapons.
    /// </summary>
    //[SerializeField] private SyncList<InventoryGun> ExplosiveInventory = new SyncList<InventoryGun>();

    /// <summary>
    /// The player's grenades.
    /// </summary>
    //[SerializeField] private SyncList<InventoryGun> GrenadeInventory = new SyncList<InventoryGun>();

    private ItemDatabase itemDatabase;
    
    private LineRenderer lineRenderer;

    private float movementSpeed;
    private float runBoost;
    private bool sprintEnabled;

    /// <summary>
    /// Animation
    /// </summary>
    private float lastX;
    private float lastY;

    // Reference to the reload coroutine so we can cancel the reload when necessary.
    private Coroutine reloadCoroutine;

    private GameOptions GameOptions { get => GameOptions.singleton; }

    public Vector3 CameraOffset;

    /// <summary>
    /// Displayed around a deadbody that has not yet been identified.
    /// </summary>
    public Outline PlayerOutline;

    [SyncVar(hook = nameof(OnPlayerBodyIdentified))]
    public bool Identified;

    void Update()
    {
        if (!isLocalPlayer) return;

        if (CurrentWeapon != null && CurrentWeapon.DoShootTest())
            ShootWeapon();

        if (Input.GetKeyDown(KeyCode.R))
            ReloadButton();
        else if (Input.GetKeyDown(KeyCode.V))
            DropButton();
        else if (Input.GetKeyDown(KeyCode.H))
            CmdTakeDamage(10.0f);
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Dictionary<Gun.GunType, IEnumerable<string>> gunNameLists = inventory.GetGunsNamesOrganized();
            Player.PlayerUI.ShowWeaponUI(gunNameLists[Gun.GunType.PRIMARY], 
                gunNameLists[Gun.GunType.SECONDARY], gunNameLists[Gun.GunType.EXPLOSIVE]);
            CmdTryCycleInventory(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Dictionary<Gun.GunType, IEnumerable<string>> gunNameLists = inventory.GetGunsNamesOrganized();
            Player.PlayerUI.ShowWeaponUI(gunNameLists[Gun.GunType.PRIMARY], 
                gunNameLists[Gun.GunType.SECONDARY], gunNameLists[Gun.GunType.EXPLOSIVE]);
            CmdTryCycleInventory(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Dictionary<Gun.GunType, IEnumerable<string>> gunNameLists = inventory.GetGunsNamesOrganized();
            Player.PlayerUI.ShowWeaponUI(gunNameLists[Gun.GunType.PRIMARY], 
                gunNameLists[Gun.GunType.SECONDARY], gunNameLists[Gun.GunType.EXPLOSIVE]);
            CmdTryCycleInventory(2);
        }
    }

    internal void ShootWeapon()
    {
        if (!Player.IsDead && CurrentWeaponID >= 0 && CurrentWeapon != null)
            CmdTryShoot();
    }

    void LateUpdate()
    {
        if (!isLocalPlayer) return;

        if (Camera != null && Camera.enabled)
        {
            Camera.transform.position = transform.position + CameraOffset;
        }
    }

    #region Client RPC
    [ClientRpc]
    public void RpcAssignCurrentWeapon(int weaponId)
    {
        if (isLocalPlayer)
        {
            if (weaponId >= 0)
                Player.PlayerUI.AmmoUI.SetActive(true);
            else
                Player.PlayerUI.AmmoUI.SetActive(false);
        }
    }

    [ClientRpc]
    public void RpcPlayerFiredProjectile(uint shooterID, int gunId)
    {
        Transform shooterTransform = NetworkIdentity.spawned[shooterID].GetComponent<Player>().GetComponent<Transform>();

        float volumeDistModifier = (1000f - GetDistanceSquaredToTarget(shooterTransform)) / 1000f;
        //Debug.Log("Playing gunshot with volume modifier: " + volumeDistModifier);

        AudioClip gunshotSound = DefaultGunshotSound;
        if (itemDatabase.GetGunByID(gunId).ShootSound != null)
            gunshotSound = itemDatabase.GetGunByID(gunId).ShootSound;

        // Adjust volume of gunshot based on distance.
        this.AudioSource.PlayOneShot(gunshotSound, volumeDistModifier);
    }

    [ClientRpc]
    public void RpcPlayerFiredEntity(uint shooterID, int gunId)
    {
        //Instantiate(bulletHolePrefab, impactPos + impactRot * 0.1f, Quaternion.LookRotation(impactRot), NetworkIdentity.spawned[targetID].transform);
        //Instantiate(bulletBloodFXPrefab, impactPos, Quaternion.LookRotation(impactRot));
        NetworkIdentity.spawned[shooterID].GetComponent<Player>().MuzzleFlash();

        Transform shooterTransform = NetworkIdentity.spawned[shooterID].GetComponent<Player>().GetComponent<Transform>();

        float volumeDistModifier = (1000f - GetDistanceSquaredToTarget(shooterTransform)) / 1000f;
        //Debug.Log("Playing gunshot with volume modifier: " + volumeDistModifier);

        AudioClip gunshotSound = DefaultGunshotSound;
        if (itemDatabase.GetGunByID(gunId).ShootSound != null)
            gunshotSound = itemDatabase.GetGunByID(gunId).ShootSound;

        // Adjust volume of gunshot based on distance.
        this.AudioSource.PlayOneShot(gunshotSound, volumeDistModifier);

        //Destroy(bulletBloodGO, 2);
        //Destroy(bulletHolePrefab, 10);
    }

    [ClientRpc]
    public void RpcPlayerFired(uint shooterID, int gunId, Vector3 impactPos, Vector3 impactRot)
    {
        //Instantiate(bulletHolePrefab, impactPos + impactRot * 0.1f, Quaternion.LookRotation(impactRot));
        Instantiate(bulletFXPrefab, impactPos, Quaternion.LookRotation(impactRot));
        NetworkIdentity.spawned[shooterID].GetComponent<Player>().MuzzleFlash();

        Transform shooterTransform = NetworkIdentity.spawned[shooterID].GetComponent<Player>().GetComponent<Transform>();

        float volumeDistModifier = (1000f - GetDistanceSquaredToTarget(shooterTransform)) / 1000f;
        //Debug.Log("Playing gunshot with volume modifier: " + volumeDistModifier);

        AudioClip gunshotSound = DefaultGunshotSound;
        if (itemDatabase.GetGunByID(gunId).ShootSound != null)
            gunshotSound = itemDatabase.GetGunByID(gunId).ShootSound;

        // Adjust volume of gunshot based on distance.
        this.AudioSource.PlayOneShot(gunshotSound, volumeDistModifier);

        //Destroy(bulletFxPrefab, 2);
        //Destroy(bulletHolePrefab, 10);
    }

    #endregion

    #region Target RPC
    [TargetRpc]
    public void TargetPlayPickupAmmoSound()
    {
        AudioSource.PlayOneShot(PickupAmmoSound);
    }

    [TargetRpc]
    public void TargetPlayReloadSound()
    {
        if (CurrentWeapon != null)
            AudioSource.PlayOneShot(CurrentWeapon.ReloadSound);
    }

    [TargetRpc]
    public void TargetPlayDryFire()
    {
        AudioClip dryfireSound = DefaultDryfireSound;
        if (CurrentWeapon != null && CurrentWeapon.DryfireSound != null)
            dryfireSound = CurrentWeapon.DryfireSound;

        AudioSource.PlayOneShot(dryfireSound);
    }

    [TargetRpc]
    public void TargetPlayPickupHealthSound()
    {
        AudioSource.PlayOneShot(PickupHealthSound);
    }

    [TargetRpc]
    public void TargetPlayPickupWeaponSound()
    {
        AudioSource.PlayOneShot(PickupWeaponSound);
    }

    [TargetRpc]
    public void TargetUpdateAmmoCounts()
    {
        UpdateAmmoDisplay();
    }

    [TargetRpc]
    void TargetShoot()
    {
        // Update the ammo count on the player's screen.
        Player.PlayerUI.AmmoClipText.text = CurrentWeapon.AmmoInClip.ToString();
    }

    [TargetRpc]
    void ShowReloadBar(float reloadTime)
    {
        Player.PlayerUI.ReloadingProgressBar.health = 0;
        Player.PlayerUI.ReloadingProgressBar.healthPerSecond = 100f / reloadTime;
        Player.PlayerUI.ReloadingProgressBar.gameObject.SetActive(true);
    }

    [TargetRpc]
    void TargetReload()
    {
        // We reloaded successfully. Update our UI.
        Player.PlayerUI.AmmoClipText.text = (CurrentWeapon != null) ? CurrentWeapon.AmmoInClip.ToString() : "N/A";
        Player.PlayerUI.AmmoReserveText.text = (CurrentWeapon != null) ? AmmoCounts[CurrentWeapon.GunClass].ToString() : "N/A";
        Player.PlayerUI.ReloadingProgressBar.health = 100.0f; // Max out the reload bar, then turn it off.
        Player.PlayerUI.ReloadingProgressBar.gameObject.SetActive(false);
    }

    #endregion

    #region Commands 

    [Command]
    public void CmdTakeDamage(float damage)
    {
        Player.Damage(damage);
    }

    [Command]
    public void CmdPickupWeapon(GameObject weaponGameObject)
    {
        Gun gun = weaponGameObject.GetComponent<Gun>();

        if (!gun.OnGround)
            return;

        //Debug.Log("Weapon on ground has " + gun.AmmoInClip + " bullets in its clip.");

        bool added = inventory.AddWeaponToInventory(gun.Id, weaponGameObject);

        if (added)
        {
            ModifyWeaponCollidersAndRigidbodyOnPickup(weaponGameObject);

            // Pick it up off the ground. Set the parent to our weapon container and update the gun's position and rotation.
            weaponGameObject.transform.SetParent(weaponContainer.transform);
            weaponGameObject.transform.SetPositionAndRotation(weaponContainer.position, weaponContainer.transform.rotation);
            gun.OnGround = false;
            gun.HoldingPlayer = this;
            TargetPlayPickupWeaponSound();

            weaponGameObject.SetActive(false);
        }
    }

    [Command]
    public void CmdTryCycleInventory(int inventoryId)
    {
        int nextWeaponIndex;

        if (inventoryId == 0)
            nextWeaponIndex = inventory.GetNextWeaponOfType(Gun.GunType.PRIMARY, CurrentWeaponID);
        else if (inventoryId == 1)
            nextWeaponIndex = inventory.GetNextWeaponOfType(Gun.GunType.SECONDARY, CurrentWeaponID);
        else if (inventoryId == 2)
            nextWeaponIndex = inventory.GetNextWeaponOfType(Gun.GunType.EXPLOSIVE, CurrentWeaponID);
        else
        {
            Debug.LogError("Unknown inventory ID: " + inventoryId);
            return;
        }

        Debug.Log("After cycling inventory, nextWeaponIndex = " + nextWeaponIndex + ".");

        // Pass the unique ID of the gun stored at the identified index of the player's inventory.
        EquipWeapon(inventory.GetGunIdAtIndex(nextWeaponIndex));
    }

    [Command]
    public void CmdPickupAmmoBox(GameObject ammoBoxGameObject)
    {
        AmmoBox ammoBox = ammoBoxGameObject.GetComponent<AmmoBox>();

        if (ammoBox.IsAmmoBox)
        {
            GunClass associatedGunType = ammoBox.AssociatedGunClass;
            Debug.Log("Ammo box is of type " + associatedGunType.ToString() + ". Current ammo: " + AmmoCounts[associatedGunType] + ", Max: " + Gun.AmmoMaxCounts[associatedGunType] + ".");
            if (AmmoCounts[associatedGunType] < Gun.AmmoMaxCounts[associatedGunType])
            {
                AmmoCounts[associatedGunType] = Mathf.Min(Gun.AmmoMaxCounts[associatedGunType], AmmoCounts[associatedGunType] + ammoBox.NumberBullets);
                NetworkServer.Destroy(ammoBoxGameObject);

                TargetUpdateAmmoCounts();
                TargetPlayPickupAmmoSound();
            }
        }
        else
        {
            Debug.Log("Current HP: " + Player.Health + ", Max Health: " + Player.HealthMax);
            if (Player.Health < Player.HealthMax)
            {
                Player.Health = Mathf.Min(Player.HealthMax, Player.Health + ammoBox.NumberBullets);
                Player.FloatingHealthBar.health = Player.Health;
                Player.FloatingHealthBar.UpdateHealth();
                Player.PlayerUI.UpdateHealth(Player.Health);
                NetworkServer.Destroy(ammoBoxGameObject);
                TargetPlayPickupHealthSound();
            }
        }
    }

    [Command]
    private void CmdTryReload()
    {
        if (CurrentWeaponID < 0 || CurrentWeapon == null || CurrentWeapon.AmmoInClip == CurrentWeapon.ClipSize)
            return;


        CurrentWeapon.InitReload();
    }

    [Command]
    private void CmdTryShoot()
    {
        // TODO: Play error sound indicating no weapon? Or just punch?
        if (CurrentWeaponID < 0)
            return;

        //Server side check
        //if ammoCount > 0 && isAlive
        if (!Player.IsDead)
        {
            TargetShoot();

            CurrentWeapon.Shoot(this, Player.WeaponMuzzle.transform);
            // TODO: Projectile count, accuracy, etc.
            //Ray ray = new Ray(Player.WeaponMuzzle.transform.position, Player.WeaponMuzzle.transform.forward);
            //RaycastHit hit;
            //if (Physics.Raycast(ray, out hit, 100f))
            //{
            //    if (hit.collider.CompareTag("Player"))
            //    {
            //        RpcPlayerFiredEntity(GetComponent<NetworkIdentity>().netId, hit.collider.GetComponent<NetworkIdentity>().netId, CurrentWeaponID, hit.point, hit.normal);
            //        if (hit.collider.GetComponent<NetworkIdentity>().netId != GetComponent<NetworkIdentity>().netId)
            //            hit.collider.GetComponent<Player>().Damage(itemDatabase.GetGunByID(CurrentWeaponID).Damage);
            //    }
            //    else if (hit.collider.CompareTag("Enemy"))
            //        RpcPlayerFiredEntity(GetComponent<NetworkIdentity>().netId, hit.collider.GetComponent<NetworkIdentity>().netId, CurrentWeaponID, hit.point, hit.normal);
            //    else
            //        RpcPlayerFired(GetComponent<NetworkIdentity>().netId, itemDatabase.GetGunByID(CurrentWeaponID).Id, hit.point, hit.normal);
            //}
        }
    }

    [Command]
    public void CmdTryDropCurrentWeapon()
    {
        if (CurrentWeaponID < 0)
            return;

        // Remove the gun from the player's inventory.
        inventory.RemoveWeaponFromInventory(CurrentWeaponID);

        // Instantiate the scene object on the server a bit in front of the player so they don't instantly pick it up.
        Vector3 pos = transform.position + (transform.forward * 2.0f) ;
        Quaternion rot = weaponContainer.transform.rotation;

        GameObject currentWeaponGameObject = CurrentWeapon.gameObject;
        currentWeaponGameObject.transform.SetParent(null);
        currentWeaponGameObject.transform.SetPositionAndRotation(pos, rot);

        // Set the RigidBody as non-kinematic on the server only (isKinematic = true in prefab).
        Array.ForEach(currentWeaponGameObject.GetComponents<Collider>(), c => c.enabled = true); // Disable the colliders.
        currentWeaponGameObject.GetComponent<Rigidbody>().isKinematic = false;
        currentWeaponGameObject.GetComponent<Rigidbody>().detectCollisions = true;

        CurrentWeapon.HoldingPlayer = null;
        CurrentWeapon.OnGround = true;

        // Toss it out in front of us a bit.
        currentWeaponGameObject.GetComponent<Rigidbody>().velocity = transform.forward * 7.0f + transform.up * 5.0f;

        // Set the player's SyncVar to nothing so clients will destroy the equipped child item.
        CurrentWeaponID = -1;
        CancelReload();
        CurrentWeapon = null;
    }

    #endregion

    #region Client 

    [Client]
    public void OnDropWeaponButtonPressed()
    {
        if (CurrentWeapon == null || CurrentWeaponID < 0)
            // TODO: Play error sound.
            return;

    }

    //[Client]
    //public void OnCurrentWeaponIdChanged(int _Old, int _New)
    //{
    //    Debug.Log("Current weapon ID changed. Old value: " + _Old + ", New value: " + _New);
    //    if (CurrentWeapon != null)
    //    {
    //        CurrentWeapon.OnReloadCompleted -= TargetReload;
    //        CurrentWeapon.OnReloadStarted -= ShowReloadBar;
    //        Destroy(CurrentWeapon.gameObject);
    //        CurrentWeapon = null;
    //    }

    //    // The player could've put away all their weapons, meaning the new ID would be -1.
    //    if (_New >= 0)
    //    {
    //        AssignWeaponClientSide(_New);

    //        if (isLocalPlayer) Player.PlayerUI.AmmoUI.SetActive(true);
    //    }
    //    else
    //    {
    //        if (isLocalPlayer) Player.PlayerUI.AmmoUI.SetActive(false);
    //    }
    //}

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("AmmoBox"))
        {
            CmdPickupAmmoBox(other.gameObject);
        }   
        else if (other.gameObject.CompareTag("Weapon"))
        {
            CmdPickupWeapon(other.gameObject);
        }
    }

    public override void OnStartLocalPlayer()
    {
        enabled = true;
        MovementEnabled = true;
        GetComponent<Rigidbody>().isKinematic = false;

        movementSpeed = GameOptions.PlayerSpeed;
        runBoost = GameOptions.SprintBoost;
        sprintEnabled = GameOptions.SprintEnabled;

        GameObject cameraObject = Instantiate(CameraPrefab);
        AudioSource = GetComponent<AudioSource>();
        AudioSource.enabled = true;
        AudioSource.volume = 1.0f;
        Camera = cameraObject.GetComponent<Camera>();

        cameraObject.GetComponent<AudioListener>().enabled = true;
        Camera.enabled = true;

        Camera.transform.position += transform.position;

        EmergencyButton = GameObject.FindGameObjectWithTag("EmergencyButton");

        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.025f;
        lineRenderer.endWidth = 0.025f;
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.positionCount = 2;

        inventory = GetComponent<PlayerInventory>();

        Material whiteDiffuseMat = new Material(Shader.Find("Unlit/Texture"))
        {
            color = Color.red
        };
        lineRenderer.material = whiteDiffuseMat;

        if (itemDatabase == null)
            itemDatabase = GameObject.FindGameObjectWithTag("ItemDatabase").GetComponent<ItemDatabase>();
    }

    /// <summary>
    /// Called when a player's dead body gets identified.
    /// </summary>
    public void OnPlayerBodyIdentified(bool _Old, bool _New)
    {
        // If the player's body has been identified, then disable the outline.
        if (_New)
        {
            Debug.Log(GetPlayerDebugString() + " has been identified.");
            PlayerOutline.enabled = false;
        }
    }

    public override void OnStartAuthority()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    [Client]
    internal void ReloadButton()
    {
        if (CurrentWeaponID >= 0 && CurrentWeapon != null && !Player.IsDead)
        {
            Debug.Log("Attempting to reload...");
            CmdTryReload();
        }
    }

    [Client]
    internal void DropButton()
    {
        if (CurrentWeapon != null && CurrentWeaponID >= 0 && !Player.IsDead)
            CmdTryDropCurrentWeapon();
    }

    /// <summary>
    /// Updates the ammo values.
    /// </summary>
    [Client]
    public void UpdateAmmoDisplay()
    {
        if (!isLocalPlayer) return;

        if (CurrentWeapon == null)
        {
            Player.PlayerUI.AmmoClipText.text = "N/A";
            Player.PlayerUI.AmmoReserveText.text = "N/A";
        }
        else
        {
            Player.PlayerUI.AmmoClipText.text = CurrentWeapon.AmmoInClip.ToString();
            Player.PlayerUI.AmmoReserveText.text = AmmoCounts[CurrentWeapon.GunClass].ToString();
        }
    }

    [Client]
    /// <summary>
    /// Play the Imposter start-of-game sound.
    /// </summary>
    public void PlayImposterStart()
    {
        Debug.Log("Playing Imposter start sound.");
        AudioSource.PlayOneShot(ImposterStart, 0.25f);
    }

    [Client]
    /// <summary>
    /// Play the Crewmate start-of-game sound.
    /// </summary>
    public void PlayCrewmateStart()
    {
        Debug.Log("Playing Crewmate start sound.");
        AudioSource.PlayOneShot(CrewmateStart, 0.25f);
    }

    [Client]
    /// <summary>
    /// Play the impact sound (generally played on-death and for the Imposter who killed the player).
    /// </summary>
    public void PlayImpactSound()
    {
        Debug.Log("Playing impact sound.");
        AudioSource.PlayOneShot(ImpactSound, 0.75f);
    }

    public override void OnStartClient()
    {
        setRigidbodyState(true);
        setColliderState(false);

        Player = GetComponent<Player>();

        if (Player == null)
            Debug.LogError("Player is null for PlayerController!");

        Identified = false;
        PlayerOutline.OutlineColor = Player.PlayerColor;

        if (!isLocalPlayer)
        {
            // Disable player movement.
            enabled = false;
        }
    }

    // Update is called once per frame
    [ClientCallback]
    void FixedUpdate()
    {
        if (!isLocalPlayer || Player.IsDead || !MovementEnabled) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(KeyCode.B) && !Player.IsDead)
        {
            Player.CmdSuicide();
        }

        Vector3 movement = new Vector3(h, 0, v);

        float weaponSpeedModifier = CurrentWeapon == null ? 1.0f : CurrentWeapon.SpeedModifier;

        if (sprintEnabled && Input.GetKey(KeyCode.LeftShift))
            movement = movement.normalized * movementSpeed * runBoost * weaponSpeedModifier * Time.deltaTime;
        else
            movement = movement.normalized * movementSpeed * weaponSpeedModifier * Time.deltaTime;

        if (Input.GetKey(KeyCode.P))
            PlayImpactSound();

        rigidbody.MovePosition(transform.position + movement);

        Ray cameraRay = Camera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float rayLength;

        if (groundPlane.Raycast(cameraRay, out rayLength))
        {
            Vector3 pointToLook = cameraRay.GetPoint(rayLength);
            //Debug.DrawLine(cameraRay.origin, pointToLook, Color.blue);

            Vector3 lookAt = new Vector3(pointToLook.x, transform.position.y, pointToLook.z);

            lineRenderer.SetPosition(0, Player.WeaponMuzzle.transform.position);
            lineRenderer.SetPosition(1, transform.position + transform.forward * 10);

            transform.LookAt(lookAt);
        }

        UpdateAnimation(movement.normalized);
    }

    [Client]
    public void UpdateAnimation(Vector3 dir)
    {
        if (dir.x == 0f && dir.y == 0f)
        {
            Animator.SetFloat("LastDirX", lastX);
            Animator.SetFloat("LastDirY", lastY);
            Animator.SetBool("Movement", false);
        }
        else
        {
            lastX = dir.x;
            lastY = dir.y;
            Animator.SetBool("Movement", true);
        }

        Animator.SetFloat("DirX", dir.x);
        Animator.SetFloat("DirY", dir.y);
    }

    [Client]
    public void Die()
    {
        Animator.enabled = false;

        setRigidbodyState(false);
        setColliderState(true);

        PlayerOutline.OutlineWidth = 8;
        PlayerOutline.OutlineColor = new Color32(245, 233, 66, 255);

        if (isLocalPlayer)
            AudioSource.PlayOneShot(ImpactSound);

        float _raycastDistance = 10f;
        Vector3 dir = new Vector3(0, -1, 0);
        Debug.DrawRay(Player.transform.position, dir * _raycastDistance, Color.green);

        int mask = 1 << 13;    // Ground on layer 10 in the inspector

        if (isLocalPlayer)
        {
            Vector3 start = new Vector3(Player.transform.position.x, Player.transform.position.y + 1, Player.transform.position.z);
            if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, _raycastDistance, mask))
            {
                GameObject bloodPool = Instantiate(BloodPoolPrefab);
                bloodPool.transform.position = new Vector3(hit.point.x, hit.point.y + 0.05f, hit.point.z);
            }

            GameObject deathEffect = Instantiate(DeathEffectPrefab);
            deathEffect.transform.position = Player.transform.position;
            Destroy(deathEffect, 1.25f);
        }
    }

    [Client]
    void setRigidbodyState(bool state)
    {
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rigidbody in rigidbodies)
        {
            rigidbody.isKinematic = state;
            rigidbody.detectCollisions = !state;
        }

        GetComponent<Rigidbody>().isKinematic = !state;
        GetComponent<Rigidbody>().detectCollisions = state;
    }

    [Client]
    void setColliderState(bool state)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider collider in colliders)
        {
            collider.enabled = state;
        }

        GetComponent<Collider>().enabled = !state;
    }

    #endregion

    #region Server

    public override void OnStartServer()
    {
        inventory = GetComponent<PlayerInventory>();
        if (StartingWeaponId >= 0)
            GivePlayerWeapon(StartingWeaponId, true);
    }

    /// <summary>
    /// Stop the reload coroutine. Useful if we drop or switch guns during a reload.
    /// </summary>
    [Server]
    private void CancelReload()
    {
        Debug.Log("Cancelling reload.");
        TargetReload();
    }

    /// <summary>
    /// Add the specified weapon to the player's inventory. If the 'equip' flag is set to true,
    /// then also equip the current weapon (put away whatever weapon the player may already be holding first).
    /// 
    /// This could also be used to force a player to equip a weapon in their inventory by specifiying a
    /// gun that they already have while also passing true for 'equip'.
    /// </summary>
    [Server]
    public void GivePlayerWeapon(int weaponId, bool equip)
    {
        if (itemDatabase == null)
            itemDatabase = GameObject.FindGameObjectWithTag("ItemDatabase").GetComponent<ItemDatabase>();

        // Get the gun prefab and instantiate it.
        Gun gunPrefab = itemDatabase.GetGunByID(weaponId);
        Gun instantiatedGun = Instantiate(gunPrefab, weaponContainer.position, Quaternion.identity, weaponContainer);

        ModifyWeaponCollidersAndRigidbodyOnPickup(instantiatedGun.gameObject);
        instantiatedGun.OnGround = false;
        instantiatedGun.HoldingPlayer = this;

        // Add the weapon to the player's inventory. 
        inventory.AddWeaponToInventory(weaponId, instantiatedGun.gameObject);

        // Equip the weapon if we're supposed to. Otherwise disable the game object.
        if (equip)
            EquipWeapon(weaponId);
        else
            instantiatedGun.gameObject.SetActive(false);

        // Spawn it on the server.
        NetworkServer.Spawn(instantiatedGun.gameObject);
    }

    /// <summary>
    /// Check to make sure we have the specified gun in the associated inventory. If so, remove it
    /// from the player's inventory and equip it. Otherwise, return (while possibly logging an error).
    /// 
    /// Again, the given weapon MUST ALREADY BE IN THE PLAYER'S INVENTORY. Otherwise, this will simply return.
    /// 
    /// If nextWeaponID is less than zero, then this will simply put the current weapon away.
    /// </summary>
    [Server]
    public void EquipWeapon(int nextWeaponID)
    {
        // Stop the reload in case we switched during a reload.
        if (CurrentWeaponID > 0)
            CancelReload();

        // Deactivate our current weapon, if we're holding one.
        if (CurrentWeapon != null)
            CurrentWeapon.gameObject.SetActive(false);

        // If the next weapon ID is negative, then all we need to do is put our current weapon away.
        // We've already disabled the game object of the currently-equipped weapon. 
        // Just update the associated state variables.
        if (nextWeaponID < 0)
        {
            CurrentWeaponID = -1;
            CurrentWeapon = null;
            RpcAssignCurrentWeapon(-1);
            return;
        }

        // Do this check after putting our weapon away.
        if (!inventory.HasGun(nextWeaponID))
        {
            Debug.Log("Weapon with ID " + nextWeaponID + " not in inventory. Cannot equip.");
            return;
        }

        // Activate the weapon that we're switching to.
        GameObject nextWeaponGameObject = inventory.GetWeaponGameObjectFromInventory(nextWeaponID);
        nextWeaponGameObject.SetActive(true);

        // Update our CurrentWeapon reference.
        CurrentWeapon = nextWeaponGameObject.GetComponent<Gun>();

        Debug.Log("Server assigned " + GetPlayerDebugString() + " weapon " + nextWeaponID + " with ammo in clip " + CurrentWeapon.AmmoInClip);
        CurrentWeaponID = nextWeaponID;       // Switch to the weapon.
        RpcAssignCurrentWeapon(nextWeaponID);
    }

    #endregion

    #region Utility 

    /// <summary>
    /// This function should be called on a Gun's game object when it is being picked up.
    /// 
    /// The colliders need to be disabled and the rigid body needs to be set to kinematic.
    /// </summary>
    /// <param name="weaponGameObject"></param>
    private void ModifyWeaponCollidersAndRigidbodyOnPickup(GameObject weaponGameObject)
    {
        weaponGameObject.GetComponent<Rigidbody>().isKinematic = true;
        weaponGameObject.GetComponent<Rigidbody>().detectCollisions = false;
        Array.ForEach(weaponGameObject.GetComponents<Collider>(), c => c.enabled = false); // Disable the colliders.
    }

    public float GetSquaredDistanceToEmergencyButton()
    {
        // If there is no emergency button, just return infinity lmao.
        if (EmergencyButton == null)
            return float.PositiveInfinity;

        Vector3 directionToTarget = EmergencyButton.GetComponent<Transform>().position - transform.position;
        return directionToTarget.sqrMagnitude;
    }

    public float GetDistanceSquaredToTarget(Transform target)
    {
        return (transform.position - target.position).sqrMagnitude;
    }

    public string GetPlayerDebugString()
    {
        return "Player " + Player.Nickname + " (netId=" + Player.netId + ")";
    }

    #endregion 
}
