using Alchemist.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist;

// Phase-less reward cards: they spend run materials or manipulate the phase itself instead of entering one.
// InstantAlchemy and PhaseResonance live in ElementalCards.cs.

/// <summary>Cards that spend a material of the player's choice are unplayable without one, so they never fizzle.
/// Only 素材投入 is left here: the chosen material is its whole point (an approved exception to design-axes 6.2).</summary>
public abstract class MaterialSpendingCard(int cost, CardType type, CardRarity rarity, TargetType target)
    : AlchemyCard(cost, type, rarity, target)
{
    protected override bool IsPlayable => HasNormalMaterial;
}

public sealed class AlchInfusion() : MaterialSpendingCard(0,CardType.Skill,CardRarity.Common,TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DynamicVar("Energy",1)];
    public override List<(string,string)> Localization=>new CardLoc("素材投入","通常素材を1個選んで消費する。エナジーを{Energy:diff()}得て、その素材の相へ移る。",("selectionScreenPrompt","消費する素材を選択"));
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p)
    {
        if(await SpendMaterial(c) is not { } m) return;
        await Energy(DynamicVars["Energy"].BaseValue);
        await PhaseTransitions.Enter(c,Owner,PhaseRules.PhaseFor(m),this,null,p);
    }
    protected override void OnUpgrade()=>DynamicVars["Energy"].UpgradeValueBy(1);
}
public sealed class AlchEchoStrike() : AlchemyCard(1,CardType.Attack,CardRarity.Common,TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(6,ValueProp.Move)];
    public override List<(string,string)> Localization=>new CardLoc("反響打ち","{Damage:diff()}ダメージ。現在相の相転移効果をもう一度発動する（相は変わらない）。");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await Hit(c,p,DynamicVars.Damage.BaseValue);await PhaseTransitions.Echo(c,Owner,this,p.Target,p);}
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(3);
}
public sealed class AlchPreparation() : AlchemyCard(1,CardType.Skill,CardRarity.Common,TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new BlockVar(5,ValueProp.Move),new PowerVar<PreparationPower>(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<PreparationPower>()];
    public override List<(string,string)> Localization=>new CardLoc("調合準備","{Block:diff()}[gold]ブロック[/gold]を得る。次の相転移の効果を{PreparationPower:diff()}強化する。");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await CardBlock(p);await ApplySelf<PreparationPower>(c,DynamicVars["PreparationPower"].BaseValue);}
    protected override void OnUpgrade()=>DynamicVars["PreparationPower"].UpgradeValueBy(2);
}
public sealed class AlchSynergy() : AlchemyCard(1,CardType.Skill,CardRarity.Uncommon,TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new CardsVar(1),new PowerVar<SynergyPower>(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<SynergyPower>()];
    public override List<(string,string)> Localization=>new CardLoc("相乗","カードを{Cards:diff()}枚引く。次の相転移の効果が、追加で{SynergyPower:diff()}回発動する。");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await Draw(c,DynamicVars.Cards.BaseValue);await ApplySelf<SynergyPower>(c,DynamicVars["SynergyPower"].BaseValue);}
    // v0.19: the upgrade used to make it cost 0, a card that replaced itself for free (原則4).
    protected override void OnUpgrade()=>DynamicVars.Cards.UpgradeValueBy(1);
}
// v0.20: 素材を指定する消費カード（design-axes 6.2）. Without the material it is weak (or unplayable when it
// would otherwise be free energy); with it, clearly better than a plain card of the same cost.
public sealed class AlchMaterialBomb() : AlchemyCard(1,CardType.Attack,CardRarity.Uncommon,TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(5,ValueProp.Move),new DynamicVar("Charged",16)];
    public override List<(string,string)> Localization=>new CardLoc("素材爆弾","敵全体に{Damage:diff()}ダメージ。[gold]火薬[/gold]を1個消費できれば、代わりに敵全体に{Charged:diff()}ダメージ。");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)
        =>HitAll(c,p,TrySpend(Material.Powder)?DynamicVars["Charged"].BaseValue:DynamicVars.Damage.BaseValue);
    protected override void OnUpgrade(){DynamicVars.Damage.UpgradeValueBy(2);DynamicVars["Charged"].UpgradeValueBy(5);}
}
public sealed class AlchCatalysis() : AlchemyCard(0,CardType.Skill,CardRarity.Uncommon,TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new CardsVar(2),new DynamicVar("Energy",1)];
    public override List<(string,string)> Localization=>new CardLoc("触媒反応","[gold]エーテル[/gold]を1個消費し、カードを{Cards:diff()}枚引いてエナジーを{Energy:diff()}得る。エーテルがなければ使えない。");
    // Unplayable without ether: a free draw-and-energy card would be loop fuel (原則4).
    protected override bool IsPlayable=>HasMaterial(Material.Ether);
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p)
    {
        if(!TrySpend(Material.Ether)) return;
        await Draw(c,DynamicVars.Cards.BaseValue);
        await Energy(DynamicVars["Energy"].BaseValue);
    }
    protected override void OnUpgrade()=>DynamicVars.Cards.UpgradeValueBy(1);
}
public sealed class AlchReversal() : AlchemyCard(1,CardType.Skill,CardRarity.Uncommon,TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new BlockVar(6,ValueProp.Move)];
    public override List<(string,string)> Localization=>new CardLoc("逆相","{Block:diff()}[gold]ブロック[/gold]を得る。ひとつ前の相へ戻る。");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await CardBlock(p);await PhaseTransitions.ReturnToPrevious(c,Owner,this,null,p);}
    protected override void OnUpgrade()=>DynamicVars.Block.UpgradeValueBy(3);
}
public sealed class AlchPhilosophersStone() : AlchemyCard(3,CardType.Power,CardRarity.Rare,TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<PhilosophersStonePower>(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<PhilosophersStonePower>()];
    public override List<(string,string)> Localization=>new CardLoc("賢者の石","相転移の効果を{PhilosophersStonePower:diff()}強化する。");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<PhilosophersStonePower>(c,DynamicVars["PhilosophersStonePower"].BaseValue);
    protected override void OnUpgrade()=>EnergyCost.UpgradeBy(-1);
}
public sealed class AlchFlux() : AlchemyCard(1,CardType.Skill,CardRarity.Rare,TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords=>[CardKeyword.Retain];
    public override List<(string,string)> Localization=>new CardLoc("万象流転","好きな相へ移る（素材は消費しない。鉄＝地、薬草＝水、火薬＝火、エーテル＝風）。",("selectionScreenPrompt","移る相を素材で選択"));
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p)
    {
        if(await ChooseMaterial(c,ownedOnly:false) is { } m) await PhaseTransitions.Enter(c,Owner,PhaseRules.PhaseFor(m),this,null,p);
    }
    protected override void OnUpgrade()=>EnergyCost.UpgradeBy(-1);
}
public sealed class AlchChainReaction() : AlchemyCard(1,CardType.Attack,CardRarity.Rare,TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DynamicVar("Bonus",10)];
    public override List<(string,string)> Localization=>new CardLoc("連鎖反応","このターンに起きた相転移1回につき{Bonus:diff()}ダメージを与える。");
    // Phase-less, so playing it never adds a transition of its own before counting.
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>TransitionsThisTurn>0?Hit(c,p,TransitionsThisTurn*DynamicVars["Bonus"].BaseValue):Task.CompletedTask;
    protected override void OnUpgrade()=>DynamicVars["Bonus"].UpgradeValueBy(3);
}
