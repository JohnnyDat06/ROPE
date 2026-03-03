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

	// [MỚI] Bắt sự kiện khi Component này bị tắt (ví dụ: bị gọi enabled = false khi chết)
	private void OnDisable()
	{
		StopShooting();
	}

	private void Update()
	{
		if (_behaviorAgent == null || _behaviorAgent.BlackboardReference == null) return;

		bool isDetected = false;
		GameObject targetObj = null;

		_behaviorAgent.BlackboardReference.GetVariableValue<bool>(_detectedVariableName, out isDetected);
		_behaviorAgent.BlackboardReference.GetVariableValue<GameObject>(_targetVariableName, out targetObj);

		if (targetObj != null)
		{
			_currentTarget = targetObj.transform;
		}

		if (isDetected && _currentTarget != null)
		{
			if (!_isShooting)
			{
				_isShooting = true;
				_shootingCoroutine = StartCoroutine(ShootRoutine());
			}
		}
		else
		{
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
		while (_isShooting)
		{
			for (int i = 0; i < _bulletsPerBurst; i++)
			{
				if (!_isShooting || _currentTarget == null) yield break;

				FireSingleBullet();

				yield return new WaitForSeconds(_timeBetweenBullets);
			}

			float cooldownTimer = 0f;
			while (cooldownTimer < _cooldownBetweenBursts)
			{
				if (!_isShooting) yield break;
				cooldownTimer += Time.deltaTime;
				yield return null;
			}
		}
	}

	private void FireSingleBullet()
	{
		if (_firePoint == null || _bulletPrefab == null || _currentTarget == null) return;

		Vector3 targetPos = _currentTarget.position;

		Collider targetCollider = _currentTarget.GetComponent<Collider>();
		if (targetCollider != null)
		{
			targetPos = targetCollider.bounds.center;
		}
		else
		{
			targetPos += Vector3.up * 1.5f;
		}

		Vector3 shootDirection = (targetPos - _firePoint.position).normalized;
		Quaternion exactRotation = Quaternion.LookRotation(shootDirection);

		Vector2 randomSpread = Random.insideUnitCircle * _spreadAngle;
		Quaternion spreadRotation = Quaternion.Euler(randomSpread.x, randomSpread.y, 0f);

		Quaternion finalBulletRotation = exactRotation * spreadRotation;

		Instantiate(_bulletPrefab, _firePoint.position, finalBulletRotation);

		if (_muzzleFlash != null) _muzzleFlash.Play();
		if (_audioSource != null && _shootSound != null) _audioSource.PlayOneShot(_shootSound);
	}
}