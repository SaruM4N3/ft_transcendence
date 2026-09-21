using UnityEngine;

// Base stat set a class is defined by; each class is a tuned instance of this asset.
[CreateAssetMenu(fileName = "ClassStats", menuName = "Transcendence/Class Stats")]
public class ClassStats : ScriptableObject
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxMana = 50f;
    [SerializeField] private float healthRegenPerSecond = 1f;
    [SerializeField] private float manaRegenPerSecond = 1f;

    public float MaxHealth => maxHealth;
    public float MaxMana => maxMana;
    public float HealthRegenPerSecond => healthRegenPerSecond;
    public float ManaRegenPerSecond => manaRegenPerSecond;
}
