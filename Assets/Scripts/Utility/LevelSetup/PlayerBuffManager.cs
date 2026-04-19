using System.Collections.Generic;
using UnityEngine;

public enum BuffType
{
    MaxHealth,
    Damage,
    FireRate,
    MovementSpeed,
    BulletCount
}

[System.Serializable]
public class BuffReward
{
    public BuffType buffType;

    [Tooltip("Flat value for MaxHealth/BulletCount. Fractional multiplier for others (0.2 = +20%).")]
    public float value;

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

public class PlayerBuffManager : MonoBehaviour
{
    public static PlayerBuffManager Instance { get; private set; }

    private readonly List<BuffReward> _collected = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddBuff(BuffReward buff)
    {
        _collected.Add(buff);
        Debug.Log($"[Buff] Gained: {buff.GetDescription()}");
    }

    public void AddBuffs(IEnumerable<BuffReward> buffs)
    {
        foreach (var b in buffs) AddBuff(b);
    }

    public void ResetBuffs() => _collected.Clear();

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