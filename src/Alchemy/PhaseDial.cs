using Alchemy.Core;
using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace Alchemy;

/// <summary>
/// The current phase at a glance. 2026-10-03 (user: the first dial was too plain): the four elements as coloured
/// orbs on a ring, top, right, bottom, left in the PhaseWheel order earth, water, fire, air, joined by arrows in that
/// order like a four-way cycle; in front of them the alchemist's face sigil (a large ring and triangle); in the middle
/// the number of transitions this turn as a Roman numeral (0, I, II, III, ...). The current phase's orb glows, the
/// others stay dark; the number pops when it goes up. Shown under the material strip in the alchemist's combats only.
/// Each orb carries a small face sigil (2026-10-03, user; the element marks before).
/// </summary>
public static class PhaseDial
{
    public const float Size = 150;
    private const float OrbRing = 0.78f, OrbRadius = 21;
    /// Under the material button and its material strip (MaterialStrip), top left below the relic row (2026-10-03,
    /// user); it moves down when the strip gains its rare row. 1920x1080 layout; the HUD scales with the window.
    public static Vector2 Position => new(WorkshopUi.LauncherPosition.X + 75, MaterialStrip.Bottom + 6);
    private static readonly AlchemyPhase[] Order = [AlchemyPhase.Earth, AlchemyPhase.Water, AlchemyPhase.Fire, AlchemyPhase.Air];
    private static readonly Color Gold = new("f0c860"), GoldDim = new("a89870"), Back = new(0.055f, 0.07f, 0.095f, 0.92f);
    private static Control? dial;
    private static Label? numeral;
    private static AlchemyPhase shown = AlchemyPhase.None;
    private static int shownCount = -1;

    public static Control? Node => GodotObject.IsInstanceValid(dial) ? dial : null;
    public static AlchemyPhase Shown => shown;
    public static string Numeral => numeral?.Text ?? "";

    public static Color Colour(AlchemyPhase phase) => phase switch
    {
        AlchemyPhase.Earth => new Color("9a6532"),
        AlchemyPhase.Water => new Color("2f6fd6"),
        AlchemyPhase.Fire => new Color("d8402f"),
        AlchemyPhase.Air => new Color("8fd6f2"),
        _ => new Color("d8c08a"),
    };

    public static string Roman(int n)
    {
        if (n <= 0) return "0";
        var (values, symbols) = (new[] { 10, 9, 5, 4, 1 }, new[] { "X", "IX", "V", "IV", "I" });
        var text = "";
        for (int i = 0; i < values.Length; i++)
            while (n >= values[i]) { text += symbols[i]; n -= values[i]; }
        return text;
    }

    /// Called every frame from the run node: builds the dial once, shows it only in combat, follows the phase and
    /// this turn's transitions.
    public static void Update(NRun run, MaterialBox? box)
    {
        var combat = box?.Combat;
        if (combat is null)
        {
            if (Node is { } hidden) hidden.Visible = false;
            return;
        }
        if (Node is null) Build(run);
        dial!.Visible = true;
        dial.Position = Position;
        int count = combat.Phases.TransitionsThisTurn;
        if (count != shownCount)
        {
            bool up = count > shownCount && shownCount >= 0;
            shownCount = count;
            numeral!.Text = Roman(count);
            numeral.AddThemeFontSizeOverride("font_size", numeral.Text.Length >= 3 ? 26 : 34);
            if (up)
            {
                numeral.Scale = new Vector2(1.5f, 1.5f);
                numeral.CreateTween().TweenProperty(numeral, "scale", Vector2.One, 0.35).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            }
        }
        var phase = combat.Phases.Current;
        if (phase == shown) return;
        shown = phase;
        dial.TooltipText = $"現在相：{MaterialBox.PhaseName(phase)}\nこのターンの相転移：{count}回";
        dial.QueueRedraw();
    }

    private static Vector2 Centre => new Vector2(Size, Size) / 2;
    private static float Radius => Size / 2 - 4;
    private static Vector2 OrbAt(int i)
    {
        float a = -Mathf.Pi / 2 + i * Mathf.Pi / 2;
        return Centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Radius * OrbRing;
    }

    private static void Build(NRun run)
    {
        shown = AlchemyPhase.None;
        shownCount = -1;
        dial = new Control { Position = Position, Size = new Vector2(Size, Size), MouseFilter = Control.MouseFilterEnum.Pass, Name = "AlchemyPhaseDial" };
        // The orbs, then the large sigil drawn over them by a second canvas item in front.
        var under = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Size = dial.Size };
        under.Draw += () => PaintUnder(under);
        dial.AddChild(under);
        var front = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Size = dial.Size };
        front.Draw += () => PaintFront(front);
        dial.AddChild(front);
        numeral = new Label
        {
            Text = "0", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        numeral.Size = new Vector2(60, 46);
        numeral.Position = Centre - numeral.Size / 2;
        numeral.PivotOffset = numeral.Size / 2;
        numeral.AddThemeFontSizeOverride("font_size", 34);
        numeral.AddThemeColorOverride("font_color", new Color("fff0c8"));
        numeral.AddThemeColorOverride("font_outline_color", new Color(0.1f, 0.06f, 0.02f));
        numeral.AddThemeConstantOverride("outline_size", 6);
        dial.AddChild(numeral);
        dial.Draw += () => { under.QueueRedraw(); front.QueueRedraw(); };
        run.AddChild(dial);
    }

    /// The disc, the cycle arrows between the orbs, and the orbs (the current one glowing).
    private static void PaintUnder(Control c)
    {
        c.DrawCircle(Centre, Radius, Back);
        float ring = Radius * OrbRing;
        var arrow = new Color(0.82f, 0.74f, 0.55f, 0.75f);
        for (int i = 0; i < 4; i++)
        {
            float a0 = -Mathf.Pi / 2 + i * Mathf.Pi / 2 + 0.42f, a1 = -Mathf.Pi / 2 + (i + 1) * Mathf.Pi / 2 - 0.42f;
            c.DrawArc(Centre, ring, a0, a1, 16, arrow, 2.2f, true);
            var tip = Centre + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * ring;
            var along = new Vector2(-Mathf.Sin(a1), Mathf.Cos(a1));
            var side = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
            c.DrawColoredPolygon([tip + along * 6, tip - along * 3 + side * 5, tip - along * 3 - side * 5], arrow);
        }
        for (int i = 0; i < 4; i++)
        {
            var phase = Order[i];
            var at = OrbAt(i);
            var colour = Colour(phase);
            bool on = phase == shown;
            if (on)
                for (int k = 6; k >= 1; k--)
                    c.DrawCircle(at, OrbRadius + k * 3.2f, new Color(colour, 0.06f + 0.02f * (6 - k)));
            c.DrawCircle(at, OrbRadius, on ? colour : colour.Darkened(0.55f));
            c.DrawArc(at, OrbRadius, 0, Mathf.Tau, 32, on ? Gold : new Color("6e5f3c"), 2f, true);
            // In each orb the alchemist's face sigil (2026-10-03, user, in place of the element marks).
            Sigil(c, at, OrbRadius - 5, on ? new Color("fff0b8") : new Color("c8b07a", 0.55f), on ? 1.8f : 1.4f);
        }
    }

    /// The face sigil from the key visual, small: ring, triangle, inner circle and dot.
    private static void Sigil(Control c, Vector2 at, float r, Color colour, float width)
    {
        c.DrawArc(at, r, 0, Mathf.Tau, 32, colour, width, true);
        var tri = new Vector2[4];
        for (int k = 0; k < 3; k++)
        {
            float a = -Mathf.Pi / 2 + k * Mathf.Tau / 3;
            tri[k] = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * 0.86f;
        }
        tri[3] = tri[0];
        c.DrawPolyline(tri, colour, width * 0.9f, true);
        c.DrawArc(at, r * 0.36f, 0, Mathf.Tau, 24, colour, width * 0.8f, true);
        c.DrawCircle(at, r * 0.12f, colour);
    }

    /// The face sigil in front: the large ring, the triangle through the ring, and the inner ring round the numeral.
    private static void PaintFront(Control c)
    {
        var gold = shown == AlchemyPhase.None ? GoldDim : Gold;
        float r = Radius - 1;
        c.DrawArc(Centre, r, 0, Mathf.Tau, 72, gold, 2.6f, true);
        var tri = new Vector2[4];
        for (int k = 0; k < 3; k++)
        {
            float a = -Mathf.Pi / 2 + k * Mathf.Tau / 3;
            tri[k] = Centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }
        tri[3] = tri[0];
        c.DrawPolyline(tri, gold, 2.2f, true);
        c.DrawCircle(Centre, 27, Back with { A = 1 });
        c.DrawArc(Centre, 27, 0, Mathf.Tau, 48, gold, 2.2f, true);
        c.DrawArc(Centre, 23, 0, Mathf.Tau, 48, gold with { A = 0.55f }, 1f, true);
    }
}
