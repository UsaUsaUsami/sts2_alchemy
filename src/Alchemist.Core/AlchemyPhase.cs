namespace Alchemist.Core;

public enum AlchemyPhase { None, Earth, Water, Fire, Air }

public readonly record struct PhaseTransition(AlchemyPhase From, AlchemyPhase To, bool Triggered);

/// <summary>Combat-only elemental state. Materials remain in AlchemyState and never live here.</summary>
public sealed class AlchemyPhaseState(bool triggerFromNone = false)
{
    public AlchemyPhase Current { get; private set; } = AlchemyPhase.None;
    public bool TriggerFromNone { get; } = triggerFromNone;
    public int TransitionCount { get; private set; }

    public PhaseTransition Enter(AlchemyPhase next)
    {
        if (next == AlchemyPhase.None) throw new ArgumentOutOfRangeException(nameof(next));
        var from = Current;
        if (from == next) return new(from, next, false);
        Current = next;
        bool triggered = from != AlchemyPhase.None || TriggerFromNone;
        if (triggered) TransitionCount++;
        return new(from, next, triggered);
    }

    public static Material? MaterialFor(AlchemyPhase phase) => phase switch
    {
        AlchemyPhase.Earth => Material.Iron,
        AlchemyPhase.Water => Material.Herb,
        AlchemyPhase.Fire => Material.Powder,
        AlchemyPhase.Air => Material.Ether,
        _ => null
    };
}

[Flags]
public enum WorkshopFacility { None = 0, Synthesis = 1, Modification = 2, Brewing = 4, Enchantment = 8 }
