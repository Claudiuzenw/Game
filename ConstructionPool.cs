using UnityEngine;

// A construction pool, like Azur Lane's Light / Heavy / Special builds.
// Create via Assets > Create > Game > Construction Pool.
// Cost is paid in Gold ("coins") plus Gems ("cubes"), both earnable by playing.
[CreateAssetMenu(menuName = "Game/Construction Pool", fileName = "pool_new")]
public class ConstructionPool : ScriptableObject
{
    [Tooltip("Stable ID used in save files, e.g. pool_standard")]
    public string Id;
    public string DisplayName;
    [TextArea] public string Description;

    [Header("Cost (per build)")]
    public double CostGold = 800;
    public double CostGems = 20;

    [Header("Timing")]
    [Tooltip("Real seconds. The build keeps running while the game is closed.")]
    public int BuildSeconds = 300;

    [Header("Rarity rates (any numbers, they are normalised to 100%)")]
    public float RateR = 65;
    public float RateSR = 31;
    public float RateSSR = 4;

    [Header("Featured hero (optional rate-up)")]
    public HeroDefinition Featured;
    [Range(0f, 1f)]
    [Tooltip("Share of the SSR rate that goes to the featured hero.")]
    public float FeaturedShareOfSSR = 0.5f;

    [Header("Free copy path")]
    [Tooltip("Build points needed to claim ONE free copy of the featured hero. 0 = no claim. " +
             "Each build in this pool gives 1 point. Only usable while the hero is not owned yet.")]
    public int PityToClaim = 0;

    public float TotalRate => Mathf.Max(0.0001f, RateR + RateSR + RateSSR);

    public float Percent(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.SSR: return RateSSR / TotalRate * 100f;
            case Rarity.SR: return RateSR / TotalRate * 100f;
            default: return RateR / TotalRate * 100f;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (string.IsNullOrEmpty(Id)) Id = name;
        BuildSeconds = Mathf.Max(1, BuildSeconds);
    }
#endif
}
