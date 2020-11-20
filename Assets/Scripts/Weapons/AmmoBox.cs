using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoBox : MonoBehaviour
{
    [Header("Stats")]
    public GunType AssociatedGunType;   // What ammo (gun) type will this refill?
    public int NumberBullets;           // How many bullets will the player get from picking up this ammo box?

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
