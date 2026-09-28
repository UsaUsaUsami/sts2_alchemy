using Alchemy.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemy;

// Air reward cards: draw, energy and hand shaping.

public sealed class AirSickle() : ElementCard(1,CardType.Attack,CardRarity.Common,TargetType.AnyEnemy,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(4,ValueProp.Move),new DynamicVar("Hits",2)];
    public override List<(string,string)> Localization=>new CardLoc("かまいたち","{Damage:diff()}ダメージを{Hits:diff()}回与える。\n[gold]風相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>Hit(c,p,DynamicVars.Damage.BaseValue,DynamicVars["Hits"].IntValue);
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(2);
}
public sealed class AirGale() : ElementCard(1,CardType.Attack,CardRarity.Uncommon,TargetType.AllEnemies,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(5,ValueProp.Move),new CardsVar(1)];
    public override List<(string,string)> Localization=>new CardLoc("突風","敵全体に{Damage:diff()}ダメージ。カードを{Cards:diff()}枚引く。\n[gold]風相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await HitAll(c,p,DynamicVars.Damage.BaseValue);await Draw(c,DynamicVars.Cards.BaseValue);}
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(2);
}
// v0.19: drew a card when it transitioned; 原則5 forbids transitions adding draw, so it now blocks instead.
public sealed class AirWindBlade() : ElementCard(0,CardType.Attack,CardRarity.Uncommon,TargetType.AnyEnemy,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(4,ValueProp.Move),new BlockVar(3,ValueProp.Move)];
    public override bool GainsBlock=>true;
    public override List<(string,string)> Localization=>new CardLoc("風の刃","{Damage:diff()}ダメージ。このカードで[gold]相転移[/gold]が起きるなら、{Block:diff()}[gold]ブロック[/gold]を得る。\n[gold]風相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){bool shift=WillTransition;await Hit(c,p,DynamicVars.Damage.BaseValue);if(shift)await CardBlock(p);}
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(3);
}
// v0.19: the energy used to come back in the same turn, which with a 0-cost card made an infinite loop (原則4).
public sealed class AirMomentum() : ElementCard(1,CardType.Skill,CardRarity.Uncommon,TargetType.Self,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new CardsVar(2),new PowerVar<EnergyNextTurnPower>(1)];
    public override List<(string,string)> Localization=>new CardLoc("追い風","カードを{Cards:diff()}枚引く。このカードで[gold]相転移[/gold]が起きるなら、次のターン、エナジーを{EnergyNextTurnPower:diff()}得る。\n[gold]風相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){bool shift=WillTransition;await Draw(c,DynamicVars.Cards.BaseValue);if(shift)await ApplySelf<EnergyNextTurnPower>(c,DynamicVars["EnergyNextTurnPower"].BaseValue);}
    protected override void OnUpgrade()=>DynamicVars.Cards.UpgradeValueBy(1);
}
public sealed class AirCurrent() : ElementCard(0,CardType.Skill,CardRarity.Uncommon,TargetType.Self,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<EnergyNextTurnPower>(1),new PowerVar<DrawCardsNextTurnPower>(1)];
    public override List<(string,string)> Localization=>new CardLoc("気流","次のターン、エナジーを{EnergyNextTurnPower:diff()}得て、カードを{DrawCardsNextTurnPower:diff()}枚追加で引く。\n[gold]風相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await ApplySelf<EnergyNextTurnPower>(c,DynamicVars["EnergyNextTurnPower"].BaseValue);await ApplySelf<DrawCardsNextTurnPower>(c,DynamicVars["DrawCardsNextTurnPower"].BaseValue);}
    protected override void OnUpgrade()=>DynamicVars["DrawCardsNextTurnPower"].UpgradeValueBy(1);
}
public sealed class AirRefine() : ElementCard(1,CardType.Skill,CardRarity.Uncommon,TargetType.Self,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new CardsVar(2)];
    public override List<(string,string)> Localization=>new CardLoc("風の知らせ","カードを{Cards:diff()}枚引く。その後、手札1枚を[gold]廃棄[/gold]する。\n[gold]風相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p)
    {
        await Draw(c,DynamicVars.Cards.BaseValue);
        var card=(await CardSelectCmd.FromHand(c,Owner,new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt,1),null,this)).FirstOrDefault();
        if(card is not null) await CardCmd.Exhaust(c,card);
    }
    protected override void OnUpgrade()=>DynamicVars.Cards.UpgradeValueBy(1);
}
/// <summary>v0.23 (ユーザーレビュー): 鎮静の霧 left the reward pool for the starter; a cheap air card in its slot.</summary>
public sealed class AirBreeze() : ElementCard(1,CardType.Skill,CardRarity.Common,TargetType.Self,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new CardsVar(1)];
    public override List<(string,string)> Localization=>new CardLoc("そよ風","カードを{Cards:diff()}枚引く。\n[gold]風相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>Draw(c,DynamicVars.Cards.BaseValue);
    protected override void OnUpgrade()=>DynamicVars.Cards.UpgradeValueBy(1);
}
/// <summary>v0.23 (ユーザーレビュー): 即席錬成 left the reward pool for the starter; plain block that enters air.</summary>
public sealed class AirWindVeil() : ElementCard(1,CardType.Skill,CardRarity.Common,TargetType.Self,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new BlockVar(6,ValueProp.Move)];
    public override List<(string,string)> Localization=>new CardLoc("風の衣","{Block:diff()}[gold]ブロック[/gold]を得る。\n[gold]風相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>CardBlock(p);
    protected override void OnUpgrade()=>DynamicVars.Block.UpgradeValueBy(3);
}
// v0.23 (ユーザーレビュー): was the Silent's Afterimage at uncommon. Now it pays for this turn's transitions (原則7).
public sealed class AirAfterimage() : ElementCard(1,CardType.Power,CardRarity.Uncommon,TargetType.Self,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<WindAfterimagePower>(3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<WindAfterimagePower>()];
    public override List<(string,string)> Localization=>new CardLoc("残風の刃","ターン終了時、このターンに起きた[gold]相転移[/gold]1回につき、ランダムな敵に{WindAfterimagePower:diff()}ダメージを与える。\n[gold]風相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<WindAfterimagePower>(c,DynamicVars["WindAfterimagePower"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["WindAfterimagePower"].UpgradeValueBy(1);
}
// v0.23 (ユーザーレビュー「強すぎ。業火の意味がない」): counts only transitions into air, with a bigger hit.
// v0.23.1 (ユーザー判断): 8 → 6 (upgrade 8). It still grew in decks that never aimed for transitions; 6 keeps it with
// the base game's combat-long scalers (docs/pool-balance.md).
public sealed class AirStormBlade() : ElementCard(1,CardType.Attack,CardRarity.Rare,TargetType.AnyEnemy,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PreviewCountVar("HitCount",c=>Math.Max(1,c.TransitionsInto(AlchemyPhase.Air))),new DamageVar(6,ValueProp.Move)];
    public override List<(string,string)> Localization=>new CardLoc("ソードストリーム","{Damage:diff()}ダメージを、この戦闘で[gold]風相[/gold]へ[gold]相転移[/gold]した回数だけ与える（最低1回）。{InCombat:\n（{HitCount:diff()}回）|}\n[gold]風相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>Hit(c,p,DynamicVars.Damage.BaseValue,Math.Max(1,TransitionsInto(AlchemyPhase.Air)));
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(2);
}
// v0.19: "draw whenever you transition" broke 原則5 and looped (原則4). Now a reward for many transitions
// in one turn (原則7), paid next turn so it cannot feed the turn that earned it.
public sealed class AirSky() : ElementCard(2,CardType.Power,CardRarity.Rare,TargetType.Self,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<SkyPower>(2),new DynamicVar("Threshold",SkyPower.Threshold)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<SkyPower>()];
    public override List<(string,string)> Localization=>new CardLoc("オーバードライブ","ターン終了時、このターンに[gold]相転移[/gold]が{Threshold}回以上起きていれば、次のターン、エナジーを{SkyPower:diff()}得る。\n[gold]風相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<SkyPower>(c,DynamicVars["SkyPower"].BaseValue);
    protected override void OnUpgrade()=>EnergyCost.UpgradeBy(-1);
}
