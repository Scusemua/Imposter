﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Imposters
{
    public class Explosion : NetworkBehaviour
    {
        [Tooltip("The force with which nearby objects will be blasted outwards")]
        public float ExplosionForce = 5.0f;

        [Tooltip("The radius of the explosion")]
        public float ExplosionRadius = 10.0f;

        [Tooltip("Give camera shaking effects to nearby cameras that have the vibration component")]
        public bool ShakeCamera = true;

        [Tooltip("The amount of the camera shake effect")]
        public float CameraShakeAmount = 0.5f;

        [Tooltip("Whether or not the explosion should apply damage to nearby GameObjects with the Heatlh component")]
        public bool CausesDamage = true;

        [Tooltip("The multiplier by which the ammount of damage to be applied is determined")]
        public float Damage = 10.0f;

        // Start is called before the first frame update
        public override void OnStartClient()
        {
            StartCoroutine(DoExplosion());
        }

        public IEnumerator DoExplosion()
        {
            // Wait one frame so that explosion force will be applied to debris which
            // might not yet be instantiated
            yield return null;

            // An array of nearby colliders
            Collider[] cols = Physics.OverlapSphere(transform.position, ExplosionRadius);

            // Apply damage to any nearby GameObjects with the Health component
            if (CausesDamage)
            {
                foreach (Collider col in cols)
                {
                    if (col.gameObject.CompareTag("Player"))
                    {
                        float damageAmount = Damage * (1 / Vector3.Distance(transform.position, col.transform.position));

                        // The Easy Weapons health system
                        col.GetComponent<Player>().Damage(damageAmount);

                        float shakeAmount = 1 / (Vector3.Distance(transform.position, col.transform.position) * CameraShakeAmount);
                        col.GetComponent<Player>().TargetDoCameraShake(shakeAmount);
                    }
                }
            }

            // A list to hold the nearby rigidbodies
            List<Rigidbody> rigidbodies = new List<Rigidbody>();

            foreach (Collider col in cols)
            {
                // Get a list of the nearby rigidbodies
                if (col.attachedRigidbody != null && !rigidbodies.Contains(col.attachedRigidbody))
                {
                    rigidbodies.Add(col.attachedRigidbody);
                }
            }

            foreach (Rigidbody rb in rigidbodies)
            {
                rb.AddExplosionForce(ExplosionForce, transform.position, ExplosionRadius, 1, ForceMode.Impulse);
            }
        }
    }
}