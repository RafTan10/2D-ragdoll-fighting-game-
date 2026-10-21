using UnityEngine;

/// <summary>
/// Stamina: fast hand movement, blocked blows, jumps and lunges spend it; it refills when the fighter eases off.
/// Below a quarter the fighter tires (<see cref="Fatigue"/> weakens balance, arms and legs); at zero they faint.
/// </summary>
public class StaminaController : MonoBehaviour
{
    [Header("Pool")]
    [Tooltip("Stamina at endurance 1 with no gear. Each kg of weapons lowers it by 10%.")]
    [SerializeField] float baseStamina = 100f;

    [Header("Refill")]
    [Tooltip("Per second once rested.")]
    [SerializeField] float regenPerSecond = 18f;
    [Tooltip("Seconds of rest after a big cost (2+) before refilling.")]
    [SerializeField] float restDelay = 0.5f;
    [Tooltip("Refill multiplier while a hand is swinging fast.")]
    [SerializeField] float regenWhileSwinging = 0.35f;
    [Tooltip("Refill multiplier while a hand is held up (guard, slow aim).")]
    [SerializeField] float regenWhileBusy = 0.6f;

    [Header("Swing cost")]
    [Tooltip("Shoulder speed (deg/s) below which moving a hand is free.")]
    [SerializeField] float freeArmSpeed = 250f;
    [Tooltip("Cost per (deg/s above the free speed) per second. About 2 per full swing.")]
    [SerializeField] float costPerArmSpeed = 0.006f;
    [Tooltip("Extra cost per kg of weapon.")]
    [SerializeField] float weaponWeightCost = 0.4f;

    [Header("Tiredness")]
    [Tooltip("Below this share of the pool the fighter starts to sag.")]
    [SerializeField] float tiredBelow = 0.25f;
    [Tooltip("Strength left at zero stamina (balance, arms, legs).")]
    [SerializeField] float exhaustedStrength = 0.6f;

    public float Stamina { get; private set; }
    public float MaxStamina { get; private set; }
    /// <summary>1 when fresh, down to <see cref="exhaustedStrength"/> when nearly exhausted.</summary>
    public float Fatigue => Mathf.Lerp(exhaustedStrength, 1f, Mathf.Clamp01(Stamina / (MaxStamina * tiredBelow)));

    Fighter fighter;
    float rest;

    public void Init(Fighter owner, FighterDefinition def, WeaponDefinition main, WeaponDefinition off)
    {
        fighter = owner;
        float gear = (main != null ? main.mass : 0f) + (off != null ? off.mass : 0f);
        MaxStamina = baseStamina * def.endurance * Mathf.Clamp(1.15f - gear * 0.1f, 0.7f, 1.1f);
        Stamina = MaxStamina;
    }

    /// <summary>Spends stamina. Big costs pause recovery for a moment; small ones don't. Faints at zero.</summary>
    public void Spend(float amount)
    {
        if (amount <= 0f || fighter.IsKnockedOut) return;
        Stamina = Mathf.Max(0f, Stamina - amount);
        if (amount >= 2f) rest = restDelay;
        if (Stamina <= 0f) fighter.Knockdown.Faint();
    }

    /// <summary>After fainting, the fighter gets up with at least this share of the pool.</summary>
    public void RefillTo(float share) => Stamina = Mathf.Max(Stamina, MaxStamina * share);

    public void Tick(float dt)
    {
        var torso = fighter.Torso;
        bool swinging = false;
        foreach (var hand in new[] { fighter.MainHand, fighter.OffHand })
        {
            if (!hand.Active) continue;
            float speed = Mathf.Abs(hand.upper.angularVelocity - torso.angularVelocity);
            if (speed <= freeArmSpeed) continue;
            swinging = true;
            Spend((speed - freeArmSpeed) * costPerArmSpeed * (1f + (hand.weapon != null ? hand.weapon.mass : 0f) * weaponWeightCost) * dt);
        }
        rest -= dt;
        if (rest <= 0f)
        {
            float rate = swinging ? regenWhileSwinging : fighter.MainHand.Active || fighter.OffHand.Active ? regenWhileBusy : 1f;
            Stamina = Mathf.Min(MaxStamina, Stamina + regenPerSecond * rate * dt);
        }
    }
}
