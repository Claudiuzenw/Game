using System.Collections.Generic;
using UnityEngine;

// One asset that lists all content. Lives at Assets/Resources/GameDatabase.asset
// and is filled automatically by Tools > Game > Rebuild Database.
// (Later this can move to Addressables.)
[CreateAssetMenu(menuName = "Game/Database", fileName = "GameDatabase")]
public class GameDatabase : ScriptableObject
{
    public List<BuildingDefinition> Buildings = new List<BuildingDefinition>();
    public List<HeroDefinition> Heroes = new List<HeroDefinition>();
    public List<ConstructionPool> Pools = new List<ConstructionPool>();
    public List<SkinDefinition> Skins = new List<SkinDefinition>();

    public BuildingDefinition FindBuilding(string id)
    {
        return Buildings.Find(b => b != null && b.Id == id);
    }

    public HeroDefinition FindHero(string id)
    {
        return Heroes.Find(h => h != null && h.Id == id);
    }

    public ConstructionPool FindPool(string id)
    {
        return Pools.Find(p => p != null && p.Id == id);
    }

    public SkinDefinition FindSkin(string id)
    {
        return Skins.Find(s => s != null && s.Id == id);
    }
}
