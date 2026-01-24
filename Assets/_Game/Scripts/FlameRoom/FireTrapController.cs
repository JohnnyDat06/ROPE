using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FireTrapController : MonoBehaviour
{
    [Header("Liên kết Component")]
    [SerializeField] private PressurePlate pressurePlate;
    [SerializeField] private List<ParticleSystem> fireParticles = new List<ParticleSystem>();
    [SerializeField] private ScreenHeatEffect heatEffect;

    // --- THÊM PHẦN NÀY ---
    [Header("Âm thanh (Audio)")]
    [SerializeField] private AudioSource audioSource; // Gắn AudioSource vào đây
    [SerializeField] private AudioClip fireLoopSound; // Tiếng lửa cháy (Loop)
    [SerializeField] private AudioClip fireRetractSound; // Tiếng lửa hút vào (One shot)
    [Range(0f, 1f)][SerializeField] private float maxVolume = 0.8f;

    [Header("Thông số Lửa")]
    [SerializeField] private float fireExpandSpeed = 5f;
    [SerializeField] private float fireRetractSpeed = -8f;

    private bool isTrapActive = false;

    void Start()
    {
        // Setup AudioSource ban đầu
        if (audioSource != null)
        {
            audioSource.loop = true; // Tiếng lửa chính phải lặp
            audioSource.playOnAwake = false;
            audioSource.clip = fireLoopSound;
            audioSource.volume = 0f; // Bắt đầu im lặng
        }

        // Setup Particle như cũ...
        foreach (var fire in fireParticles)
        {
            var velModule = fire.velocityOverLifetime;
            velModule.enabled = true;
        }
        CheckTrapState();
    }

    void Update()
    {
        CheckTrapState();
        // Logic gây damage...
    }

    void CheckTrapState()
    {
        bool shouldActive = !pressurePlate.IsPressed;

        if (shouldActive && !isTrapActive) ActivateTrap();
        else if (!shouldActive && isTrapActive) DeactivateTrap();
    }

    void ActivateTrap()
    {
        isTrapActive = true;
        if (heatEffect) heatEffect.SetHeat(true);

        // 1. Kích hoạt Particle (như cũ)
        foreach (var fire in fireParticles)
        {
            var velModule = fire.velocityOverLifetime;
            velModule.z = new ParticleSystem.MinMaxCurve(fireExpandSpeed);
            if (!fire.isPlaying) fire.Play();
        }

        // 2. Xử lý Âm thanh: BẬT
        if (audioSource != null && fireLoopSound != null)
        {
            audioSource.clip = fireLoopSound;
            audioSource.loop = true;
            audioSource.Play();
            // Mẹo nhỏ: Fade in âm thanh (tăng dần volume) cho mượt
            StartCoroutine(FadeAudio(audioSource, maxVolume, 0.5f));
        }
    }

    void DeactivateTrap()
    {
        isTrapActive = false;
        if (heatEffect) heatEffect.SetHeat(false);

        // 1. Xử lý Âm thanh: CHUYỂN
        if (audioSource != null)
        {
            // Ngắt tiếng loop từ từ
            StartCoroutine(FadeAudio(audioSource, 0f, 0.3f));

            // Chơi tiếng "Hút vào" (PlayOneShot để nó đè lên tiếng loop đang tắt)
            if (fireRetractSound != null)
            {
                audioSource.PlayOneShot(fireRetractSound);
            }
        }

        // 2. Gọi Coroutine tắt lửa (như cũ)
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

    // Coroutine giúp âm thanh to dần/nhỏ dần thay vì cắt cái rụp
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