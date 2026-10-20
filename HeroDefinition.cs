using System;
using UnityEngine;

[Serializable]
public struct StatBlock
{
    public float Atk;
    public float Hp;
    public float Def;
    public float Speed;
    public float CritRate;     // 0.05 = 5%
    public float CritDamage;   // 0.5 = +50% damage on crit

    public static StatBlock operator +(StatBlock a, StatBlock b) => new StatBlock
    {
        Atk = a.Atk + b.Atk,
        Hp = a.Hp + b.Hp,
        Def = a.Def + b.Def,
        Speed = a.Speed + b.Speed,
        CritRate = a.CritRate + b.CritRate,
        CritDamage = a.CritDamage + b.CritDamage
    };

    public static StatBlock operator *(StatBlock a, float k) => new StatBlock
    {
        Atk = a.Atk * k,
        Hp = a.Hp * k,
        Def = a.Def * k,
        Speed = a.Speed * k,
        CritRate = a.CritRate * k,
        CritDamage = a.CritDamage * k
    };
}

// Read-only content. Create assets via Assets > Create > Game > Hero.
// Skills are added in the battle step; art uses plain Sprite references for now
// (switch to Addressables later when the Addressables package is installed).
[CreateAssetMenu(menuName = "Game/Hero", fileName = "hero_new")]
public class HeroDefinition : ScriptableObject
{
    [Tooltip("Stable ID used in save files. Never change it after release, e.g. hero_aria")]
    public string Id;
    public string DisplayName;
    public Rarity Rarity;
    public HeroClass Class;

    [Header("Stats")]
    public StatBlock BaseStats;
    public StatBlock GrowthPerLevel;

    [Header("Art (placeholders for now)")]
    public Sprite Portrait;
    public Sprite Icon;

    public const int MaxLevel = 100;

    public StatBlock StatsAt(int level)
    {
        level = Mathf.Clamp(level, 1, MaxLevel);
        return BaseStats + GrowthPerLevel * (level - 1);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (string.IsNullOrEmpty(Id)) Id = name;
    }
#endif
}
