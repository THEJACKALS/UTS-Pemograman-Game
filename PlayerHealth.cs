using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public Image healthBar;
    public Text healthText;
    
    [Header("Armor Settings")]
    public float maxArmor = 100f;
    public float currentArmor;
    public Image armorBar;
    public Text armorText;
    
    [Header("Damage Visual Effects")]
    public Image damageFlashImage;
    public float flashSpeed = 5f;
    public Color flashColor = new Color(1f, 0f, 0f, 0.3f);
    
    [Header("Regeneration")]
    public bool healthRegenEnabled = true;
    public float healthRegenDelay = 5f;
    public float healthRegenRate = 10f;
    public bool armorRegenEnabled = false;
    public float armorRegenDelay = 10f;
    public float armorRegenRate = 5f;
    
    [Header("Debug Settings")]
    public bool enableDebugControls = true;
    public float debugDamageAmount = 10f;
    public float debugHealAmount = 20f;
    public float debugArmorAmount = 25f;
    public KeyCode damageKey = KeyCode.F1;
    public KeyCode healKey = KeyCode.F2;
    public KeyCode armorKey = KeyCode.F3;
    public KeyCode killKey = KeyCode.F4;
    
    [Header("UI Debug")]
    public bool showUIDebugInfo = true;
    public Text uiDebugText;
    
    private float lastDamageTime;
    private bool isDead = false;
    private bool isRegenerating = false;

    void Start()
    {
        // Initialize health and armor
        currentHealth = maxHealth;
        currentArmor = maxArmor;
        
        // Log UI component status
        CheckUIComponents();
        
        // Update UI
        UpdateHealthUI();
        UpdateArmorUI();
        
        // Initialize damage flash effect
        if (damageFlashImage != null)
        {
            damageFlashImage.color = Color.clear;
        }
        
        Debug.Log("Player Health System initialized. Current Health: " + currentHealth + " / Current Armor: " + currentArmor);
    }
    
    void CheckUIComponents()
    {
        string healthStatus = "";
        string armorStatus = "";
        
        // Check Health Bar
        if (healthBar == null)
        {
            healthStatus = "MISSING!";
            Debug.LogError("Health Bar Image is not assigned!");
        }
        else
        {
            if (healthBar.type != Image.Type.Filled)
            {
                healthStatus = "WRONG TYPE! (Should be 'Filled')";
                Debug.LogError("Health Bar Image type must be set to 'Filled'!");
            }
            else
            {
                healthStatus = "OK";
            }
        }
        
        // Check Armor Bar
        if (armorBar == null)
        {
            armorStatus = "MISSING!";
            Debug.LogError("Armor Bar Image is not assigned!");
        }
        else
        {
            if (armorBar.type != Image.Type.Filled)
            {
                armorStatus = "WRONG TYPE! (Should be 'Filled')";
                Debug.LogError("Armor Bar Image type must be set to 'Filled'!");
            }
            else
            {
                armorStatus = "OK";
            }
        }
        
        // Show debug info
        if (showUIDebugInfo && uiDebugText != null)
        {
            uiDebugText.text = "Health Bar: " + healthStatus + "\nArmor Bar: " + armorStatus;
        }
        
        // Log summary
        Debug.Log($"UI Check - Health Bar: {healthStatus}, Armor Bar: {armorStatus}");
    }

    void Update()
    {
        // Handle damage flash effect fade
        if (damageFlashImage != null && damageFlashImage.color.a > 0)
        {
            damageFlashImage.color = Color.Lerp(damageFlashImage.color, Color.clear, flashSpeed * Time.deltaTime);
        }
        
        // Check if we should start regeneration
        if ((healthRegenEnabled || armorRegenEnabled) && !isRegenerating && !isDead)
        {
            float timeSinceDamage = Time.time - lastDamageTime;
            
            if (healthRegenEnabled && currentHealth < maxHealth && timeSinceDamage > healthRegenDelay)
            {
                StartCoroutine(RegenerateHealth());
            }
            
            if (armorRegenEnabled && currentArmor < maxArmor && timeSinceDamage > armorRegenDelay)
            {
                StartCoroutine(RegenerateArmor());
            }
        }
        
        // Debug controls
        if (enableDebugControls)
        {
            HandleDebugControls();
        }
    }
    
    void HandleDebugControls()
    {
        // Take damage
        if (Input.GetKeyDown(damageKey))
        {
            TakeDamage(debugDamageAmount);
            Debug.Log($"DEBUG: Player took {debugDamageAmount} damage. Health: {currentHealth}, Armor: {currentArmor}");
            
            // Debug UI update
            if (healthBar != null)
                Debug.Log($"Health Bar fill amount: {healthBar.fillAmount}");
            if (armorBar != null)
                Debug.Log($"Armor Bar fill amount: {armorBar.fillAmount}");
        }
        
        // Heal
        if (Input.GetKeyDown(healKey))
        {
            AddHealth(debugHealAmount);
            Debug.Log($"DEBUG: Player healed {debugHealAmount} points. Health: {currentHealth}");
            
            // Debug UI update
            if (healthBar != null)
                Debug.Log($"Health Bar fill amount after healing: {healthBar.fillAmount}");
        }
        
        // Add armor
        if (Input.GetKeyDown(armorKey))
        {
            AddArmor(debugArmorAmount);
            Debug.Log($"DEBUG: Player gained {debugArmorAmount} armor. Armor: {currentArmor}");
            
            // Debug UI update
            if (armorBar != null)
                Debug.Log($"Armor Bar fill amount after adding armor: {armorBar.fillAmount}");
        }
        
        // Instant kill (for testing death)
        if (Input.GetKeyDown(killKey))
        {
            TakeDamage(maxHealth + maxArmor);
            Debug.Log("DEBUG: Player instant kill triggered!");
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        lastDamageTime = Time.time;
        StopAllCoroutines();
        isRegenerating = false;
        
        // Flash the damage effect
        if (damageFlashImage != null)
        {
            damageFlashImage.color = flashColor;
        }
        
        // Calculate damage distribution between armor and health
        float remainingDamage = damage;
        float initialArmor = currentArmor;
        float initialHealth = currentHealth;
        
        // If we have armor, it absorbs 75% of the damage until depleted
        if (currentArmor > 0)
        {
            float armorDamage = Mathf.Min(currentArmor, remainingDamage * 0.75f);
            currentArmor -= armorDamage;
            remainingDamage -= armorDamage;
            
            Debug.Log($"Armor absorbed {armorDamage} damage. Armor reduced from {initialArmor} to {currentArmor}");
            UpdateArmorUI();
        }
        
        // Apply remaining damage to health
        if (remainingDamage > 0)
        {
            currentHealth -= remainingDamage;
            Debug.Log($"Health took {remainingDamage} damage. Health reduced from {initialHealth} to {currentHealth}");
            UpdateHealthUI();
            
            // Check if player is dead
            if (currentHealth <= 0)
            {
                Die();
            }
        }
    }
    
    public void AddHealth(float amount)
    {
        if (isDead) return;
        
        float initialHealth = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        
        Debug.Log($"Health increased from {initialHealth} to {currentHealth}");
        UpdateHealthUI();
    }
    
    public void AddArmor(float amount)
    {
        if (isDead) return;
        
        float initialArmor = currentArmor;
        currentArmor = Mathf.Min(currentArmor + amount, maxArmor);
        
        Debug.Log($"Armor increased from {initialArmor} to {currentArmor}");
        UpdateArmorUI();
    }
    
    void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            float fillAmount = currentHealth / maxHealth;
            healthBar.fillAmount = fillAmount;
            Debug.Log($"Setting Health Bar fill amount to {fillAmount} ({currentHealth}/{maxHealth})");
        }
        else
        {
            Debug.LogWarning("Cannot update Health Bar: Reference is missing!");
        }
        
        if (healthText != null)
        {
            healthText.text = Mathf.Ceil(currentHealth).ToString();
        }
    }
    
    void UpdateArmorUI()
    {
        if (armorBar != null)
        {
            float fillAmount = currentArmor / maxArmor;
            armorBar.fillAmount = fillAmount;
            Debug.Log($"Setting Armor Bar fill amount to {fillAmount} ({currentArmor}/{maxArmor})");
        }
        else
        {
            Debug.LogWarning("Cannot update Armor Bar: Reference is missing!");
        }
        
        if (armorText != null)
        {
            armorText.text = Mathf.Ceil(currentArmor).ToString();
        }
    }
    
    void Die()
    {
        isDead = true;
        currentHealth = 0;
        UpdateHealthUI();
        
        // You can add death animations, game over UI, etc. here
        Debug.Log("Player died! Game Over!");
        
        // Optional: Disable player controls, camera, etc.
        // GetComponent<PlayerController>().enabled = false;
        
        // Optional: Call game manager to handle respawn/game over
        // GameManager.instance.OnPlayerDeath();
    }
    
    IEnumerator RegenerateHealth()
    {
        isRegenerating = true;
        Debug.Log("Starting health regeneration...");
        
        while (currentHealth < maxHealth)
        {
            float previousHealth = currentHealth;
            currentHealth = Mathf.Min(currentHealth + (healthRegenRate * Time.deltaTime), maxHealth);
            
            // Log regeneration and update UI periodically, not every frame to avoid console spam
            if (Mathf.Floor(previousHealth) != Mathf.Floor(currentHealth))
            {
                Debug.Log($"Health regenerated to {currentHealth}");
                UpdateHealthUI();
            }
            
            yield return null;
            
            // Stop regenerating if we take damage
            if (Time.time - lastDamageTime < healthRegenDelay)
            {
                Debug.Log("Health regeneration interrupted by damage!");
                break;
            }
        }
        
        if (currentHealth >= maxHealth)
        {
            Debug.Log("Health fully regenerated!");
            UpdateHealthUI();
        }
        
        isRegenerating = false;
    }
    
    IEnumerator RegenerateArmor()
    {
        isRegenerating = true;
        Debug.Log("Starting armor regeneration...");
        
        while (currentArmor < maxArmor)
        {
            float previousArmor = currentArmor;
            currentArmor = Mathf.Min(currentArmor + (armorRegenRate * Time.deltaTime), maxArmor);
            
            // Log regeneration periodically, not every frame
            if (Mathf.Floor(previousArmor) != Mathf.Floor(currentArmor))
            {
                Debug.Log($"Armor regenerated to {currentArmor}");
                UpdateArmorUI();
            }
            
            yield return null;
            
            // Stop regenerating if we take damage
            if (Time.time - lastDamageTime < armorRegenDelay)
            {
                Debug.Log("Armor regeneration interrupted by damage!");
                break;
            }
        }
        
        if (currentArmor >= maxArmor)
        {
            Debug.Log("Armor fully regenerated!");
            UpdateArmorUI();
        }
        
        isRegenerating = false;
    }
}