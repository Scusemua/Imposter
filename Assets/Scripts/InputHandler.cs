using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class InputHandler : MonoBehaviour
{
    public Vector2 InputVector { get; set; }

    public Vector3 MousePosition { get; set; }

    // Update is called once per frame
    void Update()
    {
        //if (!hasAuthority) return;

        var h = Input.GetAxis("Horizontal");
        var v = Input.GetAxis("Vertical");
        InputVector = new Vector2(h, v);

        MousePosition = Input.mousePosition;
    }
}
