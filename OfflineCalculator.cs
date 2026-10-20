using System;

public class OfflineResult
{
    public long AwaySeconds;      // real time away
    public long CountedSeconds;   // time that earns, capped
    public double Gold;
    public double HeroExp;
    public double Gems;

    public bool HasRewards => AwaySeconds >= OfflineCalculator.MinSeconds && (Gold > 0 || HeroExp > 0 || Gems > 0);

    public void ApplyTo(Wallet wallet)
    {
        wallet.Gold += Gold;
        wallet.HeroExp += HeroExp;
        wallet.Gems += Gems;
    }
}

// OfflineReward = IncomePerSecond * min(TimeAway, 8 hours), per resource.
public static class OfflineCalculator
{
    public const long MaxSeconds = 8 * 3600;
    public const long MinSeconds = 60;   // ignore very short absences

    public static OfflineResult Compute(KingdomService kingdom, long awaySeconds)
    {
        var result = new OfflineResult
        {
            AwaySeconds = awaySeconds,
            CountedSeconds = Math.Min(awaySeconds, MaxSeconds)
        };

        result.Gold = kingdom.IncomePerSecond(ResourceType.Gold) * result.CountedSeconds;
        result.HeroExp = kingdom.IncomePerSecond(ResourceType.HeroExp) * result.CountedSeconds;
        result.Gems = kingdom.IncomePerSecond(ResourceType.Gems) * result.CountedSeconds;
        return result;
    }
}
