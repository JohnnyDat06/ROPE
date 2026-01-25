using UnityEngine;
using static UnityEngine.ParticleSystem;

[CreateAssetMenu(fileName = "DamgeConfigSO", menuName = "Guns/DamgeConfigSO", order = 1)]
public class DamgeConfigSO : ScriptableObject
{
    public MinMaxCurve damageCurve;

    private void Reset()
    {
        damageCurve.mode = ParticleSystemCurveMode.Curve;
    }

    public int GetDamage(float distance = 0)
    {
        return Mathf.CeilToInt(damageCurve.Evaluate(distance, Random.value));
    }
}
