using System.Security.AccessControl;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Building", fileName = "building_new")]
public class BuildingDefinition : ScriptableObject
{
    public string Id;
    public string DisplayName;
    public ResourceType Produces;

    [Header("Balance")]
    public double BaseOutputPerSecond;
    public double BaseUpgradeCost;
    public int MaxLevel = 50;

    [Header("Requirements")]
    public BuildingDefinition UnlockRequires;
    public int UnlockRequiresLevel;

    [Header("Worker Bonus")]
    public HeroClass PreferredClass; // The class that gets the bonus here
}
