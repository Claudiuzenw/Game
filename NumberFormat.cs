using System.Globalization;

// Idle games grow numbers fast, so show 1.5K, 2.3M, etc.
public static class NumberFormat
{
    static readonly string[] Suffixes = { "", "K", "M", "B", "T", "Qa", "Qi" };
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Short(double value)
    {
        if (value < 0) return "-" + Short(-value);

        if (value < 1000)
            return value < 10 ? value.ToString("0.###", Inv) : value.ToString("0", Inv);

        int i = 0;
        while (value >= 1000 && i < Suffixes.Length - 1)
        {
            value /= 1000;
            i++;
        }
        return value.ToString("0.##", Inv) + Suffixes[i];
    }

    public static string Duration(long seconds)
    {
        if (seconds < 60) return seconds + "s";
        long minutes = seconds / 60;
        if (minutes < 60) return minutes + "m " + (seconds % 60) + "s";
        long hours = minutes / 60;
        return hours + "h " + (minutes % 60) + "m";
    }
}
