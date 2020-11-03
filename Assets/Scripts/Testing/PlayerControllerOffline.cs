using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControllerOffline : MonoBehaviour
{
    private Player player;
    private Rigidbody rigidbody;

    public GameObject CameraPrefab;

    public Camera Camera;

    [SerializeField]
    private float RotationSpeed;

    private Vector3 moveInput;

    private Vector3 moveVelocity;

    public float MovementSpeed;

    private float RunBoost = 2.0f;

    public Animator animator;

    public bool CreateOwnCamera;
    public bool RotateToFaceMouse;
    public bool ManipulateCameraPosition;

    private bool isDead = false;

    public GameObject DeathEffectPrefab;
    public GameObject BloodPoolPrefab;

    public GameObject ChestGameObject;

    public Vector3 jump;
    public float jumpForce = 2.0f;
    public bool isGrounded;

    // Start is called before the first frame update
    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();

        if (CreateOwnCamera)
        {
            GameObject gameObject = Instantiate(CameraPrefab);
            Camera = gameObject.GetComponent<Camera>();
            AudioListener audioListener = gameObject.GetComponent<AudioListener>();
            audioListener.enabled = true;
            Camera.enabled = true;
        }

        jump = new Vector3(0.0f, 2.0f, 0.0f);

        setRigidbodyState(true);
        setColliderState(false);
    }

    void OnCollisionStay()
    {
        isGrounded = true;
    }

    void OnCollisionExit()
    {
        isGrounded = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.B) && !isDead)
            Die();

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rigidbody.AddForce(jump * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        animator.enabled = false;

        setRigidbodyState(false);
        setColliderState(true);

        float _raycastDistance = 10f;
        Vector3 dir = new Vector3(0, -1, 0);
        Debug.DrawRay(transform.position, dir * _raycastDistance, Color.green);

        int mask = 1 << 13;    // Ground on layer 10 in the inspector

        RaycastHit hit;
        if (Physics.Raycast(ChestGameObject.GetComponent<Transform>().position,
            Vector3.down,
            out hit,
            _raycastDistance,
            mask))
        {
            GameObject bloodPool = Instantiate(BloodPoolPrefab);
            bloodPool.transform.position = new Vector3(hit.point.x, hit.point.y + 0.05f, hit.point.z);
        } else
        {
            Debug.Log("No hit.");
        }

        GameObject deathEffect = Instantiate(DeathEffectPrefab);
        deathEffect.transform.position = GetComponent<Transform>().position;
        //Destroy(deathEffect, 3.0f);

        enabled = false;
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 movement = new Vector3(h, 0, v);

        if (Input.GetKey(KeyCode.LeftShift))
        {
            movement = movement.normalized * MovementSpeed * RunBoost * Time.deltaTime;

            animator.SetBool("running", true);
        }
        else
        {
            movement = movement.normalized * MovementSpeed * Time.deltaTime;

            animator.SetBool("running", false);
        }
        
        animator.SetFloat("moving", movement.magnitude);
        
        rigidbody.MovePosition(transform.position + movement);

        if (!RotateToFaceMouse) return;

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
        if (Camera != null && ManipulateCameraPosition)
        {
            Camera.transform.position = this.transform.position + new Vector3(0, 8, -4);
        }
    }

    void setRigidbodyState(bool state)
    {
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rigidbody in rigidbodies)
        {
            rigidbody.isKinematic = state;
        }

        GetComponent<Rigidbody>().isKinematic = !state;
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
