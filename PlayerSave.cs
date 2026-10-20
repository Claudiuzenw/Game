using System;
using System.Collections.Generic;

// Everything here is saved to disk as JSON. Only IDs and numbers are stored,
// never references to definitions, so content can be rebalanced without breaking saves.

[Serializable]
public class Wallet
{
    public double Gold;
    public double HeroExp;
    public double Gems;
    public double SummonTickets;
}

[Serializable]
public class HeroInstance
{
    public string HeroId;
    public int Level = 1;
    public int Stars = 1;                // = copies owned, capped (see ConstructionService.MaxStars)
    public int Copies = 1;               // total copies ever obtained
    public double Exp;
    public int BondLevel = 1;
    public double BondPoints;
    public string SkinId = "";           // equipped skin, empty = default look
    public List<string> EquipmentIds = new List<string>();
}

[Serializable]
public class BuildingState
{
    public string BuildingId;
    public int Level = 1;
    public string AssignedHeroId = "";   // empty = no worker
}

[Serializable]
public class BannerProgress
{
    public string BannerId;              // pool id
    public int PityCounter;              // build points
    public bool GuaranteedFeatured;      // unused for now
}

// One construction (summon) slot. The hero is rolled when the build starts and
// stored here, so reloading the game cannot re-roll the result.
[Serializable]
public class ConstructionSlot
{
    public bool Active;
    public string PoolId = "";
    public string ResultHeroId = "";
    public long StartUnix;
    public long CompleteUnix;
}

[Serializable]
public class PlayerSave
{
    public const int PartySize = 5;

    public int Version;                  // see SaveService.Migrate
    public long LastSavedUnix;           // used for offline rewards
    public int CurrentStage = 1;         // expedition progress
    public double IncomePerSecond;       // cached sum of building output

    public Wallet Wallet = new Wallet();
    public List<HeroInstance> Heroes = new List<HeroInstance>();
    public List<BuildingState> Buildings = new List<BuildingState>();
    public int[] Party = new int[PartySize];   // indices into Heroes, -1 = empty
    public List<BannerProgress> Banners = new List<BannerProgress>();
    public List<ConstructionSlot> Slots = new List<ConstructionSlot>();
    public List<string> OwnedSkinIds = new List<string>();

    public static PlayerSave CreateNew()
    {
        var save = new PlayerSave { Version = SaveService.CurrentVersion };
        for (int i = 0; i < save.Party.Length; i++) save.Party[i] = -1;
        save.Wallet.Gold = 50;
        save.Wallet.Gems = 300;
        return save;
    }

    public HeroInstance GetHero(string heroId)
    {
        return Heroes.Find(h => h.HeroId == heroId);
    }

    public BuildingState GetBuilding(string buildingId)
    {
        return Buildings.Find(b => b.BuildingId == buildingId);
    }

    public BannerProgress GetOrCreateBanner(string bannerId)
    {
        var banner = Banners.Find(b => b.BannerId == bannerId);
        if (banner == null)
        {
            banner = new BannerProgress { BannerId = bannerId };
            Banners.Add(banner);
        }
        return banner;
    }
}
