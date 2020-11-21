﻿using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;

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
    
    private List<Gun> primaryInventory = new List<Gun>();
    private List<Gun> secondaryInventory = new List<Gun>();
    private List<Gun> meleeInventory = new List<Gun>();
    private List<Gun> explosiveInventory = new List<Gun>();
    private List<Gun> grenadeInventory = new List<Gun>();

    private ItemDatabase itemDatabase;

    /// <summary>
    /// Refer to weapons by their ID.
    /// </summary>
    [SyncVar]
    public int CurrrentGunID;

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
    }

    internal void ShootWeapon()
    {
        if (AmmoInGun > 0 && !Player.IsDead && curCooldown <= 0.01)
        {
            //Do command
            CmdTryShoot();
            curCooldown = CurrentWeapon.WeaponCooldown;
        }
        //else if (AmmoCount <= 0)
        //    ReloadButton();
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
    void RpcPlayerFiredEntity(uint shooterID, uint targetID, Vector3 impactPos, Vector3 impactRot)
    {
        //Instantiate(bulletHolePrefab, impactPos + impactRot * 0.1f, Quaternion.LookRotation(impactRot), NetworkIdentity.spawned[targetID].transform);
        //Instantiate(bulletBloodFXPrefab, impactPos, Quaternion.LookRotation(impactRot));
        NetworkIdentity.spawned[shooterID].GetComponent<Player>().MuzzleFlash();

        Transform shooterTransform = NetworkIdentity.spawned[shooterID].GetComponent<Player>().GetComponent<Transform>();

        float volumeDistModifier = (1000f - GetDistanceSquaredToTarget(shooterTransform)) / 1000f;
        Debug.Log("Playing gunshot with volume modifier: " + volumeDistModifier);
        // Adjust volume of gunshot based on distance.
        this.AudioSource.PlayOneShot(Gunshot, volumeDistModifier);

        //Destroy(bulletBloodGO, 2);
        //Destroy(bulletHolePrefab, 10);
    }

    [ClientRpc]
    void RpcPlayerFired(uint shooterID, Vector3 impactPos, Vector3 impactRot)
    {
        Instantiate(bulletHolePrefab, impactPos + impactRot * 0.1f, Quaternion.LookRotation(impactRot));
        Instantiate(bulletFXPrefab, impactPos, Quaternion.LookRotation(impactRot));
        NetworkIdentity.spawned[shooterID].GetComponent<Player>().MuzzleFlash();

        Transform shooterTransform = NetworkIdentity.spawned[shooterID].GetComponent<Player>().GetComponent<Transform>();

        float volumeDistModifier = (1000f - GetDistanceSquaredToTarget(shooterTransform)) / 1000f;
        Debug.Log("Playing gunshot with volume modifier: " + volumeDistModifier);

        // Adjust volume of gunshot based on distance.
        this.AudioSource.PlayOneShot(Gunshot, volumeDistModifier);

        //Destroy(bulletFxPrefab, 2);
        //Destroy(bulletHolePrefab, 10);
    }

    #endregion 

    #region Target RPC

    [TargetRpc]
    void TargetShoot()
    {
        // Update the ammo count on the player's screen.
        Player.PlayerUI.AmmoClipText.text = AmmoInGun.ToString();
    }

    [TargetRpc]
    void TargetReload()
    {
        //We reloaded successfully.
        //Update UI
        Player.PlayerUI.AmmoClipText.text = AmmoInGun.ToString();
        Player.PlayerUI.AmmoReserveText.text = ammoCounts[CurrentWeapon.GunClass].ToString();
    }

    #endregion

    #region Commands 

    [Command]
    void CmdTryReload()
    {
        if (CurrentWeapon == null || Reloading || AmmoInGun == CurrentWeapon.ClipSize)
            return;

        StartCoroutine(reloadingWeapon());
    }

    [Command]
    void CmdTryShoot()
    {
        // TODO: Play error sound indicating no weapon? Or just punch?
        if (CurrentWeapon == null)
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
                Debug.Log("SERVER: Player shot: " + hit.collider.name);
                if (hit.collider.CompareTag("Player"))
                {
                    RpcPlayerFiredEntity(GetComponent<NetworkIdentity>().netId, hit.collider.GetComponent<NetworkIdentity>().netId, hit.point, hit.normal);
                    if (hit.collider.GetComponent<NetworkIdentity>().netId != GetComponent<NetworkIdentity>().netId)
                        hit.collider.GetComponent<Player>().Damage(CurrentWeapon.Damage, GetComponent<NetworkIdentity>().netId);
                }
                else if (hit.collider.CompareTag("Enemy"))
                    RpcPlayerFiredEntity(GetComponent<NetworkIdentity>().netId, hit.collider.GetComponent<NetworkIdentity>().netId, hit.point, hit.normal);
                else
                    RpcPlayerFired(GetComponent<NetworkIdentity>().netId, hit.point, hit.normal);
            }
        }

    }

    [Command]
    public void CmdDropWeapon()
    {
        // Instantiate the scene object on the server
        Vector3 pos = weaponContainer.transform.position;
        Quaternion rot = weaponContainer.transform.rotation;
        Gun droppedWeapon = Instantiate(itemDatabase.GetGunByID(CurrentWeaponID), pos, rot);
        droppedWeapon.OnGround = true;

        // Set the RigidBody as non-kinematic on the server only (isKinematic = true in prefab).
        droppedWeapon.GetComponent<Rigidbody>().isKinematic = false;

        // Toss it out in front of us a bit.
        droppedWeapon.GetComponent<Rigidbody>().velocity = transform.forward * 5.0f;

        // Set the player's SyncVar to nothing so clients will destroy the equipped child item.
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
            Destroy(CurrentWeapon.gameObject);

        // The player could've put away all their weapons, meaning the new ID would be -1.
        if (_New >= 0)
            AssignWeapon(_New);
    }

    [Client]
    public void OnReloadingStateChanged(bool _Old, bool _New)
    {
        if (!isLocalPlayer) return;

        if (_New)
            AudioSource.PlayOneShot(ReloadSound);
    }

    public override void OnStartLocalPlayer()
    {
        Debug.Log("OnStartLocalPlayer() called for Player " + netId);
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

        itemDatabase = GameObject.FindGameObjectWithTag("ItemDatabase").GetComponent<ItemDatabase>();

        if (CurrentWeaponID >= 0 && CurrentWeapon == null)
            AssignWeapon(CurrentWeaponID);

    }

    /// <summary>
    /// Called when a player's dead body gets identified.
    /// </summary>
    public void OnPlayerBodyIdentified(bool _Old, bool _New)
    {
        // If the player's body has been identified, then disable the outline.
        if (Identified) this.PlayerOutline.enabled = false;
    }

    public override void OnStartAuthority()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    [Client]
    public void AssignWeapon(int id)
    {
        Debug.Log("Assigning weapon " + id + " to player now.");
        CurrentWeapon = Instantiate(itemDatabase.GetGunByID(id), weaponContainer).GetComponent<Gun>();
        CurrentWeapon.OnGround = false;
    }

    [Client]
    internal void ReloadButton()
    {
        if (!Reloading || AmmoInGun != CurrentWeapon.ClipSize)
        {
            Debug.Log("Attempting to reload...");
            CmdTryReload();
        }
    }

    /// <summary>
    /// Updates the ammo values.
    /// </summary>
    [Client]
    public void UpdateAmmoDisplay()
    {
        if (!isLocalPlayer) return;

        Player.PlayerUI.AmmoClipText.text = AmmoInGun.ToString();
        Player.PlayerUI.AmmoReserveText.text = ammoCounts[CurrentWeapon.GunClass].ToString();
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

        float weaponSpeedModifier = 1.0f;
        if (CurrentWeaponID > 0) // If we have a weapon equipped...
            weaponSpeedModifier = itemDatabase.GetGunByID(CurrentWeaponID).SpeedModifier;

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

        //Vector3 rightMovement = Vector3.right * movement;
        //Vector3 upMovement = Vector3.up * movement;
        //Vector3 heading = Vector3.Normalize(upMovement + rightMovement);
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

        this.PlayerOutline.enabled = true;

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
        print("Giving player gun id=0.");
        CurrentWeaponID = 3;
    }

    [Server]
    IEnumerator reloadingWeapon()
    {
        if (CurrentWeapon == null)
            yield return null;
        else if (ammoCounts[CurrentWeapon.GunClass] <= 0)
        {
            ammoCounts[CurrentWeapon.GunClass] = 0;
            yield return null;
        }
        else
        {
            int currentWeaponId = CurrentWeapon.Id;
            Reloading = true;
            yield return new WaitForSeconds(CurrentWeapon.ReloadTime);

            // Make sure player hasn't switched guns to get away with any funny business.
            if (CurrentWeapon == null || CurrentWeapon.Id != currentWeaponId)
                yield return null;
            else
            {
                // Determine how much ammo the player is missing.
                // If we have enough ammo in reserve to top off the clip/magazine,
                // then do so. Otherwise, put back however much ammo we have left.
                int ammoMissing = CurrentWeapon.ClipSize - AmmoInGun;

                // Remove from our reserve ammo whatever we loaded into the weapon.
                if (ammoCounts[CurrentWeapon.GunClass] - ammoMissing >= 0)
                {
                    AmmoInGun += ammoMissing;
                    ammoCounts[CurrentWeapon.GunClass] -= ammoMissing;
                }
                else
                {
                    // We do not have enough ammo to completely top off the magazine.
                    // Just add what we have.
                    AmmoInGun += ammoCounts[CurrentWeapon.GunClass];
                    ammoCounts[CurrentWeapon.GunClass] = 0;
                }

                TargetReload();
                Reloading = false;

                yield return null;
            }
        }
    }

    #endregion 
}
