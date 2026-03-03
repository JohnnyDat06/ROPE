using System.Collections;
using Unity.Behavior;
using UnityEngine;

[RequireComponent(typeof(BehaviorGraphAgent))]
public class DroidOilCombat : MonoBehaviour
{
	[Header("References")]
	[Tooltip("Agent chứa Blackboard của con Droid này")]
	[SerializeField] private BehaviorGraphAgent _behaviorAgent;

	[Header("Gun Setup")]
	[Tooltip("Vị trí nòng súng để sinh đạn")]
	[SerializeField] private Transform _firePoint;

	[Tooltip("Prefab của viên đạn bắn ra (DroidOilBullet)")]
	[SerializeField] private GameObject _bulletPrefab;

	[Header("Burst Settings")]
	[Tooltip("Số viên đạn trong 1 lần xả")]
	[SerializeField] private int _bulletsPerBurst = 3;

	[Tooltip("Thời gian chờ giữa 2 viên đạn (giây)")]
	[SerializeField] private float _timeBetweenBullets = 0.3f;

	[Tooltip("Thời gian nghỉ nạp đạn sau khi xả hết 3 viên (giây)")]
	[SerializeField] private float _cooldownBetweenBursts = 2.0f;

	[Header("Accuracy Settings")]
	[Tooltip("Độ lệch nòng súng (Góc độ). Số càng to đạn bay càng tòe loe.")]
	[SerializeField] private float _spreadAngle = 2.5f;

	[Header("Blackboard Keys")]
	[SerializeField] private string _detectedVariableName = "IsDetected";
	[SerializeField] private string _targetVariableName = "Target";

	[Header("VFX & Audio")]
	[Tooltip("Hiệu ứng chớp lửa đầu nòng (Tùy chọn)")]
	[SerializeField] private ParticleSystem _muzzleFlash;

	[SerializeField] private AudioSource _audioSource;
	[SerializeField] private AudioClip _shootSound;

	private Coroutine _shootingCoroutine;
	private Transform _currentTarget;
	private bool _isShooting = false;

	private void Start()
	{
		if (_behaviorAgent == null)
		{
			_behaviorAgent = GetComponent<BehaviorGraphAgent>();
		}
	}

	private void Update()
	{
		// Kiểm tra thêm BlackboardReference để tránh lỗi Null
		if (_behaviorAgent == null || _behaviorAgent.BlackboardReference == null) return;

		// 1. Đọc trực tiếp giá trị từ Blackboard mỗi frame
		bool isDetected = false;
		GameObject targetObj = null;

		_behaviorAgent.BlackboardReference.GetVariableValue<bool>(_detectedVariableName, out isDetected);
		_behaviorAgent.BlackboardReference.GetVariableValue<GameObject>(_targetVariableName, out targetObj);

		if (targetObj != null)
		{
			_currentTarget = targetObj.transform;
		}

		// 2. Xử lý Logic bật/tắt súng
		if (isDetected && _currentTarget != null)
		{
			// Phát hiện Player -> Bắt đầu xả đạn (nếu chưa xả)
			if (!_isShooting)
			{
				_isShooting = true;
				_shootingCoroutine = StartCoroutine(ShootRoutine());
			}
		}
		else
		{
			// Mất dấu Player hoặc Target bị Null -> Ngừng bắn ngay lập tức
			if (_isShooting)
			{
				StopShooting();
			}
		}
	}

	private void StopShooting()
	{
		_isShooting = false;
		if (_shootingCoroutine != null)
		{
			StopCoroutine(_shootingCoroutine);
			_shootingCoroutine = null;
		}
	}

	private IEnumerator ShootRoutine()
	{
		// Vòng lặp liên tục chừng nào _isShooting còn là true
		while (_isShooting)
		{
			// 1. Xả 1 loạt (Burst) 3 viên
			for (int i = 0; i < _bulletsPerBurst; i++)
			{
				// Kiểm tra an toàn: Lỡ mất dấu giữa chừng lúc đang xả thì ngắt ngang luôn
				if (!_isShooting || _currentTarget == null) yield break;

				FireSingleBullet();

				// Đợi 0.3s trước khi bắn viên tiếp theo
				yield return new WaitForSeconds(_timeBetweenBullets);
			}

			// 2. Nghỉ 2s nạp đạn (Dùng vòng lặp while để có thể ngắt ngang bất cứ lúc nào)
			float cooldownTimer = 0f;
			while (cooldownTimer < _cooldownBetweenBursts)
			{
				if (!_isShooting) yield break;
				cooldownTimer += Time.deltaTime;
				yield return null; // Đợi sang frame tiếp theo
			}
		}
	}

	private void FireSingleBullet()
	{
		if (_firePoint == null || _bulletPrefab == null || _currentTarget == null) return;

		// 1. Tính toán hướng ngắm CHÍNH XÁC vào ngực Player
		Vector3 targetPos = _currentTarget.position + Vector3.up * 1.5f;
		Vector3 shootDirection = (targetPos - _firePoint.position).normalized;
		Quaternion exactRotation = Quaternion.LookRotation(shootDirection);

		// 2. Tính toán độ lệch NGẪU NHIÊN (Spread)
		// Random 2 trục X, Y tạo thành một vòng tròn sai số quanh hồng tâm
		Vector2 randomSpread = Random.insideUnitCircle * _spreadAngle;
		Quaternion spreadRotation = Quaternion.Euler(randomSpread.x, randomSpread.y, 0f);

		// 3. Cộng dồn góc lệch vào góc ngắm chính xác (Trong Unity, nhân 2 Quaternion = Cộng góc)
		Quaternion finalBulletRotation = exactRotation * spreadRotation;

		// 4. Sinh đạn với góc bắn đã làm lệch
		Instantiate(_bulletPrefab, _firePoint.position, finalBulletRotation);

		// 5. Chạy VFX và Sound
		if (_muzzleFlash != null) _muzzleFlash.Play();
		if (_audioSource != null && _shootSound != null) _audioSource.PlayOneShot(_shootSound);
	}
}