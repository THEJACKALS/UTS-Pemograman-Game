using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeanSystem : MonoBehaviour
{
    public Transform playerCamera;
    public Transform weaponHolder; // Referensi ke holder senjata
    public float leanAmount = 15f;  // Berapa banyak karakter akan miring
    public float leanSpeed = 8f;    // Kecepatan transisi lean
    public float horizontalOffset = 0.3f; // Seberapa jauh kamera bergeser saat lean
    
    [Header("Weapon Lean Settings")]
    public float weaponLeanAmount = 5f;   // Seberapa miring senjatanya
    public float weaponSideOffset = 0.1f; // Seberapa geser ke samping
    
    [Header("Debug")]
    public bool showDebug = false;      // Untuk melihat nilai lean saat runtime
    
    private float currentLean = 0f;
    private Vector3 originalCameraPosition;
    private Vector3 originalWeaponPosition;
    private float currentHorizontalShift = 0f;

    void Start()
    {
        // Simpan posisi awal kamera dan senjata
        if (playerCamera != null)
        {
            originalCameraPosition = playerCamera.localPosition;
        }
        
        if (weaponHolder != null)
        {
            originalWeaponPosition = weaponHolder.localPosition;
        }
        
        // Pastikan referensi sudah diatur
        if (playerCamera == null)
        {
            Debug.LogError("Player Camera belum diatur pada LeanSystem!");
        }
    }

    void Update()
    {
        // Hanya proses jika referensi sudah diatur
        if (playerCamera == null) return;
        
        float targetLean = 0f;
        
        // Mendeteksi input Q dan E untuk leaning
        if (Input.GetKey(KeyCode.Q)) 
        {
            targetLean = -leanAmount;  // Miring kiri
        }
        else if (Input.GetKey(KeyCode.E))
        {
            targetLean = leanAmount;   // Miring kanan
        }
        else
        {
            targetLean = 0f;           // Kembali normal
        }
        
        // Lerp ke target lean untuk smooth transition
        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * leanSpeed);
        
        // Hitung pergeseran horizontal berdasarkan lean (normalisasi untuk -1 hingga 1)
        float leanRatio = currentLean / leanAmount; // Nilai -1 hingga 1
        float targetHorizontalShift = leanRatio * horizontalOffset;
        currentHorizontalShift = Mathf.Lerp(currentHorizontalShift, targetHorizontalShift, Time.deltaTime * leanSpeed);
        
        // Aplikasikan rotasi ke kamera (hanya di sumbu Z)
        playerCamera.localRotation = Quaternion.Euler(0, 0, currentLean);
        
        // Aplikasikan pergeseran horizontal ke kamera
        playerCamera.localPosition = originalCameraPosition + new Vector3(currentHorizontalShift, 0, 0);
        
        // Aplikasikan ke weapon holder jika ada
        if (weaponHolder != null)
        {
            // Gunakan rotasi yang lebih sederhana, hanya tilt ke samping saja
            weaponHolder.localRotation = Quaternion.Euler(0, 0, currentLean * 0.7f);
            
            // Geser senjata ke samping sedikit, berlawanan dengan lean untuk efek yang lebih natural
            Vector3 weaponOffset = originalWeaponPosition + new Vector3(
                -leanRatio * weaponSideOffset, // Geser sedikit berlawanan dengan lean
                0,
                0
            );
            
            weaponHolder.localPosition = weaponOffset;
        }
        
        // Debug
        if (showDebug)
        {
            Debug.Log($"Current Lean: {currentLean}, Horizontal Shift: {currentHorizontalShift}");
        }
    }
}