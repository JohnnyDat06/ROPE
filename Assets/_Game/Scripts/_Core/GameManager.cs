﻿using System;
using UnityEngine;
using UnityEngine.InputSystem.iOS;

namespace DatScript
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance;

        [Header("Game Settings")]
        [Tooltip("Vị trí mặc định khi bắt đầu game")]
        [SerializeField] public Transform defaultSpawnPoint;
        [SerializeField] public GameObject gameOverPanel;
        [SerializeField] private GameObject tutorialPanel;

        private Vector3 currentRespawnPosition;
        private GameObject player;

        private void Awake()
        {
            // Singleton Pattern
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            player = GameObject.FindGameObjectWithTag("Player");

            if (defaultSpawnPoint != null)
            {
                currentRespawnPosition = defaultSpawnPoint.position;
            }
            else if (player != null)
            {
                currentRespawnPosition = player.transform.position;
            }
        }

        private void Update()
        {
            ToggleTutorialPanel();
        }

        private void ToggleTutorialPanel()
        {
            if (tutorialPanel == null) return;
            if (Input.GetKeyDown(KeyCode.H))
                tutorialPanel.SetActive(!tutorialPanel.activeSelf);
        }
        
        public void SetCheckpoint(Vector3 newPosition)
        {
            currentRespawnPosition = newPosition;
            Debug.Log($"Đã lưu Checkpoint tại: {newPosition}");
        }


        public void RespawnPlayer()
        {
            if (player == null) return;

            gameOverPanel.SetActive(false);

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = currentRespawnPosition;

            if (cc != null) cc.enabled = true;

            if (PlayerHealth.instance != null)
            {
                PlayerHealth.instance.ResetHealth();
            }

            Debug.Log("Player đã được hồi sinh!");
        }
    }
}