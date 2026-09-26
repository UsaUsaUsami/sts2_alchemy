using Alchemist.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist;

// v0.20: the ten life cards added once the homunculus became a pet (design-axes 3.2). Homunculus HP is a shield
// that lasts across turns, so gaining it is lasting defence and spending it trades that shield for an effect.
// They took the reward slots of ten poison or duplicate cards (see design-decisions.md). Numbers and names are
// prototypes.

public sealed class LifeVampireBlade() : ElementCard(1,CardType.Attack,CardRarity.Common,TargetType.AnyEnemy,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(6,ValueProp.Move),new DynamicVar("Drain",2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<LifeDrainPower>()];
    public override List<(string,string)> Localization=>new CardLoc("吸血の刃","{Damage:diff()}ダメージ。[gold]ドレイン[/gold]{Drain:diff()}を与える。\n[gold]水相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await Hit(c,p,DynamicVars.Damage.BaseValue);await ApplyTo<LifeDrainPower>(c,p.Target!,DynamicVars["Drain"].BaseValue);}
    protected override void OnUpgrade(){DynamicVars.Damage.UpgradeValueBy(3);DynamicVars["Drain"].UpgradeValueBy(1);}
}
// v0.21: 報酬60枚への縮小で報酬プールから外した（旧セーブ読込用に定義だけ残す）。
public sealed class LifeMiasmaNeedle() : ElementCard(1,CardType.Skill,CardRarity.Event,TargetType.AllEnemies,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DynamicVar("Drain",1),new DynamicVar("Homunculus",4)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<LifeDrainPower>(),HoverTipFactory.FromPower<HomunculusPower>()];
    public override List<(string,string)> Localization=>new CardLoc("瘴気の針","敵全体に[gold]ドレイン[/gold]{Drain:diff()}を与える。[gold]ホムンクルスHP[/gold]を{Homunculus:diff()}得る。\n[gold]水相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p)
    {
        await ApplyAll<LifeDrainPower>(c,DynamicVars["Drain"].BaseValue);
        await LifeAxis.GainHomunculus(c,Owner,DynamicVars["Homunculus"].IntValue);
    }
    protected override void OnUpgrade(){DynamicVars["Drain"].UpgradeValueBy(1);DynamicVars["Homunculus"].UpgradeValueBy(2);}
}
/// <summary>肉の壁. Fewer points than 土壁's block because the homunculus keeps them across turns.</summary>
public sealed class LifeFleshWall() : ElementCard(1,CardType.Skill,CardRarity.Common,TargetType.Self,AlchemyPhase.Earth)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DynamicVar("Homunculus",6)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<HomunculusPower>()];
    public override List<(string,string)> Localization=>new CardLoc("肉の壁","[gold]ホムンクルスHP[/gold]を{Homunculus:diff()}得る。\n[gold]地相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>LifeAxis.GainHomunculus(c,Owner,DynamicVars["Homunculus"].IntValue);
    protected override void OnUpgrade()=>DynamicVars["Homunculus"].UpgradeValueBy(3);
}
public sealed class LifeBullet() : ElementCard(1,CardType.Attack,CardRarity.Common,TargetType.AnyEnemy,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(7,ValueProp.Move),new DynamicVar("Charged",15),new DynamicVar("Spend",5)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<HomunculusPower>()];
    public override List<(string,string)> Localization=>new CardLoc("命の弾丸","{Damage:diff()}ダメージ。[gold]ホムンクルスHP[/gold]を{Spend}消費できれば、代わりに{Charged:diff()}ダメージ。\n[gold]火相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p)
    {
        bool spent=await LifeAxis.TrySpendHomunculus(c,Owner,DynamicVars["Spend"].IntValue);
        await Hit(c,p,spent?DynamicVars["Charged"].BaseValue:DynamicVars.Damage.BaseValue);
    }
    protected override void OnUpgrade(){DynamicVars.Damage.UpgradeValueBy(3);DynamicVars["Charged"].UpgradeValueBy(5);}
}
public sealed class LifeNourish() : ElementCard(1,CardType.Power,CardRarity.Uncommon,TargetType.Self,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<NourishPower>(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<LifeDrainPower>(),HoverTipFactory.FromPower<HomunculusPower>()];
    public override List<(string,string)> Localization=>new CardLoc("養分","[gold]ドレイン[/gold]が発動するたび、[gold]ホムンクルスHP[/gold]を追加で{NourishPower:diff()}得る。\n[gold]水相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<NourishPower>(c,DynamicVars["NourishPower"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["NourishPower"].UpgradeValueBy(1);
}
public sealed class LifeSymbiosis() : ElementCard(1,CardType.Power,CardRarity.Uncommon,TargetType.Self,AlchemyPhase.Earth)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<SymbiosisPower>(4)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<HomunculusPower>()];
    public override List<(string,string)> Localization=>new CardLoc("共生","ターン終了時、ホムンクルスが場にいれば{SymbiosisPower:diff()}[gold]ブロック[/gold]を得る。\n[gold]地相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<SymbiosisPower>(c,DynamicVars["SymbiosisPower"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["SymbiosisPower"].UpgradeValueBy(2);
}
public sealed class LifeVeinStrike() : ElementCard(2,CardType.Attack,CardRarity.Uncommon,TargetType.AnyEnemy,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(10,ValueProp.Move),new DynamicVar("Threshold",20)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<HomunculusPower>()];
    public override List<(string,string)> Localization=>new CardLoc("命脈の一撃","{Damage:diff()}ダメージ。[gold]ホムンクルスHP[/gold]が{Threshold}以上なら、2回与える。\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)
        =>Hit(c,p,DynamicVars.Damage.BaseValue,(LifeAxis.State(Owner)?.HomunculusHp ?? 0)>=DynamicVars["Threshold"].IntValue?2:1);
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(3);
}
/// <summary>腐食の抱擁. Takes over 濃縮's slot: the drain version of doubling poison.</summary>
public sealed class LifeCorrosiveEmbrace() : ElementCard(1,CardType.Skill,CardRarity.Rare,TargetType.AnyEnemy,AlchemyPhase.Water)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<LifeDrainPower>()];
    public override List<(string,string)> Localization=>new CardLoc("腐食の抱擁","対象の[gold]ドレイン[/gold]を2倍にする。\n[gold]水相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)
        =>p.Target?.GetPower<LifeDrainPower>() is { Amount: > 0 } drain ? ApplyTo<LifeDrainPower>(c,p.Target,drain.Amount) : Task.CompletedTask;
    protected override void OnUpgrade()=>EnergyCost.UpgradeBy(-1);
}
public sealed class LifeTorrent() : ElementCard(2,CardType.Attack,CardRarity.Rare,TargetType.AllEnemies,AlchemyPhase.Fire)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<HomunculusPower>()];
    public override List<(string,string)> Localization=>new CardLoc("生命の奔流","[gold]ホムンクルスHP[/gold]をすべて消費し、その量のダメージを敵全体に与える。\n[gold]火相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p)
    {
        int spent=await LifeAxis.SpendAllHomunculus(c,Owner);
        if(spent>0) await HitAll(c,p,spent);
    }
    protected override void OnUpgrade()=>EnergyCost.UpgradeBy(-1);
}
/// <summary>目覚めた器. Air to fill the slot 順風 left; the effect itself does not read the phase.</summary>
public sealed class LifeAwakenedVessel() : ElementCard(3,CardType.Power,CardRarity.Rare,TargetType.Self,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<AwakenedVesselPower>(12),new DynamicVar("Spend",AwakenedVesselPower.Cost)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<HomunculusPower>()];
    public override List<(string,string)> Localization=>new CardLoc("目覚めた器","自分のターン開始時、[gold]ホムンクルスHP[/gold]を{Spend}消費して、ランダムな敵に{AwakenedVesselPower:diff()}ダメージを与える。\n[gold]風相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<AwakenedVesselPower>(c,DynamicVars["AwakenedVesselPower"].BaseValue);
    protected override void OnUpgrade()=>EnergyCost.UpgradeBy(-1);
}

/// <summary>養分: each drain trigger that took HP adds this much more homunculus HP.</summary>
public sealed class NourishPower : AlchemyPower
{
    protected override PowerModel IconSource => ModelDb.Power<RegenPower>();
    public override List<(string,string)> Localization => new PowerLoc("養分",
        "ドレインが発動するたび、ホムンクルスHPを追加で得る。",
        "[gold]ドレイン[/gold]が発動するたび、[gold]ホムンクルスHP[/gold]を追加で{Amount}得る。");
    public async Task OnDrained()
    {
        if (Owner.Player is not { } player) return;
        Flash();
        await LifeAxis.GainHomunculus(new ThrowingPlayerChoiceContext(), player, Amount);
    }
}

/// <summary>共生: block at turn end while the homunculus stands.</summary>
public sealed class SymbiosisPower : AlchemyPower
{
    protected override PowerModel IconSource => ModelDb.Power<PlatingPower>();
    public override List<(string,string)> Localization => new PowerLoc("共生",
        "ターン終了時、ホムンクルスが場にいればブロックを得る。",
        "ターン終了時、ホムンクルスが場にいれば{Amount}[gold]ブロック[/gold]を得る。");
    public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext c, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner) || Owner.Player is not { } player || LifeAxis.Pet(player) is not { IsAlive: true }) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

/// <summary>目覚めた器: at the start of your turn the homunculus spends some of itself to strike a random enemy.</summary>
public sealed class AwakenedVesselPower : AlchemyPower
{
    public const int Cost = 5;
    protected override PowerModel IconSource => ModelDb.Power<JuggernautPower>();
    public override List<(string,string)> Localization => new PowerLoc("目覚めた器",
        "自分のターン開始時、ホムンクルスHPを消費して、ランダムな敵にダメージを与える。",
        $"自分のターン開始時、[gold]ホムンクルスHP[/gold]を{Cost}消費して、ランダムな敵に{{Amount}}ダメージを与える。");
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner) || Owner.Player is not { } player) return;
        var enemies = combatState.HittableEnemies;
        if (enemies.Count == 0) return;
        var c = new ThrowingPlayerChoiceContext();
        if (!await LifeAxis.TrySpendHomunculus(c, player, Cost)) return;
        Flash();
        if (player.RunState.Rng.CombatTargets.NextItem(enemies) is not { } target) return;
        await CreatureCmd.Damage(c, target, Amount, ValueProp.Unpowered, Owner);
    }
}

/// <summary>賢者の血 (錬成): when the homunculus soaks an attack, the attacker gets drain.</summary>
public sealed class PhilosophersBloodPower : AlchemyPower
{
    protected override PowerModel IconSource => ModelDb.Power<ThornsPower>();
    public override List<(string,string)> Localization => new PowerLoc("賢者の血",
        "ホムンクルスが攻撃を肩代わりするたび、攻撃した敵にドレインを与える。",
        "ホムンクルスが攻撃を肩代わりするたび、攻撃した敵に[gold]ドレイン[/gold]{Amount}を与える。");
    // CreatureCmd.Damage reports a redirected hit with the pet as the target (the result's receiver).
    public override async Task AfterDamageReceived(PlayerChoiceContext c, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner.Player is not { } player || target != LifeAxis.Pet(player) || !props.IsPoweredAttack()) return;
        if (result.UnblockedDamage <= 0 || dealer is not { IsAlive: true } attacker || attacker.Side == Owner.Side) return;
        Flash();
        await PowerCmd.Apply<LifeDrainPower>(c, attacker, Amount, Owner, null);
    }
}

/// <summary>培養槽 (錬成): homunculus HP at the start of each of your turns.</summary>
public sealed class CultureVatPower : AlchemyPower
{
    protected override PowerModel IconSource => ModelDb.Power<RegenPower>();
    public override List<(string,string)> Localization => new PowerLoc("培養槽",
        "自分のターン開始時、ホムンクルスHPを得る。",
        "自分のターン開始時、[gold]ホムンクルスHP[/gold]を{Amount}得る。");
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner) || Owner.Player is not { } player) return;
        Flash();
        await LifeAxis.GainHomunculus(new ThrowingPlayerChoiceContext(), player, Amount);
    }
}
