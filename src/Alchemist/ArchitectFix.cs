using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models.Events;

namespace Alchemist;

// TheArchitect is the run-win event. Its dialogue is looked up from a dictionary hardcoded to the base
// game's five characters (Ironclad/Silent/Defect/Necrobinder/Regent); for any other character
// LoadDialogue leaves its private Dialogue field null (Rng.NextItem on an empty list returns default).
// GenerateInitialOptions() null-checks Dialogue and still offers a bare "Proceed" button, but WinRun()
// (run when that button is pressed) dereferences Dialogue.EndAttackers unconditionally, which throws a
// NullReferenceException for an unlisted character and never calls RunManager.WinRun() - the run cannot
// be completed. This gives a neutral one-line, no-text fallback dialogue (EndAttackers.Both, matching
// most of the base game's own characters) whenever LoadDialogue leaves Dialogue null, so the field is
// never null by the time WinRun() runs. A single line still resolves IsOnLastLine immediately, so the
// player sees the same bare "Proceed" prompt as before; only the null Dialogue reference is fixed.
[HarmonyPatch(typeof(TheArchitect), "LoadDialogue")]
public static class ArchitectMissingCharacterPatch
{
    public static void Postfix(TheArchitect __instance)
    {
        var field = AccessTools.Field(typeof(TheArchitect), "_dialogue");
        if (field.GetValue(__instance) is not null) return;
        field.SetValue(__instance, new AncientDialogue("") { EndAttackers = ArchitectAttackers.Both });
    }
}
