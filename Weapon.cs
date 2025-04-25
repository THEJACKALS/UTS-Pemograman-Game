using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float bulletSpeed = 100;
    
    // Bullet Hole
    public GameObject bulletHolePrefab; // Prefab untuk bullet hole

    [Header("Shell Ejection")]
    public GameObject shellPrefab;         
    public Transform shellEjectionPoint;   
    public float shellEjectionForce = 2f;  
    public float shellTorque = 5f;         
    public float shellLifetime = 5f;       

    //firemode
    public enum FireMode { Safe, Semi, Auto }
    
    [Header("Fire Mode")]
    public FireMode currentFireMode = FireMode.Semi;
    public float fireRate = 10f;           // Peluru per detik di mode auto
    private float nextFireTime = 0f;
    public AudioClip fireModeChangeSound;  // Suara saat ganti mode
    private AudioSource audioSource;
    
    //ADS
    public Transform normalCamPos;
    public Transform aimCamPos;
    public float normalFOV = 60f;
    public float aimFOV = 40f;
    public float aimSpeed = 10f;

    public Camera playerCamera;
    public WeaponRecoil recoilScript;
    
    [Header("Fire SFX")]
    public AudioClip fireSound;             // Suara tembakan
    [Range(0, 1)] public float fireSoundVolume = 0.7f;
    public AudioClip emptyMagazineSound;    // Suara saat peluru habis
    [Range(0, 1)] public float emptyMagazineVolume = 0.5f;
    
    void Start()
    {
        // Tambahkan audio source jika belum ada
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.5f;
        }
        
        // Pastikan bulletPrefab memiliki komponen yang dibutuhkan
        CheckBulletPrefab();
    }

    void CheckBulletPrefab()
    {
        // Cek apakah bulletPrefab valid
        if (bulletPrefab != null)
        {
            // Pastikan ada Bullet script
            Bullet bulletScript = bulletPrefab.GetComponent<Bullet>();
            if (bulletScript == null)
            {
                Debug.LogWarning("Bullet prefab tidak memiliki Bullet.cs script! Menambahkan secara otomatis.");
                
                // Preview saja, jangan mengubah prefab asli
                // bulletPrefab.AddComponent<Bullet>();
            }
            
            // Pastikan ada Rigidbody
            Rigidbody rb = bulletPrefab.GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogWarning("Bullet prefab tidak memiliki Rigidbody! Menambahkan secara otomatis.");
                
                // Preview saja, jangan mengubah prefab asli
                // Rigidbody bulletRb = bulletPrefab.AddComponent<Rigidbody>();
                // bulletRb.useGravity = false;
                // bulletRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
            
            // Pastikan ada Collider
            Collider col = bulletPrefab.GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogWarning("Bullet prefab tidak memiliki Collider! Menambahkan secara otomatis.");
                
                // Preview saja, jangan mengubah prefab asli
                // SphereCollider bulletCol = bulletPrefab.AddComponent<SphereCollider>();
                // bulletCol.radius = 0.1f;
            }
        }
        else
        {
            Debug.LogError("Bullet prefab belum ditetapkan!");
        }
    }

    void Update()
    {
        // Fire mode selector
        if (Input.GetKeyDown(KeyCode.X))
        {
            CycleFireMode();
        }
        
        // Handling menembak berdasarkan fire mode
        HandleShooting();
        
        // ADS
        HandleAiming();
    }
    
    private void HandleShooting()
    {
        switch (currentFireMode)
        {
            case FireMode.Safe:
                // Tidak menembak di mode safe
                break;
                
            case FireMode.Semi:
                // Tembakan sekali saat tombol ditekan
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    FireWeapon();
                }
                break;
                
            case FireMode.Auto:
                // Full auto selama tombol ditahan
                if (Input.GetKey(KeyCode.Mouse0) && Time.time >= nextFireTime)
                {
                    FireWeapon();
                    nextFireTime = Time.time + 1f / fireRate;
                }
                break;
        }
    }
    
    private void CycleFireMode()
    {
        // Siklus antara Safe -> Semi -> Auto -> Safe
        switch (currentFireMode)
        {
            case FireMode.Safe:
                currentFireMode = FireMode.Semi;
                break;
            case FireMode.Semi:
                currentFireMode = FireMode.Auto;
                break;
            case FireMode.Auto:
                currentFireMode = FireMode.Safe;
                break;
        }
        
        // Tampilkan informasi fire mode yang aktif
        Debug.Log("Fire Mode: " + currentFireMode.ToString());
        
        // Mainkan suara pergantian mode
        if (fireModeChangeSound != null)
        {
            audioSource.PlayOneShot(fireModeChangeSound);
        }
    }

    private void FireWeapon()
    {
        // Mainkan suara tembakan
        PlayFireSound();
        
        // Instantiate bullet
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, bulletSpawn.rotation);
        
        // Dapatkan atau tambahkan komponen Bullet
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent != null)
        {
            // Atur bullet hole prefab jika ada
            if (bulletHolePrefab != null)
            {
                bulletComponent.bulletHolePrefab = bulletHolePrefab;
            }
            
            // Atur kecepatan peluru
            bulletComponent.bulletSpeed = this.bulletSpeed;
        }
        else
        {
            // Jika tidak ada script Bullet, gunakan metode lama
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = bulletSpawn.forward * bulletSpeed;
            }
            else
            {
                Debug.LogError("Bullet prefab tidak memiliki Rigidbody!");
            }
        }

        // Eject shell casing
        if (shellPrefab != null && shellEjectionPoint != null)
        {
            EjectShell();
        }

        // Tambahkan recoil
        if (recoilScript != null)
        {
            recoilScript.ApplyRecoil();
        }
    }

    private void PlayFireSound()
    {
        if (fireSound != null)
        {
            audioSource.PlayOneShot(fireSound, fireSoundVolume);
        }
        else
        {
            Debug.LogWarning("Fire sound clip is missing!");
        }
    }

    private void EjectShell()
    {
        // Buat selongsong di posisi ejeksi
        GameObject shell = Instantiate(shellPrefab, shellEjectionPoint.position, shellEjectionPoint.rotation);
        
        // Pastikan ada Rigidbody
        Rigidbody shellRb = shell.GetComponent<Rigidbody>();
        if (shellRb == null)
        {
            shellRb = shell.AddComponent<Rigidbody>();
            // Sesuaikan mass agar realistis
            shellRb.mass = 0.05f;
        }
        
        // Tambahkan gaya ejeksi
        Vector3 ejectionDirection = shellEjectionPoint.right + shellEjectionPoint.up * 0.3f;
        shellRb.AddForce(ejectionDirection * shellEjectionForce, ForceMode.Impulse);
        
        // Tambahkan rotasi acak pada selongsong
        shellRb.AddTorque(
            Random.Range(-shellTorque, shellTorque),
            Random.Range(-shellTorque, shellTorque),
            Random.Range(-shellTorque, shellTorque)
        );
        
        // Hancurkan selongsong setelah beberapa detik
        Destroy(shell, shellLifetime);
    }
    
    private void HandleAiming()
    {
        if (Input.GetKey(KeyCode.Mouse1))
        {
            // Posisi kamera mendekat ke aimCamPos
            playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, aimCamPos.position, Time.deltaTime * aimSpeed);
            playerCamera.transform.rotation = Quaternion.Lerp(playerCamera.transform.rotation, aimCamPos.rotation, Time.deltaTime * aimSpeed);
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, aimFOV, Time.deltaTime * aimSpeed);
        }
        else
        {
            // Balik ke posisi normal
            playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, normalCamPos.position, Time.deltaTime * aimSpeed);
            playerCamera.transform.rotation = Quaternion.Lerp(playerCamera.transform.rotation, normalCamPos.rotation, Time.deltaTime * aimSpeed);
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, normalFOV, Time.deltaTime * aimSpeed);
        }
    }
}