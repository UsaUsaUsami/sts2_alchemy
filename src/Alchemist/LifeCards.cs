using Alchemist.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist;

// Life axis exemplar cards (design-axes.md 4). Each has an element chosen by what it does, but none of them
// reads the phase. Numbers are prototype values; names marked 仮 in the design are placeholders.

/// <summary>手本A（仮名：生命の供物）. A forbidden card: its price is lost HP, never a debuff.</summary>
public sealed class LifeOffering() : ElementCard(1,CardType.Skill,CardRarity.Common,TargetType.Self,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new HpLossVar(5),new DynamicVar("Homunculus",10)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<HomunculusPower>()];
    public override List<(string,string)> Localization=>new CardLoc("生命の供物","HPを{HpLoss:diff()}失う。[gold]ホムンクルスHP[/gold]を{Homunculus:diff()}得る。\n[gold]水相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p)
    {
        await CreatureCmd.Damage(c,Owner.Creature,DynamicVars.HpLoss.BaseValue,ValueProp.Unblockable|ValueProp.Unpowered|ValueProp.Move,this,p);
        await LifeAxis.GainHomunculus(c,Owner,DynamicVars["Homunculus"].IntValue);
    }
    protected override void OnUpgrade()=>DynamicVars["Homunculus"].UpgradeValueBy(4);
}

/// <summary>手本B（仮名：吸精の瘴気）. One below the Silent's Noxious Fumes, since drain also feeds the homunculus.</summary>
public sealed class LifeDrainMiasma() : ElementCard(1,CardType.Power,CardRarity.Uncommon,TargetType.Self,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<DrainMiasmaPower>(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<LifeDrainPower>()];
    public override List<(string,string)> Localization=>new CardLoc("吸精の瘴気","自分のターン開始時、敵全体に[gold]ドレイン[/gold]{DrainMiasmaPower:diff()}を与える。\n[gold]水相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<DrainMiasmaPower>(c,DynamicVars["DrainMiasmaPower"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["DrainMiasmaPower"].UpgradeValueBy(1);
}

/// <summary>
/// 手本C（仮名：大いなる業）. Its cost falls as drain takes HP this combat; the hand shows the current cost because
/// the card answers the game's own cost hook for itself.
/// </summary>
public sealed class LifeGreatWork() : ElementCard(5,CardType.Attack,CardRarity.Uncommon,TargetType.AnyEnemy,AlchemyPhase.Fire)
{
    public const int DrainPerCost = 10;
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(50,ValueProp.Move),new DynamicVar("Step",DrainPerCost)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<LifeDrainPower>()];
    public override List<(string,string)> Localization=>new CardLoc("大いなる業","{Damage:diff()}ダメージ。この戦闘で[gold]ドレイン[/gold]により敵が失ったHP{Step}ごとに、コストが1下がる。\n[gold]火相[/gold]");
    public override bool TryModifyEnergyCostInCombat(CardModel card,decimal originalCost,out decimal modifiedCost)
    {
        modifiedCost=originalCost;
        if(card!=this || LifeAxis.State(Owner) is not { } life) return false;
        modifiedCost=DrainRules.ReducedCost((int)originalCost,life.HpDrainedThisCombat,DrainPerCost);
        return modifiedCost!=originalCost;
    }
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>Hit(c,p,DynamicVars.Damage.BaseValue);
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(15);
}

/// <summary>人体錬成. Does not spend the homunculus: the price is the death it puts on you.</summary>
public sealed class HumanTransmutation() : ElementCard(3,CardType.Attack,CardRarity.Rare,TargetType.AnyEnemy,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DynamicVar("Multiplier",3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<HomunculusPower>(),HoverTipFactory.FromPower<DeathMarkPower>()];
    public override List<(string,string)> Localization=>new CardLoc("人体錬成","敵1体に[gold]ホムンクルスHP[/gold]×{Multiplier:diff()}のダメージを与える。自分に[gold]死亡[/gold]を付与する。\n[gold]火相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p)
    {
        int homunculus=LifeAxis.State(Owner)?.HomunculusHp ?? 0;
        if(homunculus>0) await Hit(c,p,homunculus*DynamicVars["Multiplier"].IntValue);
        await ApplySelf<DeathMarkPower>(c,1);
    }
    protected override void OnUpgrade()=>DynamicVars["Multiplier"].UpgradeValueBy(1);
}

/// <summary>生命の収穫. Triggers are taken in rounds across the enemies, each decaying as usual.</summary>
public sealed class LifeHarvest() : ElementCard(1,CardType.Skill,CardRarity.Rare,TargetType.AllEnemies,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DynamicVar("Times",3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<LifeDrainPower>()];
    public override List<(string,string)> Localization=>new CardLoc("生命の収穫","すべての敵の[gold]ドレイン[/gold]を即座に{Times:diff()}回発動させる。発動ごとに通常どおり減る。\n[gold]水相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p)
    {
        for(int i=0;i<DynamicVars["Times"].IntValue;i++)
            foreach(var enemy in CombatState!.Enemies.Where(e=>e.IsAlive).ToArray())
                if(enemy.GetPower<LifeDrainPower>() is { } drain) await LifeAxis.TriggerDrain(drain);
    }
    protected override void OnUpgrade()=>DynamicVars["Times"].UpgradeValueBy(1);
}

/// <summary>
/// デバフ転写. An immediate skill on purpose (not a reservation or power), and its upgrade never lowers the cost,
/// so 人体錬成 plus this still needs five energy in one turn.
/// </summary>
public sealed class DebuffTransferCard() : ElementCard(2,CardType.Skill,CardRarity.Rare,TargetType.AnyEnemy,AlchemyPhase.Air)
{
    public override List<(string,string)> Localization=>new CardLoc("デバフ転写","自分のデバフを敵1体にコピーする。\n[gold]風相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p)
    {
        var mine=Owner.Creature.Powers.ToArray();
        var plan=DebuffTransfer.Plan(mine.Select(x=>new StatusCopy(x.Id.Entry,x.Amount,x.TypeForCurrentAmount==PowerType.Debuff)));
        foreach(var copy in plan)
        {
            var source=mine.First(x=>x.Id.Entry==copy.PowerId);
            await PowerCmd.Apply(c,ModelDb.GetById<PowerModel>(source.Id).ToMutable(),p.Target!,copy.Amount,Owner.Creature,this);
        }
    }
    protected override void OnUpgrade()=>AddKeyword(CardKeyword.Retain);
}

/// <summary>手本D（仮名：還元）. The spend-all exit that turns the vessel back into your own HP.</summary>
public sealed class LifeReclaim() : ElementCard(2,CardType.Skill,CardRarity.Rare,TargetType.Self,AlchemyPhase.Earth)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<HomunculusPower>()];
    public override List<(string,string)> Localization=>new CardLoc("還元","[gold]ホムンクルスHP[/gold]をすべて消費し、その半分だけHPを回復する。\n[gold]地相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p)
    {
        int heal=LifeAxis.SpendAllHomunculus(Owner)/2;
        if(heal>0) await CreatureCmd.Heal(Owner.Creature,heal);
    }
    protected override void OnUpgrade()=>EnergyCost.UpgradeBy(-1);
}
