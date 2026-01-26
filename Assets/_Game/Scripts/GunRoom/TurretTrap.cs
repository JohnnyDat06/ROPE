using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class TurretTrap : MonoBehaviour
{
    [Header("--- 1. Setup Elements ---")]
    public Transform coverCube;
    public Transform hiddenGun;
    public Transform firePoint;
    public ParticleSystem muzzleFlashVFX;

    [Header("--- 2. Audio Pro (Fix Lỗi Đuôi) ---")]
    public AudioClip fireSoundClip;
    [Range(0f, 1f)] public float baseVolume = 1.0f;
    public float pitchRandomness = 0.15f;

    [Header("--- 3. Combat Settings ---")]
    public float fireRate = 0.1f;
    public float minFireDuration = 7.0f;

    [Tooltip("Chọn Layer của Player và Tường. BỎ CHỌN Layer của Trigger hoặc Nắp che!")]
    public LayerMask hitLayers = ~0;

    [Header("--- 4. Animation Settings ---")]
    public Vector3 coverSlideVector = new Vector3(0.3f, 0f, 0f);
    public float coverSlideDuration = 0.2f;
    public Vector3 coverUpVector = new Vector3(0f, 1.5f, 0f);
    public float coverUpDuration = 0.3f;
    public Vector3 gunExtendVector = new Vector3(0f, 0f, 0.8f);
    public float gunExtendDuration = 0.4f;

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

        if (muzzleFlashVFX)
        {
            var main = muzzleFlashVFX.main;
            main.playOnAwake = false;
            muzzleFlashVFX.Stop();
        }
    }

    public void ActivateTrap(Transform playerTarget)
    {
        _pendingDeactivate = false;
        if (_isTrapActive) return;

        _isTrapActive = true;
        _isLocked = true;

        if (_mainRoutine != null) StopCoroutine(_mainRoutine);
        _mainRoutine = StartCoroutine(TrapSequenceRoutine(playerTarget));
    }

    public void DeactivateTrap()
    {
        _pendingDeactivate = true;

        // FIX ÂM THANH: Dừng bắn ngay lập tức khi có lệnh rút lui (dù chưa đóng nắp)
        // Lưu ý: Nếu đang Locked (trong 7s) thì chưa dừng vội.
        if (!_isLocked)
        {
            StopFiringImmediate();
        }

        if (!_isLocked && _isTrapActive)
        {
            if (_mainRoutine != null) StopCoroutine(_mainRoutine);
            _mainRoutine = StartCoroutine(CloseSequenceRoutine());
        }
    }

    // --- LOGIC CHÍNH ---
    private IEnumerator TrapSequenceRoutine(Transform target)
    {
        // Animation Mở...
        Vector3 slidePos = _initCoverPos + coverSlideVector;
        Vector3 popUpPos = slidePos + coverUpVector;
        Vector3 extendPos = _initGunPos + gunExtendVector;

        yield return StartCoroutine(MoveTransform(coverCube, _initCoverPos, slidePos, coverSlideDuration));
        yield return StartCoroutine(MoveTransform(coverCube, slidePos, popUpPos, coverUpDuration));
        yield return StartCoroutine(MoveTransform(hiddenGun, _initGunPos, extendPos, gunExtendDuration));

        // Giai đoạn Bắn
        if (muzzleFlashVFX) muzzleFlashVFX.Play();

        float timer = 0f;
        while (timer < minFireDuration)
        {
            timer += Time.deltaTime;
            HandleShooting(target);
            yield return null;
        }

        _isLocked = false; // Hết 7s khóa cứng

        if (_pendingDeactivate)
        {
            // Nếu người chơi đã ra lệnh thoát từ trước -> Đóng ngay
            yield return StartCoroutine(CloseSequenceRoutine());
        }
        else
        {
            // Nếu chưa -> Bắn tiếp
            while (!_pendingDeactivate)
            {
                HandleShooting(target);
                yield return null;
            }
            yield return StartCoroutine(CloseSequenceRoutine());
        }
    }

    private IEnumerator CloseSequenceRoutine()
    {
        StopFiringImmediate(); // Đảm bảo tắt mọi thứ trước khi thụt vào

        Vector3 slidePos = _initCoverPos + coverSlideVector;

        yield return StartCoroutine(MoveTransform(hiddenGun, hiddenGun.localPosition, _initGunPos, gunExtendDuration));
        yield return StartCoroutine(MoveTransform(coverCube, coverCube.localPosition, slidePos, coverUpDuration));
        yield return StartCoroutine(MoveTransform(coverCube, slidePos, _initCoverPos, coverSlideDuration));

        _isTrapActive = false;
        _isLocked = false;
        _mainRoutine = null;
    }

    // --- HÀM XỬ LÝ ÂM THANH & RAYCAST ---

    private void StopFiringImmediate()
    {
        if (muzzleFlashVFX) muzzleFlashVFX.Stop();
        if (_audioSource)
        {
            _audioSource.Stop(); // FIX: Cắt ngang tiếng súng ngay lập tức
        }
    }

    private void HandleShooting(Transform target)
    {
        if (Time.time >= _nextFireTime)
        {
            FireOneShot(target);
            _nextFireTime = Time.time + fireRate;
        }
    }

    private void FireOneShot(Transform target)
    {
        if (target == null) return;

        // 1. Âm thanh (Pitch Random)
        if (_audioSource && fireSoundClip)
        {
            _audioSource.pitch = Random.Range(1f - pitchRandomness, 1f + pitchRandomness);
            _audioSource.volume = baseVolume; // Reset volume
            _audioSource.PlayOneShot(fireSoundClip);
        }

        // 2. Raycast (FIX LỖI KHÔNG TRÚNG PLAYER)
        Vector3 direction = (target.position - firePoint.position).normalized;

        // QueryTriggerInteraction.Ignore: QUAN TRỌNG! Bỏ qua các Trigger Box (như vùng check phòng an ninh)
        if (Physics.Raycast(firePoint.position, direction, out RaycastHit hit, 100f, hitLayers, QueryTriggerInteraction.Ignore))
        {
            // Debug Log: In ra console xem nó trúng cái gì
            // Debug.Log($"Raycast Hit: {hit.collider.name} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");

            if (hit.collider.CompareTag("Player"))
            {
                // TRÚNG PLAYER -> MÀU ĐỎ
                Debug.DrawLine(firePoint.position, hit.point, Color.red, fireRate);
                Debug.Log($"<color=red><b>[TRÚNG MỤC TIÊU]</b></color> Player bị bắn! HP -10");
            }
            else
            {
                // TRÚNG VẬT CẢN (Tường, Nắp che...) -> MÀU VÀNG
                Debug.DrawLine(firePoint.position, hit.point, Color.yellow, fireRate);
                // Nếu thấy dòng này hiện tên "Trigger" hay "CoverCube" thì bạn biết phải chỉnh LayerMask
                Debug.Log($"<color=yellow>[BỊ CHẶN]</color> Đạn trúng: {hit.collider.name}");
            }
        }
        else
        {
            // KHÔNG TRÚNG GÌ -> MÀU XANH
            Debug.DrawRay(firePoint.position, direction * 100f, Color.green, fireRate);
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