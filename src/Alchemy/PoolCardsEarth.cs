using Alchemy.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemy;

// Earth reward cards: block and fortification. The starter 土壁 (EarthenGuard) lives in ElementalCards.cs.
// Numbers are prototype values.

public sealed class EarthTremor() : ElementCard(1,CardType.Attack,CardRarity.Uncommon,TargetType.AllEnemies,AlchemyPhase.Earth)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(5,ValueProp.Move),new BlockVar(4,ValueProp.Move)];
    public override bool GainsBlock=>true;
    public override List<(string,string)> Localization=>new CardLoc("地鳴り","敵全体に{Damage:diff()}ダメージ。{Block:diff()}[gold]ブロック[/gold]を得る。\n[gold]地相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await HitAll(c,p,DynamicVars.Damage.BaseValue);await CardBlock(p);}
    protected override void OnUpgrade(){DynamicVars.Damage.UpgradeValueBy(2);DynamicVars.Block.UpgradeValueBy(2);}
}
public sealed class EarthBulwarkBash() : ElementCard(1,CardType.Attack,CardRarity.Uncommon,TargetType.AnyEnemy,AlchemyPhase.Earth)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PreviewDamageVar((c,_)=>c.Owner.Creature.Block)];
    public override List<(string,string)> Localization=>new CardLoc("城塞打ち","現在の[gold]ブロック[/gold]と同じダメージを与える。{InCombat:\n（{Total:diff()}ダメージ）|}\n[gold]地相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>Hit(c,p,Owner.Creature.Block);
    protected override void OnUpgrade()=>EnergyCost.UpgradeBy(-1);
}
/// <summary>v0.23 (ユーザーレビュー): 土壁 left the reward pool for the starter; this is the heavier common block.</summary>
public sealed class EarthBedrock() : ElementCard(2,CardType.Skill,CardRarity.Common,TargetType.Self,AlchemyPhase.Earth)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new BlockVar(12,ValueProp.Move)];
    public override List<(string,string)> Localization=>new CardLoc("岩盤","{Block:diff()}[gold]ブロック[/gold]を得る。\n[gold]地相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>CardBlock(p);
    protected override void OnUpgrade()=>DynamicVars.Block.UpgradeValueBy(3);
}
// v0.23 (ユーザーレビュー): flat plating was the Ironclad's card. Plating now grows with this turn's transitions (原則7).
public sealed class EarthBlessing() : ElementCard(1,CardType.Skill,CardRarity.Uncommon,TargetType.Self,AlchemyPhase.Earth)
{
    // v0.24 (ユーザー判断): plating lasts, so repeating it every shuffle stacked too much; it exhausts now.
    public override IEnumerable<CardKeyword> CanonicalKeywords=>[CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PreviewCountVar("Total",c=>c.TransitionsThisTurn*c.DynamicVars["Bonus"].BaseValue),new DynamicVar("Bonus",1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<PlatingPower>()];
    public override List<(string,string)> Localization=>new CardLoc("大地の加護","このターンに起きた[gold]相転移[/gold]1回につき[gold]プレート[/gold]{Bonus:diff()}を得る。{InCombat:\n（[gold]プレート[/gold]{Total:diff()}）|}\n[gold]地相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)
        =>TransitionsThisTurn>0?ApplySelf<PlatingPower>(c,TransitionsThisTurn*DynamicVars["Bonus"].BaseValue):Task.CompletedTask;
    protected override void OnUpgrade()=>DynamicVars["Bonus"].UpgradeValueBy(1);
}
public sealed class EarthFortify() : ElementCard(1,CardType.Skill,CardRarity.Uncommon,TargetType.Self,AlchemyPhase.Earth)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new BlockVar(4,ValueProp.Move),new PowerVar<BlockNextTurnPower>(8)];
    public override List<(string,string)> Localization=>new CardLoc("補強","{Block:diff()}[gold]ブロック[/gold]を得る。次のターン開始時、{BlockNextTurnPower:diff()}[gold]ブロック[/gold]を得る。\n[gold]地相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await CardBlock(p);await ApplySelf<BlockNextTurnPower>(c,DynamicVars["BlockNextTurnPower"].BaseValue);}
    protected override void OnUpgrade()=>DynamicVars["BlockNextTurnPower"].UpgradeValueBy(3);
}
// v0.26 (2026-10-13, user: パッとしない): replaces 棘の外殻 (the base game's thorns). Earth transitions also hit, so
// the defensive element has an attack of its own. Earth only and small, as a per-transition payoff (原則7).
public sealed class EarthUpheaval() : ElementCard(1,CardType.Power,CardRarity.Uncommon,TargetType.Self,AlchemyPhase.Earth)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<UpheavalPower>(3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<UpheavalPower>()];
    public override List<(string,string)> Localization=>new CardLoc("地殻変動","[gold]地相[/gold]へ転移するたび、敵全体に{UpheavalPower:diff()}ダメージを与える。\n[gold]地相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<UpheavalPower>(c,DynamicVars["UpheavalPower"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["UpheavalPower"].UpgradeValueBy(1);
}
public sealed class UpheavalPower : TransitionListenerPower
{
    protected override PowerModel IconSource => ModelDb.Power<ThornsPower>();
    public override List<(string,string)> Localization => new PowerLoc("地殻変動","地相へ転移するたび、敵全体にダメージを与える。","[gold]地相[/gold]へ転移するたび、敵全体に{Amount}ダメージを与える。");
    protected override bool Matches(PhaseTransitionContext t) => t.To == AlchemyPhase.Earth;
    protected override Task OnTransition(PhaseTransitionContext t) => DamageAllEnemies(t.Choice, Amount);
}
// v0.26 (2026-10-13, user: パッとしない): replaces 大地の記憶 (block per transition this combat, 原則7). Plays the earth
// transition effect once for each transition this turn, so it pays for a big turn and grows with 大地の心核 and 賢者の石.
// An echo is not a transition: it neither counts nor notifies listeners, so it cannot chain.
public sealed class EarthResonance() : ElementCard(1,CardType.Skill,CardRarity.Rare,TargetType.Self,AlchemyPhase.Earth)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PreviewCountVar("Total",c=>c.TransitionsThisTurn+(c.IsUpgraded?1:0))];
    public override bool GainsBlock=>true;
    public override List<(string,string)> Localization=>new CardLoc("地脈の共鳴","このターンに起きた[gold]相転移[/gold]1回につき1回、[gold]地相[/gold]への相転移の効果を発動する（相は変わらない）。{IfUpgraded:show:さらに1回発動する。|}{InCombat:\n（{Total:diff()}回）|}\n[gold]地相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p)
    {
        int times=TransitionsThisTurn+(IsUpgraded?1:0);
        for(int i=0;i<times;i++) await PhaseTransitions.Echo(c,Owner,this,null,p,AlchemyPhase.Earth);
    }
    protected override void OnUpgrade(){}
}
// v0.23 (ユーザーレビュー「バリケードと一緒」): like StS1's Calipers, block is kept but loses a fixed amount.
// The upgrade lowers the loss, so the power keeps its amount as "block lost".
public sealed class EarthKing() : ElementCard(2,CardType.Power,CardRarity.Rare,TargetType.Self,AlchemyPhase.Earth)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<EarthKingPower>(20)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<EarthKingPower>()];
    public override List<(string,string)> Localization=>new CardLoc("大地の王","ターン開始時、[gold]ブロック[/gold]がすべて失われる代わりに、{EarthKingPower:diff()}だけ失われる。\n[gold]地相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<EarthKingPower>(c,DynamicVars["EarthKingPower"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["EarthKingPower"].UpgradeValueBy(-5);
}
/// <summary>鉄の素材消費カード（仮名：鉄壁錬成、design-axes 6.2）. Weak without iron, well above 土壁 with it.</summary>
public sealed class EarthIronBulwark() : ElementCard(1,CardType.Skill,CardRarity.Common,TargetType.Self,AlchemyPhase.Earth)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new BlockVar(5,ValueProp.Move),new DynamicVar("Charged",13)];
    public override List<(string,string)> Localization=>new CardLoc("鉄壁錬成","{Block:diff()}[gold]ブロック[/gold]を得る。[gold]鉄[/gold]を1個消費できれば、代わりに{Charged:diff()}[gold]ブロック[/gold]を得る。\n[gold]地相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)
        =>TrySpend(Material.Iron)?Block(p,DynamicVars["Charged"].BaseValue):CardBlock(p);
    protected override void OnUpgrade(){DynamicVars.Block.UpgradeValueBy(3);DynamicVars["Charged"].UpgradeValueBy(4);}
}
