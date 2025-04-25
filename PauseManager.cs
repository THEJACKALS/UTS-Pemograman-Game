using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;  // Added for scene management

public class PauseManager : MonoBehaviour
{
    public static PauseManager instance;
    
    [Header("Pause Settings")]
    public KeyCode pauseKey = KeyCode.Escape;      // Tombol untuk pause
    public GameObject pauseMenuUI;                 // Panel UI untuk menu pause
    
    [Header("Cursor Settings")]
    public bool unlockCursorOnPause = true;        // Unlock cursor saat pause
    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisibility;
    
    [Header("Optional Components")]
    public MouseMovement playerMouseLook;         // Referensi ke script mouse look
    public PlayerMovement playerMovement;         // Referensi ke script player movement
    public Weapon[] weapons;                      // Referensi ke semua senjata
    
    [Header("Scene Management")]
    public string mainMenuSceneName = "MainMenu"; // Nama scene main menu
    
    [Header("Audio Settings")]
    public AudioSource[] gameAudioSources;        // References to audio sources to pause
    private float[] originalVolumes;              // Store original volumes
    
    // State tracking
    public static bool gameIsPaused = false;
    
    void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Pastikan menu pause tidak terlihat saat mulai
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        
        // Default adalah game tidak di-pause
        gameIsPaused = false;
        Time.timeScale = 1f;
        
        // Cache all audio sources if not set in inspector
        if (gameAudioSources == null || gameAudioSources.Length == 0)
        {
            gameAudioSources = FindObjectsOfType<AudioSource>();
        }
        
        // Initialize volume array
        originalVolumes = new float[gameAudioSources.Length];
    }
    
    void Update()
    {
        // Cek input pause
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }
    }
    
    public void TogglePause()
    {
        if (gameIsPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }
    
    public void Pause()
    {
        // Aktifkan menu pause
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
        }
        
        // Simpan status cursor sebelum pause
        previousCursorLockState = Cursor.lockState;
        previousCursorVisibility = Cursor.visible;
        
        // Unlock dan tampilkan cursor
        if (unlockCursorOnPause)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        // Freeze game
        Time.timeScale = 0f;
        gameIsPaused = true;
        
        // Disable mouse look dan weapon scripts
        DisablePlayerControls();
        
        // Pause all audio sources
        PauseAllAudio();
        
        Debug.Log("Game paused");
    }
    
    public void Resume()
    {
        // Sembunyikan menu pause
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        
        // Kembalikan status cursor seperti sebelum pause
        Cursor.lockState = previousCursorLockState;
        Cursor.visible = previousCursorVisibility;
        
        // Unfreeze game
        Time.timeScale = 1f;
        gameIsPaused = false;
        
        // Re-enable mouse look dan weapon scripts
        EnablePlayerControls();
        
        // Resume all audio
        ResumeAllAudio();
        
        Debug.Log("Game resumed");
    }
    
    void DisablePlayerControls()
    {
        // Disable mouse look
        if (playerMouseLook != null)
        {
            playerMouseLook.enabled = false;
        }
        
        // Disable player movement
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
        
        // Disable weapons
        if (weapons != null)
        {
            foreach (Weapon weapon in weapons)
            {
                if (weapon != null)
                    weapon.enabled = false;
            }
        }
    }
    
    void EnablePlayerControls()
    {
        // Re-enable mouse look
        if (playerMouseLook != null)
        {
            playerMouseLook.enabled = true;
        }
        
        // Re-enable player movement
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
        
        // Re-enable weapons
        if (weapons != null)
        {
            foreach (Weapon weapon in weapons)
            {
                if (weapon != null)
                    weapon.enabled = true;
            }
        }
    }
    
    // New method to pause all audio sources
    void PauseAllAudio()
    {
        for (int i = 0; i < gameAudioSources.Length; i++)
        {
            if (gameAudioSources[i] != null)
            {
                // Store original volume
                originalVolumes[i] = gameAudioSources[i].volume;
                
                // Two options to handle audio:
                // Option 1: Pause the audio (best for most cases)
                gameAudioSources[i].Pause();
                
                // Option 2: Mute but keep playing (alternative if needed)
                // gameAudioSources[i].volume = 0f;
            }
        }
    }
    
    // New method to resume all audio sources
    void ResumeAllAudio()
    {
        for (int i = 0; i < gameAudioSources.Length; i++)
        {
            if (gameAudioSources[i] != null)
            {
                // Resume audio playback
                gameAudioSources[i].UnPause();
                
                // Restore original volume (for option 2)
                gameAudioSources[i].volume = originalVolumes[i];
            }
        }
    }
    
    // Fungsi untuk tombol UI
    public void ResumeButton()
    {
        Resume();
    }
    
    public void MainMenuButton()
    {
        // Kembali ke main menu
        Resume(); // Pastikan timeScale kembali normal
        // Ganti scene ke main menu
        SceneManager.LoadScene(mainMenuSceneName);
        Debug.Log("Loading Main Menu scene: " + mainMenuSceneName);
    }
    
    public void QuitButton()
    {
        Debug.Log("Quitting game");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}