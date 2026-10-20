public static class WalletExtensions
{
    public static double Get(this Wallet wallet, ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Gold: return wallet.Gold;
            case ResourceType.HeroExp: return wallet.HeroExp;
            default: return wallet.Gems;
        }
    }

    public static void Add(this Wallet wallet, ResourceType type, double amount)
    {
        switch (type)
        {
            case ResourceType.Gold: wallet.Gold += amount; break;
            case ResourceType.HeroExp: wallet.HeroExp += amount; break;
            default: wallet.Gems += amount; break;
        }
    }
}
