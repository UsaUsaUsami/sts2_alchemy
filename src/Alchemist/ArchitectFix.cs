using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models.Events;

namespace Alchemist;

// TheArchitect is the run-win event. Its dialogue is looked up from a dictionary hardcoded to the base
// game's five characters (Ironclad/Silent/Defect/Necrobinder/Regent); for any other character
// LoadDialogue leaves its private Dialogue field null (Rng.NextItem on an empty list returns default).
// GenerateInitialOptions() null-checks Dialogue and offers a bare "Proceed" button, but WinRun()
// (run when that button is pressed) dereferences Dialogue.EndAttackers unconditionally, which throws a
// NullReferenceException for an unlisted character and never calls RunManager.WinRun().
//
// The fallback must NOT be set in LoadDialogue: OnRoomEnter() clears the current options whenever
// Dialogue has lines, expecting PlayCurrentLine() to put them back, but PlayCurrentLine() returns early
// when the line has no LineText (a hand-made fallback line never gets loc keys). That left the event with
// no buttons at all and the game stuck (v0.12.1 bug). So Dialogue stays null through room entry - the
// base game's own null path shows "Proceed" - and the fallback is only supplied right before WinRun()
// reads it. EndAttackers.Both matches most of the base game's own characters.
[HarmonyPatch(typeof(TheArchitect), "WinRun")]
public static class ArchitectMissingCharacterPatch
{
    public static void Prefix(TheArchitect __instance)
    {
        var field = AccessTools.Field(typeof(TheArchitect), "_dialogue");
        if (field.GetValue(__instance) is not null) return;
        field.SetValue(__instance, new AncientDialogue("") { EndAttackers = ArchitectAttackers.Both });
    }
}
