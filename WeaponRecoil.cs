using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponRecoil : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;      // Kamera player
    public Transform weaponTransform;      // Transform senjata (opsional)
    
    [Header("Recoil Settings")]
    public float verticalRecoil = 2f;      // Seberapa tinggi recoil (rotasi ke atas)
    public float horizontalRecoil = 0.5f;  // Recoil ke samping (random)
    public float recoilSpeed = 10f;        // Seberapa cepat recoil terjadi
    public float returnSpeed = 5f;         // Seberapa cepat kamera kembali ke posisi semula
    
    [Header("Visual Effects")]
    public float weaponKickback = 0.1f;    // Seberapa jauh senjata bergerak ke belakang
    public float maxRecoilAmount = 5f;     // Batasan recoil maksimum

    private Vector3 currentRotation;
    private Vector3 targetRotation;
    private Quaternion originalRotation;
    private Vector3 originalWeaponPosition;
    private float accumulatedRecoil = 0f;

    void Start()
    {
        originalRotation = cameraTransform.localRotation;
        if (weaponTransform != null)
        {
            originalWeaponPosition = weaponTransform.localPosition;
        }
    }

    void Update()
    {
        // Smooth interpolasi ke posisi recoil
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Lerp(currentRotation, targetRotation, recoilSpeed * Time.deltaTime);
        cameraTransform.localRotation = Quaternion.Euler(currentRotation) * originalRotation;
        
        // Kembalikan posisi senjata jika ada
        if (weaponTransform != null)
        {
            weaponTransform.localPosition = Vector3.Lerp(
                weaponTransform.localPosition, 
                originalWeaponPosition, 
                returnSpeed * Time.deltaTime
            );
        }
        
        // Kurangi accumulated recoil
        if (accumulatedRecoil > 0)
        {
            accumulatedRecoil = Mathf.Max(0, accumulatedRecoil - (returnSpeed * Time.deltaTime));
        }
    }

    // Method ini dipanggil saat senjata ditembak
    public void ApplyRecoil(float intensity = 1.0f)
    {
        // Batasi accumulated recoil
        accumulatedRecoil = Mathf.Min(accumulatedRecoil + intensity, maxRecoilAmount);
        
        // Hitung multiplier berdasarkan accumulated recoil
        float recoilMultiplier = 1f + (accumulatedRecoil / maxRecoilAmount * 0.5f);
        
        // Terapkan recoil dengan random horizontal
        targetRotation += new Vector3(
            -verticalRecoil * intensity * recoilMultiplier, 
            Random.Range(-horizontalRecoil, horizontalRecoil) * intensity, 
            0f
        );
        
        // Tambahkan visual kickback jika ada weapon transform
        if (weaponTransform != null)
        {
            weaponTransform.localPosition -= new Vector3(0, 0, weaponKickback * intensity);
        }
    }
    
    // Reset recoil (berguna untuk reload atau ganti senjata)
    public void ResetRecoil()
    {
        targetRotation = Vector3.zero;
        currentRotation = Vector3.zero;
        accumulatedRecoil = 0f;
        
        if (weaponTransform != null)
        {
            weaponTransform.localPosition = originalWeaponPosition;
        }
    }
}