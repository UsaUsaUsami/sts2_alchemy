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

// Fire reward cards: the attack-heavy element. Ignition and FlashPowder live in ElementalCards.cs.

public sealed class FireSpark() : ElementCard(0,CardType.Attack,CardRarity.Common,TargetType.AnyEnemy,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(4,ValueProp.Move),new DynamicVar("Bonus",3)];
    public override List<(string,string)> Localization=>new CardLoc("火花","{Damage:diff()}ダメージ。このカードで相転移が起きるなら、さらに{Bonus:diff()}ダメージ。\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>Hit(c,p,DynamicVars.Damage.BaseValue+(WillTransition?DynamicVars["Bonus"].BaseValue:0));
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(2);
}
public sealed class FireFlameSlash() : ElementCard(1,CardType.Attack,CardRarity.Common,TargetType.AnyEnemy,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(8,ValueProp.Move),new DynamicVar("Bonus",4)];
    public override List<(string,string)> Localization=>new CardLoc("火炎斬り","{Damage:diff()}ダメージ。[gold]火相[/gold]で使うと、さらに{Bonus:diff()}ダメージ。\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>Hit(c,p,DynamicVars.Damage.BaseValue+(InOwnPhase?DynamicVars["Bonus"].BaseValue:0));
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(3);
}
public sealed class FireBlaze() : ElementCard(2,CardType.Attack,CardRarity.Common,TargetType.AllEnemies,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(8,ValueProp.Move)];
    public override List<(string,string)> Localization=>new CardLoc("燎原","敵全体に{Damage:diff()}ダメージ。\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>HitAll(c,p,DynamicVars.Damage.BaseValue);
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(3);
}
public sealed class FireChainBlast() : ElementCard(1,CardType.Attack,CardRarity.Uncommon,TargetType.AnyEnemy,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(3,ValueProp.Move),new DynamicVar("Hits",3)];
    public override List<(string,string)> Localization=>new CardLoc("連爆","{Damage:diff()}ダメージを{Hits:diff()}回与える。\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>Hit(c,p,DynamicVars.Damage.BaseValue,DynamicVars["Hits"].IntValue);
    protected override void OnUpgrade()=>DynamicVars["Hits"].UpgradeValueBy(1);
}
public sealed class FireScorch() : ElementCard(1,CardType.Attack,CardRarity.Uncommon,TargetType.AnyEnemy,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(7,ValueProp.Move),new PowerVar<VulnerablePower>(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<VulnerablePower>()];
    public override List<(string,string)> Localization=>new CardLoc("灼熱","{Damage:diff()}ダメージ。[gold]弱体[/gold]{VulnerablePower:diff()}を与える。\n[gold]火相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await Hit(c,p,DynamicVars.Damage.BaseValue);await ApplyTo<VulnerablePower>(c,p.Target!,DynamicVars.Vulnerable.BaseValue);}
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(3);
}
public sealed class FireIncinerate() : ElementCard(2,CardType.Attack,CardRarity.Uncommon,TargetType.AnyEnemy,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(18,ValueProp.Move)];
    public override List<(string,string)> Localization=>new CardLoc("焼却","{Damage:diff()}ダメージ。\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>Hit(c,p,DynamicVars.Damage.BaseValue);
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(6);
}
public sealed class FirePowderCharge() : ElementCard(1,CardType.Attack,CardRarity.Uncommon,TargetType.AnyEnemy,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(9,ValueProp.Move),new DynamicVar("Bonus",5)];
    public override List<(string,string)> Localization=>new CardLoc("装薬","{Damage:diff()}ダメージ。このカードで相転移が起きるなら、さらに{Bonus:diff()}ダメージ。\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>Hit(c,p,DynamicVars.Damage.BaseValue+(WillTransition?DynamicVars["Bonus"].BaseValue:0));
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(3);
}
public sealed class FireHeat() : ElementCard(1,CardType.Skill,CardRarity.Uncommon,TargetType.Self,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<VigorPower>(6)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<VigorPower>()];
    public override List<(string,string)> Localization=>new CardLoc("加熱","[gold]活力[/gold]{VigorPower:diff()}を得る。\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<VigorPower>(c,DynamicVars["VigorPower"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["VigorPower"].UpgradeValueBy(3);
}
public sealed class FireBurningWill() : ElementCard(1,CardType.Power,CardRarity.Uncommon,TargetType.Self,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<BurningWillPower>(4)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<BurningWillPower>()];
    public override List<(string,string)> Localization=>new CardLoc("燃える意志","ターン終了時、[gold]火相[/gold]なら敵全体に{BurningWillPower:diff()}ダメージを与える。\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<BurningWillPower>(c,DynamicVars["BurningWillPower"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["BurningWillPower"].UpgradeValueBy(2);
}
public sealed class FireExplosion() : ElementCard(3,CardType.Attack,CardRarity.Rare,TargetType.AllEnemies,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(22,ValueProp.Move)];
    public override List<(string,string)> Localization=>new CardLoc("大爆発","敵全体に{Damage:diff()}ダメージ。\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>HitAll(c,p,DynamicVars.Damage.BaseValue);
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(8);
}
public sealed class FireHellfire() : ElementCard(2,CardType.Attack,CardRarity.Rare,TargetType.AnyEnemy,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(6,ValueProp.Move),new DynamicVar("Bonus",3)];
    public override List<(string,string)> Localization=>new CardLoc("業火","{Damage:diff()}ダメージ。この戦闘で起きた相転移1回につき、さらに{Bonus:diff()}ダメージ。\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>Hit(c,p,DynamicVars.Damage.BaseValue+TransitionCount*DynamicVars["Bonus"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["Bonus"].UpgradeValueBy(1);
}
public sealed class FireBrand() : ElementCard(1,CardType.Skill,CardRarity.Rare,TargetType.AllEnemies,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<VulnerablePower>(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<VulnerablePower>()];
    public override List<(string,string)> Localization=>new CardLoc("炎の烙印","敵全体に[gold]弱体[/gold]{VulnerablePower:diff()}を与える。\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplyAll<VulnerablePower>(c,DynamicVars.Vulnerable.BaseValue);
    protected override void OnUpgrade()=>DynamicVars.Vulnerable.UpgradeValueBy(1);
}
public sealed class FireHeart() : ElementCard(2,CardType.Power,CardRarity.Rare,TargetType.Self,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<FlameHeartPower>(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<FlameHeartPower>(),HoverTipFactory.FromPower<StrengthPower>()];
    public override List<(string,string)> Localization=>new CardLoc("焔の心","[gold]火相[/gold]へ転移するたび、[gold]筋力[/gold]{FlameHeartPower:diff()}を得る。\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<FlameHeartPower>(c,DynamicVars["FlameHeartPower"].BaseValue);
    protected override void OnUpgrade()=>EnergyCost.UpgradeBy(-1);
}
public sealed class FireStorm() : ElementCard(2,CardType.Power,CardRarity.Rare,TargetType.Self,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<FireStormPower>(3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<FireStormPower>()];
    public override List<(string,string)> Localization=>new CardLoc("炎の嵐","相転移するたび、敵全体に{FireStormPower:diff()}ダメージを与える。\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<FireStormPower>(c,DynamicVars["FireStormPower"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["FireStormPower"].UpgradeValueBy(1);
}
