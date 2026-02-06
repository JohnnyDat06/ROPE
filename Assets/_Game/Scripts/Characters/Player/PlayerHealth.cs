using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

namespace DatScript
{
    public class PlayerHealth : MonoBehaviour
    {
        public static PlayerHealth instance;

        [Header("Health Settings")]
        public float maxHealth = 100f;
        private float currentHealth; 

        [Header("UI Reference")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private float lerpSpeed = 5f;
        private Animator animator;
        private ThirdPersonController playerController;
        private StarterAssetsInputs playerInput;
        private ActiveWeapon activeWeapon;

        private void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);
        }

        void Start()
        {
            activeWeapon = GetComponent<ActiveWeapon>();
            playerController = GetComponent<ThirdPersonController>();
            playerInput = GetComponent<StarterAssetsInputs>();
            animator = GetComponent<Animator>();

            currentHealth = maxHealth;

            if (healthSlider != null) healthSlider.value = 1f;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H)) TakeDamage(10f);
            if (Input.GetKeyDown(KeyCode.J)) Heal(10f);

            HandleHealthBarSmoothness();
        }

        private void HandleHealthBarSmoothness()
        {
            if (healthSlider != null)
            {
                float targetFillAmount = currentHealth / maxHealth;
                healthSlider.value = Mathf.Lerp(healthSlider.value, targetFillAmount, lerpSpeed * Time.deltaTime);
            }
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;

            if (healthSlider != null) healthSlider.value = 1f;

            if (animator != null)
            {
                animator.ResetTrigger("IsDead");
                animator.Rebind();
            }

            playerController.enabled = true;
            playerInput.enabled = true;
            if (activeWeapon != null) activeWeapon.enabled = true;

            playerInput.cursorLocked = true;
            playerInput.cursorInputForLook = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void TakeDamage(float damageAmount)
        {
            if (currentHealth <= 0) return;

            currentHealth -= damageAmount;

            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);            

            if (currentHealth <= 0)
            {
                if (animator != null)
                {
                    animator.SetTrigger("IsDead");
                    if (activeWeapon != null) activeWeapon.enabled = false;
                    playerController.enabled = false;
                    playerInput.enabled = false;

                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                Invoke(nameof(Die), 3.5f);
            }
            else
            {
                animator.Play("Player_Hit");
                playerInput.shoot = false;
                playerInput.move = Vector2.zero;
                playerInput.sprint = false;
            }
        }

        public void Heal(float healAmount)
        {
            currentHealth += healAmount;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        void Die()
        {
            Debug.Log("Player đã chết!");
            if (GameManager.instance != null && GameManager.instance.gameOverPanel != null)
            {
                GameManager.instance.gameOverPanel.SetActive(true);
            }
        }
    }
}