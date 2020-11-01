using UnityEngine;
using Mirror;

public class PlayerController : NetworkBehaviour
{
    private Player player;
    private Rigidbody rigidbody;

    public GameObject CameraPrefab;
    public Camera Camera;

    [SerializeField]
    private float RotationSpeed;

    private Vector3 moveInput;

    private Vector3 moveVelocity;

    public Animator animator;

    private float movementSpeed;
    private float runBoost;
    private bool sprintEnabled;

    private GameOptions gameOptions;

    public Vector3 CameraOffset;

    public override void OnStartAuthority()
    {
        enabled = true;
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
    }

    // Update is called once per frame
    [ClientCallback]
    void FixedUpdate()
    {
        if (!hasAuthority) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

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
    }

    void LateUpdate()
    {
        if (!hasAuthority) return;

        if (Camera != null && Camera.enabled)
        {
            Camera.transform.position = transform.position + CameraOffset;
        }
    }
}
