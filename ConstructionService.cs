using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum GrantKind { NewHero, LimitBreak, Surplus }

public class GrantResult
{
    public HeroDefinition Hero;
    public GrantKind Kind;
    public int Stars;
    public int Copies;
    public double ExpGained;
    public ConstructionPool Pool;
}

public class ConstructionService
{
    public const int SlotCount = 2;
    public const int MaxStars = 4;
    public const double SurplusExp = 500;

    readonly GameDatabase _db;
    readonly PlayerSave _save;
    readonly System.Random _rng;
    public event Action Changed;

    public ConstructionService(GameDatabase db, PlayerSave save, System.Random rng = null)
    {
        _db = db;
        _save = save;
        // FIXED: Changed comma to dot here
        _rng = rng ?? new System.Random();

        while (_save.Slots.Count < SlotCount) _save.Slots.Add(new ConstructionSlot());
    }

    public IReadOnlyList<ConstructionPool> Pools => _db.Pools;
    public static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    public bool IsReady(ConstructionSlot slot) => slot.Active && Now() >= slot.CompleteUnix;
    public long SecondsLeft(ConstructionSlot slot) => slot.Active ? Math.Max(0, slot.CompleteUnix - Now()) : 0;
    public bool CanAfford(ConstructionPool pool) => _save.Wallet.Gold >= pool.CostGold && _save.Wallet.Gems >= pool.CostGems;

    public bool TryStart(ConstructionPool pool, int slotIndex)
    {
        var slot = _save.Slots[slotIndex];
        if (slot.Active || !CanAfford(pool)) return false;

        var hero = Roll(pool);
        if (hero == null) return false;

        _save.Wallet.Gold -= pool.CostGold;
        _save.Wallet.Gems -= pool.CostGems;
        if (pool.PityToClaim > 0) _save.GetOrCreateBanner(pool.Id).PityCounter++;

        long now = Now();
        slot.Active = true;
        slot.PoolId = pool.Id;
        slot.ResultHeroId = hero.Id;
        slot.StartUnix = now;
        slot.CompleteUnix = now + pool.BuildSeconds;

        Changed?.Invoke();
        return true;
    }

    public GrantResult Collect(int slotIndex)
    {
        var slot = _save.Slots[slotIndex];
        if (!IsReady(slot)) return null;

        var hero = _db.FindHero(slot.ResultHeroId);
        var pool = _db.FindPool(slot.PoolId);

        slot.Active = false;
        slot.PoolId = "";
        slot.ResultHeroId = "";

        if (hero == null) return null;

        GrantResult result = Grant(hero);
        result.Pool = pool;
        Changed?.Invoke();
        return result;
    }

    public int PityPoints(ConstructionPool pool) => pool.PityToClaim > 0 ? _save.GetOrCreateBanner(pool.Id).PityCounter : 0;
    public bool OwnsHero(HeroDefinition hero) => hero != null && _save.GetHero(hero.Id) != null;

    public bool CanClaim(ConstructionPool pool) => pool != null && pool.PityToClaim > 0 && pool.Featured != null && !OwnsHero(pool.Featured) && PityPoints(pool) >= pool.PityToClaim;

    public GrantResult Claim(ConstructionPool pool)
    {
        if (!CanClaim(pool)) return null;
        _save.GetOrCreateBanner(pool.Id).PityCounter -= pool.PityToClaim;
        var result = Grant(pool.Featured);
        result.Pool = pool;
        Changed?.Invoke();
        return result;
    }

    public GrantResult Grant(HeroDefinition hero)
    {
        var result = new GrantResult { Hero = hero };
        var instance = _save.GetHero(hero.Id);

        if (instance == null)
        {
            instance = new HeroInstance { HeroId = hero.Id, Copies = 1, Stars = 1 };
            _save.Heroes.Add(instance);
            result.Kind = GrantKind.NewHero;
        }
        else
        {
            instance.Copies++;
            if (instance.Copies <= MaxStars) { instance.Stars = instance.Copies; result.Kind = GrantKind.LimitBreak; }
            else { _save.Wallet.HeroExp += SurplusExp; result.ExpGained = SurplusExp; result.Kind = GrantKind.Surplus; }
        }
        result.Stars = instance.Stars;
        result.Copies = instance.Copies;
        return result;
    }

    HeroDefinition Roll(ConstructionPool pool)
    {
        double roll = _rng.NextDouble() * 100.0;
        double ssr = pool.Percent(Rarity.SSR);
        double sr = pool.Percent(Rarity.SR);
        Rarity rarity = (roll < ssr) ? Rarity.SSR : (roll < ssr + sr) ? Rarity.SR : Rarity.R;
        if (rarity == Rarity.SSR && pool.Featured != null && _rng.NextDouble() < pool.FeaturedShareOfSSR) return pool.Featured;
        return PickFrom(rarity, pool.Featured);
    }

    HeroDefinition PickFrom(Rarity rarity, HeroDefinition featured)
    {
        for (int tier = (int)rarity; tier >= 0; tier--)
        {
            var candidates = _db.Heroes.Where(h => h != null && (int)h.Rarity == tier && h != featured).ToList();
            if (candidates.Count > 0) return candidates[_rng.Next(candidates.Count)];
            if (featured != null && (int)featured.Rarity == tier) return featured;
        }
        return null;
    }
}
