using Alchemist.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist;

// Earth reward cards: block and fortification. Two starter-era cards (EarthenGuard, StoneEdge) live in
// ElementalCards.cs. Numbers are prototype values.

// v0.21: 報酬60枚への縮小で報酬プールから外した（旧セーブ読込用に定義だけ残す）。
public sealed class EarthRockSmash() : ElementCard(2,CardType.Attack,CardRarity.Event,TargetType.AnyEnemy,AlchemyPhase.Earth)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(12,ValueProp.Move),new DynamicVar("Bonus",6)];
    public override List<(string,string)> Localization=>new CardLoc("岩砕き","{Damage:diff()}ダメージ。[gold]地相[/gold]で使うと、さらに{Bonus:diff()}ダメージ。\n[gold]地相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>Hit(c,p,DynamicVars.Damage.BaseValue+(InOwnPhase?DynamicVars["Bonus"].BaseValue:0));
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(4);
}
// v0.20: 生命軸の追加で報酬プールから外した（旧セーブ読込用に定義だけ残す）。
public sealed class EarthSandStance() : ElementCard(1,CardType.Skill,CardRarity.Event,TargetType.Self,AlchemyPhase.Earth)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new BlockVar(6,ValueProp.Move),new DynamicVar("Bonus",4)];
    public override List<(string,string)> Localization=>new CardLoc("砂塵の構え","{Block:diff()}[gold]ブロック[/gold]を得る。[gold]地相[/gold]で使うと、さらに{Bonus:diff()}[gold]ブロック[/gold]。\n[gold]地相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await CardBlock(p);if(InOwnPhase)await Block(p,DynamicVars["Bonus"].BaseValue);}
    protected override void OnUpgrade()=>DynamicVars.Block.UpgradeValueBy(3);
}
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
    public override List<(string,string)> Localization=>new CardLoc("城塞打ち","現在の[gold]ブロック[/gold]と同じダメージを与える。\n[gold]地相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>Hit(c,p,Owner.Creature.Block);
    protected override void OnUpgrade()=>EnergyCost.UpgradeBy(-1);
}
// v0.21: 報酬60枚への縮小で報酬プールから外した（旧セーブ読込用に定義だけ残す）。
public sealed class EarthIronWall() : ElementCard(2,CardType.Skill,CardRarity.Event,TargetType.Self,AlchemyPhase.Earth)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new BlockVar(14,ValueProp.Move)];
    public override List<(string,string)> Localization=>new CardLoc("鉄壁","{Block:diff()}[gold]ブロック[/gold]を得る。\n[gold]地相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>CardBlock(p);
    protected override void OnUpgrade()=>DynamicVars.Block.UpgradeValueBy(5);
}
public sealed class EarthBlessing() : ElementCard(1,CardType.Skill,CardRarity.Uncommon,TargetType.Self,AlchemyPhase.Earth)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<PlatingPower>(4)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<PlatingPower>()];
    public override List<(string,string)> Localization=>new CardLoc("大地の加護","[gold]プレート[/gold]{PlatingPower:diff()}を得る。\n[gold]地相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<PlatingPower>(c,DynamicVars["PlatingPower"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["PlatingPower"].UpgradeValueBy(2);
}
public sealed class EarthFortify() : ElementCard(1,CardType.Skill,CardRarity.Uncommon,TargetType.Self,AlchemyPhase.Earth)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new BlockVar(4,ValueProp.Move),new PowerVar<BlockNextTurnPower>(8)];
    public override List<(string,string)> Localization=>new CardLoc("補強","{Block:diff()}[gold]ブロック[/gold]を得る。次のターン開始時、{BlockNextTurnPower:diff()}[gold]ブロック[/gold]を得る。\n[gold]地相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await CardBlock(p);await ApplySelf<BlockNextTurnPower>(c,DynamicVars["BlockNextTurnPower"].BaseValue);}
    protected override void OnUpgrade()=>DynamicVars["BlockNextTurnPower"].UpgradeValueBy(3);
}
// v0.21: 報酬60枚への縮小で報酬プールから外した（旧セーブ読込用に定義だけ残す）。
public sealed class EarthVein() : ElementCard(1,CardType.Power,CardRarity.Event,TargetType.Self,AlchemyPhase.Earth)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<EarthVeinPower>(4)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<EarthVeinPower>()];
    public override List<(string,string)> Localization=>new CardLoc("地脈","ターン終了時、[gold]地相[/gold]なら{EarthVeinPower:diff()}[gold]ブロック[/gold]を得る。\n[gold]地相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<EarthVeinPower>(c,DynamicVars["EarthVeinPower"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["EarthVeinPower"].UpgradeValueBy(2);
}
public sealed class EarthThornShell() : ElementCard(1,CardType.Power,CardRarity.Uncommon,TargetType.Self,AlchemyPhase.Earth)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<ThornsPower>(3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<ThornsPower>()];
    public override List<(string,string)> Localization=>new CardLoc("棘の外殻","[gold]トゲ[/gold]{ThornsPower:diff()}を得る。\n[gold]地相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<ThornsPower>(c,DynamicVars["ThornsPower"].BaseValue);
    protected override void OnUpgrade()=>DynamicVars["ThornsPower"].UpgradeValueBy(2);
}
// v0.21: 報酬60枚への縮小で報酬プールから外した（旧セーブ読込用に定義だけ残す）。
public sealed class EarthLandslide() : ElementCard(2,CardType.Attack,CardRarity.Event,TargetType.AllEnemies,AlchemyPhase.Earth)
{
    public override List<(string,string)> Localization=>new CardLoc("山崩し","敵全体に、現在の[gold]ブロック[/gold]と同じダメージを与える。\n[gold]地相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>HitAll(c,p,Owner.Creature.Block);
    protected override void OnUpgrade()=>EnergyCost.UpgradeBy(-1);
}
// v0.21: 報酬60枚への縮小で報酬プールから外した（旧セーブ読込用に定義だけ残す）。
public sealed class EarthImmovable() : ElementCard(2,CardType.Skill,CardRarity.Event,TargetType.Self,AlchemyPhase.Earth)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new BlockVar(12,ValueProp.Move)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<BlurPower>()];
    public override List<(string,string)> Localization=>new CardLoc("不動","{Block:diff()}[gold]ブロック[/gold]を得る。次のターン、[gold]ブロック[/gold]が失われない。\n[gold]地相[/gold]");
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p){await CardBlock(p);await ApplySelf<BlurPower>(c,1);}
    protected override void OnUpgrade()=>DynamicVars.Block.UpgradeValueBy(4);
}
public sealed class EarthMemory() : ElementCard(1,CardType.Skill,CardRarity.Rare,TargetType.Self,AlchemyPhase.Earth)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DynamicVar("Bonus",3)];
    public override bool GainsBlock=>true;
    public override List<(string,string)> Localization=>new CardLoc("大地の記憶","この戦闘で起きた相転移1回につき{Bonus:diff()}[gold]ブロック[/gold]を得る。\n[gold]地相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>TransitionCount>0?Block(p,TransitionCount*DynamicVars["Bonus"].BaseValue):Task.CompletedTask;
    protected override void OnUpgrade()=>DynamicVars["Bonus"].UpgradeValueBy(1);
}
// v0.21: 報酬60枚への縮小で報酬プールから外した（旧セーブ読込用に定義だけ残す）。
public sealed class EarthEntrench() : ElementCard(2,CardType.Skill,CardRarity.Event,TargetType.Self,AlchemyPhase.Earth)
{
    public override bool GainsBlock=>true;
    public override List<(string,string)> Localization=>new CardLoc("要塞化","[gold]ブロック[/gold]を2倍にする。\n[gold]地相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>Owner.Creature.Block>0
        ?CreatureCmd.GainBlock(Owner.Creature,Owner.Creature.Block,ValueProp.Unpowered,p):Task.CompletedTask;
    protected override void OnUpgrade()=>EnergyCost.UpgradeBy(-1);
}
public sealed class EarthKing() : ElementCard(3,CardType.Power,CardRarity.Rare,TargetType.Self,AlchemyPhase.Earth)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<BarricadePower>()];
    public override List<(string,string)> Localization=>new CardLoc("大地の王","[gold]ブロック[/gold]がターン開始時に失われなくなる。\n[gold]地相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<BarricadePower>(c,1);
    protected override void OnUpgrade()=>EnergyCost.UpgradeBy(-1);
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
