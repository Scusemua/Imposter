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

    // Start is called before the first frame update
    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();

        GameObject gameObject = Instantiate(CameraPrefab);
        Camera = gameObject.GetComponent<Camera>();
        AudioListener audioListener = gameObject.GetComponent<AudioListener>();
        audioListener.enabled = true;
        Camera.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {

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

        Camera.transform.position += movement;

        Ray cameraRay = Camera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float rayLength;

        if (groundPlane.Raycast(cameraRay, out rayLength))
        {
            Vector3 pointToLook = cameraRay.GetPoint(rayLength);
            Debug.DrawLine(cameraRay.origin, pointToLook, Color.blue);

            Vector3 lookAt = new Vector3(pointToLook.x, transform.position.y, pointToLook.z);

            transform.LookAt(lookAt);

            if (Vector3.Dot(lookAt, movement) < 0)
                animator.SetBool("backwards", true);
            else
                animator.SetBool("backwards", false);
        }
    }
}
