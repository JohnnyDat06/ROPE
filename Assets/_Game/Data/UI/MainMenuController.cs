﻿using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("CÀI ĐẶT GAMEPLAY (Quan trọng)")]
    [Tooltip("Gõ chính xác tên Scene bạn muốn load vào đây (Ví dụ: Level1, GameScene...)")]
    [SerializeField] private string nameOfGameScene = "GameScene"; // Biến này hiện ở Inspector

    [Header("Core Components")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("UI Elements (Load Data)")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private TMP_Dropdown qualityDropdown;

    void Start()
    {
        // Setup ban đầu
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
        if (optionsPanel) optionsPanel.SetActive(false);

        LoadSettings();
    }

    // --- LOGIC PLAY GAME ---
    public void PlayGame()
    {
        // Kiểm tra xem người dùng có quên nhập tên Scene không
        if (string.IsNullOrEmpty(nameOfGameScene))
        {
            Debug.LogError("LỖI: Bạn chưa nhập tên Scene trong Inspector kìa!");
            return;
        }

        // Khoá chuột khi vào game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Load Scene theo cái tên bạn đã nhập ở ngoài Inspector
        SceneManager.LoadScene(nameOfGameScene);
    }

    public void ExitGame()
    {
        Debug.Log("Đã thoát game!");
        Application.Quit();
    }

    // --- LOGIC CHUYỂN PANEL ---
    public void OpenOptions()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    // --- LOGIC AUDIO & GRAPHICS ---
    public void SetMasterVolume(float sliderValue)
    {
        float volumeDB = Mathf.Log10(sliderValue) * 20;
        if (mainMixer) mainMixer.SetFloat("MasterVol", volumeDB);
        PlayerPrefs.SetFloat("MasterPref", sliderValue);
    }

    public void SetMusicVolume(float sliderValue)
    {
        float volumeDB = Mathf.Log10(sliderValue) * 20;
        if (mainMixer) mainMixer.SetFloat("MusicVol", volumeDB);
        PlayerPrefs.SetFloat("MusicPref", sliderValue);
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("QualityPref", qualityIndex);
    }

    private void LoadSettings()
    {
        // Load Volume
        float masterVal = PlayerPrefs.GetFloat("MasterPref", 1f);
        float musicVal = PlayerPrefs.GetFloat("MusicPref", 1f);

        if (masterSlider) masterSlider.value = masterVal;
        if (musicSlider) musicSlider.value = musicVal;

        SetMasterVolume(masterVal);
        SetMusicVolume(musicVal);

        // Load Quality
        int qualityVal = PlayerPrefs.GetInt("QualityPref", 2); // Mặc định mức 2 (Medium/High)
        if (qualityDropdown) qualityDropdown.value = qualityVal;
        QualitySettings.SetQualityLevel(qualityVal);
    }
}