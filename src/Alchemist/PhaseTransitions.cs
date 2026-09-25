using Alchemist.Core;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist;

/// <summary>One phase transition in flight. Modifiers may change Amount or suppress the effect.</summary>
public sealed class PhaseTransitionContext(PlayerChoiceContext choice, Player owner, AlchemyPhase from, AlchemyPhase to,
    CardModel? source, Creature? target, CardPlay? play)
{
    public PlayerChoiceContext Choice { get; } = choice;
    public Player Owner { get; } = owner;
    public AlchemyPhase From { get; } = from;
    public AlchemyPhase To { get; } = to;
    /// The card whose play caused the transition, if any.
    public CardModel? Source { get; } = source;
    public Creature? Target { get; } = target;
    public CardPlay? Play { get; } = play;
    public int Amount { get; set; } = PhaseRules.BaseAmount(to);
    /// How many times the destination effect resolves. Modifiers raise this for "trigger twice" effects.
    public int Repeats { get; set; } = 1;
    public bool Suppressed { get; set; }
    /// True for Echo: the effect resolves without the phase actually changing.
    public bool IsEcho { get; init; }
}

/// <summary>
/// Implemented by relics, powers or card enchantments that change the destination effect before it
/// resolves (strengthen, weaken, seal). Called in order: relics, then the owner's powers, then the source
/// card's enchantment.
/// </summary>
public interface IPhaseTransitionModifier
{
    void ModifyPhaseTransition(PhaseTransitionContext transition);
}

/// <summary>Implemented by models that react after a transition resolved, e.g. "whenever you transition".</summary>
public interface IPhaseTransitionListener
{
    Task AfterPhaseTransition(PhaseTransitionContext transition);
}

/// <summary>
/// The single entry point for changing the combat phase. Cards only declare an element (or call Enter for
/// "advance" style effects); detecting a transition and resolving its destination effect happens here.
/// </summary>
public static class PhaseTransitions
{
    public static async Task<PhaseTransition> Enter(PlayerChoiceContext choice, Player owner, AlchemyPhase to,
        CardModel? source = null, Creature? target = null, CardPlay? play = null)
    {
        var box = owner.GetRelic<MaterialBox>();
        if (box?.Combat is not { } combat || !PhaseRules.IsElement(to)) return default;
        var change = combat.Phases.Enter(to);
        if (!change.Triggered)
        {
            combat.LastTransition = "";
            return change;
        }
        var transition = new PhaseTransitionContext(choice, owner, change.From, change.To, source, target, play);
        foreach (var modifier in Hooks<IPhaseTransitionModifier>(owner, source)) modifier.ModifyPhaseTransition(transition);
        combat.LastTransition = $"{PhaseRules.Name(change.From)}→{PhaseRules.Name(change.To)}";
        Feedback(box, transition);
        if (!transition.Suppressed && transition.Amount > 0)
            for (int i = 0; i < transition.Repeats; i++) await Resolve(transition);
        foreach (var listener in Hooks<IPhaseTransitionListener>(owner, source)) await listener.AfterPhaseTransition(transition);
        return change;
    }

    /// Resolves the current phase's destination effect again without changing the phase. Modifiers apply,
    /// but it is not a transition: it neither counts nor notifies listeners, so echoes cannot chain.
    public static async Task Echo(PlayerChoiceContext choice, Player owner, CardModel? source = null, Creature? target = null, CardPlay? play = null)
    {
        var current = owner.GetRelic<MaterialBox>()?.Combat?.Phases.Current ?? AlchemyPhase.None;
        if (!PhaseRules.IsElement(current)) return;
        var echo = new PhaseTransitionContext(choice, owner, current, current, source, target, play) { IsEcho = true };
        foreach (var modifier in Hooks<IPhaseTransitionModifier>(owner, source)) modifier.ModifyPhaseTransition(echo);
        if (!echo.Suppressed && echo.Amount > 0)
            for (int i = 0; i < echo.Repeats; i++) await Resolve(echo);
    }

    /// Returns to the element held before the current one, which is a normal transition.
    public static Task ReturnToPrevious(PlayerChoiceContext choice, Player owner, CardModel? source = null, Creature? target = null, CardPlay? play = null)
    {
        var previous = owner.GetRelic<MaterialBox>()?.Combat?.Phases.Previous ?? AlchemyPhase.None;
        return PhaseRules.IsElement(previous) ? Enter(choice, owner, previous, source, target, play) : Task.CompletedTask;
    }

    /// Moves to the next element in the Earth→Water→Fire→Air cycle. Without an element there is nothing to
    /// advance from, so this does nothing in the neutral phase.
    public static async Task Advance(PlayerChoiceContext choice, Player owner, int steps, CardModel? source = null,
        Creature? target = null, CardPlay? play = null)
    {
        for (int i = 0; i < steps; i++)
        {
            var current = owner.GetRelic<MaterialBox>()?.Combat?.Phases.Current ?? AlchemyPhase.None;
            if (current == AlchemyPhase.None) return;
            await Enter(choice, owner, PhaseRules.Next(current), source, target, play);
        }
    }

    private static async Task Resolve(PhaseTransitionContext t)
    {
        var creature = t.Owner.Creature;
        var enemy = t.Target is { IsDead: false, Side: CombatSide.Enemy } ? t.Target
            : creature.CombatState?.HittableEnemies.FirstOrDefault();
        switch (t.To)
        {
            case AlchemyPhase.Earth:
                await CreatureCmd.GainBlock(creature, t.Amount, ValueProp.Unpowered, t.Play);
                break;
            case AlchemyPhase.Water when enemy is not null:
                await PowerCmd.Apply<WeakPower>(t.Choice, enemy, t.Amount, creature, t.Source);
                break;
            case AlchemyPhase.Fire when enemy is not null:
                await CreatureCmd.Damage(t.Choice, enemy, t.Amount, ValueProp.Unpowered, creature, t.Source, t.Play);
                break;
            case AlchemyPhase.Air:
                await CardPileCmd.Draw(t.Choice, t.Amount, t.Owner);
                break;
        }
    }

    private static IEnumerable<T> Hooks<T>(Player owner, CardModel? source) where T : class
    {
        foreach (var relic in owner.Relics) if (relic is T hook) yield return hook;
        foreach (var power in owner.Creature.Powers.ToArray()) if (power is T hook) yield return hook;
        if (source?.Enchantment is T enchantment) yield return enchantment;
    }

    // Cosmetic only: a failure to show the bubble must never interrupt combat.
    private static void Feedback(MaterialBox box, PhaseTransitionContext t)
    {
        try
        {
            box.Flash();
            var line = new LocString("relics", $"{box.Id.Entry}.phaseShift.{t.To}");
            TalkCmd.Play(line, t.Owner.Creature, PhaseColor(t.To), VfxDuration.VeryShort);
        }
        catch (Exception ex) { GD.PushWarning($"Alchemist phase feedback skipped: {ex.Message}"); }
    }

    public static VfxColor PhaseColor(AlchemyPhase phase) => phase switch
    {
        AlchemyPhase.Earth => VfxColor.Orange,
        AlchemyPhase.Water => VfxColor.Blue,
        AlchemyPhase.Fire => VfxColor.Red,
        AlchemyPhase.Air => VfxColor.Green,
        _ => VfxColor.White
    };
}
