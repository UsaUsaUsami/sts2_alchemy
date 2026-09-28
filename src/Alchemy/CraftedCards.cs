using Alchemy.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemy;

/// <summary>A workshop card that can carry one permanent rare-material inscription.</summary>
public interface IRareInscribable
{
    string AlchemistRareModifier { get; set; }
    /// Cost before the inscription, used to judge whether Void Crystal still means anything.
    int InscriptionBaseCost { get; }
}

/// <summary>
/// Workshop-only card (v0.17). Event rarity keeps it out of rewards and the merchant. The rare inscription is
/// saved per card and re-applied after load and after a downgrade resets keywords.
/// </summary>
public abstract class CraftedCard(int cost, CardType type, TargetType target) : AlchemyCard(cost, type, CardRarity.Event, target), IRareInscribable
{
    private readonly int baseCost = cost;
    private string rareModifierId = "";
    public RareMaterialDefinition? RareModifier => rareModifierId.Length == 0 ? null : RareMaterials.Get(rareModifierId);
    public int InscriptionBaseCost => baseCost;
    [SavedProperty]
    public string AlchemistRareModifier
    {
        get => rareModifierId;
        set
        {
            AssertMutable();
            if (value.Length > 0) _ = RareMaterials.Get(value); // Validate before changing existing data.
            rareModifierId = value;
            ApplyInscription();
        }
    }
    private bool Has(RareMaterial m) => RareModifier?.Material == m;
    /// What the inscription currently adds to the base cost. Tracked so the change is made relative to the cost
    /// after any upgrade (an upgraded card keeps its -1), and so re-applying never stacks it.
    private int inscribedCostDelta;
    public const int StardustCostIncrease = 1;
    private void ApplyInscription()
    {
        // v0.22.4 (ユーザー判断): a replay on top of the card is too strong for free, so stardust also costs 1 more.
        int wanted = Has(RareMaterial.Stardust) ? StardustCostIncrease : Has(RareMaterial.VoidCrystal) ? -1 : 0;
        int uninscribed = EnergyCost.GetWithModifiers(CostModifiers.None) - inscribedCostDelta;
        int inscribed = Math.Max(0, uninscribed + wanted);
        if (inscribed - uninscribed != inscribedCostDelta) EnergyCost.SetCustomBaseCost(inscribed);
        inscribedCostDelta = inscribed - uninscribed;
        if (Has(RareMaterial.Stardust) && BaseReplayCount < 1) BaseReplayCount = 1;
        if (Has(RareMaterial.VoidCrystal)) AddKeyword(CardKeyword.Exhaust);
        if (Has(RareMaterial.Mercury)) AddKeyword(CardKeyword.Retain);
    }
    // A downgrade resets the cost to the printed one, taking the inscription's change with it.
    protected override void AfterDowngraded() { inscribedCostDelta = 0; ApplyInscription(); }

    protected abstract string CardTitle { get; }
    protected abstract string CardText { get; }
    /// One description per inscription, chosen by CraftedDescriptionPatch.
    public override List<(string, string)> Localization => [
        ("title", CardTitle), ("description", CardText),
        ..RareMaterials.All.Select(r => ($"{r.Id}.description", $"{CardText}\n[gold]{r.EffectName}[/gold]：{r.Description}"))];
}

/// <summary>
/// Same material twice: a power tied to its own element. Cores are designed per element rather than as one
/// flat bonus (design-axes.md 7.2), so each may describe its own effect.
/// </summary>
public abstract class CorePowerCard<TPower>(AlchemyPhase element, int amount, string title, string? effect = null)
    : CraftedCard(1, CardType.Power, TargetType.Self) where TPower : PowerModel
{
    public override AlchemyPhase Element => element;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<TPower>(amount)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<TPower>()];
    protected override string CardTitle => title;
    protected override string CardText => (effect ?? $"[gold]{PhaseRules.Name(element)}相[/gold]への[gold]相転移[/gold]の効果を{{{typeof(TPower).Name}:diff()}}強化する。")
        + $"\n[gold]{PhaseRules.Name(element)}相[/gold]";
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => ApplySelf<TPower>(c, DynamicVars[typeof(TPower).Name].BaseValue);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
public sealed class EarthCore() : CorePowerCard<EarthCorePower>(AlchemyPhase.Earth, 3, "大地の心核");
public sealed class WaterCore() : CorePowerCard<WaterCorePower>(AlchemyPhase.Water, 1, "流水の心核",
    "[gold]水相[/gold]へ転移するたび、[gold]脱力[/gold]に加えて[gold]弱体[/gold]{WaterCorePower:diff()}を与える。");
public sealed class FireCore() : CorePowerCard<FireCorePower>(AlchemyPhase.Fire, 3, "劫火の心核");
public sealed class AirCore() : CorePowerCard<AirCorePower>(AlchemyPhase.Air, 1, "疾風の心核",
    "[gold]風相[/gold]へ転移したとき、次の[gold]相転移[/gold]の効果を{AirCorePower:diff()}強化する。")
{
    // v0.23 (ユーザーレビュー): the upgrade doubles the charge instead of making it free.
    protected override void OnUpgrade() => DynamicVars["AirCorePower"].UpgradeValueBy(1);
}

/// <summary>
/// Two different materials: after its effect the card enters its first element, then MaterialBox enters the
/// second, so one play causes up to two transitions. Replays only repeat the effect, not the extra entry.
/// </summary>
public abstract class DualElementCard(int cost, CardType type, TargetType target, AlchemyPhase first, AlchemyPhase second)
    : CraftedCard(cost, type, target)
{
    public AlchemyPhase FirstElement => first;
    public override AlchemyPhase Element => second;
    protected abstract string EffectText { get; }
    protected override string CardText => $"{EffectText}\n[gold]{PhaseRules.Name(first)}相→{PhaseRules.Name(second)}相[/gold]";
    protected abstract Task Effect(PlayerChoiceContext c, CardPlay p);
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        await Effect(c, p);
        if (p.IsFirstInSeries) await PhaseTransitions.Enter(c, Owner, first, this, p.Target, p);
    }
}
public sealed class MudRampart() : DualElementCard(1, CardType.Skill, TargetType.AnyEnemy, AlchemyPhase.Earth, AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(7, ValueProp.Move), new PowerVar<WeakPower>(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WeakPower>()];
    protected override string CardTitle => "泥の城壁";
    protected override string EffectText => "{Block:diff()}[gold]ブロック[/gold]を得る。[gold]脱力[/gold]{WeakPower:diff()}を与える。";
    protected override async Task Effect(PlayerChoiceContext c, CardPlay p) { await CardBlock(p); await ApplyTo<WeakPower>(c, p.Target!, DynamicVars.Weak.BaseValue); }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(3); DynamicVars.Weak.UpgradeValueBy(1); }
}
public sealed class LavaShot() : DualElementCard(1, CardType.Attack, TargetType.AnyEnemy, AlchemyPhase.Earth, AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9, ValueProp.Move), new BlockVar(4, ValueProp.Move)];
    public override bool GainsBlock => true;
    protected override string CardTitle => "溶岩弾";
    protected override string EffectText => "{Damage:diff()}ダメージ。{Block:diff()}[gold]ブロック[/gold]を得る。";
    protected override async Task Effect(PlayerChoiceContext c, CardPlay p) { await Hit(c, p, DynamicVars.Damage.BaseValue); await CardBlock(p); }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(3); DynamicVars.Block.UpgradeValueBy(2); }
}
public sealed class Sandstorm() : DualElementCard(1, CardType.Skill, TargetType.Self, AlchemyPhase.Earth, AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6, ValueProp.Move), new CardsVar(1)];
    protected override string CardTitle => "砂嵐";
    protected override string EffectText => "{Block:diff()}[gold]ブロック[/gold]を得る。カードを{Cards:diff()}枚引く。";
    protected override async Task Effect(PlayerChoiceContext c, CardPlay p) { await CardBlock(p); await Draw(c, DynamicVars.Cards.BaseValue); }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(3); DynamicVars.Cards.UpgradeValueBy(1); }
}
public sealed class SteamBurst() : DualElementCard(1, CardType.Attack, TargetType.AllEnemies, AlchemyPhase.Water, AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6, ValueProp.Move), new PowerVar<WeakPower>(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WeakPower>()];
    protected override string CardTitle => "蒸気爆発";
    protected override string EffectText => "敵全体に{Damage:diff()}ダメージと[gold]脱力[/gold]{WeakPower:diff()}を与える。";
    protected override async Task Effect(PlayerChoiceContext c, CardPlay p) { await HitAll(c, p, DynamicVars.Damage.BaseValue); await ApplyAll<WeakPower>(c, DynamicVars.Weak.BaseValue); }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}
// v0.23 (ユーザーレビュー): poison became drain, at the same numbers (drain halves, so it deals less in total).
public sealed class Drizzle() : DualElementCard(1, CardType.Skill, TargetType.AnyEnemy, AlchemyPhase.Water, AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<LifeDrainPower>(4), new CardsVar(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<LifeDrainPower>()];
    protected override string CardTitle => "ドレインミスト";
    protected override string EffectText => "[gold]ドレイン[/gold]{LifeDrainPower:diff()}を与える。カードを{Cards:diff()}枚引く。";
    protected override async Task Effect(PlayerChoiceContext c, CardPlay p) { await ApplyTo<LifeDrainPower>(c, p.Target!, DynamicVars["LifeDrainPower"].BaseValue); await Draw(c, DynamicVars.Cards.BaseValue); }
    protected override void OnUpgrade() => DynamicVars["LifeDrainPower"].UpgradeValueBy(3);
}
public sealed class FireWhirl() : DualElementCard(1, CardType.Attack, TargetType.AllEnemies, AlchemyPhase.Fire, AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4, ValueProp.Move), new DynamicVar("Hits", 2)];
    protected override string CardTitle => "火炎旋風";
    protected override string EffectText => "敵全体に{Damage:diff()}ダメージを{Hits:diff()}回与える。";
    protected override Task Effect(PlayerChoiceContext c, CardPlay p) => HitAll(c, p, DynamicVars.Damage.BaseValue, DynamicVars["Hits"].IntValue);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}

// v0.21: fixed recipes of three to five materials (design-axes 7.1). Numbers are prototypes.

/// <summary>Three iron. A general card that helps even at the first workshop of Act 1.</summary>
public sealed class CraftIronBastion() : CraftedCard(1, CardType.Skill, TargetType.Self)
{
    public override AlchemyPhase Element => AlchemyPhase.Earth;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(12, ValueProp.Move), new PowerVar<BlockNextTurnPower>(6)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<BlockNextTurnPower>()];
    protected override string CardTitle => "鋼の砦";
    protected override string CardText => "{Block:diff()}[gold]ブロック[/gold]を得る。次のターン開始時、{BlockNextTurnPower:diff()}[gold]ブロック[/gold]を得る。\n[gold]地相[/gold]";
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p) { await CardBlock(p); await ApplySelf<BlockNextTurnPower>(c, DynamicVars["BlockNextTurnPower"].BaseValue); }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(4); DynamicVars["BlockNextTurnPower"].UpgradeValueBy(2); }
}
/// <summary>Three powder. A general area attack.</summary>
public sealed class CraftPowderFlask() : CraftedCard(1, CardType.Attack, TargetType.AllEnemies)
{
    public override AlchemyPhase Element => AlchemyPhase.Fire;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12, ValueProp.Move)];
    protected override string CardTitle => "爆裂フラスコ";
    protected override string CardText => "敵全体に{Damage:diff()}ダメージを与える。\n[gold]火相[/gold]";
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => HitAll(c, p, DynamicVars.Damage.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4);
}
/// <summary>Three ether. Free draw and energy, exhausted so it cannot feed a loop.</summary>
public sealed class CraftEtherCatalyst() : CraftedCard(0, CardType.Skill, TargetType.Self)
{
    public override AlchemyPhase Element => AlchemyPhase.Air;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2), new DynamicVar("Energy", 1)];
    protected override string CardTitle => "精霊の触媒";
    protected override string CardText => "カードを{Cards:diff()}枚引き、エナジーを{Energy:diff()}得る。\n[gold]風相[/gold]";
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p) { await Draw(c, DynamicVars.Cards.BaseValue); await Energy(DynamicVars["Energy"].BaseValue); }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}
/// <summary>Two herb and an iron. The homunculus answers each attack it soaks with drain.</summary>
public sealed class CraftPhilosophersBlood() : CraftedCard(1, CardType.Power, TargetType.Self)
{
    public override AlchemyPhase Element => AlchemyPhase.Water;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PhilosophersBloodPower>(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<HomunculusPower>(), HoverTipFactory.FromPower<LifeDrainPower>()];
    protected override string CardTitle => "賢者の血";
    protected override string CardText => "ホムンクルスが攻撃を肩代わりするたび、攻撃した敵に[gold]ドレイン[/gold]{PhilosophersBloodPower:diff()}を与える。\n[gold]水相[/gold]";
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => ApplySelf<PhilosophersBloodPower>(c, DynamicVars["PhilosophersBloodPower"].BaseValue);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
/// <summary>Two herb and an ether. Fills the homunculus every turn.</summary>
public sealed class CraftCultureVat() : CraftedCard(2, CardType.Power, TargetType.Self)
{
    public override AlchemyPhase Element => AlchemyPhase.Water;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<CultureVatPower>(3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<HomunculusPower>()];
    protected override string CardTitle => "培養槽";
    protected override string CardText => "自分のターン開始時、[gold]ホムンクルスHP[/gold]を{CultureVatPower:diff()}得る。\n[gold]水相[/gold]";
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => ApplySelf<CultureVatPower>(c, DynamicVars["CultureVatPower"].BaseValue);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
/// <summary>Two iron and a herb. Reads the homunculus without spending it; exhausted since v0.23 (ユーザーレビュー「強い」).</summary>
public sealed class CraftFleshArmor() : CraftedCard(1, CardType.Skill, TargetType.Self)
{
    public override AlchemyPhase Element => AlchemyPhase.Earth;
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Percent", 50)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<HomunculusPower>()];
    protected override string CardTitle => "血肉の鎧";
    protected override string CardText => "[gold]ホムンクルスHP[/gold]の{Percent:diff()}%の[gold]ブロック[/gold]を得る。\n[gold]地相[/gold]";
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        int amount = (LifeAxis.State(Owner)?.HomunculusHp ?? 0) * DynamicVars["Percent"].IntValue / 100;
        return amount > 0 ? Block(p, amount) : Task.CompletedTask;
    }
    protected override void OnUpgrade() => DynamicVars["Percent"].UpgradeValueBy(50);
}
/// <summary>Three powder and a herb (two powder before v0.23). A fixed-cost exit: does nothing without the homunculus HP to spend.</summary>
public sealed class CraftFusion() : CraftedCard(2, CardType.Attack, TargetType.AnyEnemy)
{
    public override AlchemyPhase Element => AlchemyPhase.Fire;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(30, ValueProp.Move), new DynamicVar("Spend", 10)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<HomunculusPower>()];
    protected override string CardTitle => "器の融合";
    protected override string CardText => "[gold]ホムンクルスHP[/gold]を{Spend}消費できれば、{Damage:diff()}ダメージを与える。\n[gold]火相[/gold]";
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        if (await LifeAxis.TrySpendHomunculus(c, Owner, DynamicVars["Spend"].IntValue)) await Hit(c, p, DynamicVars.Damage.BaseValue);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(10);
}
/// <summary>One of each material. Doubles the homunculus once, then exhausts. v0.23 (ユーザーレビュー「強すぎ」): 2 cost
/// either way, and the upgrade adds retain so it can wait for a full homunculus.</summary>
public sealed class CraftMitosis() : CraftedCard(2, CardType.Skill, TargetType.Self)
{
    public override AlchemyPhase Element => AlchemyPhase.Air;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<HomunculusPower>()];
    protected override string CardTitle => "分裂";
    protected override string CardText => "[gold]ホムンクルスHP[/gold]を2倍にする。\n[gold]風相[/gold]";
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p)
        => LifeAxis.GainHomunculus(c, Owner, LifeAxis.State(Owner)?.HomunculusHp ?? 0);
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
/// <summary>
/// Five materials. The life axis's reading finisher (design-axes 3.2 参照型): once the homunculus is grown it hits
/// every enemy each turn without spending it. v0.23 (ユーザーレビュー): it was a spend-all hit, which is 人体錬成's
/// role without the death, so it became this power. The class keeps its name so saved cards keep their id.
/// </summary>
public sealed class CraftGateOfTruth() : CraftedCard(3, CardType.Power, TargetType.Self)
{
    public override AlchemyPhase Element => AlchemyPhase.Fire;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<GateOfTruthPower>(8), new DynamicVar("Threshold", GateOfTruthPower.Threshold)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<GateOfTruthPower>(), HoverTipFactory.FromPower<HomunculusPower>()];
    protected override string CardTitle => "真理の扉";
    protected override string CardText => "自分のターン開始時、[gold]ホムンクルスHP[/gold]が{Threshold}以上なら、敵全体に{GateOfTruthPower:diff()}ダメージを与える。\n[gold]火相[/gold]";
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => ApplySelf<GateOfTruthPower>(c, DynamicVars["GateOfTruthPower"].BaseValue);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
/// <summary>
/// Five materials. Enters earth, then air, then MaterialBox enters fire: up to three transitions from one
/// play, in a fixed order with no element repeated back to back (design-axes 7.1). v0.23.1 (ユーザー判断): named
/// EW&amp;F after Earth, Wind &amp; Fire, and the order changed to match (was earth, fire, air).
/// </summary>
public sealed class CraftThreePhaseTorrent() : CraftedCard(1, CardType.Attack, TargetType.AnyEnemy)
{
    public override AlchemyPhase Element => AlchemyPhase.Fire;
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8, ValueProp.Move), new BlockVar(5, ValueProp.Move)];
    protected override string CardTitle => "EW&F";
    // v0.23 (ユーザーレビュー): no "up to three transitions" line; the phase line already says it.
    protected override string CardText => "{Damage:diff()}ダメージ。{Block:diff()}[gold]ブロック[/gold]を得る。\n[gold]地相→風相→火相[/gold]";
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        await Hit(c, p, DynamicVars.Damage.BaseValue);
        await CardBlock(p);
        if (!p.IsFirstInSeries) return;
        await PhaseTransitions.Enter(c, Owner, AlchemyPhase.Earth, this, p.Target, p);
        await PhaseTransitions.Enter(c, Owner, AlchemyPhase.Air, this, p.Target, p);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(3); DynamicVars.Block.UpgradeValueBy(2); }
}

/// <summary>Recipe ids to workshop cards. Ids are saved in receipts, so they never change meaning.</summary>
public static class CraftedCards
{
    public static CardModel ForRecipe(string id) => id switch
    {
        "craft.earth_core.v1" => ModelDb.Card<EarthCore>(),
        "craft.water_core.v1" => ModelDb.Card<WaterCore>(),
        "craft.fire_core.v1" => ModelDb.Card<FireCore>(),
        "craft.air_core.v1" => ModelDb.Card<AirCore>(),
        "craft.mud_rampart.v1" => ModelDb.Card<MudRampart>(),
        "craft.lava_shot.v1" => ModelDb.Card<LavaShot>(),
        "craft.sandstorm.v1" => ModelDb.Card<Sandstorm>(),
        "craft.steam_burst.v1" => ModelDb.Card<SteamBurst>(),
        "craft.drizzle.v1" => ModelDb.Card<Drizzle>(),
        "craft.fire_whirl.v1" => ModelDb.Card<FireWhirl>(),
        "craft.iron_bastion.v1" => ModelDb.Card<CraftIronBastion>(),
        "craft.powder_flask.v1" => ModelDb.Card<CraftPowderFlask>(),
        "craft.ether_catalyst.v1" => ModelDb.Card<CraftEtherCatalyst>(),
        "craft.philosophers_blood.v1" => ModelDb.Card<CraftPhilosophersBlood>(),
        "craft.culture_vat.v1" => ModelDb.Card<CraftCultureVat>(),
        "craft.flesh_armor.v1" => ModelDb.Card<CraftFleshArmor>(),
        "craft.fusion.v1" => ModelDb.Card<CraftFusion>(),
        "craft.mitosis.v1" => ModelDb.Card<CraftMitosis>(),
        "craft.gate_of_truth.v1" => ModelDb.Card<CraftGateOfTruth>(),
        "craft.three_phase_torrent.v1" => ModelDb.Card<CraftThreePhaseTorrent>(),
        _ => throw new InvalidOperationException($"未対応レシピ: {id}")
    };
}

/// <summary>Shows the inscribed variant of a crafted card's description.</summary>
[HarmonyPatch(typeof(CardModel),nameof(CardModel.Description),MethodType.Getter)]
public static class CraftedDescriptionPatch
{
    public static bool Prefix(CardModel __instance, ref LocString __result)
    {
        if (__instance is not CraftedCard { AlchemistRareModifier.Length: > 0 } crafted) return true;
        __result = new LocString("cards", $"{crafted.Id.Entry}.{crafted.AlchemistRareModifier}.description");
        return false;
    }
}
