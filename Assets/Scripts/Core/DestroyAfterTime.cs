using UnityEngine;
using System.Collections;
using Mirror;

public class DestroyAfterTime : NetworkBehaviour
{
    public float lifeTime = 10.0f;

    // Use this for initialization
    public override void OnStartServer()
    {
        StartCoroutine(DestroyAfterInterval());
    }

    IEnumerator DestroyAfterInterval()
    {
        yield return new WaitForSeconds(lifeTime);

        NetworkServer.Destroy(gameObject);
        Destroy(gameObject);
    }
}
