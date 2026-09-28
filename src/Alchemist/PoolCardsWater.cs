using Alchemist.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist;

// Water reward cards: weak, drain and calming. The starter 鎮静の霧 (SoothingMist) lives in ElementalCards.cs.

public sealed class WaterTorrent() : ElementCard(1,CardType.Attack,CardRarity.Uncommon,TargetType.AllEnemies,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(4,ValueProp.Move),new PowerVar<WeakPower>(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<WeakPower>()];
    public override List<(string,string)> Localization=>new CardLoc("激流","敵全体に{Damage:diff()}ダメージと[gold]脱力[/gold]{WeakPower:diff()}を与える。\n[gold]水相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await HitAll(c,p,DynamicVars.Damage.BaseValue);await ApplyAll<WeakPower>(c,DynamicVars.Weak.BaseValue);}
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(2);
}
public sealed class WaterErosion() : ElementCard(1,CardType.Attack,CardRarity.Uncommon,TargetType.AnyEnemy,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PreviewDamageVar((c,t)=>c.DynamicVars.Damage.BaseValue+(t?.GetPower<WeakPower>() is not null?c.DynamicVars["Bonus"].BaseValue:0)),new DamageVar(8,ValueProp.Move),new DynamicVar("Bonus",6)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<WeakPower>()];
    public override List<(string,string)> Localization=>new CardLoc("浸食","{Damage:diff()}ダメージ。対象が[gold]脱力[/gold]状態なら、さらに{Bonus:diff()}ダメージ。{InCombat:\n（{Total:diff()}ダメージ）|}\n[gold]水相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)
        =>Hit(c,p,DynamicVars.Damage.BaseValue+(p.Target?.GetPower<WeakPower>() is not null?DynamicVars["Bonus"].BaseValue:0));
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(3);
}
public sealed class WaterRequiem() : ElementCard(1,CardType.Skill,CardRarity.Rare,TargetType.AnyEnemy,AlchemyPhase.Water)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords=>[CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DynamicVar("Bonus",3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<StrengthPower>()];
    public override List<(string,string)> Localization=>new CardLoc("鎮魂","対象の[gold]筋力[/gold]を{Bonus:diff()}下げる。\n[gold]水相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplyTo<StrengthPower>(c,p.Target!,-DynamicVars["Bonus"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["Bonus"].UpgradeValueBy(1);
}
// v0.22.1 (ユーザー判断): a bridge card between the phase and life axes. Poison became drain, a little lower than
// the old poison (3 + 2 per transition) because drain also feeds the homunculus (design-axes 3.1).
public sealed class WaterClearStream() : ElementCard(1,CardType.Skill,CardRarity.Rare,TargetType.AnyEnemy,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<LifeDrainPower>(2),new DynamicVar("Bonus",1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<LifeDrainPower>()];
    public override List<(string,string)> Localization=>new CardLoc("ライフストリーム","[gold]ドレイン[/gold]{LifeDrainPower:diff()}を与える。さらに、この戦闘で起きた[gold]相転移[/gold]1回につき[gold]ドレイン[/gold]{Bonus:diff()}を与える。\n[gold]水相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)
        =>ApplyTo<LifeDrainPower>(c,p.Target!,DynamicVars["LifeDrainPower"].BaseValue+TransitionCount*DynamicVars["Bonus"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["Bonus"].UpgradeValueBy(1);
}
/// <summary>薬草の素材消費カード（仮名：薬毒の滴、design-axes 6.2）. Drain 4 is 10 HP over four turns, the same
/// homunculus gain as 生命の供物 without the HP cost, paid for with a herb.</summary>
public sealed class WaterHerbDrip() : ElementCard(1,CardType.Skill,CardRarity.Common,TargetType.AnyEnemy,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DynamicVar("Drain",1),new DynamicVar("Charged",4)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<LifeDrainPower>()];
    public override List<(string,string)> Localization=>new CardLoc("薬草のしずく","[gold]ドレイン[/gold]{Drain:diff()}を与える。[gold]薬草[/gold]を1個消費できれば、代わりに[gold]ドレイン[/gold]{Charged:diff()}を与える。\n[gold]水相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)
        =>ApplyTo<LifeDrainPower>(c,p.Target!,TrySpend(Material.Herb)?DynamicVars["Charged"].BaseValue:DynamicVars["Drain"].BaseValue);
    protected override void OnUpgrade(){DynamicVars["Drain"].UpgradeValueBy(1);DynamicVars["Charged"].UpgradeValueBy(2);}
}
