using Alchemist.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist;

public abstract class ElementCard(int cost, CardType type, CardRarity rarity, TargetType target, AlchemyPhase element)
    : AlchemyCard(cost, type, rarity, target)
{
    public sealed override AlchemyPhase Element => element;
}

public sealed class EarthenGuard() : ElementCard(1,CardType.Skill,CardRarity.Common,TargetType.Self,AlchemyPhase.Earth)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(8,ValueProp.Move)];
    public override List<(string,string)> Localization => new CardLoc("土壁","{Block:diff()}[gold]ブロック[/gold]を得る。\n[gold]地相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>CreatureCmd.GainBlock(Owner.Creature,DynamicVars.Block,p);
    protected override void OnUpgrade()=>DynamicVars.Block.UpgradeValueBy(3);
}
public sealed class StoneEdge() : ElementCard(1,CardType.Attack,CardRarity.Common,TargetType.AnyEnemy,AlchemyPhase.Earth)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7,ValueProp.Move),new BlockVar(3,ValueProp.Move)];
    public override bool GainsBlock=>true;
    public override List<(string,string)> Localization => new CardLoc("石刃","{Damage:diff()}ダメージ。{Block:diff()}[gold]ブロック[/gold]を得る。\n[gold]地相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this,p).Targeting(p.Target!).Execute(c);await CreatureCmd.GainBlock(Owner.Creature,DynamicVars.Block,p);}
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(3);
}
public sealed class SoothingMist() : ElementCard(1,CardType.Skill,CardRarity.Common,TargetType.AnyEnemy,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<WeakPower>(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<WeakPower>()];
    public override List<(string,string)> Localization => new CardLoc("鎮静の霧","[gold]脱力[/gold]{WeakPower:diff()}を与える。\n[gold]水相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>PowerCmd.Apply<WeakPower>(c,p.Target!,DynamicVars.Weak.BaseValue,Owner.Creature,this);
    protected override void OnUpgrade()=>DynamicVars.Weak.UpgradeValueBy(1);
}
public sealed class TidalGuard() : ElementCard(1,CardType.Skill,CardRarity.Common,TargetType.Self,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new BlockVar(6,ValueProp.Move),new CardsVar(1)];
    public override List<(string,string)> Localization=>new CardLoc("潮の守り","{Block:diff()}[gold]ブロック[/gold]を得る。カードを{Cards}枚引く。\n[gold]水相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await CreatureCmd.GainBlock(Owner.Creature,DynamicVars.Block,p);await CardPileCmd.Draw(c,DynamicVars.Cards.BaseValue,Owner);}
    protected override void OnUpgrade()=>DynamicVars.Block.UpgradeValueBy(3);
}
public sealed class Ignition() : ElementCard(1,CardType.Attack,CardRarity.Common,TargetType.AnyEnemy,AlchemyPhase.Fire)
{
    protected override CardModel Artwork=>ModelDb.Card<Thunderclap>();
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(10,ValueProp.Move)];
    public override List<(string,string)> Localization=>new CardLoc("点火","{Damage:diff()}ダメージ。\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this,p).Targeting(p.Target!).Execute(c);
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(4);
}
public sealed class FlashPowder() : ElementCard(1,CardType.Attack,CardRarity.Uncommon,TargetType.AllEnemies,AlchemyPhase.Fire)
{
    protected override CardModel Artwork=>ModelDb.Card<Thunderclap>();
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(6,ValueProp.Move)];
    public override List<(string,string)> Localization=>new CardLoc("閃光火薬","敵全体に{Damage:diff()}ダメージ。\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this,p).TargetingAllOpponents(CombatState!).Execute(c);
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(3);
}
public sealed class Tailwind() : ElementCard(0,CardType.Skill,CardRarity.Common,TargetType.Self,AlchemyPhase.Air)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords=>[CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new CardsVar(1)];
    public override List<(string,string)> Localization=>new CardLoc("追い風","カードを{Cards:diff()}枚引く。\n[gold]風相[/gold] [gold]廃棄[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>CardPileCmd.Draw(c,DynamicVars.Cards.BaseValue,Owner);
    protected override void OnUpgrade()=>DynamicVars.Cards.UpgradeValueBy(1);
}
public sealed class Slipstream() : ElementCard(1,CardType.Skill,CardRarity.Common,TargetType.Self,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new BlockVar(5,ValueProp.Move),new CardsVar(1)];
    public override List<(string,string)> Localization=>new CardLoc("風路","{Block:diff()}[gold]ブロック[/gold]を得る。カードを{Cards}枚引く。\n[gold]風相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await CreatureCmd.GainBlock(Owner.Creature,DynamicVars.Block,p);await CardPileCmd.Draw(c,DynamicVars.Cards.BaseValue,Owner);}
    protected override void OnUpgrade()=>DynamicVars.Block.UpgradeValueBy(3);
}

public sealed class InstantAlchemy() : AlchemyCard(1,CardType.Skill,CardRarity.Common,TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords=>[CardKeyword.Exhaust];
    public override List<(string,string)> Localization=>new CardLoc("即席錬成","通常素材を1個選んで消費する。対応する0コストの一時カードを手札に加える。 [gold]廃棄[/gold]",("selectionScreenPrompt","消費する素材を選択"));
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p)
    {
        var box=Owner.GetRelic<MaterialBox>(); if(box is null) return;
        List<CardModel> choices=[];
        void Add<T>(Material m) where T:CardModel { if(box.Inventory.Counts[(int)m]>0){var x=ModelDb.Card<T>().ToMutable();x.Owner=Owner;x.AfterCreated();choices.Add(x);} }
        Add<IronImprovisation>(Material.Iron);Add<HerbImprovisation>(Material.Herb);Add<PowderImprovisation>(Material.Powder);Add<EtherImprovisation>(Material.Ether);
        if(choices.Count==0) return;
        var chosen=(await CardSelectCmd.FromSimpleGrid(c,choices,Owner,new CardSelectorPrefs(SelectionScreenPrompt,1))).FirstOrDefault();
        foreach(var x in choices)x.Owner=null!;
        if(chosen is null) return;
        Material material=chosen switch{IronImprovisation=>Material.Iron,HerbImprovisation=>Material.Herb,PowderImprovisation=>Material.Powder,_=>Material.Ether};
        if(!box.Inventory.TryConsume(material)) return;
        CardModel result=chosen switch{IronImprovisation=>CombatState!.CreateCard<IronImprovisation>(Owner),HerbImprovisation=>CombatState!.CreateCard<HerbImprovisation>(Owner),PowderImprovisation=>CombatState!.CreateCard<PowderImprovisation>(Owner),_=>CombatState!.CreateCard<EtherImprovisation>(Owner)};
        await CardPileCmd.AddGeneratedCardsToCombat([result],PileType.Hand,Owner);
    }
    protected override void OnUpgrade()=>EnergyCost.UpgradeBy(-1);
}

public abstract class ImprovisationCard(AlchemyPhase phase):ElementCard(0,CardType.Skill,CardRarity.Token,TargetType.Self,phase)
{ public override IEnumerable<CardKeyword> CanonicalKeywords=>[CardKeyword.Exhaust]; }
public sealed class IronImprovisation():ImprovisationCard(AlchemyPhase.Earth)
{ protected override IEnumerable<DynamicVar> CanonicalVars=>[new BlockVar(7,ValueProp.Move)];public override List<(string,string)> Localization=>new CardLoc("即席の鉄壁","{Block}[gold]ブロック[/gold]を得る。 [gold]地相 廃棄[/gold]");protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>CreatureCmd.GainBlock(Owner.Creature,DynamicVars.Block,p);protected override void OnUpgrade(){} }
public sealed class HerbImprovisation():ImprovisationCard(AlchemyPhase.Water)
{ public override TargetType TargetType=>TargetType.AnyEnemy;protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<WeakPower>(1)];public override List<(string,string)> Localization=>new CardLoc("即席の鎮静薬","[gold]脱力[/gold]{WeakPower}を与える。 [gold]水相 廃棄[/gold]");protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>PowerCmd.Apply<WeakPower>(c,p.Target!,DynamicVars.Weak.BaseValue,Owner.Creature,this);protected override void OnUpgrade(){} }
public sealed class PowderImprovisation():ImprovisationCard(AlchemyPhase.Fire)
{ public override TargetType TargetType=>TargetType.AnyEnemy;protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(8,ValueProp.Move)];public override List<(string,string)> Localization=>new CardLoc("即席の炸薬","{Damage}ダメージ。 [gold]火相 廃棄[/gold]");protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this,p).Targeting(p.Target!).Execute(c);protected override void OnUpgrade(){} }
public sealed class EtherImprovisation():ImprovisationCard(AlchemyPhase.Air)
{ protected override IEnumerable<DynamicVar> CanonicalVars=>[new CardsVar(2)];public override List<(string,string)> Localization=>new CardLoc("即席の霊風","カードを{Cards}枚引く。 [gold]風相 廃棄[/gold]");protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>CardPileCmd.Draw(c,DynamicVars.Cards.BaseValue,Owner);protected override void OnUpgrade(){} }

// The pool needs at least one Power: the merchant stocks one character card of each type and fails to
// populate without it. This one is also the reference use of the transition-listener hook.
public sealed class PhaseResonance() : AlchemyCard(1,CardType.Power,CardRarity.Uncommon,TargetType.Self)
{
    protected override CardModel Artwork=>ModelDb.Card<Inflame>();
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<PhaseResonancePower>(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<PhaseResonancePower>()];
    public override List<(string,string)> Localization=>new CardLoc("相の共鳴","相転移するたび、{PhaseResonancePower:diff()}[gold]ブロック[/gold]を得る。");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>PowerCmd.Apply<PhaseResonancePower>(c,Owner.Creature,DynamicVars["PhaseResonancePower"].BaseValue,Owner.Creature,this);
    protected override void OnUpgrade()=>DynamicVars["PhaseResonancePower"].UpgradeValueBy(1);
}

public sealed class PhaseResonancePower : CustomPowerModel, IPhaseTransitionListener
{
    public override PowerType Type=>PowerType.Buff;
    public override PowerStackType StackType=>PowerStackType.Counter;
    public override string? CustomPackedIconPath=>ModelDb.Power<PlatingPower>().PackedIconPath;
    public override string? CustomBigIconPath=>"res://images/powers/plating_power.png";
    public override List<(string,string)> Localization=>new PowerLoc("相の共鳴","相転移するたび、ブロックを得る。","相転移するたび、[gold]ブロック[/gold]を{Amount}得る。");
    public Task AfterPhaseTransition(PhaseTransitionContext transition)
        => transition.Owner.Creature==Owner ? CreatureCmd.GainBlock(Owner,Amount,ValueProp.Unpowered,null) : Task.CompletedTask;
}
