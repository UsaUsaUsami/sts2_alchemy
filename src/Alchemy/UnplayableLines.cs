using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Alchemy;

/// <summary>
/// Why a card cannot be played (2026-10-14, UI review #9): trying an unplayable 炉の起動 made the alchemist say the
/// game's generic "使えない", and the reason (no phase yet, used up, nothing to burn) was nowhere on screen. The game
/// asks CanPlay and then, straight away, turns the reason into a line; CanPlay remembers the alchemy card's own
/// reason (AlchemyCard.UnplayableLine) and the line is swapped when nothing more specific (energy, a hook) applies.
/// </summary>
public static class UnplayableLines
{
    [ThreadStatic] internal static LocString? Pending;
}

[HarmonyPatch]
static class UnplayableReasonCapturePatch
{
    static MethodBase TargetMethod() => AccessTools.Method(typeof(CardModel), nameof(CardModel.CanPlay),
        [typeof(UnplayableReason).MakeByRefType(), typeof(AbstractModel).MakeByRefType()]);

    static void Postfix(CardModel __instance, bool __result, ref UnplayableReason reason)
    {
        UnplayableLines.Pending = !__result && reason.HasFlag(UnplayableReason.BlockedByCardLogic)
            && __instance is AlchemyCard { UnplayableLine: { } key }
            ? new LocString("cards", $"{__instance.Id.Entry}.{key}") : null;
    }
}

[HarmonyPatch]
static class UnplayableLinePatch
{
    const UnplayableReason MoreSpecific = UnplayableReason.NoLivingAllies | UnplayableReason.EnergyCostTooHigh
        | UnplayableReason.StarCostTooHigh | UnplayableReason.BlockedByHook;

    static MethodBase TargetMethod() => AccessTools.Method(
        AccessTools.TypeByName("MegaCrit.Sts2.Core.Entities.Cards.UnplayableReasonExtensions"), "GetPlayerDialogueLine");

    static void Postfix(UnplayableReason reason, ref LocString? __result)
    {
        if (UnplayableLines.Pending is { } line && reason.HasFlag(UnplayableReason.BlockedByCardLogic)
            && (reason & MoreSpecific) == 0 && line.Exists())
            __result = line;
        UnplayableLines.Pending = null;
    }
}
