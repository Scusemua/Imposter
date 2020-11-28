using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    public bool CrosshairEnabled = true;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;
        GetComponent<Image>().enabled = true;
    }

    void Alive()
    {
        Cursor.visible = false;
        GetComponent<Image>().enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (CrosshairEnabled)
            transform.position = Input.mousePosition;
    }

    /// <summary>
    /// Set the crosshair to on or off, depending on the parameter.
    /// </summary>
    public void ToggleCrosshair(bool on)
    {
        if (on)
            EnableCrosshair();
        else
            DisableCrosshair();
    }

    /// <summary>
    /// Turn the crosshair on.
    /// </summary>
    public void EnableCrosshair()
    {
        CrosshairEnabled = true;
        Cursor.visible = false;
        GetComponent<Image>().enabled = true;
    }

    /// <summary>
    /// Turn the crosshair off.
    /// </summary>
    public void DisableCrosshair()
    {
        CrosshairEnabled = false;
        Cursor.visible = true;
        GetComponent<Image>().enabled = false;
    }
}
