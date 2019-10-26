using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TankDemoML {
    /// <summary>
    /// Replacement for ShellExplosion.
    /// We need the shell to know who fired it and who it hit,
    /// so we can reward/penalize agents appropriately
    /// </summary>
    public class SmartShell : MonoBehaviour
    {
        public Action<SmartShell, Dictionary<int, float>> OnExplode;  // send damage report to listeners

        public LayerMask tankLayer;
        public ParticleSystem explosionParticles;
        public float maxDamage = 100f;
        public float explosionForce = 1000f;
        public float maxLifetime = 2f;
        public float explosionRadius = 5f;
        public int owner;

        private Dictionary<int, float> damages = new Dictionary<int, float>();
        private const string LOGTAG = nameof(SmartShell);


        void Start()
        {
            Destroy(gameObject, maxLifetime);  // destroy after lifetime expires
        }

        private void OnTriggerEnter(Collider other)
        {
            damages.Clear();
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, tankLayer);
            foreach(var collider in colliders)
            {
                Rigidbody targetRigidbody = collider.GetComponent<Rigidbody>();
                if (!targetRigidbody) continue;

                targetRigidbody.AddExplosionForce(explosionForce, transform.position, explosionRadius);

                SimpleTankAgent agent = targetRigidbody.GetComponent<SimpleTankAgent>();
                if (!agent) continue;

                float damage = CalculateDamage(targetRigidbody.position);
                agent.TakeDamage(damage);
                damages.Add(agent.id, damage);
            }

            // send out damage report to listeners
            if (OnExplode != null) OnExplode(this, damages);

            explosionParticles.transform.parent = null;
            explosionParticles.Play();
            ParticleSystem.MainModule mainModule = explosionParticles.main;
            Destroy(explosionParticles.gameObject, mainModule.duration);
            Destroy(gameObject);
        }

        private float CalculateDamage(Vector3 targetPosition)
        {
            Vector3 explosionToTarget = targetPosition - transform.position;
            float explosionDistance = explosionToTarget.magnitude;
            float relativeDistance = (explosionRadius - explosionDistance) / explosionRadius;
            float damage = relativeDistance * maxDamage;
            damage = Mathf.Max(0f, damage);
            return damage;
        }
    }
}
