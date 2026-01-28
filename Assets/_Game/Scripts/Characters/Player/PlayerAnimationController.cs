using UnityEngine;

namespace StarterAssets
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationController : MonoBehaviour
    {
        [Header("Animation Smooth Settings")]
        [Tooltip("Thời gian làm mượt khi thay đổi hướng Strafe")]
        public float StrafeSmoothTime = 0.1f;

        [Header("Audio Settings")]
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        // References
        private Animator _animator;
        private CharacterController _controller; // Cần reference này để phát âm thanh tại vị trí nhân vật

        // Animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDStrafe;
        private int _animIDInputX;
        private int _animIDInputY;

        // Internal variables for smoothing
        private float _currentInputX;
        private float _currentInputY;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _controller = GetComponent<CharacterController>();
            AssignAnimationIDs();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");

            _animIDStrafe = Animator.StringToHash("Strafe");
            _animIDInputX = Animator.StringToHash("InputX");
            _animIDInputY = Animator.StringToHash("InputY");
        }


        public void UpdateGrounded(bool isGrounded)
        {
            if (_animator) _animator.SetBool(_animIDGrounded, isGrounded);
        }

        public void UpdateJump(bool isJumping)
        {
            if (_animator) _animator.SetBool(_animIDJump, isJumping);
        }

        public void UpdateFreeFall(bool isFalling)
        {
            if (_animator) _animator.SetBool(_animIDFreeFall, isFalling);
        }

        public void UpdateLocomotion(float animationBlend, float inputMagnitude)
        {
            if (_animator)
            {
                _animator.SetFloat(_animIDSpeed, animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        public void UpdateStrafe(Vector2 moveInput, bool isStrafing, float deltaTime)
        {
            if (!_animator) return;

            _animator.SetBool(_animIDStrafe, isStrafing);

            if (isStrafing)
            {
                _currentInputX = Mathf.Lerp(_currentInputX, moveInput.x, deltaTime / StrafeSmoothTime);
                _currentInputY = Mathf.Lerp(_currentInputY, moveInput.y, deltaTime / StrafeSmoothTime);

                if (moveInput.x == 0 && Mathf.Abs(_currentInputX) < 0.01f) _currentInputX = 0f;
                if (moveInput.y == 0 && Mathf.Abs(_currentInputY) < 0.01f) _currentInputY = 0f;

                _animator.SetFloat(_animIDInputX, _currentInputX);
                _animator.SetFloat(_animIDInputY, _currentInputY);
            }
            else
            {
                _currentInputX = 0f;
                _currentInputY = 0f;
                _animator.SetFloat(_animIDInputX, 0f);
                _animator.SetFloat(_animIDInputY, 0f);
            }
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }
}