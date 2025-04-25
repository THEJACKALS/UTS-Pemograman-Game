using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponCollisionAvoidance : MonoBehaviour
{
    [Header("Wall Detection")]
    public Transform weaponTransform;       // Transform dari senjata yang akan dirotasi
    public Transform weaponPivot;           // Pivot point untuk rotasi senjata (biasanya parent dari weaponTransform)
    public float raycastDistance = 1f;      // Jarak raycast untuk mendeteksi tembok
    public float minDistanceToWall = 0.3f;  // Jarak minimum sebelum senjata memiringkan
    public LayerMask wallLayers;            // Layer untuk tembok yang akan dideteksi
    
    [Header("Weapon Rotation")]
    public float maxTiltAngle = 45f;        // Sudut maksimum memiringkan senjata
    public float rotationSpeed = 8f;        // Kecepatan rotasi senjata
    public bool tiltLeft = true;            // Arah memiringkan (ke kiri = true, ke kanan = false)
    
    [Header("Camera Reference")]
    public Camera playerCamera;             // Referensi ke kamera player yang aktif
    
    [Header("Debug")]
    public bool showDebugRays = true;
    
    // State tracking
    private Quaternion originalRotation;    // Rotasi asli senjata
    private bool isNearWall = false;        // Status apakah dekat tembok
    private float currentTiltAngle = 0f;    // Sudut kemiringan saat ini
    
    void Start()
    {
        // Simpan rotasi asli senjata
        if (weaponTransform != null)
        {
            originalRotation = weaponTransform.localRotation;
        }
        else
        {
            Debug.LogError("Weapon transform belum ditetapkan!");
        }
        
        // Cek camera reference
        if (playerCamera == null)
        {
            // Coba dapatkan kamera dari Weapon component jika ada
            Weapon weaponComponent = GetComponent<Weapon>();
            if (weaponComponent != null && weaponComponent.playerCamera != null)
            {
                playerCamera = weaponComponent.playerCamera;
                Debug.Log("Using camera reference from Weapon component");
            }
            else
            {
                // Coba cari kamera dengan tag MainCamera sebagai fallback
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    playerCamera = mainCam;
                    Debug.Log("Using Camera.main as fallback");
                }
                else
                {
                    Debug.LogWarning("Tidak ada kamera yang ditetapkan! Pastikan untuk menetapkan Camera di inspector.");
                }
            }
        }
        
        // Setup pivot jika tidak ada
        if (weaponPivot == null && weaponTransform != null)
        {
            weaponPivot = weaponTransform.parent;
            if (weaponPivot == null)
            {
                weaponPivot = weaponTransform;
            }
        }
    }
    
    void Update()
    {
        // Pastikan ada kamera yang ditetapkan
        if (playerCamera == null)
        {
            return;
        }
        
        // Cek dinding di depan
        CheckForWalls();
        
        // Update rotasi senjata
        UpdateWeaponRotation();
    }
    
    void CheckForWalls()
    {
        if (weaponTransform == null || playerCamera == null)
            return;
        
        RaycastHit hit;
        Vector3 direction = playerCamera.transform.forward;
        Vector3 start = playerCamera.transform.position;
        
        // Raycast dari kamera ke depan
        if (Physics.Raycast(start, direction, out hit, raycastDistance, wallLayers))
        {
            // Kalkulasi jarak ke tembok
            float distanceToWall = hit.distance;
            isNearWall = distanceToWall < minDistanceToWall;
            
            // Debug visual
            if (showDebugRays)
            {
                Debug.DrawRay(start, direction * hit.distance, Color.red);
                Debug.DrawRay(hit.point, hit.normal, Color.green);
            }
        }
        else
        {
            isNearWall = false;
            
            // Debug visual
            if (showDebugRays)
            {
                Debug.DrawRay(start, direction * raycastDistance, Color.blue);
            }
        }
    }
    
    void UpdateWeaponRotation()
    {
        if (weaponTransform == null)
            return;
        
        // Target tilt angle berdasarkan status tembok
        float targetTiltAngle = isNearWall ? maxTiltAngle : 0f;
        // Arah tilt (positif atau negatif)
        float tiltDirection = tiltLeft ? -1f : 1f;
        
        // Update sudut tilt saat ini dengan smooth lerp
        currentTiltAngle = Mathf.Lerp(currentTiltAngle, targetTiltAngle, Time.deltaTime * rotationSpeed);
        
        // Buat rotasi baru di sekitar sumbu Z (ke kiri/kanan)
        Quaternion tiltRotation = Quaternion.Euler(0, 0, currentTiltAngle * tiltDirection);
        
        // Terapkan rotasi ke senjata
        weaponTransform.localRotation = originalRotation * tiltRotation;
    }
    
    // Fungsi untuk mengubah kamera yang digunakan (bisa dipanggil dari script lain)
    public void SetActiveCamera(Camera newCamera)
    {
        if (newCamera != null)
        {
            playerCamera = newCamera;
        }
    }
}