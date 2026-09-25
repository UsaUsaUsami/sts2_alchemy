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

/// <summary>"End your turn in phase X": rewards holding a phase rather than switching.</summary>
public abstract class PhaseStancePower(AlchemyPhase phase) : AlchemyPower
{
    public AlchemyPhase Phase => phase;
    protected abstract Task OnStance(PlayerChoiceContext c);
    public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext c, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner) || CurrentPhase != Phase) return;
        Flash();
        await OnStance(c);
    }
}

/// <summary>"Whenever you transition": the reference listener shape for reward-pool powers.</summary>
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

public sealed class EarthVeinPower() : PhaseStancePower(AlchemyPhase.Earth)
{
    protected override PowerModel IconSource => ModelDb.Power<PlatingPower>();
    public override List<(string,string)> Localization => new PowerLoc("地脈","ターン終了時、地相ならブロックを得る。","ターン終了時、[gold]地相[/gold]なら{Amount}[gold]ブロック[/gold]を得る。");
    protected override Task OnStance(PlayerChoiceContext c) => CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
}
public sealed class StillWaterPower() : PhaseStancePower(AlchemyPhase.Water)
{
    protected override PowerModel IconSource => ModelDb.Power<RegenPower>();
    public override List<(string,string)> Localization => new PowerLoc("静水","ターン終了時、水相なら敵全体に脱力を与える。","ターン終了時、[gold]水相[/gold]なら敵全体に[gold]脱力[/gold]{Amount}を与える。");
    protected override Task OnStance(PlayerChoiceContext c)
        => PowerCmd.Apply<WeakPower>(c, Owner.CombatState!.HittableEnemies, Amount, Owner, null);
}
public sealed class BurningWillPower() : PhaseStancePower(AlchemyPhase.Fire)
{
    protected override PowerModel IconSource => ModelDb.Power<InfernoPower>();
    public override List<(string,string)> Localization => new PowerLoc("燃える意志","ターン終了時、火相なら敵全体にダメージを与える。","ターン終了時、[gold]火相[/gold]なら敵全体に{Amount}ダメージを与える。");
    protected override Task OnStance(PlayerChoiceContext c) => DamageAllEnemies(c, Amount);
}
public sealed class WindReadingPower() : PhaseStancePower(AlchemyPhase.Air)
{
    protected override PowerModel IconSource => ModelDb.Power<DrawCardsNextTurnPower>();
    public override List<(string,string)> Localization => new PowerLoc("風読み","ターン終了時、風相なら次のターンに追加でカードを引く。","ターン終了時、[gold]風相[/gold]なら次のターンにカードを{Amount}枚追加で引く。");
    protected override Task OnStance(PlayerChoiceContext c)
        => PowerCmd.Apply<DrawCardsNextTurnPower>(c, Owner, Amount, Owner, null);
}

public sealed class PhaseResonancePower : TransitionListenerPower
{
    protected override PowerModel IconSource => ModelDb.Power<PlatingPower>();
    public override List<(string,string)> Localization => new PowerLoc("相の共鳴","相転移するたび、ブロックを得る。","相転移するたび、{Amount}[gold]ブロック[/gold]を得る。");
    protected override Task OnTransition(PhaseTransitionContext t) => CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
}
public sealed class WaterBlessingPower : TransitionListenerPower
{
    protected override PowerModel IconSource => ModelDb.Power<NoxiousFumesPower>();
    public override List<(string,string)> Localization => new PowerLoc("水神の加護","相転移するたび、敵全体に毒を与える。","相転移するたび、敵全体に[gold]毒[/gold]{Amount}を与える。");
    protected override Task OnTransition(PhaseTransitionContext t)
        => PowerCmd.Apply<PoisonPower>(t.Choice, Owner.CombatState!.HittableEnemies, Amount, Owner, null);
}
public sealed class FlameHeartPower : TransitionListenerPower
{
    protected override PowerModel IconSource => ModelDb.Power<RagePower>();
    public override List<(string,string)> Localization => new PowerLoc("焔の心","火相へ転移するたび、筋力を得る。","[gold]火相[/gold]へ転移するたび、[gold]筋力[/gold]{Amount}を得る。");
    protected override bool Matches(PhaseTransitionContext t) => t.To == AlchemyPhase.Fire;
    protected override Task OnTransition(PhaseTransitionContext t)
        => PowerCmd.Apply<StrengthPower>(t.Choice, Owner, Amount, Owner, null);
}
public sealed class FireStormPower : TransitionListenerPower
{
    protected override PowerModel IconSource => ModelDb.Power<InfernoPower>();
    public override List<(string,string)> Localization => new PowerLoc("炎の嵐","相転移するたび、敵全体にダメージを与える。","相転移するたび、敵全体に{Amount}ダメージを与える。");
    protected override Task OnTransition(PhaseTransitionContext t) => DamageAllEnemies(t.Choice, Amount);
}
public sealed class SkyPower : TransitionListenerPower
{
    protected override PowerModel IconSource => ModelDb.Power<MachineLearningPower>();
    public override List<(string,string)> Localization => new PowerLoc("天空","相転移するたび、カードを引く。","相転移するたび、カードを{Amount}枚引く。");
    protected override Task OnTransition(PhaseTransitionContext t) => CardPileCmd.Draw(t.Choice, Amount, t.Owner);
}

/// <summary>Strengthens the next real transition once, then removes itself.</summary>
public sealed class PreparationPower : AlchemyPower, IPhaseTransitionModifier, IPhaseTransitionListener
{
    protected override PowerModel IconSource => ModelDb.Power<VigorPower>();
    public override List<(string,string)> Localization => new PowerLoc("調合準備","次の相転移の効果を強化する。","次の相転移の効果を{Amount}強化する。");
    public void ModifyPhaseTransition(PhaseTransitionContext t) { if (!t.IsEcho && t.Owner.Creature == Owner) t.Amount += Amount; }
    public Task AfterPhaseTransition(PhaseTransitionContext t) => t.Owner.Creature == Owner ? PowerCmd.Remove(this) : Task.CompletedTask;
}
/// <summary>Makes the next real transition resolve extra times, then removes itself.</summary>
public sealed class SynergyPower : AlchemyPower, IPhaseTransitionModifier, IPhaseTransitionListener
{
    protected override PowerModel IconSource => ModelDb.Power<EchoFormPower>();
    public override List<(string,string)> Localization => new PowerLoc("相乗","次の相転移の効果が追加で発動する。","次の相転移の効果が、追加で{Amount}回発動する。");
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
public sealed class WaterCorePower() : ElementCorePower(AlchemyPhase.Water)
{
    protected override PowerModel IconSource => ModelDb.Power<RegenPower>();
    public override List<(string,string)> Localization => new PowerLoc("流水の心核","水相への相転移の効果を強化する。","[gold]水相[/gold]への相転移の効果を{Amount}強化する。");
}
public sealed class FireCorePower() : ElementCorePower(AlchemyPhase.Fire)
{
    protected override PowerModel IconSource => ModelDb.Power<InfernoPower>();
    public override List<(string,string)> Localization => new PowerLoc("劫火の心核","火相への相転移の効果を強化する。","[gold]火相[/gold]への相転移の効果を{Amount}強化する。");
}
public sealed class AirCorePower() : ElementCorePower(AlchemyPhase.Air)
{
    protected override PowerModel IconSource => ModelDb.Power<MachineLearningPower>();
    public override List<(string,string)> Localization => new PowerLoc("疾風の心核","風相への相転移の効果を強化する。","[gold]風相[/gold]への相転移の効果を{Amount}強化する。");
}
