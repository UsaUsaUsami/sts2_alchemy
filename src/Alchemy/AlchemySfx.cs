using Godot;
using MegaCrit.Sts2.Core.Commands;

namespace Alchemy;

/// <summary>
/// The alchemist's own moments get a sound (2026-10-01, 残課題4). The mod ships no audio: every path is an FMOD event
/// the base game itself plays somewhere (found in the decompiled sts2.dll), so it is known to exist in the banks.
/// Which event fits which moment is a trial choice, chosen without listening; the user judges them in game.
/// </summary>
public static class AlchemySfx
{
    /// 相転移: one short chime on top of the destination effect's own sound (block, debuff, hit, draw).
    public const string PhaseShift = "event:/sfx/ui/enchant_simple";
    /// 炉の起動: the material lands in the box. The Regent's forge, the closest thing to a furnace.
    public const string Furnace = "event:/sfx/characters/regent/regent_forge";
    /// 錬成: a new card is made.
    public const string Craft = "event:/sfx/ui/cards/card_transform";
    /// 改造: the base game's sound for enchanting a card.
    public const string Modify = "event:/sfx/ui/enchant_simple";
    /// 付与: a rare material is inscribed, the more special enchant sound.
    public const string Inscribe = "event:/sfx/ui/enchant_shimmer";
    /// 調薬: the potion appears in its slot, which the game shows without a sound of its own.
    public const string Brew = "event:/sfx/heal";

    // Cosmetic only: a missing event or audio failure must never interrupt combat or the workshop.
    public static void Play(string sfx)
    {
        try { SfxCmd.Play(sfx); }
        catch (Exception ex) { GD.PushWarning($"[Alchemy] sound skipped ({sfx}): {ex.Message}"); }
    }
}
