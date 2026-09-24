using Alchemist.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist;

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
    private void ApplyInscription()
    {
        if (Has(RareMaterial.Stardust) && BaseReplayCount < 1) BaseReplayCount = 1;
        if (Has(RareMaterial.VoidCrystal)) { EnergyCost.SetCustomBaseCost(Math.Max(0, baseCost - 1)); AddKeyword(CardKeyword.Exhaust); }
        if (Has(RareMaterial.Mercury)) AddKeyword(CardKeyword.Retain);
    }
    protected override void AfterDowngraded() => ApplyInscription();

    protected abstract string CardTitle { get; }
    protected abstract string CardText { get; }
    /// One description per inscription, chosen by CraftedDescriptionPatch.
    public override List<(string, string)> Localization => [
        ("title", CardTitle), ("description", CardText),
        ..RareMaterials.All.Select(r => ($"{r.Id}.description", $"{CardText}\n[gold]{r.EffectName}[/gold]：{r.Description}"))];
}

/// <summary>Same material twice: a power that strengthens transitions into its own element.</summary>
public abstract class CorePowerCard<TPower>(AlchemyPhase element, int amount, string title)
    : CraftedCard(1, CardType.Power, TargetType.Self) where TPower : PowerModel
{
    public override AlchemyPhase Element => element;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<TPower>(amount)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<TPower>()];
    protected override string CardTitle => title;
    protected override string CardText => $"[gold]{PhaseRules.Name(element)}相[/gold]への相転移の効果を{{{typeof(TPower).Name}:diff()}}強化する。\n[gold]{PhaseRules.Name(element)}相[/gold]";
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => ApplySelf<TPower>(c, DynamicVars[typeof(TPower).Name].BaseValue);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
public sealed class EarthCore() : CorePowerCard<EarthCorePower>(AlchemyPhase.Earth, 3, "大地の心核");
public sealed class WaterCore() : CorePowerCard<WaterCorePower>(AlchemyPhase.Water, 1, "流水の心核");
public sealed class FireCore() : CorePowerCard<FireCorePower>(AlchemyPhase.Fire, 3, "劫火の心核");
public sealed class AirCore() : CorePowerCard<AirCorePower>(AlchemyPhase.Air, 1, "疾風の心核");

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
public sealed class Drizzle() : DualElementCard(1, CardType.Skill, TargetType.AnyEnemy, AlchemyPhase.Water, AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PoisonPower>(4), new CardsVar(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<PoisonPower>()];
    protected override string CardTitle => "毒霧雨";
    protected override string EffectText => "[gold]毒[/gold]{PoisonPower:diff()}を与える。カードを{Cards:diff()}枚引く。";
    protected override async Task Effect(PlayerChoiceContext c, CardPlay p) { await ApplyTo<PoisonPower>(c, p.Target!, DynamicVars.Poison.BaseValue); await Draw(c, DynamicVars.Cards.BaseValue); }
    protected override void OnUpgrade() => DynamicVars.Poison.UpgradeValueBy(3);
}
public sealed class FireWhirl() : DualElementCard(1, CardType.Attack, TargetType.AllEnemies, AlchemyPhase.Fire, AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4, ValueProp.Move), new DynamicVar("Hits", 2)];
    protected override string CardTitle => "火炎旋風";
    protected override string EffectText => "敵全体に{Damage:diff()}ダメージを{Hits:diff()}回与える。";
    protected override Task Effect(PlayerChoiceContext c, CardPlay p) => HitAll(c, p, DynamicVars.Damage.BaseValue, DynamicVars["Hits"].IntValue);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
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
        _ => throw new InvalidOperationException($"未対応レシピ: {id}")
    };
}
