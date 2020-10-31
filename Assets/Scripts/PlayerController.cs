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
        GetComponent<Rigidbody2D>().velocity = new Vector2(horizontalMovement * movementSpeed, verticalMovement * movementSpeed);
    }
}
