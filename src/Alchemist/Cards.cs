using Alchemist.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist;

[Pool(typeof(AlchemyCardPool))]
public abstract class AlchemyCard(int cost, CardType type, CardRarity rarity, TargetType target)
    : CustomCardModel(cost, type, rarity, target)
{
    public virtual AlchemyPhase Element => AlchemyPhase.None;
    protected virtual CardModel Artwork => ModelDb.Card<TrueGrit>();
    public override string PortraitPath => Artwork.PortraitPath;
    public override string? CustomPortraitPath => Artwork.PortraitPath;
    public override string BetaPortraitPath => Artwork.BetaPortraitPath;
}

public sealed class PortableFurnace() : AlchemyCard(1, CardType.Power, CardRarity.Basic, TargetType.Self)
{
    public override List<(string, string)> Localization => new CardLoc("携帯錬金炉",
        "この戦闘で初めて使用した時、[gold]炉の起動[/gold]を2枚手札に加える。\n炉の起動は戦闘全体で2回まで。廃棄した時の素材相を採取する。");
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combat = Owner.GetRelic<MaterialBox>()?.Combat;
        if (combat is null || combat.FurnaceTokensGranted) return;
        combat.FurnaceTokensGranted = true;
        var cards = Enumerable.Range(0, 2).Select(_ => CombatState!.CreateCard<FurnaceActivation>(Owner)).ToArray();
        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, Owner);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class FurnaceActivation() : AlchemyCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];
    public override List<(string, string)> Localization => new CardLoc("炉の起動",
        "手札1枚を廃棄し、現在相に対応する素材を2個得る。無相では使用できない。\n地：鉄、水：薬草、火：火薬、風：エーテル。戦闘全体で2回まで。");
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var box = Owner.GetRelic<MaterialBox>();
        var combat = box?.Combat;
        if (combat is null || combat.FurnaceUsed >= 2 || combat.Phases.Current==AlchemyPhase.None) return;
        var card = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1), null, this)).FirstOrDefault();
        if (card is null) return;
        await CardCmd.Exhaust(choiceContext, card);
        if (!combat.UseFurnace()) return;
        box!.GrantFromFurnace();
    }
    protected override void OnUpgrade() { }
}

public sealed class IronGuard() : AlchemyCard(1, CardType.Skill, CardRarity.Event, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(10, ValueProp.Move)];
    public override List<(string, string)> Localization => new CardLoc("鍛鉄の護り", "{Block:diff()}[gold]ブロック[/gold]を得る。");
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p) => await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, p);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}
public sealed class HerbalEdge() : AlchemyCard(1, CardType.Attack, CardRarity.Event, TargetType.AnyEnemy)
{
    protected override CardModel Artwork => ModelDb.Card<Thunderclap>();
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8, ValueProp.Move), new PowerVar<VulnerablePower>(1)];
    public override List<(string, string)> Localization => new CardLoc("薬刃", "{Damage:diff()}ダメージ。\n[gold]弱体[/gold]{VulnerablePower:diff()}を与える。");
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this,p).Targeting(p.Target!).Execute(c);
        await PowerCmd.Apply<VulnerablePower>(c,p.Target!,DynamicVars.Vulnerable.BaseValue,Owner.Creature,this);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(3); DynamicVars.Vulnerable.UpgradeValueBy(1); }
}
public sealed class AlchemicalBlast() : AlchemyCard(1, CardType.Attack, CardRarity.Event, TargetType.AllEnemies)
{
    protected override CardModel Artwork => ModelDb.Card<Thunderclap>();
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8, ValueProp.Move)];
    public override List<(string, string)> Localization => new CardLoc("炸裂弾", "敵全体に{Damage:diff()}ダメージ。");
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p) => await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this,p).TargetingAllOpponents(CombatState!).Execute(c);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}
public sealed class EtherLens() : AlchemyCard(1, CardType.Skill, CardRarity.Event, TargetType.Self)
{
    protected override CardModel Artwork => ModelDb.Card<ShrugItOff>();
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(4, ValueProp.Move), new CardsVar(2)];
    public override List<(string, string)> Localization => new CardLoc("エーテルレンズ", "{Block:diff()}[gold]ブロック[/gold]を得る。\nカードを{Cards}枚引く。");
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p) { await CreatureCmd.GainBlock(Owner.Creature,DynamicVars.Block,p); await CardPileCmd.Draw(c,DynamicVars.Cards.BaseValue,Owner); }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}
public sealed class HerbalGuard() : AlchemyCard(1, CardType.Skill, CardRarity.Event, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(8, ValueProp.Move),new PowerVar<WeakPower>(1)];
    public override List<(string, string)> Localization => new CardLoc("薬草の被膜", "{Block:diff()}[gold]ブロック[/gold]を得る。\n敵全体に[gold]脱力[/gold]{WeakPower}を与える。");
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p) { await CreatureCmd.GainBlock(Owner.Creature,DynamicVars.Block,p); await PowerCmd.Apply<WeakPower>(c,CombatState!.HittableEnemies,DynamicVars.Weak.BaseValue,Owner.Creature,this); }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}
