namespace Alchemy.Core;

public enum AlchemyPhase { None, Earth, Water, Fire, Air }

public readonly record struct PhaseTransition(AlchemyPhase From, AlchemyPhase To, bool Triggered);

/// <summary>Combat-only elemental state. Materials remain in AlchemyState and never live here.</summary>
public sealed class AlchemyPhaseState(bool triggerFromNone = false)
{
    public AlchemyPhase Current { get; private set; } = AlchemyPhase.None;
    /// The phase held before Current, for "return to the previous phase" effects. None until the second element.
    public AlchemyPhase Previous { get; private set; } = AlchemyPhase.None;
    /// Whether leaving the neutral phase is a transition. Off by default; 方位盤 turns it on for its owner's combats.
    public bool TriggerFromNone { get; set; } = triggerFromNone;
    public int TransitionCount { get; private set; }
    /// Transitions since the start of the current player turn.
    public int TransitionsThisTurn { get; private set; }
    private readonly int[] transitionsInto = new int[5];
    /// Transitions into `phase` this combat (嵐刃 counts air, v0.23).
    public int TransitionsInto(AlchemyPhase phase) => transitionsInto[(int)phase];
    private readonly HashSet<AlchemyPhase> enteredThisTurn = [];
    /// Distinct elements transitioned into since the turn began (大坩堝, 2026-10-01).
    public int KindsEnteredThisTurn => enteredThisTurn.Count;
    public void StartTurn() { TransitionsThisTurn = 0; enteredThisTurn.Clear(); }

    /// Sets the phase a combat opens in (design-axes 6.3 G-1). Not a transition: nothing resolves, nothing
    /// counts and there is no previous phase. Only valid before the first transition of the combat.
    public void Open(AlchemyPhase start)
    {
        if (start != AlchemyPhase.None && !PhaseRules.IsElement(start)) throw new ArgumentOutOfRangeException(nameof(start));
        if (TransitionCount > 0) throw new InvalidOperationException("相転移の後には開始相を変えられません。");
        Current = start;
        Previous = AlchemyPhase.None;
    }

    public PhaseTransition Enter(AlchemyPhase next)
    {
        if (!PhaseRules.IsElement(next)) throw new ArgumentOutOfRangeException(nameof(next));
        var from = Current;
        if (from == next) return new(from, next, false);
        Previous = from;
        Current = next;
        bool triggered = from != AlchemyPhase.None || TriggerFromNone;
        if (triggered) { TransitionCount++; TransitionsThisTurn++; transitionsInto[(int)next]++; enteredThisTurn.Add(next); }
        return new(from, next, triggered);
    }

    public static Material? MaterialFor(AlchemyPhase phase) => PhaseRules.MaterialFor(phase);
}

/// <summary>
/// The only phase rules the rest of the mod may depend on. The effect of a transition is decided by the
/// destination alone, so four base amounts are all a player has to learn; powers, relics and enchantments
/// adjust these through the game-side transition hooks instead of new tables.
/// </summary>
public static class PhaseRules
{
    public static readonly IReadOnlyList<AlchemyPhase> Elements = [AlchemyPhase.Earth, AlchemyPhase.Water, AlchemyPhase.Fire, AlchemyPhase.Air];

    public static bool IsElement(AlchemyPhase phase) => phase is AlchemyPhase.Earth or AlchemyPhase.Water or AlchemyPhase.Fire or AlchemyPhase.Air;

    /// Earth: block, Water: weak, Fire: damage, Air: draw.
    public static int BaseAmount(AlchemyPhase to) => to switch
    {
        AlchemyPhase.Earth => 2,
        AlchemyPhase.Water => 1,
        AlchemyPhase.Fire => 3,
        AlchemyPhase.Air => 1,
        _ => 0
    };

    /// design-axes.md 原則5: transitions must not raise the number of cards drawn, because transition and
    /// draw feeding each other is loop fuel. So the air destination's draw ignores every modifier (powers,
    /// cores, modifications, "trigger twice") and always resolves once at the base amount. Other
    /// destinations keep whatever the modifiers made of them.
    public static (int Amount, int Repeats) Finalize(AlchemyPhase to, int amount, int repeats)
        => to == AlchemyPhase.Air ? (BaseAmount(AlchemyPhase.Air), 1) : (amount, repeats);

    /// Cards drawn by one transition into `to`.
    public static int TransitionDraw(AlchemyPhase to) => to == AlchemyPhase.Air ? BaseAmount(AlchemyPhase.Air) : 0;

    /// Used by effects that "advance" the phase rather than naming one.
    public static AlchemyPhase Next(AlchemyPhase phase) => phase switch
    {
        AlchemyPhase.Earth => AlchemyPhase.Water,
        AlchemyPhase.Water => AlchemyPhase.Fire,
        AlchemyPhase.Fire => AlchemyPhase.Air,
        AlchemyPhase.Air => AlchemyPhase.Earth,
        _ => AlchemyPhase.None
    };

    /// The element a combat opens in, from the run seed and a per-combat key. Like the reward slots, it
    /// draws from none of the game's own random streams.
    public static AlchemyPhase Opening(ulong seed, string key)
        => Elements[(int)(MaterialOffers.Mix(seed ^ MaterialOffers.Hash(key)) % (ulong)Elements.Count)];

    public static Material? MaterialFor(AlchemyPhase phase) => phase switch
    {
        AlchemyPhase.Earth => Material.Iron,
        AlchemyPhase.Water => Material.Herb,
        AlchemyPhase.Fire => Material.Powder,
        AlchemyPhase.Air => Material.Ether,
        _ => null
    };

    public static AlchemyPhase PhaseFor(Material material) => material switch
    {
        Material.Iron => AlchemyPhase.Earth,
        Material.Herb => AlchemyPhase.Water,
        Material.Powder => AlchemyPhase.Fire,
        Material.Ether => AlchemyPhase.Air,
        _ => AlchemyPhase.None
    };

    /// The most used material's element, ties going to the one listed first.
    public static AlchemyPhase FromMaterials(IReadOnlyList<Material> materials)
    {
        if (materials.Count == 0) return AlchemyPhase.None;
        var dominant = materials.Select((m, i) => (m, i)).GroupBy(x => x.m)
            .OrderByDescending(g => g.Count()).ThenBy(g => g.Min(x => x.i)).First().Key;
        return PhaseFor(dominant);
    }

    public static string Name(AlchemyPhase phase) => phase switch
    {
        AlchemyPhase.Earth => "地",
        AlchemyPhase.Water => "水",
        AlchemyPhase.Fire => "火",
        AlchemyPhase.Air => "風",
        _ => "無相"
    };
}

[Flags]
public enum WorkshopFacility { None = 0, Synthesis = 1, Modification = 2, Brewing = 4, Enchantment = 8 }
