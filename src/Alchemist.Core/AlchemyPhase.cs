namespace Alchemist.Core;

public enum AlchemyPhase { None, Earth, Water, Fire, Air }

public readonly record struct PhaseTransition(AlchemyPhase From, AlchemyPhase To, bool Triggered);

/// <summary>Combat-only elemental state. Materials remain in AlchemyState and never live here.</summary>
public sealed class AlchemyPhaseState(bool triggerFromNone = false)
{
    public AlchemyPhase Current { get; private set; } = AlchemyPhase.None;
    /// The phase held before Current, for "return to the previous phase" effects. None until the second element.
    public AlchemyPhase Previous { get; private set; } = AlchemyPhase.None;
    public bool TriggerFromNone { get; } = triggerFromNone;
    public int TransitionCount { get; private set; }
    /// Transitions since the start of the current player turn.
    public int TransitionsThisTurn { get; private set; }
    public void StartTurn() => TransitionsThisTurn = 0;

    public PhaseTransition Enter(AlchemyPhase next)
    {
        if (!PhaseRules.IsElement(next)) throw new ArgumentOutOfRangeException(nameof(next));
        var from = Current;
        if (from == next) return new(from, next, false);
        Previous = from;
        Current = next;
        bool triggered = from != AlchemyPhase.None || TriggerFromNone;
        if (triggered) { TransitionCount++; TransitionsThisTurn++; }
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

    /// Used by effects that "advance" the phase rather than naming one.
    public static AlchemyPhase Next(AlchemyPhase phase) => phase switch
    {
        AlchemyPhase.Earth => AlchemyPhase.Water,
        AlchemyPhase.Water => AlchemyPhase.Fire,
        AlchemyPhase.Fire => AlchemyPhase.Air,
        AlchemyPhase.Air => AlchemyPhase.Earth,
        _ => AlchemyPhase.None
    };

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
