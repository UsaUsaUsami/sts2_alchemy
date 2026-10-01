using Alchemy.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace Alchemy;

/// <summary>
/// The starter relic after Orobas refines it (design-axes 6.3 G-1, G-2): combats open in a random element, and
/// one more furnace activation per combat.
/// It is still a MaterialBox, so every GetRelic&lt;MaterialBox&gt;() keeps finding it.
/// </summary>
[Pool(typeof(AlchemyRelicPool))]
public sealed class RefinedMaterialBox : MaterialBox
{
    public override int FurnaceTokens => HarvestCombat.RefinedFurnaceLimit;
    public override bool OpensInRandomPhase => true;
    public override string PackedIconPath => IconArt.Packed(IconArt.Slug(GetType())) ?? "res://images/atlases/relic_atlas.sprites/black_blood.tres";
    protected override string PackedIconOutlinePath
        => IconArt.Outline(IconArt.Slug(GetType())) ?? "res://images/atlases/relic_outline_atlas.sprites/black_blood.tres";
    protected override string BigIconPath => IconArt.Big(IconArt.Slug(GetType())) ?? "res://images/relics/black_blood.png";
    public override List<(string,string)> Localization => BoxLoc("精錬された素材ボックス", "炉の火が強まった。");
}

/// <summary>Carries the inventory over when one form of the material box replaces another.</summary>
[HarmonyPatch(typeof(RelicCmd), nameof(RelicCmd.Replace))]
public static class MaterialBoxReplacePatch
{
    public static void Prefix(RelicModel original, RelicModel replace)
    {
        if (original is MaterialBox from && replace is MaterialBox to) to.TakeInventoryFrom(from);
    }
}

/// <summary>
/// One material of the current phase at the start of a turn (design-axes 6.3 G-3), shared by the ancient card's
/// power and the v0.22.0-v0.22.2 relic. A full box simply does not receive it (6.4).
/// </summary>
public static class CrucibleTrickle
{
    public static bool Grant(Player owner, string source, int round)
    {
        if (owner.GetRelic<MaterialBox>() is not { Combat: { } combat } box
            || PhaseRules.MaterialFor(combat.Phases.Current) is not { } material) return false;
        string id = $"{owner.RunState.TotalFloor}:{owner.RunState.CurrentRoom?.Id}:{source}{round}";
        if (box.Inventory.Received.Contains(id)) return false;
        if (box.Inventory.GrantIfRoom(id, material)) { box.RefreshCount(); return true; }
        // Cosmetic only, like the phase bubble.
        try { TalkCmd.Play(new LocString("relics", $"{box.Id.Entry}.boxFull"), owner.Creature, VfxColor.White, VfxDuration.VeryShort); }
        catch (Exception ex) { GD.PushWarning($"Alchemy box-full bubble skipped: {ex.Message}"); }
        return false;
    }
}

/// <summary>
/// v0.22.0-v0.22.2 put the crucible on a relic that Darv offered. The effect belongs to the ancient card Darv's
/// Dusty Tome gives instead (DarvCrucibleCard); this definition stays only so saves holding the relic load.
/// </summary>
[Pool(typeof(AlchemyRelicPool))]
public sealed class DarvCrucible : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override string PackedIconPath => IconArt.Packed("darv_crucible") ?? "res://images/atlases/relic_atlas.sprites/philosophers_stone.tres";
    protected override string PackedIconOutlinePath
        => IconArt.Outline("darv_crucible") ?? "res://images/atlases/relic_outline_atlas.sprites/philosophers_stone.tres";
    protected override string BigIconPath => IconArt.Big("darv_crucible") ?? "res://images/relics/philosophers_stone.png";
    public override List<(string,string)> Localization => new RelicLoc("ダーヴの坩堝（旧）",
        "自分のターン開始時、現在相に対応する素材を1個得る。無相では得ない。素材ボックスが満杯なら得ない。",
        "古い坩堝は、今も火を覚えている。");
    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == CombatSide.Player && participants.Contains(Owner.Creature) && CrucibleTrickle.Grant(Owner, "darv", combatState.RoundNumber)) Flash();
        return Task.CompletedTask;
    }
}
