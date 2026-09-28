namespace Alchemy.Core;

/// <summary>
/// Workshop modification (design-axes.md 7.3): each material put into a card adds one of its effect. Materials
/// mix freely, and a card holds four in total whatever the kinds. Counts are indexed by Material.
/// Powder = damage (attacks only), Iron = block on play, Herb = drain on play, Ether = Preparation on play.
/// </summary>
public static class InfusionRules
{
    public const int Limit = 4;

    public static int[] Empty() => new int[Enum.GetValues<Material>().Length];

    public static bool Allows(IReadOnlyList<int> current, IReadOnlyList<int> add, bool isAttack)
    {
        int kinds = Enum.GetValues<Material>().Length;
        if (current.Count != kinds || add.Count != kinds || current.Any(n => n < 0) || add.Any(n => n < 0)) return false;
        int adding = add.Sum();
        return adding > 0 && current.Sum() + adding <= Limit && (isAttack || add[(int)Material.Powder] == 0);
    }

    /// How many more materials the card can take.
    public static int Room(IReadOnlyList<int> current) => Math.Max(0, Limit - current.Sum());
}
