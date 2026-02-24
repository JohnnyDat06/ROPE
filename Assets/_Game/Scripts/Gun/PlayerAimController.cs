using UnityEngine;
using Cinemachine;
using StarterAssets;

public class PlayerAimController : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera aimVirtualCamera;
    [SerializeField] private GameObject crosshair;

    [SerializeField] private float normalSensitivity = 1f;
    [SerializeField] private float aimSensitivity = 0.5f;
    [SerializeField] private float normalMoveSpeed = 3f;
    [SerializeField] private float aimMoveSpeed = 1.5f;

    private StarterAssetsInputs _input;
    private ThirdPersonController _thirdPersonController;

    private void Awake()
    {
        _input = GetComponent<StarterAssetsInputs>();
        _thirdPersonController = GetComponent<ThirdPersonController>();
    }

    private void Start()
    {
        if (crosshair != null) crosshair.SetActive(false);
        if (aimVirtualCamera != null) aimVirtualCamera.Priority = 0;
    }

    private void Update()
    {
        HandleAim();
    }

    private void HandleAim()
    {
        if (_input.aim)
        {

            if (aimVirtualCamera != null) aimVirtualCamera.Priority = 20;

            if (crosshair != null) crosshair.SetActive(true);

            _thirdPersonController.SetSensitivity(aimSensitivity);

            _thirdPersonController.StrafeMoveSpeed = aimMoveSpeed;
        }
        else
        {
            if (aimVirtualCamera != null) aimVirtualCamera.Priority = 0;

            if (crosshair != null) crosshair.SetActive(false);
            _thirdPersonController.SetSensitivity(normalSensitivity);
            _thirdPersonController.StrafeMoveSpeed = normalMoveSpeed;
        }
    }
}