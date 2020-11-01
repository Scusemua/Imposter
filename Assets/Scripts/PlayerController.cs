using UnityEngine;
using Mirror;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Player))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField]
    private float walkingSpeed;      // Walking speed.

    [SerializeField]
    private float runMultiplier;     // How much the player's speed gets increased by running.

    private Player player;

    private Vector3 previousInput = Vector3.zero;

    public override void OnStartAuthority()
    {
        enabled = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<Player>();

        if (player == null)
            Debug.LogError("Player is null for PlayerController!");
    }

    // Update is called once per frame
    [ClientCallback]
    void Update()
    {
        if (!hasAuthority) return;

        if (!player.isDead)
        {
            float horizontalMovement = Input.GetAxis("Horizontal");
            float verticalMovement = Input.GetAxis("Vertical");

            float movementSpeed = walkingSpeed;
            if (Input.GetKey(KeyCode.LeftShift))
                movementSpeed *= runMultiplier;

            Move(horizontalMovement, verticalMovement, movementSpeed);
        }
    }

    [Client]
    private void Move(float horizontalMovement, float verticalMovement, float movementSpeed)
    {
        //GetComponent<Rigidbody>().velocity = new Vector3(horizontalMovement * movementSpeed, verticalMovement * movementSpeed, 0);

        Vector3 right = transform.right;
        Vector3 forward = transform.forward;

        right.y = 0f;
        forward.y = 0f;

        Vector3 movement = right.normalized * previousInput.x + forward.normalized * previousInput.y;

        transform.position += transform.TransformDirection(movement * movementSpeed * Time.deltaTime);

        transform.position += transform.TransformDirection(new Vector3(horizontalMovement * movementSpeed, verticalMovement * movementSpeed, 0));

        previousInput = movement;
    }
}
