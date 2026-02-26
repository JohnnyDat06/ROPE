using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FireTrapController : MonoBehaviour
{
    [Header("Liên kết Logic")]
    [SerializeField] private PressurePlate pressurePlate;
    [SerializeField] private ScreenHeatEffect heatEffect;

    [Header("Hệ thống Lửa & Sát thương")]
    [Tooltip("Kéo các Particle System lửa vào đây")]
    [SerializeField] private List<ParticleSystem> fireParticles = new List<ParticleSystem>();
    [Tooltip("Kéo các Box Collider (Is Trigger) dùng để gây sát thương vào đây")]
    [SerializeField] private List<Collider> damageColliders = new List<Collider>();

    [Header("Cài đặt Âm thanh")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fireLoopSound;
    [SerializeField] private AudioClip fireRetractSound;
    [Range(0f, 1f)][SerializeField] private float maxVolume = 0.8f;

    [Header("Cài đặt Lửa")]
    [SerializeField] private float fireExpandSpeed = 5f;
    [SerializeField] private float fireRetractSpeed = -8f;

    private bool isTrapActive = false;

    IEnumerator Start()
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

        // Tắt toàn bộ vùng sát thương ngay từ đầu game để đảm bảo an toàn
        foreach (var col in damageColliders)
        {
            if (col != null) col.enabled = false;
        }

        // Đợi một chút để vật lý ổn định rồi mới kiểm tra trạng thái đĩa
        yield return new WaitForSeconds(0.1f);
        CheckTrapState();
    }

    void Update()
    {
        CheckTrapState();
    }

    void CheckTrapState()
    {
        // Nếu đĩa KHÔNG bị đè (IsPressed = false) -> Kích hoạt bẫy lửa
        bool shouldActive = !pressurePlate.IsPressed;

        if (shouldActive && !isTrapActive) ActivateTrap();
        else if (!shouldActive && isTrapActive) DeactivateTrap();
    }

    void ActivateTrap()
    {
        isTrapActive = true;
        if (heatEffect) heatEffect.SetHeat(true);

        // Bật vùng gây sát thương cùng lúc với lửa cháy
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

        // Tắt vùng gây sát thương ngay khi lửa rút lại
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