using System;
using System.Collections.Generic;

public class KingdomService
{
    readonly GameDatabase _db;
    readonly PlayerSave _save;

    public KingdomService(GameDatabase db, PlayerSave save)
    {
        _db = db;
        _save = save;
    }

    public IReadOnlyList<BuildingDefinition> Buildings => _db.Buildings;

    public void EnsureStarterBuildings()
    {
        foreach (var def in _db.Buildings)
        {
            if (def == null) continue;
            if (def.UnlockRequires == null && _save.GetBuilding(def.Id) == null)
                _save.Buildings.Add(new BuildingState { BuildingId = def.Id, Level = 1 });
        }
        Recalculate();
    }

    public int LevelOf(BuildingDefinition def)
    {
        var state = _save.GetBuilding(def.Id);
        return state == null ? 0 : state.Level;
    }

    public bool IsUnlocked(BuildingDefinition def) => def.UnlockRequires == null || LevelOf(def.UnlockRequires) >= def.UnlockRequiresLevel;
    public bool IsMaxed(BuildingDefinition def) => LevelOf(def) >= def.MaxLevel;
    public double UpgradeCost(BuildingDefinition def) => Math.Ceiling(def.BaseUpgradeCost * Math.Pow(1.18, LevelOf(def)));
    public bool CanUpgrade(BuildingDefinition def) => IsUnlocked(def) && !IsMaxed(def) && _save.Wallet.Gold >= UpgradeCost(def);

    public bool TryUpgrade(BuildingDefinition def)
    {
        if (!CanUpgrade(def)) return false;
        _save.Wallet.Gold -= UpgradeCost(def);
        var state = _save.GetBuilding(def.Id);
        if (state == null) _save.Buildings.Add(new BuildingState { BuildingId = def.Id, Level = 1 });
        else state.Level++;
        Recalculate();
        return true;
    }

    // FIXED: Function to get the actual name from the database
    public string GetHeroName(string heroId)
    {
        if (string.IsNullOrEmpty(heroId)) return "None";
        var def = _db.FindHero(heroId);
        return def != null ? def.DisplayName : "Unknown Hero";
    }

    // FIXED: Function to assign a hero and log it
    public void AssignHero(string buildingId, string heroId)
    {
        UnityEngine.Debug.Log($"[SERVICE] Attempting to assign Hero {heroId} to Building {buildingId}");
        var state = _save.GetBuilding(buildingId);
        if (state != null)
        {
            state.AssignedHeroId = heroId;
            UnityEngine.Debug.Log($"[SERVICE] Success! {heroId} is now working at {buildingId}");
            Recalculate();
        }
        else
        {
            UnityEngine.Debug.LogError($"[SERVICE] Error: Building {buildingId} not found in save data!");
        }
    }

    public double OutputPerSecond(BuildingDefinition def)
    {
        int level = LevelOf(def);
        if (level <= 0) return 0;
        double baseOut = def.BaseOutputPerSecond * Math.Pow(1.15, level - 1);
        return baseOut * (1 + GetWorkerBonus(def));
    }

    double GetWorkerBonus(BuildingDefinition def)
    {
        var state = _save.GetBuilding(def.Id);
        if (state == null || string.IsNullOrEmpty(state.AssignedHeroId)) return 0;
        var hero = _save.GetHero(state.AssignedHeroId);
        if (hero == null) return 0;
        var defHero = _db.FindHero(hero.HeroId);
        if (defHero == null) return 0;
        double classFit = (defHero.Class == def.PreferredClass) ? 1.0 : 0.5;
        return classFit * (0.10 + 0.02 * hero.Stars) + (0.03 * hero.BondLevel);
    }

    public double IncomePerSecond(ResourceType type)
    {
        double total = 0;
        foreach (var def in _db.Buildings) if (def != null && def.Produces == type) total += OutputPerSecond(def);
        return total;
    }

    public void Tick(double seconds)
    {
        foreach (var def in _db.Buildings)
        {
            if (def == null) continue;
            double perSecond = OutputPerSecond(def);
            if (perSecond > 0) _save.Wallet.Add(def.Produces, perSecond * seconds);
        }
        foreach (var state in _save.Buildings)
        {
            if (string.IsNullOrEmpty(state.AssignedHeroId)) continue;
            var hero = _save.GetHero(state.AssignedHeroId);
            if (hero != null) { hero.BondPoints += seconds / 60.0; int newL = 1 + (int)(hero.BondPoints / 100); if (newL > hero.BondLevel && newL <= 10) hero.BondLevel = newL; }
        }
    }

    void Recalculate() => _save.IncomePerSecond = IncomePerSecond(ResourceType.Gold);
}
