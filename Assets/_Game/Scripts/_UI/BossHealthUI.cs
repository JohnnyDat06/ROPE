using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _bossHealthBarContainer;
    [SerializeField] private Slider _mainHealthSlider;
    [SerializeField] private Slider _smoothEaseSlider;
    [SerializeField] private TextMeshProUGUI _bossNameText;

    [Header("Boss References")]
    [SerializeField] private EnemyHitbox _bossHitbox;
    
    [Header("Smooth Animation Settings")]
    [SerializeField] private float _mainLerpSpeed = 10f;
    [SerializeField] private float _easeSlerpSpeed = 2f;

    private float _maxHealth;
    private float _targetHealth;
    private bool _isInitialized = false;
    private bool _bossIsDead = false;

    private void Start()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        if (_bossHitbox != null && _bossHitbox.MainHealth != null && _bossHitbox.MainHealth.IsBoss)
        {
            _maxHealth = _bossHitbox.MainHealth.maxHealth;
            _targetHealth = _bossHitbox.MainHealth.curentHealth;
            _bossIsDead = false;
            
            _mainHealthSlider.maxValue = _maxHealth;
            _smoothEaseSlider.maxValue = _maxHealth;
            
            _mainHealthSlider.value = _targetHealth;
            _smoothEaseSlider.value = _targetHealth;

            _bossHealthBarContainer.SetActive(true);
            _isInitialized = true;

            if (_bossNameText != null)
                _bossNameText.text = _bossHitbox.MainHealth.gameObject.name;
        }
        else
        {
            _bossHealthBarContainer.SetActive(false);
        }
    }

    private void Update()
    {
        if (!_isInitialized) return;

        if (_bossHitbox != null && _bossHitbox.MainHealth != null)
        {
            _targetHealth = (float)_bossHitbox.MainHealth.curentHealth;
            if (_targetHealth <= 0) _bossIsDead = true;
        }
        else
        {
            _targetHealth = 0;
            _bossIsDead = true;
        }
        
        if (Mathf.Abs(_mainHealthSlider.value - _targetHealth) > 0.01f)
        {
            _mainHealthSlider.value = Mathf.Lerp(_mainHealthSlider.value, _targetHealth, Time.deltaTime * _mainLerpSpeed);
        }
        else
        {
            _mainHealthSlider.value = _targetHealth;
        }
        
        if (Mathf.Abs(_smoothEaseSlider.value - _mainHealthSlider.value) > 0.01f)
        {
            Vector3 currentVector = new Vector3(_smoothEaseSlider.value, _maxHealth * 0.2f, 0);
            Vector3 targetVector = new Vector3(_mainHealthSlider.value, _maxHealth * 0.2f, 0);

            _smoothEaseSlider.value = Vector3.Slerp(currentVector, targetVector, Time.deltaTime * _easeSlerpSpeed).x;
        }
        else
        {
            _smoothEaseSlider.value = _mainHealthSlider.value;
        }
        if (_bossIsDead && _smoothEaseSlider.value <= 0.01f)
        {
            _smoothEaseSlider.value = 0;
            _mainHealthSlider.value = 0;
            _isInitialized = false;
            StartCoroutine(HideUIWithDelay(1f));
        }
    }

    private System.Collections.IEnumerator HideUIWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_bossHealthBarContainer != null)
            _bossHealthBarContainer.SetActive(false);
    }

    public void SetBoss(EnemyHitbox newBoss)
    {
        _bossHitbox = newBoss;
        InitializeUI();
    }
}
