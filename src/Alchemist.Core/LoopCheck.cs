namespace Alchemist.Core;

/// <summary>How a card moves the phase when played.</summary>
public enum LoopPhaseMove { None, Element, Dual, Previous, Any }

/// <summary>
/// The loop-relevant part of one card (design-axes.md 2 原則4・5): what it costs, what it gives back in the
/// same turn and how it moves the phase. Anything that only matters on a later turn (next-turn energy or
/// draw) is left out, because it cannot feed a loop inside one turn. An upgraded variant is its own entry
/// with a "+" suffix.
/// </summary>
public sealed record LoopCard(string Id, int Cost, LoopPhaseMove Move = LoopPhaseMove.None,
    AlchemyPhase Element = AlchemyPhase.None, AlchemyPhase First = AlchemyPhase.None,
    int Draw = 0, int Energy = 0, bool Exhaust = false, bool SpendsMaterial = false,
    int DrawIfTransition = 0, int EnergyIfTransition = 0, bool IsPower = false,
    int PowerDrawPerTransition = 0, int PowerEnergyPerTransition = 0);

public sealed record LoopResult(bool Looped, int Plays);

/// <summary>
/// Plays one turn greedily with a deck built from a few cards plus filler, and reports whether it keeps
/// going past a play cap. The cap stands in for "does not stop": a deck of real cards that still has
/// plays left after hundreds of them in one turn is an infinite loop for our purposes.
/// </summary>
public static class LoopCheck
{
    public const int PlayCap = 300;
    public const int HandLimit = 10;
    public const int OpeningHand = 5;
    public const int TurnEnergy = 3;
    /// Materials the box can hold at most; material-spending cards can never exceed this in one turn.
    public const int MaterialBudget = 20;

    /// Filler: a 1-cost phase-less card with no payoff, standing in for the rest of a thinned deck.
    public static readonly LoopCard Filler = new("filler", 1);

    public static LoopResult Simulate(IReadOnlyList<LoopCard> deck, int seed)
    {
        var rng = new Random(seed);
        var draw = deck.Where(c => !c.IsPower).OrderBy(_ => rng.Next()).ToList();
        var powers = deck.Where(c => c.IsPower).ToList(); // Powers count as already in play: the worst case.
        var hand = new List<LoopCard>();
        var discard = new List<LoopCard>();
        int energy = TurnEnergy, materials = MaterialBudget, plays = 0;
        var phase = AlchemyPhase.None; var previous = AlchemyPhase.None;

        void Draw(int n)
        {
            for (int i = 0; i < n; i++)
            {
                if (hand.Count >= HandLimit) return;
                if (draw.Count == 0)
                {
                    if (discard.Count == 0) return;
                    draw = discard.OrderBy(_ => rng.Next()).ToList();
                    discard.Clear();
                }
                hand.Add(draw[0]); draw.RemoveAt(0);
            }
        }
        bool Enter(AlchemyPhase next)
        {
            if (!PhaseRules.IsElement(next) || next == phase) return false;
            bool triggered = phase != AlchemyPhase.None;
            previous = phase; phase = next;
            if (!triggered) return false;
            // The air destination's draw is fixed (PhaseRules.TransitionDraw); powers add their own.
            Draw(PhaseRules.TransitionDraw(next) + powers.Sum(p => p.PowerDrawPerTransition));
            energy += powers.Sum(p => p.PowerEnergyPerTransition);
            return true;
        }
        AlchemyPhase Destination(LoopCard c) => c.Move switch
        {
            LoopPhaseMove.Element or LoopPhaseMove.Dual => c.Element,
            LoopPhaseMove.Previous => previous,
            // "Any phase" picks the draw destination whenever it can.
            LoopPhaseMove.Any => phase == AlchemyPhase.Air ? AlchemyPhase.Fire : AlchemyPhase.Air,
            _ => AlchemyPhase.None
        };
        bool WouldTransition(LoopCard c)
        {
            if (c.Move == LoopPhaseMove.Dual) return phase != AlchemyPhase.None || c.First != c.Element;
            var to = Destination(c);
            return PhaseRules.IsElement(to) && PhaseRules.IsElement(phase) && to != phase;
        }
        bool Playable(LoopCard c) => c.Cost <= energy && (!c.SpendsMaterial || materials > 0);
        int Score(LoopCard c)
        {
            if (c == Filler) return -100;
            // Energy is the scarcer resource: a draw engine that spends energy without getting it back ends
            // the turn, so energy counts for more than a card.
            bool shift = WouldTransition(c);
            int energyNet = c.Energy - c.Cost + (shift ? c.EnergyIfTransition : 0);
            int cards = c.Draw + (shift ? c.DrawIfTransition : 0)
                + (shift && Destination(c) == AlchemyPhase.Air ? PhaseRules.TransitionDraw(AlchemyPhase.Air) : 0);
            return energyNet * 30 + cards * 10 + (shift ? 5 : 0) + (c.Exhaust ? -1 : 0);
        }

        Draw(OpeningHand);
        while (plays < PlayCap)
        {
            var card = hand.Where(Playable).OrderByDescending(Score).FirstOrDefault();
            if (card is null) break;
            hand.Remove(card);
            energy -= card.Cost;
            if (card.SpendsMaterial) materials--;
            plays++;
            bool shift = WouldTransition(card);
            energy += card.Energy;
            Draw(card.Draw);
            if (shift) { energy += card.EnergyIfTransition; Draw(card.DrawIfTransition); }
            if (card.Move == LoopPhaseMove.Dual) { Enter(card.First); Enter(card.Element); }
            else Enter(Destination(card));
            if (!card.Exhaust) discard.Add(card);
        }
        return new(plays >= PlayCap, plays);
    }

    /// Every multiset of up to `size` entries from `pool`, each included twice in the deck (a thinned deck
    /// that doubled up on its combo), plus filler. Returns the combinations that looped on any seed.
    public static IReadOnlyList<string> FindLoops(IReadOnlyList<LoopCard> pool, int size = 3, int fillers = 4, int seeds = 4)
    {
        List<string> loops = [];
        void Try(List<LoopCard> combo)
        {
            var deck = combo.SelectMany(c => new[] { c, c }).Concat(Enumerable.Repeat(Filler, fillers)).ToList();
            for (int seed = 0; seed < seeds; seed++)
                if (Simulate(deck, seed).Looped) { loops.Add(string.Join(" + ", combo.Select(c => c.Id))); return; }
        }
        void Walk(int start, List<LoopCard> combo)
        {
            if (combo.Count > 0) Try(combo);
            if (combo.Count == size) return;
            for (int i = start; i < pool.Count; i++) { combo.Add(pool[i]); Walk(i, combo); combo.RemoveAt(combo.Count - 1); }
        }
        Walk(0, []);
        return loops;
    }
}

/// <summary>
/// Loop profiles of every card that could fuel an in-turn loop: anything that costs 0 (before or after an
/// upgrade), gains energy, draws, or moves the phase for free. The in-game smoke test checks that no such
/// card is missing here. Numbers mirror the card definitions; keep them in sync when a card changes.
/// </summary>
public static class LoopProfiles
{
    private static LoopCard El(string id, int cost, AlchemyPhase e, int draw = 0, int energy = 0, bool exhaust = false)
        => new(id, cost, LoopPhaseMove.Element, e, Draw: draw, Energy: energy, Exhaust: exhaust);
    private static LoopCard Dual(string id, int cost, AlchemyPhase first, AlchemyPhase second, int draw = 0)
        => new(id, cost, LoopPhaseMove.Dual, second, first, Draw: draw);
    private const AlchemyPhase E = AlchemyPhase.Earth, W = AlchemyPhase.Water, F = AlchemyPhase.Fire, A = AlchemyPhase.Air;

    public static readonly IReadOnlyList<LoopCard> All = [
        // Starter and tokens
        new("InstantAlchemy", 1, Exhaust: true, SpendsMaterial: true), new("InstantAlchemy+", 0, Exhaust: true, SpendsMaterial: true),
        El("IronImprovisation", 0, E, exhaust: true), El("HerbImprovisation", 0, W, exhaust: true),
        El("PowderImprovisation", 0, F, exhaust: true), El("EtherImprovisation", 0, A, draw: 2, exhaust: true),
        new("FurnaceActivation", 0, Exhaust: true),
        // Earth, water, fire
        El("TidalGuard", 1, W, draw: 1),
        El("EarthBulwarkBash", 1, E), El("EarthBulwarkBash+", 0, E),
        El("WaterCatalyst", 1, W), El("WaterCatalyst+", 0, W),
        El("LifeCorrosiveEmbrace", 1, W), El("LifeCorrosiveEmbrace+", 0, W),
        El("FireSpark", 0, F), El("FireSpark+", 0, F),
        // Air
        El("Tailwind", 0, A, draw: 1, exhaust: true), El("Tailwind+", 0, A, draw: 2, exhaust: true),
        El("Slipstream", 1, A, draw: 1), El("AirGust", 1, A, draw: 1), El("AirGale", 1, A, draw: 1),
        El("AirWindBlade", 0, A), El("AirWindBlade+", 0, A),
        El("AirMomentum", 1, A, draw: 2), El("AirMomentum+", 1, A, draw: 3),
        El("AirCurrent", 0, A), El("AirCurrent+", 0, A),
        El("AirRefine", 1, A, draw: 2), El("AirRefine+", 1, A, draw: 3),
        El("AirGift", 0, A, energy: 2, exhaust: true), El("AirGift+", 0, A, energy: 3, exhaust: true),
        El("AirRevelation", 1, A, draw: 3, energy: 1, exhaust: true), El("AirRevelation+", 1, A, draw: 4, energy: 1, exhaust: true),
        // Phase-less
        new("AlchInfusion", 0, LoopPhaseMove.Any, Energy: 1, SpendsMaterial: true),
        new("AlchInfusion+", 0, LoopPhaseMove.Any, Energy: 2, SpendsMaterial: true),
        new("AlchCatalysis", 0, Draw: 2, Energy: 1, SpendsMaterial: true),
        new("AlchCatalysis+", 0, Draw: 3, Energy: 1, SpendsMaterial: true),
        new("AlchSynergy", 1, Draw: 1), new("AlchSynergy+", 1, Draw: 2),
        new("AlchFlux", 1, LoopPhaseMove.Any), new("AlchFlux+", 0, LoopPhaseMove.Any),
        new("AlchReversal", 1, LoopPhaseMove.Previous),
        // Workshop
        Dual("Sandstorm", 1, E, A, draw: 1), Dual("Sandstorm+", 1, E, A, draw: 2),
        Dual("Drizzle", 1, W, A, draw: 1), Dual("FireWhirl", 1, F, A),
        // Life axis
        El("LifeGreatWork", 5, F), El("LifeGreatWork@0", 0, F), // 手本C once drain has paid its cost down
        // Powers (0 cost once upgraded). None of them draws or gains energy within the turn any more.
        new("AirSky", 2, IsPower: true), new("AirWindReading", 1, IsPower: true), new("AirAfterimage", 1, IsPower: true),
        new("WaterStill", 1, IsPower: true),
        new("CraftPhilosophersBlood", 1, IsPower: true),
        El("CraftEtherCatalyst", 0, A, draw: 2, energy: 1, exhaust: true), El("CraftEtherCatalyst+", 0, A, draw: 3, energy: 1, exhaust: true),
        El("CraftMitosis", 1, A, exhaust: true), El("CraftMitosis+", 0, A, exhaust: true),
        new("EarthCore", 1, IsPower: true), new("WaterCore", 1, IsPower: true), new("FireCore", 1, IsPower: true), new("AirCore", 1, IsPower: true)];
}
