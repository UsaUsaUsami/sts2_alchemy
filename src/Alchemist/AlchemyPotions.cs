using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist;

// These potions are crafted directly in the workshop. Passing false keeps BaseLib from adding them
// to a random-drop potion pool while still registering their model types with ModelDb.
public sealed class EarthPhial() : CustomPotionModel(false)
{
    public override PotionRarity Rarity=>PotionRarity.Common; public override PotionUsage Usage=>PotionUsage.CombatOnly; public override TargetType TargetType=>TargetType.AnyPlayer;
    public override string? CustomPackedImagePath=>ModelDb.Potion<BlockPotion>().ImagePath;
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new BlockVar(12,ValueProp.Unpowered)];
    public override List<(string,string)> Localization=>new PotionLoc("地相の小瓶","{Block}[gold]ブロック[/gold]を得る。");
    protected override Task OnUse(PlayerChoiceContext c,Creature? target){PotionModel.AssertValidForTargetedPotion(target);return CreatureCmd.GainBlock(target!,DynamicVars.Block,null);}
}
public sealed class WaterPhial() : CustomPotionModel(false)
{
    public override PotionRarity Rarity=>PotionRarity.Common; public override PotionUsage Usage=>PotionUsage.CombatOnly; public override TargetType TargetType=>TargetType.AnyEnemy;
    public override string? CustomPackedImagePath=>ModelDb.Potion<WeakPotion>().ImagePath;
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<WeakPower>(2)];
    public override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<WeakPower>()];
    public override List<(string,string)> Localization=>new PotionLoc("水相の小瓶","[gold]脱力[/gold]{WeakPower}を与える。");
    protected override Task OnUse(PlayerChoiceContext c,Creature? target){PotionModel.AssertValidForTargetedPotion(target);return PowerCmd.Apply<WeakPower>(c,target!,DynamicVars.Weak.BaseValue,Owner.Creature,null);}
}
public sealed class FirePhial() : CustomPotionModel(false)
{
    public override PotionRarity Rarity=>PotionRarity.Common; public override PotionUsage Usage=>PotionUsage.CombatOnly; public override TargetType TargetType=>TargetType.AnyEnemy;
    public override string? CustomPackedImagePath=>ModelDb.Potion<FirePotion>().ImagePath;
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(15,ValueProp.Unpowered)];
    public override List<(string,string)> Localization=>new PotionLoc("火相の小瓶","{Damage}ダメージを与える。");
    protected override Task OnUse(PlayerChoiceContext c,Creature? target){PotionModel.AssertValidForTargetedPotion(target);return CreatureCmd.Damage(c,target!,DynamicVars.Damage.BaseValue,DynamicVars.Damage.Props,Owner.Creature,null,null);}
}
public sealed class AirPhial() : CustomPotionModel(false)
{
    public override PotionRarity Rarity=>PotionRarity.Common; public override PotionUsage Usage=>PotionUsage.CombatOnly; public override TargetType TargetType=>TargetType.AnyPlayer;
    public override string? CustomPackedImagePath=>ModelDb.Potion<SwiftPotion>().ImagePath;
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new CardsVar(2)];
    public override List<(string,string)> Localization=>new PotionLoc("風相の小瓶","カードを{Cards}枚引き、[energy]を1得る。");
    protected override async Task OnUse(PlayerChoiceContext c,Creature? target){PotionModel.AssertValidForTargetedPotion(target);var player=target!.Player!;await CardPileCmd.Draw(c,DynamicVars.Cards.BaseValue,player);await PlayerCmd.GainEnergy(1,player);}
}
