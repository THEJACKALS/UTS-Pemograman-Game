using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float bulletSpeed = 50f;
    public float maxLifetime = 3f;
    public GameObject bulletHolePrefab;  // Prefab untuk bullet hole
    public float bulletHoleOffset = 0.01f;  // Sedikit offset agar tidak z-fighting

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Pastikan ada Rigidbody
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Setting Rigidbody untuk peluru
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Penting untuk mencegah tunneling
        rb.velocity = transform.forward * bulletSpeed;
        
        // Destroy peluru setelah waktu tertentu untuk optimasi
        Destroy(gameObject, maxLifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Dapatkan informasi kontak pertama
        ContactPoint contact = collision.contacts[0];
        
        if (collision.gameObject.CompareTag("target"))
        {
            Debug.Log("Hit " + collision.gameObject.name + "!");
            
            // Tambahkan kode damage/efek untuk target disini
            // Contoh: collision.gameObject.GetComponent<TargetScript>().TakeDamage(10);
        }
        
        // Buat bullet hole
        CreateBulletHole(contact.point, contact.normal, collision.gameObject.tag);
        
        // Selalu hancurkan peluru saat menabrak apapun
        Destroy(gameObject);
    }

    private void CreateBulletHole(Vector3 hitPoint, Vector3 hitNormal, string hitObjectTag)
    {
        // Cek apakah kita punya prefab bullet hole
        if (bulletHolePrefab != null)
        {
            // Rotasi bullet hole menghadap ke normal permukaan
            Quaternion hitRotation = Quaternion.LookRotation(-hitNormal);
            
            // Buat bullet hole sedikit di depan permukaan untuk menghindari z-fighting
            Vector3 adjustedHitPoint = hitPoint + (hitNormal * bulletHoleOffset);
            
            // Instantiate bullet hole
            GameObject bulletHoleObject = Instantiate(bulletHolePrefab, adjustedHitPoint, hitRotation);
            
            // Parent ke object yang kena tembak agar mengikuti jika objek bergerak
            bulletHoleObject.transform.SetParent(null);  // Atau set parent ke object yang ditembak
            
            // Hancurkan bullet hole setelah beberapa waktu
            Destroy(bulletHoleObject, 10f);  // Hapus bullet hole after 10 detik, bisa disesuaikan
            
            Debug.Log($"Created bullet hole on {hitObjectTag} at {hitPoint}");
        }
    }
}