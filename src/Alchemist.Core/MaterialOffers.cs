namespace Alchemist.Core;

/// One reward slot that offers a choice of materials. The candidates are rolled once and then stored in
/// the run state, so reopening the rewards screen or reloading the save cannot reroll them.
public sealed record MaterialOffer(string Id, MaterialChoice[] Candidates);

/// AGENTS.md 4.4: reward-screen material choices are separate from furnace harvesting. Elite rooms add
/// one mixed slot; boss rooms add one mixed slot and one guaranteed-rare slot.
public static class MaterialOffers
{
    public const int CandidateCount = 3;
    public const int EliteSlots = 1;
    public const int BossSlots = 2;

    /// Weights live in data so rarity can be tuned without touching the roll. A weight of zero drops the
    /// material from the pool; rare materials join this table when they exist (AGENTS.md M3).
    public const int MixedRarePercent = 25;
    public static readonly int[] Weights = [1, 1, 1, 1];

    /// Derived from the run seed and the slot key alone, so the candidates are stable across reopens and
    /// reloads without drawing from - or advancing - any of the game's own random streams.
    public static Material[] Roll(ulong seed, string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("報酬枠のIDが空です。", nameof(key));
        if (Weights.Length != Enum.GetValues<Material>().Length)
            throw new InvalidOperationException("素材の重み表が素材の種類数と一致していません。");
        var pool = Enum.GetValues<Material>().Where(m => Weights[(int)m] > 0).ToList();
        if (pool.Count < CandidateCount)
            throw new InvalidOperationException($"候補に使える素材が{CandidateCount}種類ありません。");
        ulong state = Mix(seed ^ Hash(key));
        var picked = new Material[CandidateCount];
        for (int slot = 0; slot < CandidateCount; slot++)
        {
            state = Mix(state);
            int roll = (int)(state % (ulong)pool.Sum(m => Weights[(int)m]));
            int index = 0;
            while (roll >= Weights[(int)pool[index]]) roll -= Weights[(int)pool[index++]];
            picked[slot] = pool[index];
            // Drawn without replacement so a slot never shows the same material twice.
            pool.RemoveAt(index);
        }
        return picked;
    }

    public static MaterialChoice[] RollMixed(ulong seed, string key)
    {
        var normal = Roll(seed, key).Select(MaterialChoice.Normal).ToArray();
        ulong state = Mix(seed ^ Hash(key + ":rare"));
        if (state % 100 >= MixedRarePercent) return normal;
        int position = (int)(Mix(state) % CandidateCount);
        var rare = (RareMaterial)(Mix(state ^ 0xA11CE5EEDUL) % (ulong)Enum.GetValues<RareMaterial>().Length);
        normal[position] = MaterialChoice.Rare(rare);
        return normal;
    }

    public static MaterialChoice[] RollRare(ulong seed, string key)
    {
        var pool = Enum.GetValues<RareMaterial>().ToList();
        if (pool.Count < CandidateCount) throw new InvalidOperationException("希少素材が3種類未満です。");
        ulong state = Mix(seed ^ Hash(key));
        var result = new MaterialChoice[CandidateCount];
        for (int i = 0; i < CandidateCount; i++)
        {
            state = Mix(state);
            int index = (int)(state % (ulong)pool.Count);
            result[i] = MaterialChoice.Rare(pool[index]);
            pool.RemoveAt(index);
        }
        return result;
    }

    private static ulong Hash(string key)
    {
        ulong hash = 14695981039346656037;
        foreach (char c in key) unchecked { hash = (hash ^ c) * 1099511628211; }
        return hash;
    }

    private static ulong Mix(ulong x) // splitmix64
    {
        unchecked
        {
            x += 0x9E3779B97F4A7C15;
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EB;
            return x ^ (x >> 31);
        }
    }
}
