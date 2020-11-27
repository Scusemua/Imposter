using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingObject : MonoBehaviour
{
    [Tooltip("The speed at which the object rotates.")]
    public float RotateSpeed = 5.0f;

    private Vector3 yAxis = new Vector3(0, 1, 0);

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(yAxis, RotateSpeed * Time.deltaTime);
    }
}
