using Alchemist.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace Alchemist;

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
    public override string PackedIconPath => "res://images/atlases/relic_atlas.sprites/black_blood.tres";
    protected override string PackedIconOutlinePath => "res://images/atlases/relic_outline_atlas.sprites/black_blood.tres";
    protected override string BigIconPath => "res://images/relics/black_blood.png";
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
/// Darv's relic for the alchemist (design-axes 6.3 G-3, name tentative): at the start of each of your turns,
/// one material of the current phase. A full box simply does not receive it (6.4).
/// </summary>
[Pool(typeof(AlchemyRelicPool))]
public sealed class DarvCrucible : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override string PackedIconPath => "res://images/atlases/relic_atlas.sprites/philosophers_stone.tres";
    protected override string PackedIconOutlinePath => "res://images/atlases/relic_outline_atlas.sprites/philosophers_stone.tres";
    protected override string BigIconPath => "res://images/relics/philosophers_stone.png";
    public override List<(string,string)> Localization => new RelicLoc("ダーヴの坩堝",
        "自分のターン開始時、現在相に対応する素材を1個得る。無相では得ない。素材ボックスが満杯なら得ない。",
        "古い坩堝は、今も火を覚えている。");
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner.Creature)) return;
        if (Owner.GetRelic<MaterialBox>() is not { Combat: { } combat } box
            || PhaseRules.MaterialFor(combat.Phases.Current) is not { } material) return;
        string id = $"{Owner.RunState.TotalFloor}:{Owner.RunState.CurrentRoom?.Id}:darv{combatState.RoundNumber}";
        if (box.Inventory.Received.Contains(id)) return;
        if (box.Inventory.GrantIfRoom(id, material))
        {
            Flash();
            box.RefreshCount();
            return;
        }
        // Cosmetic only, like the phase bubble.
        try { TalkCmd.Play(new LocString("relics", $"{box.Id.Entry}.boxFull"), Owner.Creature, VfxColor.White, VfxDuration.VeryShort); }
        catch (Exception ex) { GD.PushWarning($"Alchemist box-full bubble skipped: {ex.Message}"); }
        await Task.CompletedTask;
    }
}

/// <summary>
/// Darv offers from a fixed list with no hook for character relics, so for the alchemist one of the drawn
/// base-game relics (never Dusty Tome, which is always last) gives way to the crucible. The event's own Rng is
/// seeded from the run seed and the event, and nothing extra is drawn from it, so reopening or reloading shows
/// the same options and other characters are untouched.
/// </summary>
[HarmonyPatch(typeof(Darv), "GenerateInitialOptions")]
public static class DarvCruciblePatch
{
    private static readonly System.Reflection.MethodInfo RelicOption = AccessTools.Method(typeof(AncientEventModel), "RelicOption",
        [typeof(RelicModel), typeof(string), typeof(string)]);

    public static void Postfix(Darv __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (__instance.Owner?.Character is not AlchemistCharacter || __result.Count == 0 || __result.Any(o => o.Relic is DarvCrucible)) return;
        var option = (EventOption)RelicOption.Invoke(__instance, [ModelDb.Relic<DarvCrucible>().ToMutable(), "INITIAL", null])!;
        var list = __result.ToList();
        list[0] = option;
        __result = list;
    }
}
