using UnityEngine;
using System.Collections;
using DatScript; // <-- THÊM DÒNG NÀY để gọi được PlayerHealth

[RequireComponent(typeof(AudioSource))]
public class TurretTrap : MonoBehaviour
{
    [Header("--- 1. Setup Elements ---")]
    public Transform coverCube;
    public Transform hiddenGun;
    [Tooltip("Trục Z (Mũi tên xanh) của object này là hướng đạn bắn")]
    public Transform firePoint;
    public ParticleSystem muzzleFlashVFX;

    [Header("--- 2. Post-Fire Smoke ---")]
    public ParticleSystem smokeVFX;
    public float smokeDelayDuration = 2.0f;

    [Header("--- 3. Audio Settings ---")]
    public AudioClip fireSoundClip;
    [Range(0f, 1f)] public float baseVolume = 1.0f;
    public float pitchRandomness = 0.15f;

    [Header("--- 4. Combat Settings ---")]
    public float fireRate = 0.1f;
    public float minFireDuration = 7.0f;
    public LayerMask hitLayers = ~0;

    [Header("--- 5. Animation Settings ---")]
    public Vector3 coverSlideVector = new Vector3(0.3f, 0f, 0f);
    public float coverSlideDuration = 0.2f;
    public Vector3 coverUpVector = new Vector3(0f, 1.5f, 0f);
    public float coverUpDuration = 0.3f;
    public Vector3 gunExtendVector = new Vector3(0f, 0f, 0.8f);
    public float gunExtendDuration = 0.4f;

    [Header("--- 6. Damage Settings ---")]
    [Tooltip("Lượng máu bị trừ mỗi khi trúng 1 viên đạn")]
    public float damagePerShot = 5f; // <-- THÊM BIẾN NÀY

    // --- Private ---
    private Vector3 _initCoverPos;
    private Vector3 _initGunPos;
    private AudioSource _audioSource;
    private Coroutine _mainRoutine;

    private bool _isTrapActive = false;
    private bool _isLocked = false;
    private bool _pendingDeactivate = false;
    private float _nextFireTime = 0f;

    void Awake()
    {
        if (coverCube) _initCoverPos = coverCube.localPosition;
        if (hiddenGun) _initGunPos = hiddenGun.localPosition;

        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;

        if (muzzleFlashVFX) { var m = muzzleFlashVFX.main; m.playOnAwake = false; muzzleFlashVFX.Stop(); }
        if (smokeVFX) { var m = smokeVFX.main; m.playOnAwake = false; smokeVFX.Stop(); }
    }

    public void ActivateTrap()
    {
        _pendingDeactivate = false;
        if (_isTrapActive) return;

        _isTrapActive = true;
        _isLocked = true;

        if (_mainRoutine != null) StopCoroutine(_mainRoutine);
        _mainRoutine = StartCoroutine(TrapSequenceRoutine());
    }

    public void DeactivateTrap()
    {
        _pendingDeactivate = true;
        if (!_isLocked) StopFiringImmediate();

        if (!_isLocked && _isTrapActive)
        {
            if (_mainRoutine != null) StopCoroutine(_mainRoutine);
            _mainRoutine = StartCoroutine(CloseSequenceRoutine());
        }
    }

    private IEnumerator TrapSequenceRoutine()
    {
        Vector3 slidePos = _initCoverPos + coverSlideVector;
        Vector3 popUpPos = slidePos + coverUpVector;
        Vector3 extendPos = _initGunPos + gunExtendVector;

        yield return StartCoroutine(MoveTransform(coverCube, _initCoverPos, slidePos, coverSlideDuration));
        yield return StartCoroutine(MoveTransform(coverCube, slidePos, popUpPos, coverUpDuration));
        yield return StartCoroutine(MoveTransform(hiddenGun, _initGunPos, extendPos, gunExtendDuration));

        if (muzzleFlashVFX) muzzleFlashVFX.Play();
        if (smokeVFX) smokeVFX.Stop();

        float timer = 0f;
        while (timer < minFireDuration)
        {
            timer += Time.deltaTime;
            HandleShooting();
            yield return null;
        }

        _isLocked = false;

        if (_pendingDeactivate) yield return StartCoroutine(CloseSequenceRoutine());
        else
        {
            while (!_pendingDeactivate)
            {
                HandleShooting();
                yield return null;
            }
            yield return StartCoroutine(CloseSequenceRoutine());
        }
    }

    private IEnumerator CloseSequenceRoutine()
    {
        StopFiringImmediate();

        if (smokeVFX)
        {
            smokeVFX.Play();
            yield return new WaitForSeconds(smokeDelayDuration);
            smokeVFX.Stop();
        }

        Vector3 slidePos = _initCoverPos + coverSlideVector;
        yield return StartCoroutine(MoveTransform(hiddenGun, hiddenGun.localPosition, _initGunPos, gunExtendDuration));
        yield return StartCoroutine(MoveTransform(coverCube, coverCube.localPosition, slidePos, coverUpDuration));
        yield return StartCoroutine(MoveTransform(coverCube, slidePos, _initCoverPos, coverSlideDuration));

        _isTrapActive = false;
        _isLocked = false;
        _mainRoutine = null;
    }

    private void StopFiringImmediate()
    {
        if (muzzleFlashVFX) muzzleFlashVFX.Stop();
        if (_audioSource) _audioSource.Stop();
    }

    private void HandleShooting()
    {
        if (Time.time >= _nextFireTime)
        {
            FireStraightShot();
            _nextFireTime = Time.time + fireRate;
        }
    }

    // --- LOGIC GÂY SÁT THƯƠNG NẰM Ở ĐÂY ---
    private void FireStraightShot()
    {
        if (_audioSource && fireSoundClip)
        {
            _audioSource.pitch = Random.Range(1f - pitchRandomness, 1f + pitchRandomness);
            _audioSource.volume = baseVolume;
            _audioSource.PlayOneShot(fireSoundClip);
        }

        Vector3 direction = firePoint.forward;

        if (Physics.Raycast(firePoint.position, direction, out RaycastHit hit, 100f, hitLayers, QueryTriggerInteraction.Ignore))
        {
            // 1. Kiểm tra xem tia đạn có đụng trúng Player không
            if (hit.collider.CompareTag("Player"))
            {
                // 2. Gọi hàm trừ máu thông qua biến instance tĩnh (Singleton)
                if (PlayerHealth.instance != null)
                {
                    PlayerHealth.instance.TakeDamage(damagePerShot);
                    // (Tuỳ chọn) Bạn có thể sinh ra Particle System máu văng tại hit.point ở đây
                }
            }
        }
    }

    private IEnumerator MoveTransform(Transform obj, Vector3 start, Vector3 end, float duration)
    {
        if (duration <= 0f) { obj.localPosition = end; yield break; }
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            obj.localPosition = Vector3.Lerp(start, end, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
        obj.localPosition = end;
    }
}