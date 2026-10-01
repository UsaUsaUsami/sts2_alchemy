using Alchemy.Core;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Alchemy;

/// <summary>
/// The HP a creature will lose to drain at its next turn start, shown on its health bar the way the base game shows
/// poison (user, 2026-10-01): a crimson segment at the right end of the remaining HP. Poison keeps the rightmost part
/// (the game draws it), drain sits just left of it, so the two read as one "lost next turn" stretch. When poison and
/// drain together are lethal, the whole bar is the drain colour. Doom is left as the game draws it.
/// The amount is DrainRules.Damage of the stacks, the same number LifeAxis.TriggerDrain deals.
/// </summary>
[HarmonyPatch(typeof(NHealthBar), "RefreshForeground")]
public static class DrainHealthBarPatch
{
    public const string NodeName = "AlchemyDrainForeground";
    public static readonly Color Colour = new("a3195b");
    private static readonly AccessTools.FieldRef<NHealthBar, Creature> CreatureRef = AccessTools.FieldRefAccess<NHealthBar, Creature>("_creature");
    private static readonly AccessTools.FieldRef<NHealthBar, Control> HpRef = AccessTools.FieldRefAccess<NHealthBar, Control>("_hpForeground");
    private static readonly System.Reflection.MethodInfo FgWidth = AccessTools.Method(typeof(NHealthBar), "GetFgWidth", [typeof(int)]);
    private static readonly System.Reflection.MethodInfo MaxWidth = AccessTools.PropertyGetter(typeof(NHealthBar), "MaxFgWidth");

    /// The drain segment's node on this bar, if one was made.
    public static Control? Segment(NHealthBar bar) => HpRef(bar).GetParent()?.GetNodeOrNull<Control>(NodeName);

    public static void Postfix(NHealthBar __instance)
    {
        try { Refresh(__instance); }
        catch (Exception ex) { GD.PushWarning($"[Alchemy] drain health bar skipped: {ex.Message}"); }
    }

    private static void Refresh(NHealthBar bar)
    {
        var creature = CreatureRef(bar);
        var hp = HpRef(bar);
        int drain = creature is null ? 0 : DrainRules.Damage(creature.GetPower<LifeDrainPower>()?.Amount ?? 0);
        int poison = creature?.GetPower<PoisonPower>()?.CalculateTotalDamageNextTurn() ?? 0;
        int hpAfterPoison = creature is null ? 0 : Math.Max(0, creature.CurrentHp - poison);
        var segment = Segment(bar);
        if (creature is null || drain <= 0 || hpAfterPoison <= 0 || !hp.Visible || creature.HpDisplay.IsInfinite())
        {
            if (segment is not null) segment.Visible = false;
            return;
        }
        segment ??= Create(hp);
        float Width(int amount) => (float)FgWidth.Invoke(bar, [amount])!;
        float max = (float)MaxWidth.Invoke(bar, null)!;
        int after = Math.Max(0, hpAfterPoison - drain);
        segment.Visible = true;
        segment.OffsetRight = Width(hpAfterPoison) - max;
        if (after == 0)
        {
            segment.OffsetLeft = 0;
            hp.Visible = false;
        }
        else
        {
            int margin = segment is NinePatchRect patch ? patch.PatchMarginLeft : 0;
            float left = Width(after);
            segment.OffsetLeft = Math.Max(0f, left - margin);
            hp.OffsetRight = left - max;
        }
    }

    // A copy of the red HP bar, recoloured, placed right above it so the game's poison segment still draws on top.
    private static Control Create(Control hp)
    {
        var segment = (Control)hp.Duplicate();
        segment.Name = NodeName;
        segment.UniqueNameInOwner = false;
        segment.SelfModulate = Colour;
        foreach (var child in segment.GetChildren()) child.QueueFree();
        var parent = hp.GetParent();
        parent.AddChild(segment);
        parent.MoveChild(segment, hp.GetIndex() + 1);
        return segment;
    }
}
