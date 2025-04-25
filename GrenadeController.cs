using UnityEngine;
using System.Collections;

public class GrenadeController : MonoBehaviour
{
    public float explosionDamage = 100f;
    public float explosionRadius = 5f;
    public float fuseTime = 3f;
    public GameObject explosionEffect;
    public AudioClip explosionSound;
    public GameObject thrower;
    public LayerMask damageLayers;
    
    private bool hasExploded = false;
    
    private void Start()
    {
        StartCoroutine(ExplosionCountdown());
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Play bounce sound if you have one
    }
    
    IEnumerator ExplosionCountdown()
    {
        yield return new WaitForSeconds(fuseTime);
        Explode();
    }
    
    public void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        
        // Show explosion effect
        if (explosionEffect != null)
        {
            GameObject explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(explosion, 3f);
        }
        
        // Play explosion sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }
        
        // Damage nearby objects
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, damageLayers);
        foreach (Collider hit in colliders)
        {
            // Don't damage thrower (or reduce damage)
            if (hit.gameObject == thrower)
            {
                continue;
            }
            
            // Apply damage to hit objects
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Calculate damage based on distance
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                float damage = explosionDamage * (1 - distance / explosionRadius);
                damageable.TakeDamage(Mathf.Max(0, damage));
            }
            
            // Apply explosion force to rigidbodies
            Rigidbody hitRb = hit.GetComponent<Rigidbody>();
            if (hitRb != null)
            {
                hitRb.AddExplosionForce(explosionDamage * 10, transform.position, explosionRadius, 1f, ForceMode.Impulse);
            }
        }
        
        // Destroy the grenade
        Destroy(gameObject);
    }
    
    // Helper for visualizing in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}