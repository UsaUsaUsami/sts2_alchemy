using Alchemist.Core;
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

namespace Alchemist;

// Air reward cards: draw, energy and hand shaping. Tailwind and Slipstream live in ElementalCards.cs.

public sealed class AirGust() : ElementCard(1,CardType.Attack,CardRarity.Common,TargetType.AnyEnemy,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(6,ValueProp.Move),new CardsVar(1)];
    public override List<(string,string)> Localization=>new CardLoc("疾風","{Damage:diff()}ダメージ。カードを{Cards:diff()}枚引く。\n[gold]風相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await Hit(c,p,DynamicVars.Damage.BaseValue);await Draw(c,DynamicVars.Cards.BaseValue);}
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(3);
}
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
public sealed class AirWindBlade() : ElementCard(0,CardType.Attack,CardRarity.Uncommon,TargetType.AnyEnemy,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(4,ValueProp.Move),new CardsVar(1)];
    public override List<(string,string)> Localization=>new CardLoc("風の刃","{Damage:diff()}ダメージ。このカードで相転移が起きるなら、カードを{Cards:diff()}枚引く。\n[gold]風相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){bool shift=WillTransition;await Hit(c,p,DynamicVars.Damage.BaseValue);if(shift)await Draw(c,DynamicVars.Cards.BaseValue);}
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(3);
}
public sealed class AirMomentum() : ElementCard(1,CardType.Skill,CardRarity.Uncommon,TargetType.Self,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new CardsVar(2),new DynamicVar("Energy",1)];
    public override List<(string,string)> Localization=>new CardLoc("勢い","カードを{Cards:diff()}枚引く。このカードで相転移が起きるなら、エナジーを{Energy:diff()}得る。\n[gold]風相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){bool shift=WillTransition;await Draw(c,DynamicVars.Cards.BaseValue);if(shift)await Energy(DynamicVars["Energy"].BaseValue);}
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
    public override List<(string,string)> Localization=>new CardLoc("風選","カードを{Cards:diff()}枚引く。その後、手札1枚を[gold]廃棄[/gold]する。\n[gold]風相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p)
    {
        await Draw(c,DynamicVars.Cards.BaseValue);
        var card=(await CardSelectCmd.FromHand(c,Owner,new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt,1),null,this)).FirstOrDefault();
        if(card is not null) await CardCmd.Exhaust(c,card);
    }
    protected override void OnUpgrade()=>DynamicVars.Cards.UpgradeValueBy(1);
}
public sealed class AirWindReading() : ElementCard(1,CardType.Power,CardRarity.Uncommon,TargetType.Self,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<WindReadingPower>(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<WindReadingPower>()];
    public override List<(string,string)> Localization=>new CardLoc("風読み","ターン終了時、[gold]風相[/gold]なら次のターンにカードを{WindReadingPower:diff()}枚追加で引く。\n[gold]風相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<WindReadingPower>(c,DynamicVars["WindReadingPower"].BaseValue);
    protected override void OnUpgrade()=>EnergyCost.UpgradeBy(-1);
}
public sealed class AirAfterimage() : ElementCard(1,CardType.Power,CardRarity.Uncommon,TargetType.Self,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<AfterimagePower>(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<AfterimagePower>()];
    public override List<(string,string)> Localization=>new CardLoc("風の残像","カードを使うたび、{AfterimagePower:diff()}[gold]ブロック[/gold]を得る。\n[gold]風相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<AfterimagePower>(c,DynamicVars["AfterimagePower"].BaseValue);
    protected override void OnUpgrade()=>EnergyCost.UpgradeBy(-1);
}
public sealed class AirStormBlade() : ElementCard(1,CardType.Attack,CardRarity.Rare,TargetType.AnyEnemy,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(4,ValueProp.Move)];
    public override List<(string,string)> Localization=>new CardLoc("嵐刃","{Damage:diff()}ダメージを、この戦闘で起きた相転移の回数だけ与える（最低1回）。\n[gold]風相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>Hit(c,p,DynamicVars.Damage.BaseValue,Math.Max(1,TransitionCount));
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(2);
}
public sealed class AirGift() : ElementCard(0,CardType.Skill,CardRarity.Rare,TargetType.Self,AlchemyPhase.Air)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords=>[CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DynamicVar("Energy",2)];
    public override List<(string,string)> Localization=>new CardLoc("風の贈り物","エナジーを{Energy:diff()}得る。\n[gold]風相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>Energy(DynamicVars["Energy"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["Energy"].UpgradeValueBy(1);
}
public sealed class AirRevelation() : ElementCard(1,CardType.Skill,CardRarity.Rare,TargetType.Self,AlchemyPhase.Air)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords=>[CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new CardsVar(3),new DynamicVar("Energy",1)];
    public override List<(string,string)> Localization=>new CardLoc("天啓","カードを{Cards:diff()}枚引き、エナジーを{Energy:diff()}得る。\n[gold]風相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await Draw(c,DynamicVars.Cards.BaseValue);await Energy(DynamicVars["Energy"].BaseValue);}
    protected override void OnUpgrade()=>DynamicVars.Cards.UpgradeValueBy(1);
}
public sealed class AirFavor() : ElementCard(1,CardType.Skill,CardRarity.Rare,TargetType.Self,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<EnergyNextTurnPower>(2)];
    public override List<(string,string)> Localization=>new CardLoc("順風","次のターン、エナジーを{EnergyNextTurnPower:diff()}得る。\n[gold]風相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<EnergyNextTurnPower>(c,DynamicVars["EnergyNextTurnPower"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["EnergyNextTurnPower"].UpgradeValueBy(1);
}
public sealed class AirSky() : ElementCard(2,CardType.Power,CardRarity.Rare,TargetType.Self,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<SkyPower>(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<SkyPower>()];
    public override List<(string,string)> Localization=>new CardLoc("天空","相転移するたび、カードを{SkyPower:diff()}枚引く。\n[gold]風相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<SkyPower>(c,DynamicVars["SkyPower"].BaseValue);
    protected override void OnUpgrade()=>EnergyCost.UpgradeBy(-1);
}
