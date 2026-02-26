using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FireTrapController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PressurePlate pressurePlate;
    [SerializeField] private List<ParticleSystem> fireParticles = new List<ParticleSystem>();
    [SerializeField] private ScreenHeatEffect heatEffect;

    [Header("Damage Setup")]
    [Tooltip("Kéo TẤT CẢ các Box Collider (IsTrigger) của các vùng lửa vào danh sách này")]
    // 1. Chuyển từ Collider đơn sang một List<Collider>
    [SerializeField] private List<Collider> damageColliders = new List<Collider>();

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

        foreach (var fire in fireParticles)
        {
            var velModule = fire.velocityOverLifetime;
            velModule.enabled = true;
        }

        // 2. Tắt toàn bộ Collider sát thương lúc mới vào game nếu bẫy chưa kích hoạt
        foreach (var col in damageColliders)
        {
            if (col != null) col.enabled = false;
        }

        CheckTrapState();
    }

    void Update()
    {
        CheckTrapState();
    }

    void CheckTrapState()
    {
        // Bàn đạp bị đè -> Tắt lửa | Bàn đạp trống -> Bật lửa
        bool shouldBeActive = !pressurePlate.IsPressed;

        if (shouldBeActive && !isTrapActive) ActivateTrap();
        else if (!shouldBeActive && isTrapActive) DeactivateTrap();
    }

    void ActivateTrap()
    {
        isTrapActive = true;
        if (heatEffect) heatEffect.SetHeat(true);

        // 3. BẬT SÁT THƯƠNG: Duyệt qua danh sách và bật tất cả Box Collider lên
        foreach (var col in damageColliders)
        {
            if (col != null) col.enabled = true;
        }

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
            StopAllCoroutines();
            StartCoroutine(FadeAudio(audioSource, maxVolume, 0.5f));
        }
    }

    void DeactivateTrap()
    {
        isTrapActive = false;
        if (heatEffect) heatEffect.SetHeat(false);

        // 4. TẮT SÁT THƯƠNG: Duyệt qua danh sách và tắt tất cả Box Collider đi
        foreach (var col in damageColliders)
        {
            if (col != null) col.enabled = false;
        }

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