using Alchemist.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist;

/// <summary>Stacking buff with placeholder art borrowed from a base-game power.</summary>
public abstract class AlchemyPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected abstract PowerModel IconSource { get; }
    public override string? CustomPackedIconPath => IconSource.PackedIconPath;
    public override string? CustomBigIconPath => $"res://images/powers/{IconSource.Id.Entry.ToLowerInvariant()}.png";

    protected AlchemyPhase CurrentPhase => Owner.Player?.GetRelic<MaterialBox>()?.Combat?.Phases.Current ?? AlchemyPhase.None;
    protected async Task DamageAllEnemies(PlayerChoiceContext c, decimal amount)
    {
        foreach (var enemy in Owner.CombatState?.HittableEnemies.ToArray() ?? [])
            await CreatureCmd.Damage(c, enemy, amount, ValueProp.Unpowered, Owner, null, null);
    }
}

/// <summary>"Whenever you transition into ...": the shared listener shape for transition powers.</summary>
public abstract class TransitionListenerPower : AlchemyPower, IPhaseTransitionListener
{
    protected virtual bool Matches(PhaseTransitionContext t) => true;
    protected abstract Task OnTransition(PhaseTransitionContext t);
    public async Task AfterPhaseTransition(PhaseTransitionContext t)
    {
        if (t.Owner.Creature != Owner || !Matches(t)) return;
        Flash();
        await OnTransition(t);
    }
}

public sealed class FlameHeartPower : TransitionListenerPower
{
    protected override PowerModel IconSource => ModelDb.Power<RagePower>();
    public override List<(string,string)> Localization => new PowerLoc("焔の心","火相へ転移するたび、筋力を得る。","[gold]火相[/gold]へ転移するたび、[gold]筋力[/gold]{Amount}を得る。");
    protected override bool Matches(PhaseTransitionContext t) => t.To == AlchemyPhase.Fire;
    protected override Task OnTransition(PhaseTransitionContext t)
        => PowerCmd.Apply<StrengthPower>(t.Choice, Owner, Amount, Owner, null);
}
/// <summary>Pays next-turn energy when this turn saw at least Threshold transitions.</summary>
public sealed class SkyPower : AlchemyPower
{
    public const int Threshold = 4;
    protected override PowerModel IconSource => ModelDb.Power<MachineLearningPower>();
    public override List<(string,string)> Localization => new PowerLoc("オーバードライブ",$"ターン終了時、このターンに相転移が{Threshold}回以上起きていれば、次のターンにエナジーを得る。",$"ターン終了時、このターンに相転移が{Threshold}回以上起きていれば、次のターン、エナジーを{{Amount}}得る。");
    public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext c, CombatSide side, IEnumerable<Creature> participants)
    {
        var phases = Owner.Player?.GetRelic<MaterialBox>()?.Combat?.Phases;
        if (!participants.Contains(Owner) || phases is null || phases.TransitionsThisTurn < Threshold) return;
        Flash();
        await PowerCmd.Apply<EnergyNextTurnPower>(c, Owner, Amount, Owner, null);
    }
}

/// <summary>風の残像 (v0.23): at turn end, one hit on a random enemy for each transition this turn.</summary>
public sealed class WindAfterimagePower : AlchemyPower
{
    protected override PowerModel IconSource => ModelDb.Power<AfterimagePower>();
    public override List<(string,string)> Localization => new PowerLoc("残風の刃",
        "ターン終了時、このターンに起きた相転移1回につき、ランダムな敵にダメージを与える。",
        "ターン終了時、このターンに起きた[gold]相転移[/gold]1回につき、ランダムな敵に{Amount}ダメージを与える。");
    public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext c, CombatSide side, IEnumerable<Creature> participants)
    {
        var phases = Owner.Player?.GetRelic<MaterialBox>()?.Combat?.Phases;
        if (!participants.Contains(Owner) || Owner.Player is not { } player || phases is not { TransitionsThisTurn: > 0 }) return;
        Flash();
        for (int i = 0; i < phases.TransitionsThisTurn; i++)
        {
            var enemies = Owner.CombatState?.HittableEnemies;
            if (enemies is not { Count: > 0 } || player.RunState.Rng.CombatTargets.NextItem(enemies) is not { } target) return;
            await CreatureCmd.Damage(c, target, Amount, ValueProp.Unpowered, Owner, null, null);
        }
    }
}

/// <summary>
/// 大地の王 (v0.23): StS1's Calipers. The turn-start block clear is prevented and only Amount is lost instead.
/// Single, so a second copy never raises the loss. When Blur or Barricade also holds the block, nothing is lost.
/// </summary>
public sealed class EarthKingPower : AlchemyPower
{
    public override PowerStackType StackType => PowerStackType.Single;
    protected override PowerModel IconSource => ModelDb.Power<BarricadePower>();
    public override List<(string,string)> Localization => new PowerLoc("大地の王",
        "ターン開始時、ブロックがすべて失われる代わりに、一定量だけ失われる。",
        "ターン開始時、[gold]ブロック[/gold]がすべて失われる代わりに、{Amount}だけ失われる。");
    public override bool ShouldClearBlock(Creature creature) => creature != Owner;
    public override async Task AfterPreventingBlockClear(AbstractModel preventer, Creature creature)
    {
        if (creature != Owner || Owner.GetPower<BlurPower>() is not null || Owner.GetPower<BarricadePower>() is not null) return;
        Flash();
        if (Owner.Block > 0) await CreatureCmd.LoseBlock(new ThrowingPlayerChoiceContext(), Owner, Math.Min(Amount, Owner.Block), Owner);
    }
}

/// <summary>Strengthens the next real transition once, then removes itself.</summary>
public sealed class PreparationPower : AlchemyPower, IPhaseTransitionModifier, IPhaseTransitionListener
{
    protected override PowerModel IconSource => ModelDb.Power<VigorPower>();
    // v0.23 (ユーザーレビュー): the buff had the card's name. 励起 is the state just before a transition.
    public override List<(string,string)> Localization => new PowerLoc("励起","次の相転移の効果を強化する。風への転移でも消費される。","次の相転移の効果を{Amount}強化する。風への転移でも消費される。");
    public void ModifyPhaseTransition(PhaseTransitionContext t) { if (!t.IsEcho && t.Owner.Creature == Owner) t.Amount += Amount; }
    public Task AfterPhaseTransition(PhaseTransitionContext t) => t.Owner.Creature == Owner ? PowerCmd.Remove(this) : Task.CompletedTask;
}
/// <summary>Makes the next real transition resolve extra times, then removes itself.</summary>
public sealed class SynergyPower : AlchemyPower, IPhaseTransitionModifier, IPhaseTransitionListener
{
    protected override PowerModel IconSource => ModelDb.Power<EchoFormPower>();
    public override List<(string,string)> Localization => new PowerLoc("ダブルシフト","次の相転移の効果が追加で発動する。","次の相転移の効果が、追加で{Amount}回発動する。");
    public void ModifyPhaseTransition(PhaseTransitionContext t) { if (!t.IsEcho && t.Owner.Creature == Owner) t.Repeats += Amount; }
    public Task AfterPhaseTransition(PhaseTransitionContext t) => t.Owner.Creature == Owner ? PowerCmd.Remove(this) : Task.CompletedTask;
}
public sealed class PhilosophersStonePower : AlchemyPower, IPhaseTransitionModifier
{
    protected override PowerModel IconSource => ModelDb.Power<DemonFormPower>();
    public override List<(string,string)> Localization => new PowerLoc("賢者の石","相転移の効果を強化する。","相転移の効果を{Amount}強化する。");
    public void ModifyPhaseTransition(PhaseTransitionContext t) { if (t.Owner.Creature == Owner) t.Amount += Amount; }
}

/// <summary>Workshop pure-phase power: strengthens every transition into one element.</summary>
public abstract class ElementCorePower(AlchemyPhase phase) : AlchemyPower, IPhaseTransitionModifier
{
    public void ModifyPhaseTransition(PhaseTransitionContext t) { if (t.Owner.Creature == Owner && t.To == phase) t.Amount += Amount; }
}
public sealed class EarthCorePower() : ElementCorePower(AlchemyPhase.Earth)
{
    protected override PowerModel IconSource => ModelDb.Power<PlatingPower>();
    public override List<(string,string)> Localization => new PowerLoc("大地の心核","地相への相転移の効果を強化する。","[gold]地相[/gold]への相転移の効果を{Amount}強化する。");
}
/// <summary>
/// Water core (design-axes.md 7.2): transitions into water also apply Vulnerable, on the same enemy the weak
/// went to. The weak itself is left at the base amount.
/// </summary>
public sealed class WaterCorePower : AlchemyPower, IPhaseTransitionListener
{
    protected override PowerModel IconSource => ModelDb.Power<RegenPower>();
    public override List<(string,string)> Localization => new PowerLoc("流水の心核","水相へ転移するたび、脱力に加えて弱体を与える。","[gold]水相[/gold]へ転移するたび、[gold]脱力[/gold]に加えて[gold]弱体[/gold]{Amount}を与える。");
    public async Task AfterPhaseTransition(PhaseTransitionContext t)
    {
        if (t.Owner.Creature != Owner || t.To != AlchemyPhase.Water || t.Suppressed) return;
        var enemy = t.Target is { IsDead: false, Side: CombatSide.Enemy } ? t.Target : Owner.CombatState?.HittableEnemies.FirstOrDefault();
        if (enemy is null) return;
        Flash();
        await PowerCmd.Apply<VulnerablePower>(t.Choice, enemy, Amount, Owner, null);
    }
}
public sealed class FireCorePower() : ElementCorePower(AlchemyPhase.Fire)
{
    protected override PowerModel IconSource => ModelDb.Power<InfernoPower>();
    public override List<(string,string)> Localization => new PowerLoc("劫火の心核","火相への相転移の効果を強化する。","[gold]火相[/gold]への相転移の効果を{Amount}強化する。");
}
/// <summary>
/// Air core (design-axes.md 7.2): entering air charges the next transition's base effect by Amount. The next
/// transition always leaves air, so this never adds draw (原則5).
/// </summary>
public sealed class AirCorePower : AlchemyPower, IPhaseTransitionModifier, IPhaseTransitionListener
{
    private int charge;
    protected override PowerModel IconSource => ModelDb.Power<MachineLearningPower>();
    public override List<(string,string)> Localization => new PowerLoc("疾風の心核","風相へ転移したとき、次の相転移の効果を強化する。","[gold]風相[/gold]へ転移したとき、次の相転移の効果を{Amount}強化する。");
    public void ModifyPhaseTransition(PhaseTransitionContext t)
    {
        if (t.Owner.Creature == Owner && !t.IsEcho && t.To != AlchemyPhase.Air) t.Amount += charge;
    }
    public Task AfterPhaseTransition(PhaseTransitionContext t)
    {
        if (t.Owner.Creature == Owner) charge = t.To == AlchemyPhase.Air ? Amount : 0;
        return Task.CompletedTask;
    }
}
