using UnityEngine;
using Mirror;

public class PlayerController : NetworkBehaviour
{
    private Player player;
    private Rigidbody rigidbody;

    public AudioClip deathSound;
    public AudioSource audioSource;

    public GameObject CameraPrefab;
    public Camera Camera;

    [SerializeField]
    private float RotationSpeed;

    private Vector3 moveInput;

    private Vector3 moveVelocity;

    public Animator animator;

    public GameObject DeathEffectPrefab;
    public GameObject BloodPoolPrefab;

    private float movementSpeed;
    private float runBoost;
    private bool sprintEnabled;

    private GameOptions gameOptions;

    public Vector3 CameraOffset;
    
    //public Vector3 jump;
    //public float jumpForce = 2.0f;
    //public bool isGrounded = true;

    public override void OnStartLocalPlayer()
    {
        enabled = true;
        //jump = new Vector3(0.0f, 2.0f, 0.0f);
        GetComponent<Rigidbody>().isKinematic = false;
    }

    public override void OnStartAuthority()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    public override void OnStartClient()
    {
        if (!isLocalPlayer)
        {
            // Disable player movement.
            enabled = false;
        }
        else
        {
            GameObject cameraObject = Instantiate(CameraPrefab);
            audioSource = GetComponent<AudioSource>();
            audioSource.enabled = true;
            Camera = cameraObject.GetComponent<Camera>();

            if (hasAuthority)
            {
                cameraObject.GetComponent<AudioListener>().enabled = true;
                Camera.enabled = true;

                Camera.transform.position += transform.position;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<Player>();
        gameOptions = GameOptions.singleton;

        if (player == null)
            Debug.LogError("Player is null for PlayerController!");

        movementSpeed = gameOptions.playerSpeed;
        runBoost = gameOptions.sprintBoost;
        sprintEnabled = gameOptions.sprintEnabled;

        // Configure ragdoll.
        setRigidbodyState(true);
        setColliderState(false);
    }

    //void OnCollisionStay()
    //{
    //    isGrounded = true;
    //}

    //void OnCollisionExit()
    //{
    //    isGrounded = false;
    //}

    // Update is called once per frame
    [ClientCallback]
    void FixedUpdate()
    {
        if (!isLocalPlayer || player.isDead) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(KeyCode.B) && !player.isDead)
        {
            player.CmdSuicide();
        }

        Vector3 movement = new Vector3(h, 0, v);

        if (sprintEnabled && Input.GetKey(KeyCode.LeftShift))
        {
            movement = movement.normalized * movementSpeed * runBoost * Time.deltaTime;

            animator.SetBool("running", true);
        }
        else
        {
            movement = movement.normalized * movementSpeed * Time.deltaTime;

            animator.SetBool("running", false);
        }

        animator.SetFloat("moving", movement.magnitude);

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
                animator.SetBool("backwards", true);
            else
                animator.SetBool("backwards", false);
        }

        //if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        //{
        //    rigidbody.AddForce(jump * jumpForce, ForceMode.Impulse);
        //    isGrounded = false;
        //}
    }

    void LateUpdate()
    {
        if (!isLocalPlayer) return;

        if (Camera != null && Camera.enabled)
        {
            Camera.transform.position = transform.position + CameraOffset;
        }
    }

    [Client]
    public void Die()
    {
        animator.enabled = false;

        setRigidbodyState(false);
        setColliderState(true);
        
        if (hasAuthority && isLocalPlayer)
            audioSource.PlayOneShot(deathSound);

        float _raycastDistance = 10f;
        Vector3 dir = new Vector3(0, -1, 0);
        Debug.DrawRay(player.transform.position, dir * _raycastDistance, Color.green);

        int mask = 1 << 13;    // Ground on layer 10 in the inspector

        if (isLocalPlayer)
        {
            Vector3 start = new Vector3(player.transform.position.x, player.transform.position.y + 1, player.transform.position.z);
            if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, _raycastDistance, mask))
            {
                GameObject bloodPool = Instantiate(BloodPoolPrefab);
                bloodPool.transform.position = new Vector3(hit.point.x, hit.point.y + 0.05f, hit.point.z);
            }

            GameObject deathEffect = Instantiate(DeathEffectPrefab);
            deathEffect.transform.position = player.transform.position;
            Destroy(deathEffect, 1.25f);
        }
        //GameObject bloodPool = Instantiate(BloodPoolPrefab);
        //deathEffect.transform.position = player.transform.position;
    }

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

    void setColliderState(bool state)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider collider in colliders)
        {
            collider.enabled = state;
        }

        GetComponent<Collider>().enabled = !state;
    }
}
