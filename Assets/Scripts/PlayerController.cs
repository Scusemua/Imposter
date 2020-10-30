using UnityEngine;
using Mirror;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(ConfigurableJoint))]
public class PlayerController : NetworkBehaviour
{
   [SerializeField]
   private float walkingSpeed;      // Walking speed.

   [SerializeField]
   private float runMultiplier;     // How much the player's speed gets increased by running.

   // Start is called before the first frame update
   void Start()
   {
        
   }

    // Update is called once per frame
    void Update()
    {
        
    }

   void FixedUpdate()
   {
      if (this.isLocalPlayer)
      {
         float horizontalMovement = Input.GetAxis("Horizontal");
         float verticalMovement = Input.GetAxis("Vertical");

         float movementSpeed = walkingSpeed;

         if (Input.GetKey(KeyCode.LeftShift))
         {
            movementSpeed *= runMultiplier;
         }

         GetComponent<Rigidbody2D>().velocity = new Vector2(horizontalMovement * movementSpeed, verticalMovement * movementSpeed);
      }
   }
}
