// BuffSystem.cs
// Defines buff types, the serializable BuffReward struct, and the
// PlayerBuffManager singleton that persists across scenes and accumulates
// all buffs collected during a run.

using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
public enum BuffType
{
    MaxHealth,
    Damage,
    FireRate,
    MovementSpeed,
    BulletCount
}

// ─────────────────────────────────────────────────────────────────────────────
[System.Serializable]
public class BuffReward
{
    public BuffType buffType;

    [Tooltip("Flat value for MaxHealth/BulletCount. Fractional multiplier for others (0.2 = +20%).")]
    public float value;

    /// <summary>Human-readable description shown in the level select panel.</summary>
    public string GetDescription() => buffType switch
    {
        BuffType.MaxHealth     => $"+{value} Max Health",
        BuffType.Damage        => $"+{value * 100:F0}% Damage",
        BuffType.FireRate      => $"+{value * 100:F0}% Fire Rate",
        BuffType.MovementSpeed => $"+{value * 100:F0}% Move Speed",
        BuffType.BulletCount   => $"+{(int)value} Bullet(s) per Shot",
        _                      => $"+{value} {buffType}"
    };
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Persists across scenes (DontDestroyOnLoad). Accumulates every BuffReward
/// the player earns by clearing levels. Player scripts read the bonus values
/// from this manager and apply them on top of their base stats.
///
/// Example usage in a player health script:
///     float maxHp = baseMaxHealth + PlayerBuffManager.Instance.HealthBonus;
/// </summary>
public class PlayerBuffManager : MonoBehaviour
{
    public static PlayerBuffManager Instance { get; private set; }

    private readonly List<BuffReward> _collected = new();

    // ── Lifecycle ────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Mutation ─────────────────────────────────────────────────────────────
    public void AddBuff(BuffReward buff)
    {
        _collected.Add(buff);
        Debug.Log($"[Buff] Gained: {buff.GetDescription()}");
    }

    public void AddBuffs(IEnumerable<BuffReward> buffs)
    {
        foreach (var b in buffs) AddBuff(b);
    }

    /// <summary>Call at the start of a new run to wipe accumulated buffs.</summary>
    public void ResetBuffs() => _collected.Clear();

    // ── Queries ──────────────────────────────────────────────────────────────
    public float GetTotalValue(BuffType type)
    {
        float total = 0f;
        foreach (var b in _collected)
            if (b.buffType == type) total += b.value;
        return total;
    }

    // Convenience properties so player scripts don't need to know BuffType names.
    public float HealthBonus      => GetTotalValue(BuffType.MaxHealth);
    public float DamageBonus      => GetTotalValue(BuffType.Damage);
    public float FireRateBonus    => GetTotalValue(BuffType.FireRate);
    public float MoveSpeedBonus   => GetTotalValue(BuffType.MovementSpeed);
    public int   BulletCountBonus => (int)GetTotalValue(BuffType.BulletCount);

    public IReadOnlyList<BuffReward> GetAllBuffs() => _collected.AsReadOnly();
}