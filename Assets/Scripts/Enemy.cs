using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Enemy : NetworkBehaviour
{
    public float MovementSpeed = 10;

    [SerializeField] private Player target;

    private Rigidbody rigidbody;

    // Start is called before the first frame update
    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (target == null)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

            if (players.Length > 0)
                target = players[0].GetComponent<Player>();
        }

        if (target != null)
        {
            transform.LookAt(target.transform);

            rigidbody.MovePosition(transform.forward * MovementSpeed * Time.deltaTime);
        }
    }
}
