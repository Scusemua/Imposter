using UnityEngine;
using Mirror;

public class PlayerController : NetworkBehaviour
{
    public Player Player;
    private Rigidbody rigidbody;

    [Header("Audio")]
    public AudioClip DeathSound;
    public AudioClip ImposterStart;
    public AudioClip CrewmateStart;
    public AudioClip ImpactSound;
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

    private float movementSpeed;
    private float runBoost;
    private bool sprintEnabled;

    private GameOptions GameOptions { get => GameOptions.singleton; }

    public Vector3 CameraOffset;

    /// <summary>
    /// Displayed around a deadbody that has not yet been identified.
    /// </summary>
    public Outline PlayerOutline;

    [SyncVar(hook = nameof(OnPlayerBodyIdentified))]
    public bool Identified;
    
    void LateUpdate()
    {
        if (!isLocalPlayer) return;

        if (Camera != null && Camera.enabled)
        {
            Camera.transform.position = transform.position + CameraOffset;
        }
    }

    #region Client 

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
        Camera = cameraObject.GetComponent<Camera>();

        cameraObject.GetComponent<AudioListener>().enabled = true;
        Camera.enabled = true;

        Camera.transform.position += transform.position;

        EmergencyButton = GameObject.FindGameObjectWithTag("EmergencyButton");
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
    /// <summary>
    /// Play the Imposter start-of-game sound.
    /// </summary>
    public void PlayImposterStart()
    {
        Debug.Log("Playing Imposter start sound.");
        AudioSource.PlayOneShot(ImposterStart);
    }

    [Client]
    /// <summary>
    /// Play the Crewmate start-of-game sound.
    /// </summary>
    public void PlayCrewmateStart()
    {
        Debug.Log("Playing Crewmate start sound.");
        AudioSource.PlayOneShot(CrewmateStart);
    }

    [Client]
    /// <summary>
    /// Play the impact sound (generally played on-death and for the Imposter who killed the player).
    /// </summary>
    public void PlayImpactSound()
    {
        Debug.Log("Playing impact sound.");
        AudioSource.PlayOneShot(ImpactSound, 1);
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

        if (sprintEnabled && Input.GetKey(KeyCode.LeftShift))
        {
            movement = movement.normalized * movementSpeed * runBoost * Time.deltaTime;

            Animator.SetBool("running", true);
        }
        else
        {
            movement = movement.normalized * movementSpeed * Time.deltaTime;

            Animator.SetBool("running", false);
        }

        if (Input.GetKey(KeyCode.P))
        {
            PlayImpactSound();
        }

        Animator.SetFloat("moving", movement.magnitude);

        rigidbody.MovePosition(transform.position + movement);

        Ray cameraRay = Camera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float rayLength;

        if (groundPlane.Raycast(cameraRay, out rayLength))
        {
            Vector3 pointToLook = cameraRay.GetPoint(rayLength);
            //Debug.DrawLine(cameraRay.origin, pointToLook, Color.blue);

            Vector3 lookAt = new Vector3(pointToLook.x, transform.position.y, pointToLook.z);

            transform.LookAt(lookAt);

            if (Vector3.Dot(lookAt, movement) < 0)
                Animator.SetBool("backwards", true);
            else
                Animator.SetBool("backwards", false);
        }
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

    #endregion

    #region Server
    
    #endregion 
}
