using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class GameTools
{
    const string ContentFolder = "Assets/_Game/Content";
    const string BuildingFolder = "Assets/_Game/Content/Buildings";
    const string HeroFolder = "Assets/_Game/Content/Heroes";
    const string PoolFolder = "Assets/_Game/Content/Pools";
    const string ResourcesFolder = "Assets/Resources";
    const string DatabasePath = "Assets/Resources/GameDatabase.asset";

    [MenuItem("Tools/Game/Create Starter Content")]
    static void CreateStarterContent()
    {
        EnsureFolder(BuildingFolder);
        EnsureFolder(HeroFolder);
        EnsureFolder(PoolFolder);
        EnsureFolder(ResourcesFolder);

        var market = FindOrCreateBuilding("building_market", "Market", ResourceType.Gold, 1, 10, null, 1);
        var tavern = FindOrCreateBuilding("building_tavern", "Tavern", ResourceType.HeroExp, 0.5, 50, market, 3);
        FindOrCreateBuilding("building_mine", "Mine", ResourceType.Gems, 0.004, 200, tavern, 3);

        FindOrCreateHero("hero_tomas", "Tomas", Rarity.R, HeroClass.Fighter);
        FindOrCreateHero("hero_lena", "Lena", Rarity.R, HeroClass.Archer);
        FindOrCreateHero("hero_bram", "Bram", Rarity.R, HeroClass.Tank);
        FindOrCreateHero("hero_nia", "Nia", Rarity.R, HeroClass.Mage);
        FindOrCreateHero("hero_ivo", "Ivo", Rarity.R, HeroClass.Healer);
        FindOrCreateHero("hero_elric", "Elric", Rarity.SR, HeroClass.Tank);
        FindOrCreateHero("hero_orla", "Orla", Rarity.SR, HeroClass.Mage);
        FindOrCreateHero("hero_sera", "Sera", Rarity.SR, HeroClass.Archer);
        FindOrCreateHero("hero_vael", "Vael", Rarity.SR, HeroClass.Healer);
        FindOrCreateHero("hero_aria", "Aria", Rarity.SSR, HeroClass.Mage);
        var kaede = FindOrCreateHero("hero_kaede", "Kaede", Rarity.SSR, HeroClass.Fighter);
        FindOrCreateHero("hero_selene", "Selene", Rarity.SSR, HeroClass.Healer);

        FindOrCreatePool("pool_standard", "Standard Construction", "Everyday builds.", 800, 20, 300, 65, 31, 4, null, 0, 0);
        FindOrCreatePool("pool_featured", "Featured Construction", "Rate-up for featured hero.", 1500, 40, 900, 60, 34, 6, kaede, 0.5f, 100);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RebuildDatabase();
    }

    [MenuItem("Tools/Game/Rebuild Database")]
    static void RebuildDatabase()
    {
        EnsureFolder(ResourcesFolder);
        var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<GameDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
        }
        db.Buildings = AllAssets<BuildingDefinition>().OrderBy(b => b.BaseUpgradeCost).ToList();
        db.Heroes = AllAssets<HeroDefinition>().OrderBy(h => h.DisplayName).ToList();
        db.Pools = AllAssets<ConstructionPool>().OrderBy(p => p.CostGold).ToList();
        db.Skins = AllAssets<SkinDefinition>().OrderBy(s => s.DisplayName).ToList();
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Save/Pretend Away 1 Hour")]
    static void PretendAwayOneHour() { PretendAway(3600); }

    [MenuItem("Tools/Save/Pretend Away 10 Hours")]
    static void PretendAwayTenHours() { PretendAway(36000); }

    static void PretendAway(long seconds)
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Stop Play mode first!");
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, "save.json");
        if (!File.Exists(path))
        {
            Debug.LogWarning("No save file yet. Press Play once first.");
            return;
        }

        var save = JsonUtility.FromJson<PlayerSave>(File.ReadAllText(path));

        // 1. Move the global save timestamp back (for Gold/Exp/Gems)
        save.LastSavedUnix -= seconds;

        // 2. MOVE THE HERO TIMERS BACK (The fix!)
        // By subtracting seconds from the completion time, the hero finishes "sooner"
        if (save.Slots != null)
        {
            foreach (var slot in save.Slots)
            {
                if (slot.Active)
                {
                    slot.CompleteUnix -= seconds;
                }
            }
        }

        File.WriteAllText(path, JsonUtility.ToJson(save, true));
        Debug.Log($"Pretended away {seconds}s. Save and Hero timers shifted. Press Play.");
    }

    static BuildingDefinition FindOrCreateBuilding(string id, string displayName, ResourceType produces, double baseOutput, double baseCost, BuildingDefinition requires, int requiresLevel)
    {
        var existing = AllAssets<BuildingDefinition>().FirstOrDefault(b => b.Id == id);
        if (existing != null) return existing;
        var building = ScriptableObject.CreateInstance<BuildingDefinition>();
        building.Id = id; building.DisplayName = displayName; building.Produces = produces;
        building.BaseOutputPerSecond = baseOutput; building.BaseUpgradeCost = baseCost;
        building.UnlockRequires = requires; building.UnlockRequiresLevel = requiresLevel;
        AssetDatabase.CreateAsset(building, $"{BuildingFolder}/{id}.asset");
        return building;
    }

    static HeroDefinition FindOrCreateHero(string id, string displayName, Rarity rarity, HeroClass heroClass)
    {
        var existing = AllAssets<HeroDefinition>().FirstOrDefault(h => h.Id == id);
        if (existing != null) return existing;
        var hero = ScriptableObject.CreateInstance<HeroDefinition>();
        hero.Id = id; hero.DisplayName = displayName; hero.Rarity = rarity; hero.Class = heroClass;
        AssetDatabase.CreateAsset(hero, $"{HeroFolder}/{id}.asset");
        return hero;
    }

    static ConstructionPool FindOrCreatePool(string id, string displayName, string description, double gold, double gems, int seconds, float rateR, float rateSR, float rateSSR, HeroDefinition featured, float featuredShare, int pityToClaim)
    {
        var existing = AllAssets<ConstructionPool>().FirstOrDefault(p => p.Id == id);
        if (existing != null) return existing;
        var pool = ScriptableObject.CreateInstance<ConstructionPool>();
        pool.Id = id; pool.DisplayName = displayName; pool.Description = description;
        pool.CostGold = gold; pool.CostGems = gems; pool.BuildSeconds = seconds;
        pool.RateR = rateR; pool.RateSR = rateSR; pool.RateSSR = rateSSR;
        pool.Featured = featured; pool.FeaturedShareOfSSR = featuredShare;
        pool.PityToClaim = pityToClaim;
        AssetDatabase.CreateAsset(pool, $"{PoolFolder}/{id}.asset");
        return pool;
    }

    static List<T> AllAssets<T>() where T : Object => AssetDatabase.FindAssets("t:" + typeof(T).Name)
        .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid))).Where(asset => asset != null).ToList();

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
