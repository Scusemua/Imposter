using UnityEngine;
using Mirror;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System;

public class PlayerController : NetworkBehaviour
{
    public Player Player;
    [Tooltip("This is displayed to the player and any other dead players once this player has died.")]
    public GameObject DeadPlayerBody;
    public GameObject LivingPlayerBody;
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
    public AudioClip PickupItemSound;
    public AudioSource AudioSource;

    [Header("Visual")]
    public GameObject DeathEffectPrefab;
    public GameObject BloodPoolPrefab;
    public GameObject CameraPrefab;
    public GameObject CameraContainer;
    public Camera Camera;
    public Animator AliveAnimator;
    public Animator DeadAnimator;

    [Header("Game-Related")]
    public GameObject EmergencyButton;
    public bool MovementEnabled;

    [Header("Weapon")]
    [SerializeField] Transform itemContainer; // This is where the weapon goes.
    [SerializeField] GameObject bulletHolePrefab;
    [SerializeField] GameObject bulletFXPrefab;
    [SerializeField] GameObject bulletBloodFXPrefab;
    public int StartingWeaponId = -1;

    [SyncVar] public int CurrentItemId = -1;
    public UsableItem CurrentItem;
    
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
    /// The largest SQUARED distance the player may be from an interactable to still be able to interact with it.
    /// </summary>
    private const float maximumIdentificationDistance = 25.0f;

    private ItemDatabase itemDatabase;
    
    //private LineRenderer lineRenderer;

    private float movementSpeed;
    private float runBoost;
    private bool sprintEnabled;

    /// <summary>
    /// Animation
    /// </summary>
    private float lastX;
    private float lastY;

    private NetworkGameManager NetworkGameManager
    {
        get => NetworkManager.singleton as NetworkGameManager;
    }

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

        // Disable movement when typing into console.
        if (Input.GetKey(KeyCode.Tilde))
        {
            MovementEnabled = !MovementEnabled;
            print("MovementEnabled = " + MovementEnabled.ToString());
        }

        // Everything in here can only be performed when the player is alive.
        if (!Player.IsDead)
        {

            if (Input.GetKeyDown(KeyCode.E))
            {
                InteractableInput();
            }

            if (Input.GetKey(KeyCode.B))
                Player.CmdSuicide();

            if (CurrentItem != null && CurrentItem is Gun && (CurrentItem as Gun).DoShootTest())
                ShootWeapon();

            if (Input.GetKeyDown(KeyCode.R))
                ReloadButton();
            else if (Input.GetKeyDown(KeyCode.V))
                DropButton();
            else if (Input.GetKeyDown(KeyCode.H))
                Player.CmdDoDamage(10f, 0f, Player.netId, DamageSource.Suicide, transform.position);
            else if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Dictionary<Gun.GunType, IEnumerable<string>> gunNameLists = inventory.GetGunNamesOrganized();
                Player.PlayerUI.ShowWeaponUI(gunNameLists[Gun.GunType.PRIMARY],
                    gunNameLists[Gun.GunType.SECONDARY], gunNameLists[Gun.GunType.EXPLOSIVE]);
                CmdTryCycleInventory(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Dictionary<Gun.GunType, IEnumerable<string>> gunNameLists = inventory.GetGunNamesOrganized();
                Player.PlayerUI.ShowWeaponUI(gunNameLists[Gun.GunType.PRIMARY],
                    gunNameLists[Gun.GunType.SECONDARY], gunNameLists[Gun.GunType.EXPLOSIVE]);
                CmdTryCycleInventory(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Dictionary<Gun.GunType, IEnumerable<string>> gunNameLists = inventory.GetGunNamesOrganized();
                Player.PlayerUI.ShowWeaponUI(gunNameLists[Gun.GunType.PRIMARY],
                    gunNameLists[Gun.GunType.SECONDARY], gunNameLists[Gun.GunType.EXPLOSIVE]);
                CmdTryCycleInventory(2);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                Dictionary<Gun.GunType, IEnumerable<string>> gunNameLists = inventory.GetGunNamesOrganized();
                Player.PlayerUI.ShowWeaponUI(gunNameLists[Gun.GunType.PRIMARY],
                    gunNameLists[Gun.GunType.SECONDARY], gunNameLists[Gun.GunType.EXPLOSIVE]);
                CmdTryCycleInventory(3);
            }
        }
    }

    /// <summary>
    /// Check for input pertaining to interactables.
    /// </summary>
    public void InteractableInput()
    {
        if (!isLocalPlayer) return;

        float distToEmergencyButton = GetSquaredDistanceToEmergencyButton();

        bool interactableWithinRange = false;
        bool canInteractWithEmergencyButton;
        bool bodyWithinRange = false;

        if (distToEmergencyButton <= maximumIdentificationDistance)
        {
            interactableWithinRange = true;
            canInteractWithEmergencyButton = true;
        }
        else
            canInteractWithEmergencyButton = false;

        // Calculate the distance to all dead players. If we're close enough to a body to interact with it, then keep track of that.
        Player closestDeadPlayer = null;
        float closestDistanceToBody = float.PositiveInfinity;
        foreach (Player player in NetworkGameManager.GamePlayers)
        {
            if (player.IsDead && !player.Identified)
            {
                float distance = (player.GetComponent<Transform>().position - transform.position).sqrMagnitude;
                if (distance < closestDistanceToBody)
                {
                    closestDeadPlayer = player;
                    closestDistanceToBody = distance;
                }
            }
        }

        if (closestDeadPlayer != null && closestDistanceToBody <= maximumIdentificationDistance)
        {
            interactableWithinRange = true;
            bodyWithinRange = true;
        }

        //if (interactableWithinRange)
        //    Player.PlayerUI.InteractableButton.enabled = true;
        //else
        //    Player.PlayerUI.InteractableButton.enabled = false;

        if (Input.GetKey(KeyCode.E) && interactableWithinRange)
        {
            // If the player could both interact with the button AND identify the body, then we'll just have them interact with the body.
            if (bodyWithinRange)
                closestDeadPlayer.CmdIdentify(Player.netId);
            else if (canInteractWithEmergencyButton)
                Player.CmdStartVote();
        }
    }

    internal void ShootWeapon()
    {
        if (!Player.IsDead && CurrentItemId >= 0 && CurrentItem != null)
            CmdTryShoot();
    }

    void LateUpdate()
    {
        if (!isLocalPlayer) return;

        if (CameraContainer != null && Camera != null && Camera.enabled)
        {
            CameraContainer.transform.position = transform.position + CameraOffset;
        }
    }

    #region Client RPC
    [ClientRpc]
    public void RpcAssignCurrentItem(int itemId)
    {
        if (isLocalPlayer)
        {
            if (itemId >= 0)
            {
                Player.PlayerUI.AmmoUI.SetActive(true);
                UpdateAmmoDisplay();
            }
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

    /// <summary>
    /// Instantiate blood fx at the entity which was hit by the player.
    /// </summary>
    [ClientRpc]
    public void RpcGunshotHitEntity(Vector3 impactPos, Vector3 impactRot)
    {
        Instantiate(bulletBloodFXPrefab, impactPos, Quaternion.LookRotation(impactRot));
    }

    /// <summary>
    /// Instantiate bullet-hit FX at the location where the bullet hit.
    /// </summary>
    [ClientRpc]
    public void RpcGunshotHitEnvironment(uint shooterID, int gunId, Vector3 impactPos, Vector3 impactRot)
    {
        //Instantiate(bulletHolePrefab, impactPos + impactRot * 0.1f, Quaternion.LookRotation(impactRot));
        Instantiate(bulletFXPrefab, impactPos, Quaternion.LookRotation(impactRot));
        NetworkIdentity.spawned[shooterID].GetComponent<PlayerController>().MuzzleFlash();
    }

    /// <summary>
    /// Play gunshot sound and show muzzle flash.
    /// </summary>
    [ClientRpc]
    public void RpcPlayerShotGun(uint shooterId, int gunId)
    {
        NetworkIdentity.spawned[shooterId].GetComponent<PlayerController>().MuzzleFlash();

        Transform shooterTransform = NetworkIdentity.spawned[shooterId].GetComponent<Player>().GetComponent<Transform>();
        float volumeDistModifier = (1000f - GetDistanceSquaredToTarget(shooterTransform)) / 1000f;

        AudioClip gunshotSound = DefaultGunshotSound;
        if (itemDatabase.GetGunByID(gunId).ShootSound != null)
            gunshotSound = itemDatabase.GetGunByID(gunId).ShootSound;

        // Adjust volume of gunshot based on distance.
        this.AudioSource.PlayOneShot(gunshotSound, volumeDistModifier);
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
        if (CurrentItem != null && CurrentItem is Gun)
            AudioSource.PlayOneShot((CurrentItem as Gun).ReloadSound);
    }

    [TargetRpc]
    public void TargetPlayDryFire()
    {
        AudioClip dryfireSound = DefaultDryfireSound;
        if (CurrentItem != null && CurrentItem is Gun && (CurrentItem as Gun).DryfireSound != null)
            dryfireSound = (CurrentItem as Gun).DryfireSound;

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
    public void TargetPlayPickupItemSound()
    {
        AudioSource.PlayOneShot(PickupItemSound);
    }

    [TargetRpc]
    public void TargetUpdateAmmoCounts()
    {
        UpdateAmmoDisplay();
    }

    [TargetRpc]
    public void TargetShoot()
    {
        if (CurrentItem is Gun)
            // Update the ammo count on the player's screen.
            Player.PlayerUI.AmmoClipText.text = (CurrentItem as Gun).AmmoInClip.ToString();
    }

    [TargetRpc]
    public void TargetShowReloadBar(float reloadTime)
    {
        Player.PlayerUI.ReloadingProgressBar.health = 0;
        Player.PlayerUI.ReloadingProgressBar.healthPerSecond = 100f / reloadTime;
        Player.PlayerUI.ReloadingProgressBar.gameObject.SetActive(true);
    }

    [TargetRpc]
    public void TargetReload()
    {
        if (!CurrentItem is Gun) return;

        // We reloaded successfully. Update our UI.
        Player.PlayerUI.AmmoClipText.text = (CurrentItem != null) ? (CurrentItem as Gun).AmmoInClip.ToString() : "N/A";
        Player.PlayerUI.AmmoReserveText.text = (CurrentItem != null) ? AmmoCounts[(CurrentItem as Gun).GunClass].ToString() : "N/A";
        Player.PlayerUI.ReloadingProgressBar.health = 100.0f; // Max out the reload bar, then turn it off.
        Player.PlayerUI.ReloadingProgressBar.gameObject.SetActive(false);
    }

    #endregion

    #region Commands 
    [Command(ignoreAuthority = true)]
    public void CmdSetPosition(Vector3 newPosition)
    {
        transform.SetPositionAndRotation(newPosition, transform.rotation);
    }

    [Command(ignoreAuthority = true)]
    public void CmdMoveToPlayer(uint netId)
    {
        Player otherPlayer = (NetworkManager.singleton as NetworkGameManager).NetIdMap[netId];

        if (otherPlayer != null)
        {
            Debug.Log("Teleporting " + GetPlayerDebugString() + " to " + otherPlayer.GetComponent<PlayerController>().GetPlayerDebugString());
            Vector3 pos = new Vector3(otherPlayer.transform.position.x,
                                      otherPlayer.transform.position.y,
                                      otherPlayer.transform.position.z);
            transform.SetPositionAndRotation(pos, transform.rotation);
        }
    }

    [Command(ignoreAuthority = true)]
    public void CmdInfiniteAmmo()
    {
        foreach (GunClass gunClass in Enum.GetValues(typeof(GunClass)))
        {
            AmmoCounts[gunClass] = 999999;
        }
    }

    [Command(ignoreAuthority = true)]
    public void CmdGivePlayerWeapon(int weaponId, bool equip)
    {
        GivePlayerItem(weaponId, equip);
    }

    [Command]
    public void CmdPickupItem(GameObject itemGameObject)
    {
        UsableItem item = itemGameObject.GetComponent<UsableItem>();

        if (!item.OnGround)
            return;

        bool added = inventory.AddItemToInventory(item.Id, itemGameObject);

        if (added)
        {
            ModifyItemCollidersAndRigidbodyOnPickup(itemGameObject);

            // Pick it up off the ground. Set the parent to our weapon container and update the gun's position and rotation.
            itemGameObject.transform.SetParent(itemContainer.transform);
            itemGameObject.transform.SetPositionAndRotation(itemContainer.position, itemContainer.transform.rotation);
            item.OnGround = false;
            item.HoldingPlayer = this;

            if (item is Gun)
                TargetPlayPickupWeaponSound();
            else
                TargetPlayPickupItemSound();

            itemGameObject.SetActive(false);
        }
    }

    [Command]
    public void CmdTryCycleInventory(int inventoryId)
    {
        int nextItemIndex;

        if (inventoryId == 0)
            nextItemIndex = inventory.GetNextWeaponOfType(Gun.GunType.PRIMARY, CurrentItemId);
        else if (inventoryId == 1)
            nextItemIndex = inventory.GetNextWeaponOfType(Gun.GunType.SECONDARY, CurrentItemId);
        else if (inventoryId == 2)
            nextItemIndex = inventory.GetNextWeaponOfType(Gun.GunType.EXPLOSIVE, CurrentItemId);
        else if (inventoryId == 3)
            nextItemIndex = inventory.GetNextItem(CurrentItemId);
        else
        {
            Debug.LogError("Unknown inventory ID: " + inventoryId);
            return;
        }

        Debug.Log("After cycling inventory, nextItemIndex = " + nextItemIndex + ".");

        if (inventoryId <= 2)
            // Pass the unique ID of the gun stored at the identified index of the player's inventory.
            EquipItem(inventory.GetGunIdAtIndex(nextItemIndex));
        else
            // Pass the unique ID of the item stored at the identified index of the player's inventory.
            EquipItem(inventory.GetItemIdAtIndex(nextItemIndex));
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
        if (CurrentItemId < 0 || CurrentItem == null || !CurrentItem is Gun || (CurrentItem as Gun).AmmoInClip == (CurrentItem as Gun).ClipSize)
            return;

        // Do not try to reload if we don't have any reserve ammo of the correct type.
        if (AmmoCounts[(CurrentItem as Gun).GunClass] <= 0)
            return;

        Debug.Log("Reloading.");
        (CurrentItem as Gun).InitReload();
    }

    [Command]
    private void CmdTryShoot()
    {
        // TODO: Play error sound indicating no weapon? Or just punch?
        if (CurrentItemId < 0 || CurrentItem == null || !CurrentItem is Gun || Player.IsDead)
            return;

        (CurrentItem as Gun).Shoot(this, Player.WeaponMuzzle.transform);
    }

    [Command]
    public void CmdTryDropCurrentItem()
    {
        if (CurrentItemId < 0)
            return;

        // Remove the gun from the player's inventory.
        inventory.RemoveItemFromInventory(CurrentItemId);

        // Instantiate the scene object on the server a bit in front of the player so they don't instantly pick it up.
        Vector3 pos = transform.position + (transform.forward * 2.0f) ;
        Quaternion rot = itemContainer.transform.rotation;

        GameObject currentItemGameObject = CurrentItem.gameObject;
        currentItemGameObject.transform.SetParent(null);
        currentItemGameObject.transform.SetPositionAndRotation(pos, rot);

        // Set the RigidBody as non-kinematic on the server only (isKinematic = true in prefab).
        Array.ForEach(currentItemGameObject.GetComponents<Collider>(), c => c.enabled = true); // Disable the colliders.
        currentItemGameObject.GetComponent<Rigidbody>().isKinematic = false;
        currentItemGameObject.GetComponent<Rigidbody>().detectCollisions = true;

        CurrentItem.HoldingPlayer = null;
        CurrentItem.OnGround = true;

        // Toss it out in front of us a bit.
        currentItemGameObject.GetComponent<Rigidbody>().velocity = transform.forward * 7.0f + transform.up * 5.0f;

        // Set the player's SyncVar to nothing so clients will destroy the equipped child item.
        CurrentItemId = -1;
        CancelReload();
        CurrentItem = null;
    }

    #endregion

    #region Client 

    [Client]
    public void MuzzleFlash()
    {
        if (CurrentItem != null && CurrentItem is Gun)
            (CurrentItem as Gun).MuzzleFlash();
    }

    [Client]
    public void OnDropItemButtonPressed()
    {
        if (CurrentItem == null || CurrentItemId < 0)
            // TODO: Play error sound.
            return;

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("AmmoBox"))
        {
            CmdPickupAmmoBox(other.gameObject);
        }   
        else if (other.gameObject.CompareTag("Item") || other.gameObject.CompareTag("Weapon"))
        {
            CmdPickupItem(other.gameObject);
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

        CameraContainer = Instantiate(CameraPrefab);
        AudioSource = GetComponentInChildren<AudioSource>();
        AudioSource.enabled = true;
        AudioSource.volume = 1.0f;
        Camera = CameraContainer.GetComponentInChildren<Camera>();

        CameraContainer.GetComponentInChildren<AudioListener>().enabled = true;
        Camera.enabled = true;

        CameraContainer.transform.position += transform.position;

        EmergencyButton = GameObject.FindGameObjectWithTag("EmergencyButton");

        //lineRenderer = gameObject.AddComponent<LineRenderer>();
        //lineRenderer.startWidth = 0.025f;
        //lineRenderer.endWidth = 0.025f;
        //lineRenderer.startColor = Color.red;
        //lineRenderer.endColor = Color.red;
        //lineRenderer.positionCount = 2;

        inventory = GetComponent<PlayerInventory>();

        //Material whiteDiffuseMat = new Material(Shader.Find("Unlit/Texture"))
        //{
        //    color = Color.red
        //};
        //lineRenderer.material = whiteDiffuseMat;

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
        if (CurrentItemId >= 0 && CurrentItem != null && !Player.IsDead)
        {
            Debug.Log("Attempting to reload...");
            CmdTryReload();
        }
    }

    [Client]
    internal void DropButton()
    {
        if (CurrentItem != null && CurrentItemId >= 0 && !Player.IsDead)
            CmdTryDropCurrentItem();
    }

    /// <summary>
    /// Updates the ammo values.
    /// </summary>
    [Client]
    public void UpdateAmmoDisplay()
    {
        if (!isLocalPlayer) return;

        if (CurrentItem == null || !CurrentItem is Gun)
        {
            Player.PlayerUI.AmmoClipText.text = "N/A";
            Player.PlayerUI.AmmoReserveText.text = "N/A";
        }
        else
        {
            Player.PlayerUI.AmmoClipText.text = (CurrentItem as Gun).AmmoInClip.ToString();
            Player.PlayerUI.AmmoReserveText.text = AmmoCounts[(CurrentItem as Gun).GunClass].ToString();
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
        if (!isLocalPlayer || !MovementEnabled) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 movement = new Vector3(h, rigidbody.velocity.y, v);

        float itemSpeedModifier = CurrentItem == null ? 1.0f : CurrentItem.SpeedModifier;

        if (sprintEnabled && Input.GetKey(KeyCode.LeftShift))
            movement = movement.normalized * movementSpeed * runBoost * itemSpeedModifier * Time.deltaTime;
        else
            movement = movement.normalized * movementSpeed * itemSpeedModifier * Time.deltaTime;

        rigidbody.MovePosition(transform.position + movement);

        Ray cameraRay = Camera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float rayLength;

        if (groundPlane.Raycast(cameraRay, out rayLength))
        {
            Vector3 pointToLook = cameraRay.GetPoint(rayLength);
            //Debug.DrawLine(cameraRay.origin, pointToLook, Color.blue);

            Vector3 lookAt = new Vector3(pointToLook.x, transform.position.y, pointToLook.z);

            //lineRenderer.SetPosition(0, Player.WeaponMuzzle.transform.position);
            //lineRenderer.SetPosition(1, transform.position + transform.forward * 10);

            transform.LookAt(lookAt);
        }

        UpdateAnimation(movement.normalized);
    }

    [Client]
    public void UpdateAnimation(Vector3 dir)
    {
        if (dir.x == 0f && dir.y == 0f)
        {
            if (Player.IsDead)
            {
                DeadAnimator.SetFloat("LastDirX", lastX);
                DeadAnimator.SetFloat("LastDirY", lastY);
                DeadAnimator.SetBool("Movement", false);
            }
            else
            {
                AliveAnimator.SetFloat("LastDirX", lastX);
                AliveAnimator.SetFloat("LastDirY", lastY);
                AliveAnimator.SetBool("Movement", false);
            }
        }
        else
        {
            lastX = dir.x;
            lastY = dir.y;
            if (Player.IsDead)
                DeadAnimator.SetBool("Movement", true);
            else
                AliveAnimator.SetBool("Movement", true);
        }

        if (Player.IsDead)
        {
            AliveAnimator.SetFloat("DirX", dir.x);
            AliveAnimator.SetFloat("DirY", dir.y);
        }
        else
        {
            DeadAnimator.SetFloat("DirX", dir.x);
            DeadAnimator.SetFloat("DirY", dir.y);
        }
    }

    [Client]
    public void DieFromExplosion(float explosiveForce, float explosionRadius, Vector3 explosionPosition)
    {
        Debug.Log(GetPlayerDebugString() + " is dying from an explosion.");
        Die();

        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rigidbody in rigidbodies)
        {
            rigidbody.AddExplosionForce(explosiveForce * 10, explosionPosition, explosionRadius, 1, ForceMode.Impulse);
        }
    }

    [Client]
    public void Die()
    {
        AliveAnimator.enabled = false;
        DeadAnimator.enabled = true;

        // Enable the ghost.
        DeadPlayerBody.GetComponent<Outline>().OutlineColor = Player.PlayerColor;
        DeadPlayerBody.SetActive(true);

        // Turn the player's living body into a ragdoll now that they've died.
        setRigidbodyState(false, DeadPlayerBody.GetComponentsInChildren<Rigidbody>());
        setColliderState(true, DeadPlayerBody.GetComponentsInChildren<Collider>());

        // Update the outline of the dead body.
        PlayerOutline.OutlineWidth = 8;
        PlayerOutline.OutlineColor = new Color32(245, 233, 66, 255);

        // Detach the old body from the current player so it doesn't follow the ghost around.
        LivingPlayerBody.transform.SetParent(null);

        // Raycast to the ground to create some blood.
        float _raycastDistance = 10f;
        int mask = 1 << 13;    // Ground on layer 10 in the inspector
        Vector3 start = new Vector3(Player.transform.position.x, Player.transform.position.y + 1, Player.transform.position.z);
        if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, _raycastDistance, mask))
        {
            GameObject bloodPool = Instantiate(BloodPoolPrefab); // Client-side only.
            bloodPool.transform.position = new Vector3(hit.point.x, hit.point.y + 0.05f, hit.point.z);
        }

        GameObject deathEffect = Instantiate(DeathEffectPrefab);    // Client-side only.
        deathEffect.transform.position = Player.transform.position;
        Destroy(deathEffect, 1.25f);

        if (isLocalPlayer)
        {
            // Play dead sound.
            AudioSource.PlayOneShot(ImpactSound);

            // Drop your weapon when you die.
            // TODO: Should this only happen for the local player?
            CmdTryDropCurrentItem();
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
    void setRigidbodyState(bool state, Rigidbody[] rigidbodies)
    {
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
    }

    [Client]
    void setColliderState(bool state, Collider[] colliders)
    {
        foreach (Collider collider in colliders)
        {
            collider.enabled = state;
        }
    }

    #endregion

    #region Server

    public override void OnStartServer()
    {
        inventory = GetComponent<PlayerInventory>();
        if (StartingWeaponId >= 0)
            GivePlayerItem(StartingWeaponId, true);
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
    /// Add the specified item to the player's inventory. If the 'equip' flag is set to true,
    /// then also equip the current item (put away whatever item the player may already be holding first).
    /// 
    /// This could also be used to force a player to equip a item in their inventory by specifiying a
    /// gun that they already have while also passing true for 'equip'.
    /// </summary>
    [Server]
    public void GivePlayerItem(int itemId, bool equip)
    {
        if (itemDatabase == null)
            itemDatabase = GameObject.FindGameObjectWithTag("ItemDatabase").GetComponent<ItemDatabase>();

        // Get the gun prefab and instantiate it.
        UsableItem itemPrefab = itemDatabase.GetGunByID(itemId);
        UsableItem instantiatedItem = Instantiate(itemPrefab, itemContainer.position, itemPrefab.transform.rotation, itemContainer);

        ModifyItemCollidersAndRigidbodyOnPickup(instantiatedItem.gameObject);
        instantiatedItem.OnGround = false;
        instantiatedItem.HoldingPlayer = this;

        // Add the item to the player's inventory. 
        inventory.AddItemToInventory(itemId, instantiatedItem.gameObject);

        // Equip the item if we're supposed to. Otherwise disable the game object.
        if (equip)
            EquipItem(itemId);
        else
            instantiatedItem.gameObject.SetActive(false);

        // Spawn it on the server.
        NetworkServer.Spawn(instantiatedItem.gameObject);
    }

    /// <summary>
    /// Check to make sure we have the specified gun in the associated inventory. If so, remove it
    /// from the player's inventory and equip it. Otherwise, return (while possibly logging an error).
    /// 
    /// Again, the given item MUST ALREADY BE IN THE PLAYER'S INVENTORY. Otherwise, this will simply return.
    /// 
    /// If nextItemId is less than zero, then this will simply put the current item away.
    /// </summary>
    [Server]
    public void EquipItem(int nextItemId)
    {
        // Stop the reload in case we switched during a reload.
        if (CurrentItemId > 0)
            CancelReload();

        // Deactivate our current item, if we're holding one.
        if (CurrentItem != null)
            CurrentItem.gameObject.SetActive(false);

        // If the next item ID is negative, then all we need to do is put our current item away.
        // We've already disabled the game object of the currently-equipped item. 
        // Just update the associated state variables.
        if (nextItemId < 0)
        {
            CurrentItemId = -1;
            CurrentItem = null;
            RpcAssignCurrentItem(-1);
            return;
        }

        // Do this check after putting our item away.
        if (!inventory.HasItem(nextItemId))
        {
            Debug.Log("Item with ID " + nextItemId + " not in inventory. Cannot equip.");
            return;
        }

        // Activate the item that we're switching to.
        GameObject nextItemGameObject = inventory.GetItemGameObjectFromInventory(nextItemId);
        nextItemGameObject.SetActive(true);

        // Update our CurrentItem reference.
        CurrentItem = nextItemGameObject.GetComponent<UsableItem>();

        Debug.Log("Server assigned " + GetPlayerDebugString() + " item " + nextItemId + ".");
        CurrentItemId = nextItemId;       // Switch to the item.
        RpcAssignCurrentItem(nextItemId);
    }

    #endregion

    #region Utility 

    /// <summary>
    /// This function should be called on a Gun's game object when it is being picked up.
    /// 
    /// The colliders need to be disabled and the rigid body needs to be set to kinematic.
    /// </summary>
    /// <param name="itemGameObject"></param>
    private void ModifyItemCollidersAndRigidbodyOnPickup(GameObject itemGameObject)
    {
        itemGameObject.GetComponent<Rigidbody>().isKinematic = true;
        itemGameObject.GetComponent<Rigidbody>().detectCollisions = false;
        Array.ForEach(itemGameObject.GetComponents<Collider>(), c => c.enabled = false); // Disable the colliders.
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
