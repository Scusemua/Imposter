using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Quaternion rotation;

    // Start is called before the first frame update
    void Start()
    {
        rotation = Quaternion.Euler(30, 0, 0);

        Debug.Log("Initial camera rotation: " + rotation);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void LateUpdate()
    {
        transform.rotation = rotation;
    }
}
