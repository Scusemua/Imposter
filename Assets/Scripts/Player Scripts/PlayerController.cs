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

    [Header("Audio")]
    public AudioClip DeathSound;
    public AudioClip ImposterStart;
    public AudioClip CrewmateStart;
    public AudioClip ImpactSound;
    public AudioClip Gunshot;
    public AudioClip ReloadSound;
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
    [SyncVar] public int AmmoInGun = 20;
    [SyncVar(hook = nameof(OnReloadingStateChanged))] bool Reloading;
    [SerializeField] GameObject bulletHolePrefab;
    [SerializeField] GameObject bulletFXPrefab;
    [SerializeField] GameObject bulletBloodFXPrefab;

    //private float weaponSpeedModifier = 1.0f;

    [SyncVar(hook = nameof(OnCurrentWeaponIdChanged))]
    public int CurrentWeaponID = -1;
    public Gun CurrentWeapon;
    
    private Dictionary<GunClass, int> ammoCounts = new Dictionary<GunClass, int>
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
    [SerializeField] private SyncList<InventoryGun> PrimaryInventory = new SyncList<InventoryGun>();

    /// <summary>
    /// The player's secondary weapons.
    /// </summary>
    [SerializeField] private SyncList<InventoryGun> SecondaryInventory = new SyncList<InventoryGun>();

    /// <summary>
    /// The player's melee weapons.
    /// </summary>
    //[SerializeField] private SyncList<InventoryGun> MeleeInventory = new SyncList<InventoryGun>();

    /// <summary>
    /// The player's explosive weapons.
    /// </summary>
    [SerializeField] private SyncList<InventoryGun> ExplosiveInventory = new SyncList<InventoryGun>();

    /// <summary>
    /// The player's grenades.
    /// </summary>
    //[SerializeField] private SyncList<InventoryGun> GrenadeInventory = new SyncList<InventoryGun>();

    private ItemDatabase itemDatabase;
    
    private LineRenderer lineRenderer;

    private float curCooldown;

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

        curCooldown -= Time.deltaTime;

        if (Input.GetMouseButton(0))
            ShootWeapon();

        if (Input.GetKeyDown(KeyCode.R))
            ReloadButton();
        else if (Input.GetKeyDown(KeyCode.V))
            DropButton();
        else if (Input.GetKeyDown(KeyCode.H))
            CmdTakeDamage(10.0f);
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            string[] primaryWeaponNames = PrimaryInventory.Select(gun => itemDatabase.GetGunByID(gun.Id).Name).ToArray<string>();
            string[] secondaryWeaponNames = SecondaryInventory.Select(gun => itemDatabase.GetGunByID(gun.Id).Name).ToArray<string>();
            string[] explosiveWeaponNames = ExplosiveInventory.Select(gun => itemDatabase.GetGunByID(gun.Id).Name).ToArray<string>();

            Player.PlayerUI.ShowWeaponUI(primaryWeaponNames, secondaryWeaponNames, explosiveWeaponNames);
            CmdTryCycleInventory(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            string[] primaryWeaponNames = PrimaryInventory.Select(gun => itemDatabase.GetGunByID(gun.Id).Name).ToArray<string>();
            string[] secondaryWeaponNames = SecondaryInventory.Select(gun => itemDatabase.GetGunByID(gun.Id).Name).ToArray<string>();
            string[] explosiveWeaponNames = ExplosiveInventory.Select(gun => itemDatabase.GetGunByID(gun.Id).Name).ToArray<string>();

            Player.PlayerUI.ShowWeaponUI(primaryWeaponNames, secondaryWeaponNames, explosiveWeaponNames);
            CmdTryCycleInventory(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            string[] primaryWeaponNames = PrimaryInventory.Select(gun => itemDatabase.GetGunByID(gun.Id).Name).ToArray<string>();
            string[] secondaryWeaponNames = SecondaryInventory.Select(gun => itemDatabase.GetGunByID(gun.Id).Name).ToArray<string>();
            string[] explosiveWeaponNames = ExplosiveInventory.Select(gun => itemDatabase.GetGunByID(gun.Id).Name).ToArray<string>();

            Player.PlayerUI.ShowWeaponUI(primaryWeaponNames, secondaryWeaponNames, explosiveWeaponNames);
            CmdTryCycleInventory(2);
        }
    }

    internal void ShootWeapon()
    {
        if (AmmoInGun > 0 && !Reloading && !Player.IsDead && curCooldown <= 0.01 && CurrentWeaponID >= 0 && CurrentWeapon != null)
        {
            //Do command
            CmdTryShoot();
            curCooldown = CurrentWeapon.WeaponCooldown;
        }
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
    void RpcPlayerFiredEntity(uint shooterID, uint targetID, int gunId, Vector3 impactPos, Vector3 impactRot)
    {
        //Instantiate(bulletHolePrefab, impactPos + impactRot * 0.1f, Quaternion.LookRotation(impactRot), NetworkIdentity.spawned[targetID].transform);
        //Instantiate(bulletBloodFXPrefab, impactPos, Quaternion.LookRotation(impactRot));
        NetworkIdentity.spawned[shooterID].GetComponent<Player>().MuzzleFlash();

        Transform shooterTransform = NetworkIdentity.spawned[shooterID].GetComponent<Player>().GetComponent<Transform>();

        float volumeDistModifier = (1000f - GetDistanceSquaredToTarget(shooterTransform)) / 1000f;
        //Debug.Log("Playing gunshot with volume modifier: " + volumeDistModifier);

        AudioClip gunshotSound = Gunshot;
        if (itemDatabase.GetGunByID(gunId).ShootSound != null)
            gunshotSound = itemDatabase.GetGunByID(gunId).ShootSound;

        // Adjust volume of gunshot based on distance.
        this.AudioSource.PlayOneShot(gunshotSound, volumeDistModifier);

        //Destroy(bulletBloodGO, 2);
        //Destroy(bulletHolePrefab, 10);
    }

    [ClientRpc]
    void RpcPlayerFired(uint shooterID, int gunId, Vector3 impactPos, Vector3 impactRot)
    {
        //Instantiate(bulletHolePrefab, impactPos + impactRot * 0.1f, Quaternion.LookRotation(impactRot));
        Instantiate(bulletFXPrefab, impactPos, Quaternion.LookRotation(impactRot));
        NetworkIdentity.spawned[shooterID].GetComponent<Player>().MuzzleFlash();

        Transform shooterTransform = NetworkIdentity.spawned[shooterID].GetComponent<Player>().GetComponent<Transform>();

        float volumeDistModifier = (1000f - GetDistanceSquaredToTarget(shooterTransform)) / 1000f;
        //Debug.Log("Playing gunshot with volume modifier: " + volumeDistModifier);

        AudioClip gunshotSound = Gunshot;
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
        Player.PlayerUI.AmmoClipText.text = AmmoInGun.ToString();
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
        //We reloaded successfully.
        //Update UI
        Player.PlayerUI.AmmoClipText.text = AmmoInGun.ToString();
        Player.PlayerUI.AmmoReserveText.text = ammoCounts[CurrentWeapon.GunClass].ToString();
        Player.PlayerUI.ReloadingProgressBar.gameObject.SetActive(false);
    }

    #endregion

    #region Commands 

    [Command]
    public void CmdTakeDamage(float damage)
    {
        Player.Damage(damage, Player.netId);
    }

    [Command]
    public void CmdPickupWeapon(GameObject weaponGameObject)
    {
        Gun gun = weaponGameObject.GetComponent<Gun>();
        Gun.GunType gunType = gun._GunType;

        // Used to check if the player already has this gun.
        InventoryGun temp = new InventoryGun(-1, gun.Id);

        if (gunType == Gun.GunType.PRIMARY && PrimaryInventory.Count < GameOptions.GunTypeInventoryLimits[gunType] && !PrimaryInventory.Contains(temp))
        {
            Debug.Log("Adding weapon " + gun.Id + ", " + gun.Name + " to player's primary inventory.");
            PrimaryInventory.Add(new InventoryGun(gun.AmmoInClip, gun.Id));
            NetworkServer.Destroy(weaponGameObject);

            Debug.Log("Added weapon " + gun.Id + ", " + gun.Name + " to player's primary inventory.");
            Debug.Log("Primary inventory: " + PrimaryInventory.Select(g => g.Id).ToArray().ToString());

            TargetPlayPickupWeaponSound();
        }
        else if (gunType == Gun.GunType.SECONDARY && SecondaryInventory.Count < GameOptions.GunTypeInventoryLimits[gunType] && !SecondaryInventory.Contains(temp))
        {
            Debug.Log("Adding weapon " + gun.Id + ", " + gun.Name + " to player's secondary inventory.");
            SecondaryInventory.Add(new InventoryGun(gun.AmmoInClip, gun.Id));
            NetworkServer.Destroy(weaponGameObject);

            Debug.Log("Added weapon " + gun.Id + ", " + gun.Name + " to player's secondary inventory.");
            Debug.Log("Secondary inventory: " + SecondaryInventory.Select(g => g.Id).ToArray().ToString());

            TargetPlayPickupWeaponSound();
        }
        else if (gunType == Gun.GunType.EXPLOSIVE && ExplosiveInventory.Count < GameOptions.GunTypeInventoryLimits[gunType] && !ExplosiveInventory.Contains(temp))
        {
            Debug.Log("Adding weapon " + gun.Id + ", " + gun.Name + " to player's explosive inventory.");
            ExplosiveInventory.Add(new InventoryGun(gun.AmmoInClip, gun.Id));
            NetworkServer.Destroy(weaponGameObject);

            Debug.Log("Added weapon " + gun.Id + ", " + gun.Name + " to player's explosive inventory.");
            Debug.Log("Explosive inventory: " + ExplosiveInventory.Select(g => g.Id).ToArray().ToString());

            TargetPlayPickupWeaponSound();
        }
    }

    [Command]
    public void CmdTryCycleInventory(int inventoryId)
    {
        SyncList<InventoryGun> inventoryWeAreCyclingThrough = GetInventoryByInventoryId(inventoryId);
        SyncList<InventoryGun> currentWeaponAssociatedInventory = GetAssociatedInventoryByGunId(CurrentWeaponID);

        if (inventoryWeAreCyclingThrough == null)
            return;

        // There are multiple weapons in the inventory we're cycling through.
        if (inventoryWeAreCyclingThrough.Count > 1)
        {
            // There are two main cases.
            // (1a) The cycling inventory and current gun's inventory are the same. We move to the next gun
            //      in the inventory in this case.
            // (2a) They are different. We put our gun away. Then, we equip the first gun from the other inventory.
            if (inventoryWeAreCyclingThrough == currentWeaponAssociatedInventory)
            {
                // Case (1a)
                InventoryGun temp = new InventoryGun(-1, CurrentWeaponID);
                int currentGunIndexInInventory = currentWeaponAssociatedInventory.IndexOf(temp);
                int nextGunIndex = -1;

                // Either increment the index or loop it back around to zero if current gun is last in the inventory.
                if (currentGunIndexInInventory == currentWeaponAssociatedInventory.Count - 1)
                    nextGunIndex = 0;
                else
                    nextGunIndex = currentGunIndexInInventory + 1;

                StoreCurrentGunAndSwitch(inventoryWeAreCyclingThrough[nextGunIndex].Id);
            }
            else
            {
                // Case (2a)
                // This function call handles equipping the new gun in addition to putting our current gun away.
                StoreCurrentGunAndSwitch(inventoryWeAreCyclingThrough[0].Id);
            }
        }
        else if (inventoryWeAreCyclingThrough.Count == 1)
        {
            // There is only one weapon in the inventory that the player is cycling through,
            // and the cycling inventory is the same as the current gun's inventory. In this case,
            // we have no weapons to switch to, so we're done.
            if (inventoryWeAreCyclingThrough == currentWeaponAssociatedInventory)
                return;
            else
            {
                // There is only one weapon in the inventory that the player is cycling through, and
                // the two inventories are different. There are now two cases:
                // (1b) We are holding a weapon already and need to put it away first. 
                // (2b) We are not holding a weapon and can just equip the specified weapon.
                // This function call handles both. If we have a gun already, we'll put it away.
                // Then, we'll equip the new gun.
                StoreCurrentGunAndSwitch(inventoryWeAreCyclingThrough[0].Id);
            }
        }
    }

    [Command]
    public void CmdPickupAmmoBox(GameObject ammoBoxGameObject)
    {
        AmmoBox ammoBox = ammoBoxGameObject.GetComponent<AmmoBox>();

        if (ammoBox.IsAmmoBox)
        {
            GunClass associatedGunType = ammoBox.AssociatedGunClass;
            Debug.Log("Ammo box is of type " + associatedGunType.ToString() + ". Current ammo: " + ammoCounts[associatedGunType] + ", Max: " + Gun.AmmoMaxCounts[associatedGunType] + ".");
            if (ammoCounts[associatedGunType] < Gun.AmmoMaxCounts[associatedGunType])
            {
                ammoCounts[associatedGunType] = Mathf.Min(Gun.AmmoMaxCounts[associatedGunType], ammoCounts[associatedGunType] + ammoBox.NumberBullets);
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
                NetworkServer.Destroy(ammoBoxGameObject);
                TargetPlayPickupHealthSound();
            }
        }
    }

    [Command]
    private void CmdTryReload()
    {
        Debug.Log(GetPlayerDebugString() + " [server side] is trying to reload. Current Weapon ID = " + CurrentWeaponID + ", CurrentWeapon != null: " + (CurrentWeapon != null) + ", Ammo In Gun: " + AmmoInGun + "Current Weapon Clip Size: " + (CurrentWeapon != null ? CurrentWeapon.ClipSize.ToString() : "N/A"));
        if (CurrentWeaponID < 0 || Reloading || AmmoInGun == itemDatabase.GetGunByID(CurrentWeaponID).ClipSize)
            return;

        reloadCoroutine = StartCoroutine(reloadingWeapon());
    }

    [Command]
    private void CmdTryShoot()
    {
        // TODO: Play error sound indicating no weapon? Or just punch?
        if (CurrentWeaponID < 0)
            return;

        //Server side check
        //if ammoCount > 0 && isAlive
        if (AmmoInGun > 0 && !Player.IsDead)
        {
            AmmoInGun--;
            TargetShoot();
            
            // TODO: Projectile count, accuracy, etc.
            Ray ray = new Ray(Player.WeaponMuzzle.transform.position, Player.WeaponMuzzle.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f))
            {
                Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.red, 1);
                //Debug.Log("SERVER: Player shot: " + hit.collider.name);
                if (hit.collider.CompareTag("Player"))
                {
                    RpcPlayerFiredEntity(GetComponent<NetworkIdentity>().netId, hit.collider.GetComponent<NetworkIdentity>().netId, CurrentWeaponID, hit.point, hit.normal);
                    if (hit.collider.GetComponent<NetworkIdentity>().netId != GetComponent<NetworkIdentity>().netId)
                        hit.collider.GetComponent<Player>().Damage(itemDatabase.GetGunByID(CurrentWeaponID).Damage, GetComponent<NetworkIdentity>().netId);
                }
                else if (hit.collider.CompareTag("Enemy"))
                    RpcPlayerFiredEntity(GetComponent<NetworkIdentity>().netId, hit.collider.GetComponent<NetworkIdentity>().netId, CurrentWeaponID, hit.point, hit.normal);
                else
                    RpcPlayerFired(GetComponent<NetworkIdentity>().netId, itemDatabase.GetGunByID(CurrentWeaponID).Id, hit.point, hit.normal);
            }
        }

    }

    [Command]
    public void CmdTryDropCurrentWeapon()
    {
        if (CurrentWeaponID < 0)
            return;

        // Remove the gun from the player's inventory.
        InventoryGun temp = new InventoryGun(-1, CurrentWeaponID);
        if (GetAssociatedInventoryByGunId(CurrentWeaponID).Contains(temp))
            GetAssociatedInventoryByGunId(CurrentWeaponID).Remove(temp);
        else
        {
            Debug.LogError("Player " + Player.Nickname + " (netId=" + Player.netId + ") attempting to drop gun " + CurrentWeaponID + " which is not in player's inventory...");
            AmmoInGun = 0;
            CurrentWeaponID = -1; // Remove the weapon as the player definitely shouldn't have it.

            // Set this to false in-case we were reloading when we dropped the gun.
            CancelReload();

            return;
        }

        // Instantiate the scene object on the server a bit in front of the player so they don't instantly pick it up.
        Vector3 pos = transform.position + (transform.forward * 2.0f) ;
        Quaternion rot = weaponContainer.transform.rotation;
        Gun droppedWeapon = Instantiate(itemDatabase.GetGunByID(CurrentWeaponID), pos, rot);

        // Set this to false in-case we were reloading when we dropped the gun.
        CancelReload();

        // Set the RigidBody as non-kinematic on the server only (isKinematic = true in prefab).
        Array.ForEach(droppedWeapon.GetComponents<Collider>(), c => c.enabled = true); // Disable the colliders.
        droppedWeapon.GetComponent<Rigidbody>().isKinematic = false;
        droppedWeapon.GetComponent<Rigidbody>().detectCollisions = true;
        droppedWeapon.OnGround = true;
        droppedWeapon.AmmoInClip = AmmoInGun;

        // Toss it out in front of us a bit.
        droppedWeapon.GetComponent<Rigidbody>().velocity = transform.forward * 7.0f + transform.up * 5.0f;

        // Set the player's SyncVar to nothing so clients will destroy the equipped child item.
        AmmoInGun = 0;
        CurrentWeaponID = -1;

        // Spawn the scene object on the network for all to see
        NetworkServer.Spawn(droppedWeapon.gameObject);
    }

    [Command]
    public void CmdPickupItem(GameObject item)
    {

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

    [Client]
    public void OnCurrentWeaponIdChanged(int _Old, int _New)
    {
        Debug.Log("Current weapon ID changed. Old value: " + _Old + ", New value: " + _New);
        if (CurrentWeapon != null)
        {
            Destroy(CurrentWeapon.gameObject);
            CurrentWeapon = null;
        }

        // The player could've put away all their weapons, meaning the new ID would be -1.
        if (_New >= 0)
        {
            AssignWeaponClientSide(_New);

            if (isLocalPlayer) Player.PlayerUI.AmmoUI.SetActive(true);
        }
        else
        {
            if (isLocalPlayer) Player.PlayerUI.AmmoUI.SetActive(false);
        }
    }

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

    [Client]
    public void OnReloadingStateChanged(bool _Old, bool _New)
    {
        if (!isLocalPlayer) return;

        if (_New)
        {
            if (CurrentWeapon.ReloadSound != null)
                AudioSource.PlayOneShot(CurrentWeapon.ReloadSound);
            else
                AudioSource.PlayOneShot(ReloadSound);
        }
    }

    public override void OnStartLocalPlayer()
    {
        //Debug.Log("OnStartLocalPlayer() called for Player " + netId);
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
    public void AssignWeaponClientSide(int id)
    {
        if (id == -1)
        {
            if (CurrentWeapon != null)
            {
                Destroy(CurrentWeapon.gameObject);
                CurrentWeapon = null;
            }

            // Make sure to update the ammo display.
            UpdateAmmoDisplay();
            //weaponSpeedModifier = 1.0f; // Reset this.
            return;
        }

        if (itemDatabase == null)
            itemDatabase = GameObject.FindGameObjectWithTag("ItemDatabase").GetComponent<ItemDatabase>();

        Debug.Log("Assigning weapon " + id + " to player now.");
        CurrentWeapon = Instantiate(itemDatabase.GetGunByID(id), weaponContainer).GetComponent<Gun>();
        Array.ForEach(CurrentWeapon.GetComponents<Collider>(), c => c.enabled = false); // Disable the colliders.
        CurrentWeapon.GetComponent<Rigidbody>().isKinematic = true;
        CurrentWeapon.GetComponent<Rigidbody>().detectCollisions= false;
        CurrentWeapon.OnGround = false;

        // Make sure to update the ammo display.
        UpdateAmmoDisplay();
    }

    [Client]
    internal void ReloadButton()
    {
        Debug.Log(GetPlayerDebugString() + " [client side] is trying to reload. Current Weapon ID = " + CurrentWeaponID + ", CurrentWeapon != null: " + (CurrentWeapon != null) + ", Ammo In Gun: " + AmmoInGun + "Current Weapon Clip Size: " + (CurrentWeapon != null ? CurrentWeapon.ClipSize.ToString() : "N/A"));
        if (CurrentWeaponID >= 0 && CurrentWeapon != null && AmmoInGun != CurrentWeapon.ClipSize && !Reloading && !Player.IsDead)
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
            Player.PlayerUI.AmmoClipText.text = AmmoInGun.ToString();
            Player.PlayerUI.AmmoReserveText.text = ammoCounts[CurrentWeapon.GunClass].ToString();
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

    public float GetSquaredDistanceToEmergencyButton()
    {
        Vector3 directionToTarget = EmergencyButton.GetComponent<Transform>().position - transform.position;
        return directionToTarget.sqrMagnitude;
    }

    public float GetDistanceSquaredToTarget(Transform target)
    {
        return (transform.position - target.position).sqrMagnitude;
    }

    #endregion

    #region Server

    public override void OnStartServer()
    {
        GivePlayerWeapon(3, false, false);
    }

    /// <summary>
    /// Stop the reload coroutine. Useful if we drop or switch guns during a reload.
    /// </summary>
    [Server]
    private void CancelReload()
    {
        if (Reloading)
        {
            StopCoroutine(reloadCoroutine);
            TargetReload();
            Reloading = false;
        }
    }

    /// <summary>
    /// Add the specified weapon to the player's inventory. If the 'equip' flag is set to true,
    /// then also equip the current weapon (put away whatever weapon the player may already be holding first).
    /// 
    /// This could also be used to force a player to equip a weapon in their inventory by specifiying a
    /// gun that they already have while also passing true for 'equip'.
    /// </summary>
    [Server]
    public void GivePlayerWeapon(int weaponId, bool overrideInventorySizeLimit, bool equip)
    {
        if (itemDatabase == null)
            itemDatabase = GameObject.FindGameObjectWithTag("ItemDatabase").GetComponent<ItemDatabase>();

        Gun gun = itemDatabase.GetGunByID(weaponId);
        InventoryGun inventoryGun = new InventoryGun(gun.ClipSize, weaponId);

        // Make sure that we're either ignoring inventory size limits or the player does have enough space.
        if (overrideInventorySizeLimit || GetAssociatedInventoryByGunId(weaponId).Count < GameOptions.GunTypeInventoryLimits[gun._GunType])
        {
            if (GetAssociatedInventoryByGunId(weaponId).Contains(inventoryGun))
                Debug.LogWarning("Player " + Player.Nickname + " (netId=" + Player.netId + ") already has weapon " + weaponId);
            else
                GetAssociatedInventoryByGunId(weaponId).Add(inventoryGun);
        }

        if (equip)
            StoreCurrentGunAndSwitch(weaponId);
    }

    [Server]
    private SyncList<InventoryGun> GetInventoryByInventoryId(int inventoryId)
    {
        switch (inventoryId)
        {
            case 0:
                return PrimaryInventory;
            case 1:
                return SecondaryInventory;
            case 2:
                return ExplosiveInventory;
            default:
                return null;
        }
    }


    [Server]
    private SyncList<InventoryGun> GetAssociatedInventoryByGunId(int weaponId)
    {
        Gun gun = itemDatabase.GetGunByID(weaponId);

        return GetAssociatedInventoryByGun(gun);
    }

    [Server]
    private SyncList<InventoryGun> GetAssociatedInventoryByGun(Gun gun)
    {
        if (gun == null)
            return null;

        if (gun._GunType == Gun.GunType.PRIMARY)
            return PrimaryInventory;
        else if (gun._GunType == Gun.GunType.SECONDARY)
            return SecondaryInventory;
        else if (gun._GunType == Gun.GunType.EXPLOSIVE)
            return ExplosiveInventory;
        else
            return null;
    }

    /// <summary>
    /// Store the player's current weapon in their inventory. Then, attempt to switch
    /// guns to the one specified by the parameter. Check to make sure they have the gun first.
    /// </summary>
    /// <param name="nextWeaponID">The gun to switch to.</param>
    [Server]
    public void StoreCurrentGunAndSwitch(int nextWeaponID)
    {
        // If the player has a gun equipped, put it away first.
        if (CurrentWeaponID >= 0)
        {
            InventoryGun inventoryGun = new InventoryGun(AmmoInGun, CurrentWeaponID);

            // IndexOf only checks against the Id. So we find the gun in the list, then replace
            // it with this updated object which has a up-to-date AmmoInGun value.
            int idx = GetAssociatedInventoryByGunId(CurrentWeaponID).IndexOf(inventoryGun);
            GetAssociatedInventoryByGunId(CurrentWeaponID)[idx] = inventoryGun;
            //GetAssociatedInventoryByGunId(CurrentWeaponID).Add(inventoryGun);
        }

        // Stop the reload in case we switched during a reload.
        CancelReload();

        // If we're also switching weapons, then make sure we indeed have the weapon that we're trying to switch to.
        // This will handle the case where we aren't switching to a gun and instead are just putting are gun away.
        AssignWeaponServerSide(nextWeaponID);
    }

    /// <summary>
    /// Check to make sure we have the specified gun in the associated inventory. If so, remove it
    /// from the player's inventory and equip it. Otherwise, return (while possibly logging an error).
    /// </summary>
    [Server]
    public void AssignWeaponServerSide(int nextWeaponID)
    {
        if (nextWeaponID < 0)
            return;

        InventoryGun temp = new InventoryGun(-1, nextWeaponID);
        SyncList<InventoryGun> associatedInventory = GetAssociatedInventoryByGunId(nextWeaponID);
        int indexOfGunInInventory = associatedInventory.IndexOf(temp);
        // Make sure we have the specified weapon before equipping it.
        if (indexOfGunInInventory == -1)
        {
            Debug.LogError("ERROR: Player " + Player.Nickname + " (netId=" + Player.netId + ") attempting to switch to weapon (id=" + nextWeaponID + "), which they do not have.");
            return;
        }
        else
        {
            //associatedInventory.Remove(temp);   // Remove the weapon from the player's inventory.
            AmmoInGun = GetAssociatedInventoryByGunId(nextWeaponID)[indexOfGunInInventory].AmmoInClip;
            CurrentWeaponID = nextWeaponID;       // Switch to the weapon.
        }
    }

    [Server]
    IEnumerator reloadingWeapon()
    {
        if (CurrentWeaponID < 0)
            yield return null;
        else if (ammoCounts[itemDatabase.GetGunByID(CurrentWeaponID).GunClass] <= 0)
        {
            ammoCounts[itemDatabase.GetGunByID(CurrentWeaponID).GunClass] = 0;
            yield return null;
        }
        else
        {
            int currentWeaponId = CurrentWeaponID;
            Gun currentGun = itemDatabase.GetGunByID(currentWeaponId);
            Reloading = true;
            ShowReloadBar(itemDatabase.GetGunByID(CurrentWeaponID).ReloadTime);
            yield return new WaitForSeconds(currentGun.ReloadTime);

            // Make sure player hasn't switched guns to get away with any funny business.
            if (currentGun == null || currentGun.Id != currentWeaponId)
            {
                Reloading = false;
                TargetReload();
                yield return null;
            }
            else
            {
                // Determine how much ammo the player is missing.
                // If we have enough ammo in reserve to top off the clip/magazine,
                // then do so. Otherwise, put back however much ammo we have left.
                int ammoMissing = currentGun.ClipSize - AmmoInGun;

                // Remove from our reserve ammo whatever we loaded into the weapon.
                if (ammoCounts[currentGun.GunClass] - ammoMissing >= 0)
                {
                    AmmoInGun += ammoMissing;
                    ammoCounts[currentGun.GunClass] -= ammoMissing;
                }
                else
                {
                    // We do not have enough ammo to completely top off the magazine.
                    // Just add what we have.
                    AmmoInGun += ammoCounts[currentGun.GunClass];
                    ammoCounts[currentGun.GunClass] = 0;
                }

                TargetReload();
                Reloading = false;

                yield return null;
            }
        }
    }

    /// <summary>
    /// Used to keep track of how much ammo is in the clip of the gun when in the player's inventory.
    /// </summary>
    internal class InventoryGun
    {
        public int AmmoInClip;
        public int Id;

        public InventoryGun()
        {
            AmmoInClip = 0;
            Id = -1;
        }

        public InventoryGun(int ammoInClip, int id)
        {
            AmmoInClip = ammoInClip;
            Id = id;
        }

        public override bool Equals(object obj)
        {
            InventoryGun other = obj as InventoryGun;

            if (other == null)
                return false;

            return other.Id == Id;
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + Id.GetHashCode();
            return hash;
        }
    }

    #endregion

    #region Utility 

    public string GetPlayerDebugString()
    {
        return "Player " + Player.Nickname + " (netId=" + Player.netId + ")";
    }

    #endregion 
}
