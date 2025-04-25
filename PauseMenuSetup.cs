using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseMenuSetup : MonoBehaviour
{
    [Header("UI Elements")]
    public Button resumeButton;
    public Button optionsButton;
    public Button mainMenuButton;
    public Button quitButton;
    public Button backButton;  // Back button in options menu
    
    [Header("Panels")]
    public GameObject mainPausePanel;
    public GameObject optionsPanel;
    
    [Header("Options")]
    public Slider sensitivitySlider;
    public Toggle invertYToggle;
    public TMP_Dropdown graphicsDropdown;
    public Slider volumeSlider;
    
    private PauseManager pauseManager;
    
    void Start()
    {
        pauseManager = PauseManager.instance;
        if (pauseManager == null)
        {
            Debug.LogError("Tidak dapat menemukan PauseManager!");
        }
        
        // Setup button listeners
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);
            
        if (optionsButton != null)
            optionsButton.onClick.AddListener(ShowOptions);
            
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
            
        // Setup back button
        if (backButton != null)
            backButton.onClick.AddListener(HideOptions);
        
        // Setup slider listeners (alternative to setting in Inspector)
        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            
        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            
        if (invertYToggle != null)
            invertYToggle.onValueChanged.AddListener(OnInvertYChanged);
            
        if (graphicsDropdown != null)
            graphicsDropdown.onValueChanged.AddListener(OnGraphicsQualityChanged);
        
        // Sembunyikan options panel saat mulai
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
            
        // Load dan apply settings
        LoadSettings();
    }
    
    // Slider OnValueChanged event handlers
    public void OnSensitivityChanged(float newValue)
    {
        // Apply immediately without waiting to exit options menu
        MouseMovement mouseLook = FindObjectOfType<MouseMovement>();
        if (mouseLook != null)
        {
            mouseLook.mouseSensivity = newValue * 1000f; // Adjust scaling as needed
        }
        
        // Save to PlayerPrefs immediately
        PlayerPrefs.SetFloat("MouseSensitivity", newValue);
    }
    
    public void OnVolumeChanged(float newValue)
    {
        // Apply immediately
        AudioListener.volume = newValue;
        
        // Save to PlayerPrefs immediately
        PlayerPrefs.SetFloat("MasterVolume", newValue);
    }
    
    public void OnInvertYChanged(bool newValue)
    {
        PlayerPrefs.SetInt("InvertY", newValue ? 1 : 0);
        
        // Apply to game immediately if needed
        // For example:
        MouseMovement mouseLook = FindObjectOfType<MouseMovement>();
        if (mouseLook != null)
        {
            // Assuming your MouseMovement script has an invertY property
            // mouseLook.invertY = newValue;
        }
    }
    
    public void OnGraphicsQualityChanged(int newValue)
    {
        PlayerPrefs.SetInt("QualityLevel", newValue);
        QualitySettings.SetQualityLevel(newValue);
    }
    
    public void Resume()
    {
        if (pauseManager != null)
            pauseManager.Resume();
    }
    
    public void ShowOptions()
    {
        if (mainPausePanel != null)
            mainPausePanel.SetActive(false);
            
        if (optionsPanel != null)
            optionsPanel.SetActive(true);
    }
    
    public void HideOptions()
    {
        // Important: First deactivate options panel, then activate main panel
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
            
        if (mainPausePanel != null)
            mainPausePanel.SetActive(true);
            
        // Save settings before returning (this is redundant now that we save immediately)
        SaveSettings();
    }
    
    public void ReturnToMainMenu()
    {
        if (pauseManager != null)
            pauseManager.MainMenuButton();
    }
    
    public void QuitGame()
    {
        if (pauseManager != null)
            pauseManager.QuitButton();
    }
    
    // Load settings at startup
    void LoadSettings()
    {
        // Load mouse sensitivity
        if (sensitivitySlider != null)
        {
            float sensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 0.5f);
            sensitivitySlider.value = sensitivity;
            
            // Apply to game directly
            MouseMovement mouseLook = FindObjectOfType<MouseMovement>();
            if (mouseLook != null)
            {
                mouseLook.mouseSensivity = sensitivity * 1000f;
            }
        }
        
        // Load invert Y
        if (invertYToggle != null)
        {
            bool invertY = PlayerPrefs.GetInt("InvertY", 0) == 1;
            invertYToggle.isOn = invertY;
        }
        
        // Load graphics quality
        if (graphicsDropdown != null)
        {
            int qualityLevel = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
            graphicsDropdown.value = qualityLevel;
        }
        
        // Load volume
        if (volumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
            volumeSlider.value = volume;
            AudioListener.volume = volume;
        }
    }
    
    // This method is now mainly a backup since we save immediately on each change
    void SaveSettings()
    {
        // Make sure all settings are saved
        if (sensitivitySlider != null)
            PlayerPrefs.SetFloat("MouseSensitivity", sensitivitySlider.value);
            
        if (invertYToggle != null)
            PlayerPrefs.SetInt("InvertY", invertYToggle.isOn ? 1 : 0);
            
        if (graphicsDropdown != null)
            PlayerPrefs.SetInt("QualityLevel", graphicsDropdown.value);
            
        if (volumeSlider != null)
            PlayerPrefs.SetFloat("MasterVolume", volumeSlider.value);
        
        // Save all changes to disk
        PlayerPrefs.Save();
    }
}