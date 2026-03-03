using UnityEngine;
using System.Collections;
using System.Reflection;
using DatScript;

[RequireComponent(typeof(AudioSource))]
public class TurretTrap : MonoBehaviour
{
    [Header("--- 1. Setup Elements ---")]
    public Transform coverCube;
    public Transform hiddenGun;
    public Transform firePoint;
    public ParticleSystem muzzleFlashVFX;
    public ParticleSystem smokeVFX;

    [Header("--- 2. Combat Settings ---")]
    public float fireRate = 0.1f;
    public float maxFireDuration = 7.0f; // Dừng sau 7s
    public float extraFireAfterExit = 1.5f; // Bắn đuổi 1.5s khi thoát
    public float damagePerShot = 5f;
    public LayerMask hitLayers = ~0;

    [Header("--- 3. Audio & Animation ---")]
    public AudioClip fireSoundClip;
    [Range(0f, 1f)] public float baseVolume = 1.0f;
    public float pitchRandomness = 0.15f;
    public float animDuration = 0.3f;
    public Vector3 coverSlideVector = new Vector3(0.3f, 0f, 0f);
    public Vector3 coverUpVector = new Vector3(0f, 1.5f, 0f);
    public Vector3 gunExtendVector = new Vector3(0f, 0f, 0.8f);

    private Vector3 _initCoverPos;
    private Vector3 _initGunPos;
    private AudioSource _audioSource;
    private Coroutine _mainRoutine;

    private bool _isTrapActive = false;
    private bool _isPlayerInside = false;
    private float _nextFireTime = 0f;
    private FieldInfo _hpField;

    void Awake()
    {
        if (coverCube) _initCoverPos = coverCube.localPosition;
        if (hiddenGun) _initGunPos = hiddenGun.localPosition;

        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;

        _hpField = typeof(PlayerHealth).GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);

        // Tắt Particle ngay từ trong trứng nước
        ForceStopParticles();
    }

    void Start()
    {
        // Ép khối và súng luôn ở vị trí tắt/đóng hoàn toàn khi vừa bật game
        if (coverCube) coverCube.localPosition = _initCoverPos;
        if (hiddenGun) hiddenGun.localPosition = _initGunPos;

        // Khẳng định lại một lần nữa ở Start cho chắc chắn
        ForceStopParticles();
        StopFiringImmediate();
    }

    // Hàm ép tắt và xóa sạch dấu vết của Particle
    private void ForceStopParticles()
    {
        if (muzzleFlashVFX != null)
        {
            var mainFlash = muzzleFlashVFX.main;
            mainFlash.playOnAwake = false;
            // Dừng tạo hạt mới VÀ xóa luôn các hạt đang bay lơ lửng
            muzzleFlashVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (smokeVFX != null)
        {
            var mainSmoke = smokeVFX.main;
            mainSmoke.playOnAwake = false;
            smokeVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public void ActivateTrap()
    {
        _isPlayerInside = true;
        if (_isTrapActive) return;
        if (_mainRoutine != null) StopCoroutine(_mainRoutine);
        _mainRoutine = StartCoroutine(TrapLifecycleRoutine());
    }

    public void DeactivateTrap()
    {
        _isPlayerInside = false;

        // Đảm bảo nếu bẫy đang không chạy, nó phải ở trạng thái ĐÓNG
        if (!_isTrapActive)
        {
            if (coverCube) coverCube.localPosition = _initCoverPos;
            if (hiddenGun) hiddenGun.localPosition = _initGunPos;
        }
    }

    private IEnumerator TrapLifecycleRoutine()
    {
        _isTrapActive = true;

        // Mở bẫy
        yield return StartCoroutine(AnimateParts(_initCoverPos + coverSlideVector, _initCoverPos + coverSlideVector + coverUpVector, _initGunPos + gunExtendVector));

        if (muzzleFlashVFX) muzzleFlashVFX.Play();

        float totalTimer = 0f;
        float exitGraceTimer = 0f;

        while (totalTimer < maxFireDuration)
        {
            // CẮT NGAY LẬP TỨC NẾU MÁU BẰNG 0
            if (IsPlayerDead()) break;

            if (!_isPlayerInside)
            {
                exitGraceTimer += Time.deltaTime;
                if (exitGraceTimer >= extraFireAfterExit) break;
            }
            else
            {
                exitGraceTimer = 0f;
            }

            totalTimer += Time.deltaTime;
            ExecuteFiring();
            yield return null;
        }

        // TẮT SÚNG VÀ ÂM THANH TỨC THÌ
        StopFiringImmediate();

        // Chỉ xả khói rườm rà nếu người chơi còn sống và tự thoát được
        if (!IsPlayerDead() && smokeVFX)
        {
            smokeVFX.Play();
            yield return new WaitForSeconds(1f);
            smokeVFX.Stop();
        }

        // Đóng bẫy nhanh chóng
        yield return StartCoroutine(AnimateParts(_initCoverPos + coverSlideVector, _initCoverPos, _initGunPos, true));

        _isTrapActive = false;
        _mainRoutine = null;
    }

    private void ExecuteFiring()
    {
        if (Time.time >= _nextFireTime)
        {
            if (_audioSource && fireSoundClip)
            {
                _audioSource.pitch = Random.Range(1f - pitchRandomness, 1f + pitchRandomness);
                _audioSource.PlayOneShot(fireSoundClip, baseVolume);
            }

            if (Physics.Raycast(firePoint.position, firePoint.forward, out RaycastHit hit, 100f, hitLayers))
            {
                if (hit.collider.CompareTag("Player") && PlayerHealth.instance != null)
                {
                    PlayerHealth.instance.TakeDamage(damagePerShot);
                }
            }
            _nextFireTime = Time.time + fireRate;
        }
    }

    private bool IsPlayerDead()
    {
        if (PlayerHealth.instance == null || _hpField == null) return false;
        return (float)_hpField.GetValue(PlayerHealth.instance) <= 0;
    }

    private void StopFiringImmediate()
    {
        if (muzzleFlashVFX) muzzleFlashVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (_audioSource) _audioSource.Stop();
    }

    private IEnumerator AnimateParts(Vector3 slide, Vector3 up, Vector3 gun, bool reverse = false)
    {
        if (!reverse)
        {
            yield return StartCoroutine(LerpPos(coverCube, coverCube.localPosition, slide, animDuration));
            yield return StartCoroutine(LerpPos(coverCube, coverCube.localPosition, up, animDuration));
            yield return StartCoroutine(LerpPos(hiddenGun, hiddenGun.localPosition, gun, animDuration));
        }
        else
        {
            yield return StartCoroutine(LerpPos(hiddenGun, hiddenGun.localPosition, gun, animDuration));
            yield return StartCoroutine(LerpPos(coverCube, coverCube.localPosition, slide, animDuration));
            yield return StartCoroutine(LerpPos(coverCube, coverCube.localPosition, up, animDuration));
        }
    }

    private IEnumerator LerpPos(Transform t, Vector3 start, Vector3 end, float time)
    {
        float elapsed = 0;
        while (elapsed < time)
        {
            t.localPosition = Vector3.Lerp(start, end, Mathf.SmoothStep(0, 1, elapsed / time));
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localPosition = end;
    }
}