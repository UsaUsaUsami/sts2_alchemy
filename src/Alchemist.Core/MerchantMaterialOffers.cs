namespace Alchemist.Core;

public readonly record struct MerchantMaterialOffer(string Id, Material Material);

public static class MerchantMaterialOffers
{
    public const int Slots = 3;
    public const int Price = 35;

    // Independent deterministic stream: opening the shop cannot move the game's reward/map RNG.
    public static MerchantMaterialOffer[] Roll(ulong seed, string visitId)
    {
        if (string.IsNullOrWhiteSpace(visitId)) throw new ArgumentException("商人訪問IDが空です。", nameof(visitId));
        var pool = Enum.GetValues<Material>().ToList();
        ulong state = Mix(seed ^ Hash(visitId));
        var result = new MerchantMaterialOffer[Slots];
        for (int i = 0; i < Slots; i++)
        {
            state = Mix(state + 0x9E3779B97F4A7C15UL);
            int index = (int)(state % (ulong)pool.Count);
            result[i] = new($"shop:{visitId}:{i}", pool[index]);
            pool.RemoveAt(index);
        }
        return result;
    }

    private static ulong Hash(string text)
    {
        ulong hash = 1469598103934665603UL;
        foreach (char c in text) { hash ^= c; hash *= 1099511628211UL; }
        return hash;
    }
    private static ulong Mix(ulong value)
    {
        value ^= value >> 30; value *= 0xBF58476D1CE4E5B9UL;
        value ^= value >> 27; value *= 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }
}
