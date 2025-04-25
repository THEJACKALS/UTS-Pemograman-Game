using UnityEngine;

public class BulletController : MonoBehaviour
{
    public float damage = 10f;
    public float lifeTime = 5f;
    public GameObject impactEffect;
    public GameObject shooter;
    
    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Don't damage the shooter
        if (collision.gameObject == shooter)
        {
            return;
        }
        
        // Check if hit damageable object
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
        
        // Create impact effect
        if (impactEffect != null)
        {
            GameObject impact = Instantiate(impactEffect, collision.contacts[0].point, Quaternion.LookRotation(collision.contacts[0].normal));
            Destroy(impact, 2f);
        }
        
        // Destroy bullet
        Destroy(gameObject);
    }
}