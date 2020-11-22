using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Imposters
{
    /// <summary>
    /// Projectiles can cause direct-hit damage (e.g., crossbow bolts) or explode (rockets, grenades).
    /// </summary>
    public enum DamageType
    {
        DirectHit,
        Explosion
    }

    public class Projectile : NetworkBehaviour
    {
        [Tooltip("The type of damage inflicted by this projectile.")]
        public DamageType DamageType = DamageType.DirectHit;

        [Tooltip("Damage applied if damage type is direct hit (rather than explosion).")]
        public float Damage = 50.0f;

        [Tooltip("The initial force applied to this projectile.")]
        public float InitialForce = 1000.0f;

        [Tooltip("The time, in seconds, that the projectile may exist before exploding.")]
        public float Lifetime = 30.0f;

        [Tooltip("The explosion created by this projectile, if any.")]
        public GameObject ExplosionPrefab;

        [Tooltip("If true, this projectile explodes upon hitting something. Otherwise, it exists until its life timer goes off.")]
        public bool ExplodeOnImpact = true;

        [Tooltip("If true, this will speed up as it travels.")]
        public bool Speedup = false;

        [Tooltip("The factor by which this project is propelled if Speedup is true.")]
        public float SpeedupFactor = 2.0f;

        /// <summary>
        /// We only do direct hit damage the first time this hits something. That way, running  
        /// into a stationary grenade that hasn't gone off yet won't do damage to the player.
        /// </summary>
        private bool firstHit = false;

        private float lifeTimer = 0.0f; // How long this projectile has been alive.

        public override void OnStartClient()
        {
            GetComponent<Rigidbody>().AddRelativeForce(0, 0, InitialForce);
        }

        void Update()
        {
            if (!isLocalPlayer) return;

            // Update the timer
            lifeTimer += Time.deltaTime;

            if (Speedup)
                GetComponent<Rigidbody>().AddRelativeForce(transform.forward * SpeedupFactor);

            // Destroy the projectile if the time is up
            if (lifeTimer >= Lifetime)
            {
                CmdExplode(transform.position);
            }
        }

        void OnCollisionEnter(Collision col)
        {
            // If the projectile collides with something, call the Hit() function
            Hit(col);
        }

        void Hit(Collision col)
        {
            if (ExplodeOnImpact)
                // Make the projectile explode
                CmdExplode(col.contacts[0].point);

            if (!firstHit)
            {
                // Apply damage to the hit object if damageType is set to Direct
                if (DamageType == DamageType.DirectHit)
                {
                    if (col.collider.gameObject.CompareTag("Player"))
                    {
                        col.collider.GetComponent<Player>().CmdDoDamage(Damage);
                    }
                }

                firstHit = true;
            }
        }

        #region Commands 

        [Command]
        public void CmdExplode(Vector3 position)
        {
            if (ExplosionPrefab != null)
            {
                GameObject explosionGameObject = Instantiate(ExplosionPrefab, position, Quaternion.identity);
                NetworkServer.Spawn(explosionGameObject);
            }

            NetworkServer.Destroy(gameObject);
            Destroy(gameObject);
        }

        #endregion 
    }
}