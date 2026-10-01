using Alchemy.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemy;

/// <summary>
/// The alchemist's own relic pool (2026-10-01, user-approved list), shaped like each base game character's: the
/// starter plus Common 1, Uncommon 2, Rare 3 and Shop 1. It replaced the borrowed Ironclad relics. None of them adds
/// materials: only ancients may exceed the furnace cap (design-axes 6.1). None adds draw on a transition (原則5).
/// Icons are the mod's own (IconArt, by class name); the named base-game relic is the fallback without the file.
/// </summary>
public abstract class AlchemyRelic(string icon) : CustomRelicModel
{
    public override string PackedIconPath
        => IconArt.Packed(IconArt.Slug(GetType())) ?? $"res://images/atlases/relic_atlas.sprites/{icon}.tres";
    protected override string PackedIconOutlinePath
        => IconArt.Outline(IconArt.Slug(GetType())) ?? $"res://images/atlases/relic_outline_atlas.sprites/{icon}.tres";
    protected override string BigIconPath => IconArt.Big(IconArt.Slug(GetType())) ?? $"res://images/relics/{icon}.png";
    protected HarvestCombat? Combat => Owner.GetRelic<MaterialBox>()?.Combat;
}

/// <summary>方位盤: leaving the neutral phase counts as a transition in this owner's combats (MaterialBox sets it).</summary>
[Pool(typeof(AlchemyRelicPool))]
public sealed class PhaseCompass() : AlchemyRelic("golden_compass")
{
    public override RelicRarity Rarity => RelicRarity.Common;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [PhaseTransitionTip.Tip];
    public override List<(string,string)> Localization => new RelicLoc("方位盤",
        "無相から最初の相へ移ったときにも[gold]相転移[/gold]が起きる。", "針はいつも、次に向かう相を指している。");
}

/// <summary>血の杯: drain on one random enemy when combat begins (the timing of the base game's Bag of Marbles).</summary>
[Pool(typeof(AlchemyRelicPool))]
public sealed class BloodChalice() : AlchemyRelic("blood_vial")
{
    public const int Drain = 3;
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<LifeDrainPower>()];
    public override List<(string,string)> Localization => new RelicLoc("血の杯",
        $"戦闘開始時、ランダムな敵1体に[gold]ドレイン[/gold]{Drain}を与える。", "底に残った一滴が、次の一滴を呼ぶ。");
    public override async Task BeforeSideTurnStart(PlayerChoiceContext c, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature) || Owner.PlayerCombatState?.TurnNumber > 1 || combatState.HittableEnemies.Count == 0) return;
        Flash();
        var target = Owner.RunState.Rng.CombatTargets.NextItem(combatState.HittableEnemies);
        await PowerCmd.Apply<LifeDrainPower>(c, target!, Drain, Owner.Creature, null);
    }
}

/// <summary>
/// 脈打つ核石: golem HP at the start of each turn. A new source of golem HP besides drain and cards, added by the
/// user (2026-10-01); it also brings the golem out from the first turn.
/// </summary>
[Pool(typeof(AlchemyRelicPool))]
public sealed class PulsingCore() : AlchemyRelic("beating_remnant")
{
    public const int Gain = 2;
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    public override List<(string,string)> Localization => new RelicLoc("脈打つ核石",
        $"自分のターン開始時、[gold]ゴーレムHP[/gold]を{Gain}得る。", "石の奥で、何かが脈を打っている。");
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner.Creature) || Combat is null) return;
        Flash();
        await LifeAxis.GainHomunculus(new ThrowingPlayerChoiceContext(), Owner, Gain);
    }
}

/// <summary>四分儀: the third transition of a turn gives energy. Only the third, so once a turn.</summary>
[Pool(typeof(AlchemyRelicPool))]
public sealed class Quadrant() : AlchemyRelic("orrery"), IPhaseTransitionListener
{
    public const int Nth = 3;
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [PhaseTransitionTip.Tip];
    public override List<(string,string)> Localization => new RelicLoc("四分儀",
        $"1ターンに{Nth}回目の[gold]相転移[/gold]が起きたとき、[energy]を1得る。", "四つの相を測り終えたとき、次の一手が見える。");
    public async Task AfterPhaseTransition(PhaseTransitionContext transition)
    {
        if (transition.Owner != Owner || Combat?.Phases.TransitionsThisTurn != Nth) return;
        Flash();
        await PlayerCmd.GainEnergy(1, Owner);
    }
}

/// <summary>大坩堝: block at the end of the turn for each distinct element entered this turn.</summary>
[Pool(typeof(AlchemyRelicPool))]
public sealed class GreatCrucible() : AlchemyRelic("cauldron")
{
    public const int BlockPerKind = 3;
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [PhaseTransitionTip.Tip];
    public override List<(string,string)> Localization => new RelicLoc("大坩堝",
        $"ターン終了時、このターンに[gold]相転移[/gold]で入った相の種類1つにつき{BlockPerKind}[gold]ブロック[/gold]を得る。", "四つを一つの器で煮る。");
    // The base game's Orichalcum uses the same hook for its end-of-turn block.
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext c, CombatSide side, IEnumerable<Creature> participants)
    {
        int kinds = Combat?.Phases.KindsEnteredThisTurn ?? 0;
        if (side != CombatSide.Player || !participants.Contains(Owner.Creature) || kinds == 0) return;
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, kinds * BlockPerKind, ValueProp.Unpowered, null);
    }
}

/// <summary>番人の礎: when the golem falls, drain every enemy. Its HP comes back through that drain.</summary>
[Pool(typeof(AlchemyRelicPool))]
public sealed class WardensFoundation() : AlchemyRelic("white_beast_statue")
{
    public const int Drain = 5;
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<LifeDrainPower>()];
    public override List<(string,string)> Localization => new RelicLoc("番人の礎",
        $"ゴーレムが倒れるたび、敵全体に[gold]ドレイン[/gold]{Drain}を与える。", "崩れた石は、次の石の土台になる。");
    public override async Task AfterDeath(PlayerChoiceContext c, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature != LifeAxis.Pet(Owner) || Owner.Creature.CombatState is not { } combatState || combatState.HittableEnemies.Count == 0) return;
        Flash();
        await PowerCmd.Apply<LifeDrainPower>(c, combatState.HittableEnemies, Drain, Owner.Creature, null);
    }
}

/// <summary>大きな素材鞄: a bigger material box. MaterialBox.Inventory reads it; capacity is not supply.</summary>
[Pool(typeof(AlchemyRelicPool))]
public sealed class LargeMaterialBag() : AlchemyRelic("bag_of_preparation")
{
    public override RelicRarity Rarity => RelicRarity.Shop;
    public override List<(string,string)> Localization => new RelicLoc("大きな素材鞄",
        $"素材ボックスの容量が{AlchemyState.LargeBagBonus}増える。", "詰め込めば、まだ入る。");
}
