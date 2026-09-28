using Alchemist.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist;

// Fire reward cards: the attack-heavy element. Ignition and FlashPowder live in ElementalCards.cs.

public sealed class FireSpark() : ElementCard(0,CardType.Attack,CardRarity.Common,TargetType.AnyEnemy,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PreviewDamageVar((c,_)=>c.DynamicVars.Damage.BaseValue+(c.WillTransition?c.DynamicVars["Bonus"].BaseValue:0)),new DamageVar(4,ValueProp.Move),new DynamicVar("Bonus",3)];
    public override List<(string,string)> Localization=>new CardLoc("火花","{Damage:diff()}ダメージ。このカードで[gold]相転移[/gold]が起きるなら、さらに{Bonus:diff()}ダメージ。{InCombat:\n（{Total:diff()}ダメージ）|}\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>Hit(c,p,DynamicVars.Damage.BaseValue+(WillTransition?DynamicVars["Bonus"].BaseValue:0));
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(2);
}
// v0.20: 生命軸の追加で報酬プールから外した（旧セーブ読込用に定義だけ残す）。
public sealed class FireFlameSlash() : ElementCard(1,CardType.Attack,CardRarity.Event,TargetType.AnyEnemy,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PreviewDamageVar((c,_)=>c.DynamicVars.Damage.BaseValue+(c.InOwnPhase?c.DynamicVars["Bonus"].BaseValue:0)),new DamageVar(8,ValueProp.Move),new DynamicVar("Bonus",4)];
    public override List<(string,string)> Localization=>new CardLoc("火炎斬り","{Damage:diff()}ダメージ。[gold]火相[/gold]で使うと、さらに{Bonus:diff()}ダメージ。{InCombat:\n（{Total:diff()}ダメージ）|}\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>Hit(c,p,DynamicVars.Damage.BaseValue+(InOwnPhase?DynamicVars["Bonus"].BaseValue:0));
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(3);
}
// v0.21: 報酬60枚への縮小で報酬プールから外した（旧セーブ読込用に定義だけ残す）。
public sealed class FireBlaze() : ElementCard(2,CardType.Attack,CardRarity.Event,TargetType.AllEnemies,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(8,ValueProp.Move)];
    public override List<(string,string)> Localization=>new CardLoc("燎原","敵全体に{Damage:diff()}ダメージ。\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>HitAll(c,p,DynamicVars.Damage.BaseValue);
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(3);
}
// v0.21: 報酬60枚への縮小で報酬プールから外した（旧セーブ読込用に定義だけ残す）。
public sealed class FireChainBlast() : ElementCard(1,CardType.Attack,CardRarity.Event,TargetType.AnyEnemy,AlchemyPhase.Fire)
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
// v0.20: 生命軸の追加で報酬プールから外した（旧セーブ読込用に定義だけ残す）。
public sealed class FireIncinerate() : ElementCard(2,CardType.Attack,CardRarity.Event,TargetType.AnyEnemy,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(18,ValueProp.Move)];
    public override List<(string,string)> Localization=>new CardLoc("焼却","{Damage:diff()}ダメージ。\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>Hit(c,p,DynamicVars.Damage.BaseValue);
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(6);
}
// v0.21: 報酬60枚への縮小で報酬プールから外した（旧セーブ読込用に定義だけ残す）。
public sealed class FirePowderCharge() : ElementCard(1,CardType.Attack,CardRarity.Event,TargetType.AnyEnemy,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PreviewDamageVar((c,_)=>c.DynamicVars.Damage.BaseValue+(c.WillTransition?c.DynamicVars["Bonus"].BaseValue:0)),new DamageVar(9,ValueProp.Move),new DynamicVar("Bonus",5)];
    public override List<(string,string)> Localization=>new CardLoc("装薬","{Damage:diff()}ダメージ。このカードで[gold]相転移[/gold]が起きるなら、さらに{Bonus:diff()}ダメージ。{InCombat:\n（{Total:diff()}ダメージ）|}\n[gold]火相[/gold]");
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
// v0.21: 報酬60枚への縮小で報酬プールから外した（旧セーブ読込用に定義だけ残す）。
public sealed class FireBurningWill() : ElementCard(1,CardType.Power,CardRarity.Event,TargetType.Self,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<BurningWillPower>(4)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<BurningWillPower>()];
    public override List<(string,string)> Localization=>new CardLoc("燃える意志","ターン終了時、[gold]火相[/gold]なら敵全体に{BurningWillPower:diff()}ダメージを与える。\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<BurningWillPower>(c,DynamicVars["BurningWillPower"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["BurningWillPower"].UpgradeValueBy(2);
}
// v0.23 (ユーザーレビュー「弱い」): one enemy, 4 cost, and a stun like the base game's Whistle (CreatureCmd.Stun).
public sealed class FireExplosion() : ElementCard(4,CardType.Attack,CardRarity.Rare,TargetType.AnyEnemy,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(35,ValueProp.Move)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[StunIntent.GetStaticHoverTip()];
    public override List<(string,string)> Localization=>new CardLoc("大爆発","{Damage:diff()}ダメージ。対象を[gold]スタン[/gold]させる。\n[gold]火相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p)
    {
        await Hit(c,p,DynamicVars.Damage.BaseValue);
        if(p.Target is { IsAlive: true } target) await CreatureCmd.Stun(target);
    }
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(10);
}
// v0.23 (ユーザーレビュー「強い気がする」): 2 per transition (was 3).
public sealed class FireHellfire() : ElementCard(2,CardType.Attack,CardRarity.Rare,TargetType.AnyEnemy,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PreviewDamageVar((c,_)=>c.DynamicVars.Damage.BaseValue+c.TransitionCount*c.DynamicVars["Bonus"].BaseValue),new DamageVar(6,ValueProp.Move),new DynamicVar("Bonus",2)];
    public override List<(string,string)> Localization=>new CardLoc("業火","{Damage:diff()}ダメージ。この戦闘で起きた[gold]相転移[/gold]1回につき、さらに{Bonus:diff()}ダメージ。{InCombat:\n（{Total:diff()}ダメージ）|}\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>Hit(c,p,DynamicVars.Damage.BaseValue+TransitionCount*DynamicVars["Bonus"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["Bonus"].UpgradeValueBy(1);
}
// v0.23 (ユーザーレビュー「弱くね？」): gained a 10 damage hit, so it became an attack.
public sealed class FireBrand() : ElementCard(1,CardType.Attack,CardRarity.Rare,TargetType.AllEnemies,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(10,ValueProp.Move),new PowerVar<VulnerablePower>(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<VulnerablePower>()];
    public override List<(string,string)> Localization=>new CardLoc("炎の烙印","敵全体に{Damage:diff()}ダメージと[gold]弱体[/gold]{VulnerablePower:diff()}を与える。\n[gold]火相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await HitAll(c,p,DynamicVars.Damage.BaseValue);await ApplyAll<VulnerablePower>(c,DynamicVars.Vulnerable.BaseValue);}
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
// v0.21: 報酬60枚への縮小で報酬プールから外した（旧セーブ読込用に定義だけ残す）。
public sealed class FireStorm() : ElementCard(2,CardType.Power,CardRarity.Event,TargetType.Self,AlchemyPhase.Fire)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<FireStormPower>(3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<FireStormPower>()];
    public override List<(string,string)> Localization=>new CardLoc("炎の嵐","[gold]相転移[/gold]するたび、敵全体に{FireStormPower:diff()}ダメージを与える。\n[gold]火相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<FireStormPower>(c,DynamicVars["FireStormPower"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["FireStormPower"].UpgradeValueBy(1);
}
