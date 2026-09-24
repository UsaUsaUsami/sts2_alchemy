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

// Water reward cards: weak, poison and calming. SoothingMist and TidalGuard live in ElementalCards.cs.

public sealed class WaterBlade() : ElementCard(1,CardType.Attack,CardRarity.Common,TargetType.AnyEnemy,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(6,ValueProp.Move),new PowerVar<WeakPower>(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<WeakPower>()];
    public override List<(string,string)> Localization=>new CardLoc("水刃","{Damage:diff()}ダメージ。[gold]脱力[/gold]{WeakPower:diff()}を与える。\n[gold]水相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await Hit(c,p,DynamicVars.Damage.BaseValue);await ApplyTo<WeakPower>(c,p.Target!,DynamicVars.Weak.BaseValue);}
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(3);
}
public sealed class WaterVenomDrop() : ElementCard(1,CardType.Attack,CardRarity.Common,TargetType.AnyEnemy,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(5,ValueProp.Move),new PowerVar<PoisonPower>(3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<PoisonPower>()];
    public override List<(string,string)> Localization=>new CardLoc("毒の雫","{Damage:diff()}ダメージ。[gold]毒[/gold]{PoisonPower:diff()}を与える。\n[gold]水相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await Hit(c,p,DynamicVars.Damage.BaseValue);await ApplyTo<PoisonPower>(c,p.Target!,DynamicVars.Poison.BaseValue);}
    protected override void OnUpgrade()=>DynamicVars.Poison.UpgradeValueBy(2);
}
public sealed class WaterTorrent() : ElementCard(1,CardType.Attack,CardRarity.Uncommon,TargetType.AllEnemies,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(4,ValueProp.Move),new PowerVar<WeakPower>(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<WeakPower>()];
    public override List<(string,string)> Localization=>new CardLoc("奔流","敵全体に{Damage:diff()}ダメージと[gold]脱力[/gold]{WeakPower:diff()}を与える。\n[gold]水相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await HitAll(c,p,DynamicVars.Damage.BaseValue);await ApplyAll<WeakPower>(c,DynamicVars.Weak.BaseValue);}
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(2);
}
public sealed class WaterErosion() : ElementCard(1,CardType.Attack,CardRarity.Uncommon,TargetType.AnyEnemy,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(8,ValueProp.Move),new DynamicVar("Bonus",6)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<WeakPower>()];
    public override List<(string,string)> Localization=>new CardLoc("浸食","{Damage:diff()}ダメージ。対象が[gold]脱力[/gold]状態なら、さらに{Bonus:diff()}ダメージ。\n[gold]水相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)
        =>Hit(c,p,DynamicVars.Damage.BaseValue+(p.Target?.GetPower<WeakPower>() is not null?DynamicVars["Bonus"].BaseValue:0));
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(3);
}
public sealed class WaterCalmingWave() : ElementCard(1,CardType.Skill,CardRarity.Uncommon,TargetType.AllEnemies,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<WeakPower>(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<WeakPower>()];
    public override List<(string,string)> Localization=>new CardLoc("鎮静の波","敵全体に[gold]脱力[/gold]{WeakPower:diff()}を与える。\n[gold]水相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplyAll<WeakPower>(c,DynamicVars.Weak.BaseValue);
    protected override void OnUpgrade()=>DynamicVars.Weak.UpgradeValueBy(1);
}
public sealed class WaterVenomSpray() : ElementCard(1,CardType.Skill,CardRarity.Uncommon,TargetType.AllEnemies,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<PoisonPower>(4)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<PoisonPower>()];
    public override List<(string,string)> Localization=>new CardLoc("毒液散布","敵全体に[gold]毒[/gold]{PoisonPower:diff()}を与える。\n[gold]水相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplyAll<PoisonPower>(c,DynamicVars.Poison.BaseValue);
    protected override void OnUpgrade()=>DynamicVars.Poison.UpgradeValueBy(2);
}
public sealed class WaterMistVeil() : ElementCard(1,CardType.Skill,CardRarity.Uncommon,TargetType.Self,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new BlockVar(7,ValueProp.Move),new PowerVar<WeakPower>(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<WeakPower>()];
    public override List<(string,string)> Localization=>new CardLoc("霧隠れ","{Block:diff()}[gold]ブロック[/gold]を得る。[gold]水相[/gold]で使うと、敵全体に[gold]脱力[/gold]{WeakPower:diff()}を与える。\n[gold]水相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await CardBlock(p);if(InOwnPhase)await ApplyAll<WeakPower>(c,DynamicVars.Weak.BaseValue);}
    protected override void OnUpgrade()=>DynamicVars.Block.UpgradeValueBy(3);
}
public sealed class WaterMiasma() : ElementCard(1,CardType.Power,CardRarity.Uncommon,TargetType.Self,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<NoxiousFumesPower>(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<NoxiousFumesPower>(),HoverTipFactory.FromPower<PoisonPower>()];
    public override List<(string,string)> Localization=>new CardLoc("瘴気","自分のターン開始時、敵全体に[gold]毒[/gold]{NoxiousFumesPower:diff()}を与える。\n[gold]水相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<NoxiousFumesPower>(c,DynamicVars["NoxiousFumesPower"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["NoxiousFumesPower"].UpgradeValueBy(1);
}
public sealed class WaterStill() : ElementCard(1,CardType.Power,CardRarity.Uncommon,TargetType.Self,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<StillWaterPower>(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<StillWaterPower>(),HoverTipFactory.FromPower<WeakPower>()];
    public override List<(string,string)> Localization=>new CardLoc("静水","ターン終了時、[gold]水相[/gold]なら敵全体に[gold]脱力[/gold]{StillWaterPower:diff()}を与える。\n[gold]水相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<StillWaterPower>(c,DynamicVars["StillWaterPower"].BaseValue);
    protected override void OnUpgrade()=>EnergyCost.UpgradeBy(-1);
}
public sealed class WaterTsunami() : ElementCard(2,CardType.Attack,CardRarity.Rare,TargetType.AllEnemies,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(10,ValueProp.Move),new PowerVar<WeakPower>(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<WeakPower>()];
    public override List<(string,string)> Localization=>new CardLoc("大海嘯","敵全体に{Damage:diff()}ダメージと[gold]脱力[/gold]{WeakPower:diff()}を与える。\n[gold]水相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await HitAll(c,p,DynamicVars.Damage.BaseValue);await ApplyAll<WeakPower>(c,DynamicVars.Weak.BaseValue);}
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(4);
}
public sealed class WaterCatalyst() : ElementCard(1,CardType.Skill,CardRarity.Rare,TargetType.AnyEnemy,AlchemyPhase.Water)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<PoisonPower>()];
    public override List<(string,string)> Localization=>new CardLoc("濃縮","対象の[gold]毒[/gold]を2倍にする。\n[gold]水相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)
        =>p.Target?.GetPower<PoisonPower>() is { Amount: > 0 } poison ? ApplyTo<PoisonPower>(c,p.Target,poison.Amount) : Task.CompletedTask;
    protected override void OnUpgrade()=>EnergyCost.UpgradeBy(-1);
}
public sealed class WaterRequiem() : ElementCard(1,CardType.Skill,CardRarity.Rare,TargetType.AnyEnemy,AlchemyPhase.Water)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords=>[CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DynamicVar("Bonus",3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<StrengthPower>()];
    public override List<(string,string)> Localization=>new CardLoc("鎮魂","対象の[gold]筋力[/gold]を{Bonus:diff()}下げる。\n[gold]水相[/gold] [gold]廃棄[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplyTo<StrengthPower>(c,p.Target!,-DynamicVars["Bonus"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["Bonus"].UpgradeValueBy(1);
}
public sealed class WaterClearStream() : ElementCard(1,CardType.Skill,CardRarity.Rare,TargetType.AnyEnemy,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<PoisonPower>(3),new DynamicVar("Bonus",2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<PoisonPower>()];
    public override List<(string,string)> Localization=>new CardLoc("清流","[gold]毒[/gold]{PoisonPower:diff()}を与える。さらに、この戦闘で起きた相転移1回につき[gold]毒[/gold]{Bonus:diff()}を与える。\n[gold]水相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)
        =>ApplyTo<PoisonPower>(c,p.Target!,DynamicVars.Poison.BaseValue+TransitionCount*DynamicVars["Bonus"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["Bonus"].UpgradeValueBy(1);
}
public sealed class WaterBlessing() : ElementCard(2,CardType.Power,CardRarity.Rare,TargetType.Self,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<WaterBlessingPower>(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<WaterBlessingPower>(),HoverTipFactory.FromPower<PoisonPower>()];
    public override List<(string,string)> Localization=>new CardLoc("水神の加護","相転移するたび、敵全体に[gold]毒[/gold]{WaterBlessingPower:diff()}を与える。\n[gold]水相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<WaterBlessingPower>(c,DynamicVars["WaterBlessingPower"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["WaterBlessingPower"].UpgradeValueBy(1);
}
