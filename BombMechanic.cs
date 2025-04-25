using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class BombMechanic : MonoBehaviour
{
    [Header("Bomb Settings")]
    public GameObject bombPrefab;
    public float throwForce = 10f;
    public float throwUpwardForce = 2f;
    public float explosionRadius = 5f;
    public float explosionDamage = 100f;
    public float fuseTime = 3f;
    public LayerMask damageLayers;
    
    [Header("References")]
    public Camera playerCamera; // Tambahkan opsi untuk drag kamera secara manual
    public GameObject explosionEffect;
    public PlayerHealth playerHealth; // Referensi ke script PlayerHealth
    
    [Header("Player Damage Settings")]
    public bool canDamagePlayer = true;
    public float playerDamageMultiplier = 0.5f; // Modifier untuk damage ke player (bisa diatur lebih rendah agar tidak terlalu mematikan)
    public float minSelfDamageDistance = 1f; // Jarak minimal untuk self-damage
    
    [Header("Audio")]
    public AudioClip throwSound;
    public AudioClip explosionSound;
    
    [Header("Controls")]
    public KeyCode throwKey = KeyCode.G;
    
    private AudioSource audioSource;
    private bool canThrow = true;
    private float cooldownTime = 2f;
    
    void Start()
    {
        // Coba dapatkan kamera dari reference
        if (playerCamera == null)
        {
            // Jika tidak ada referensi manual, coba gunakan Camera.main
            playerCamera = Camera.main;
            
            // Jika masih null, berikan pesan error
            if (playerCamera == null)
            {
                Debug.LogError("No camera found! Please assign a camera in the Inspector or add a camera with 'MainCamera' tag.");
            }
        }
    
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("No AudioSource component found!");
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Coba dapatkan PlayerHealth jika belum diset
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
            
            // Jika masih null, coba cari di parent/children
            if (playerHealth == null)
            {
                playerHealth = GetComponentInParent<PlayerHealth>();
                
                if (playerHealth == null)
                {
                    playerHealth = FindObjectOfType<PlayerHealth>();
                    
                    if (playerHealth == null)
                    {
                        Debug.LogWarning("PlayerHealth component not found! Player won't take damage from explosions.");
                    }
                }
            }
        }
    }
    
    void Update()
    {
        // Check if player wants to throw a bomb
        if (Input.GetKeyDown(throwKey) && canThrow)
        {
            ThrowBomb();
            StartCoroutine(ThrowCooldown());
        }
    }
    
    void ThrowBomb()
    {
        // Check if bomb prefab is assigned
        if (bombPrefab == null)
        {
            Debug.LogError("Bomb prefab is not assigned!");
            return;
        }
    
        // Check if camera is assigned
        if (playerCamera == null)
        {
            // Coba sekali lagi untuk dapatkan kamera
            playerCamera = Camera.main;
            
            if (playerCamera == null)
            {
                Debug.LogError("Player camera is not found! Please assign a camera in the Inspector.");
                return;
            }
        }

        // Play throw sound
        if (throwSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(throwSound);
        }
        
        // Instantiate bomb
        GameObject bomb = Instantiate(bombPrefab, playerCamera.transform.position + playerCamera.transform.forward * 0.5f, Quaternion.identity);
        
        // Add rigidbody if it doesn't exist
        Rigidbody rb = bomb.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = bomb.AddComponent<Rigidbody>();
        }
        
        // Apply force to throw the bomb
        Vector3 throwDirection = playerCamera.transform.forward * throwForce + Vector3.up * throwUpwardForce;
        rb.AddForce(throwDirection, ForceMode.Impulse);
        
        // Add random rotation for realism
        rb.AddTorque(Random.insideUnitSphere * 10, ForceMode.Impulse);
        
        // Start bomb timer
        StartCoroutine(BombTimer(bomb));
    }
    
    IEnumerator BombTimer(GameObject bomb)
    {
        yield return new WaitForSeconds(fuseTime);
        
        if (bomb != null)
        {
            Vector3 explosionPos = bomb.transform.position;
            
            // Show explosion effect
            if (explosionEffect != null)
            {
                Instantiate(explosionEffect, explosionPos, Quaternion.identity);
            }
            
            // Play explosion sound
            if (explosionSound != null)
            {
                AudioSource.PlayClipAtPoint(explosionSound, explosionPos);
            }
            
            // Damage nearby objects
            Collider[] colliders = Physics.OverlapSphere(explosionPos, explosionRadius, damageLayers);
            foreach (Collider hit in colliders)
            {
                // Apply damage to hit objects
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    // Calculate damage based on distance
                    float distance = Vector3.Distance(explosionPos, hit.transform.position);
                    float damage = explosionDamage * (1 - distance / explosionRadius);
                    damageable.TakeDamage(Mathf.Max(0, damage));
                }
                
                // Apply explosion force to rigidbodies
                Rigidbody hitRb = hit.GetComponent<Rigidbody>();
                if (hitRb != null)
                {
                    hitRb.AddExplosionForce(explosionDamage * 10, explosionPos, explosionRadius, 1f, ForceMode.Impulse);
                }
            }
            
            // Check if player is within explosion radius
            if (canDamagePlayer && playerHealth != null)
            {
                // Calculate distance to player
                float distanceToPlayer = Vector3.Distance(explosionPos, transform.position);
                
                // Only damage player if within explosion radius
                if (distanceToPlayer <= explosionRadius && distanceToPlayer >= minSelfDamageDistance)
                {
                    // Calculate damage based on distance (further = less damage)
                    float damagePercent = 1 - (distanceToPlayer / explosionRadius);
                    float finalDamage = explosionDamage * damagePercent * playerDamageMultiplier;
                    
                    // Apply damage to player
                    playerHealth.TakeDamage(finalDamage);
                    Debug.Log($"Player takes {finalDamage} damage from explosion at distance {distanceToPlayer}");
                }
            }
            
            // Destroy the bomb
            Destroy(bomb);
        }
    }
    
    IEnumerator ThrowCooldown()
    {
        canThrow = false;
        yield return new WaitForSeconds(cooldownTime);
        canThrow = true;
    }
    
    // Optional: Draw gizmos to visualize explosion radius in editor
    void OnDrawGizmosSelected()
    {
        if (bombPrefab != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}

// Interface for objects that can take damage
public interface IDamageable
{
    void TakeDamage(float damage);
}

// Example implementation for enemies
public class DamageableEntity : MonoBehaviour, IDamageable
{
    public float health = 100f;
    
    public void TakeDamage(float damage)
    {
        health -= damage;
        
        if (health <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        // Handle death logic here
        Debug.Log(gameObject.name + " has died");
        
        // Example: Play death animation, drop items, etc.
        Destroy(gameObject, 0.1f);
    }
}

// Implementation for player (connects to PlayerHealth system)
public class PlayerDamageReceiver : MonoBehaviour, IDamageable
{
    public PlayerHealth playerHealth;
    
    private void Start()
    {
        // Auto-find PlayerHealth if not assigned
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
            
            if (playerHealth == null)
            {
                Debug.LogError("PlayerHealth component not found!");
            }
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
    }
}