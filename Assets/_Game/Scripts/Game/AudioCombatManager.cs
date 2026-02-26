using UnityEngine;

public class AudioCombatManager : MonoBehaviour
{
    public static AudioCombatManager Instance { get; private set; }

    [Header("Audio Settings")]
    [Tooltip("Kéo AudioSource vào đây")]
    public AudioSource musicSource;
    
    [Tooltip("Clip nhạc chiến đấu")]
    public AudioClip combatMusicClip;

    [Header("Logic Settings")]
    [Tooltip("Thời gian chờ trước khi quyết định dừng nhạc nếu không còn gây sát thương (giây)")]
    public float keepCombatTime = 3.0f;

    private float _lastDamageTime;
    private bool _isInCombat;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();
        
        if(musicSource != null && combatMusicClip != null)
        {
            musicSource.clip = combatMusicClip;
        }
    }

    private void Update()
    {
        if (_isInCombat)
        {
            if (Time.time - _lastDamageTime > keepCombatTime)
            {
                ExitCombatMode();
            }
        }
    }

    public void TriggerCombatMusic()
    {
        _lastDamageTime = Time.time;

        if (!_isInCombat || !musicSource.loop)
        {
            EnterCombatMode();
        }
    }

    private void EnterCombatMode()
    {
        _isInCombat = true;
        
        if (musicSource != null && combatMusicClip != null)
        {
            musicSource.loop = true;
            
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }
    }

    private void ExitCombatMode()
    {
        _isInCombat = false;

        if (musicSource != null)
        {
            musicSource.loop = false;
        }
    }
}