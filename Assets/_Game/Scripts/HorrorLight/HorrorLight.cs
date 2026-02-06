using UnityEngine;
using System.Collections;

public class HorrorLight : MonoBehaviour
{
    [Header("--- CÀI ĐẶT ĐÈN ---")]
    public Light myLight;
    public float maxIntensity = 1000.0f;

    [Header("--- CÀI ĐẶT ÂM THANH ---")]
    public AudioSource audioSource;
    public AudioClip flickerSound;
    [Range(0, 1)]
    public float soundVolume = 1.0f;
    public float maxSoundDuration = 1.0f;

    [Header("--- CẤU HÌNH CHU KỲ ---")]
    public float burstDurationMin = 3.0f; // Tăng lên để chớp lâu hơn
    public float burstDurationMax = 6.0f;

    public float breakDurationMin = 2.0f; // Giảm xuống để ít nghỉ hơn
    public float breakDurationMax = 4.0f;

    [Header("--- TỐC ĐỘ CHỚP ---")]
    public float minFlickerSpeed = 0.05f;
    public float maxFlickerSpeed = 0.4f;

    private void Start()
    {
        if (myLight == null) myLight = GetComponent<Light>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (audioSource != null && flickerSound != null)
        {
            audioSource.clip = flickerSound;
            // SỬA QUAN TRỌNG: Bật Loop để tiếng rè rè kéo dài liên tục khi đèn sáng
            audioSource.loop = true;
            audioSource.playOnAwake = false;
        }

        StartCoroutine(LifeCycleLoop());
    }

    IEnumerator LifeCycleLoop()
    {
        while (true)
        {
            // --- GIAI ĐOẠN HOẠT ĐỘNG ---
            float burstTime = Random.Range(burstDurationMin, burstDurationMax);
            float burstTimer = 0f;

            while (burstTimer < burstTime)
            {
                // 1. Tắt đèn & Cắt tiếng
                if (Random.value > 0.3f)
                {
                    myLight.enabled = false;
                    audioSource.Stop(); // Dùng Stop() thay vì ngắt Loop
                    float offTime = Random.Range(0.05f, 0.2f);
                    yield return new WaitForSeconds(offTime);
                    burstTimer += offTime;
                }

                // 2. Bật đèn & Phát tiếng
                myLight.enabled = true;
                myLight.intensity = Random.Range(100f, maxIntensity);

                if (audioSource != null && flickerSound != null)
                {
                    if (!audioSource.isPlaying) // Chỉ Play nếu chưa Play
                    {
                        audioSource.volume = soundVolume;
                        audioSource.pitch = Random.Range(0.9f, 1.1f);
                        audioSource.Play();
                    }
                }

                float lightOnTime = Random.Range(minFlickerSpeed, maxFlickerSpeed);

                // Đếm ngược để tắt tiếng nếu quá 1s (Dù đang Loop cũng bắt tắt)
                float subTimer = 0f;
                while (subTimer < lightOnTime)
                {
                    subTimer += Time.deltaTime;
                    if (subTimer >= maxSoundDuration && audioSource.isPlaying)
                    {
                        audioSource.Stop();
                    }
                    yield return null;
                }
                burstTimer += lightOnTime;
            }

            // --- GIAI ĐOẠN NGHỈ NGƠI ---
            myLight.enabled = false;
            audioSource.Stop();
            yield return new WaitForSeconds(Random.Range(breakDurationMin, breakDurationMax));
        }
    }
}