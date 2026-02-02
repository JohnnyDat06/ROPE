using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FireTrapController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PressurePlate pressurePlate;
    [SerializeField] private List<ParticleSystem> fireParticles = new List<ParticleSystem>();
    [SerializeField] private ScreenHeatEffect heatEffect;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fireLoopSound;
    [SerializeField] private AudioClip fireRetractSound;
    [Range(0f, 1f)][SerializeField] private float maxVolume = 0.8f;

    [Header("Fire Settings")]
    [SerializeField] private float fireExpandSpeed = 5f;
    [SerializeField] private float fireRetractSpeed = -8f;

    private bool isTrapActive = false;

    void Start()
    {
        if (audioSource != null)
        {
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.clip = fireLoopSound;
            audioSource.volume = 0f;
        }

        // Đảm bảo module Velocity được bật
        foreach (var fire in fireParticles)
        {
            var velModule = fire.velocityOverLifetime;
            velModule.enabled = true;
        }

        // Kiểm tra trạng thái đầu game
        CheckTrapState();
    }

    void Update()
    {
        CheckTrapState();
    }

    void CheckTrapState()
    {
        // Logic: Đè lên đĩa (IsPressed) -> Tắt lửa. Không đè -> Bật lửa.
        bool shouldActive = !pressurePlate.IsPressed;

        if (shouldActive && !isTrapActive) ActivateTrap();
        else if (!shouldActive && isTrapActive) DeactivateTrap();
    }

    void ActivateTrap()
    {
        isTrapActive = true;
        if (heatEffect) heatEffect.SetHeat(true);

        foreach (var fire in fireParticles)
        {
            var velModule = fire.velocityOverLifetime;
            velModule.z = new ParticleSystem.MinMaxCurve(fireExpandSpeed);
            if (!fire.isPlaying) fire.Play();
        }

        if (audioSource != null && fireLoopSound != null)
        {
            audioSource.clip = fireLoopSound;
            audioSource.loop = true;
            audioSource.Play();
            StopAllCoroutines(); // Reset các fade cũ
            StartCoroutine(FadeAudio(audioSource, maxVolume, 0.5f));
        }
    }

    void DeactivateTrap()
    {
        isTrapActive = false;
        if (heatEffect) heatEffect.SetHeat(false);

        if (audioSource != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeAudio(audioSource, 0f, 0.3f));
            if (fireRetractSound != null) audioSource.PlayOneShot(fireRetractSound);
        }

        StartCoroutine(RetractFireRoutine());
    }

    IEnumerator RetractFireRoutine()
    {
        foreach (var fire in fireParticles)
        {
            var velModule = fire.velocityOverLifetime;
            velModule.z = new ParticleSystem.MinMaxCurve(fireRetractSpeed);
        }
        yield return new WaitForSeconds(0.5f);
        foreach (var fire in fireParticles) fire.Stop();
    }

    IEnumerator FadeAudio(AudioSource source, float targetVolume, float duration)
    {
        float startVolume = source.volume;
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, time / duration);
            yield return null;
        }
        source.volume = targetVolume;
    }
}