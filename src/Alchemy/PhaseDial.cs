using Alchemy.Core;
using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace Alchemy;

/// <summary>
/// The current phase at a glance (user, 2026-10-01): a round dial with the alchemist's face sigil in the middle and
/// the four element marks on the circle at angles of n·π/2, clockwise from the top in the PhaseWheel order earth,
/// water, fire, air. Each mark sits on its own quarter of the disc in its colour (earth brown, water blue, fire red,
/// air light blue); the current phase's quarter and mark light up, the others stay dim. In the neutral phase all four
/// are dim. Shown under the material button during the alchemist's combats only. The marks are art/icons/phase_*.ctex (scripts/generate-icons.py materials); without them, plain dots.
/// </summary>
public static class PhaseDial
{
    public const float Size = 128;
    /// Under the material button and its material strip (MaterialStrip), top left below the relic row (2026-10-03,
    /// user); it moves down when the strip gains its rare row. 1920x1080 layout; the HUD scales with the window.
    public static Vector2 Position => new(WorkshopUi.LauncherPosition.X + 86, MaterialStrip.Bottom + 8);
    private static readonly AlchemyPhase[] Order = [AlchemyPhase.Earth, AlchemyPhase.Water, AlchemyPhase.Fire, AlchemyPhase.Air];
    private static Control? dial;
    private static readonly Dictionary<AlchemyPhase, TextureRect> marks = [];
    private static AlchemyPhase shown = AlchemyPhase.None;

    public static Control? Node => GodotObject.IsInstanceValid(dial) ? dial : null;
    public static AlchemyPhase Shown => shown;

    public static Color Colour(AlchemyPhase phase) => phase switch
    {
        AlchemyPhase.Earth => new Color("9a6532"),
        AlchemyPhase.Water => new Color("2f6fd6"),
        AlchemyPhase.Fire => new Color("d8402f"),
        AlchemyPhase.Air => new Color("8fd6f2"),
        _ => new Color("d8c08a"),
    };

    /// Called every frame from the run node: builds the dial once, shows it only in combat, follows the phase.
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
        var phase = combat.Phases.Current;
        if (phase == shown) return;
        shown = phase;
        dial.TooltipText = $"現在相：{MaterialBox.PhaseName(phase)}";
        foreach (var (p, mark) in marks)
        {
            bool on = p == phase;
            mark.Modulate = on ? Colors.White : new Color(0.5f, 0.5f, 0.5f, 0.75f);
            mark.Scale = Vector2.One;
            if (on)
            {
                // A short pop on the newly entered phase, as feedback for the transition.
                mark.Scale = new Vector2(1.45f, 1.45f);
                mark.CreateTween().TweenProperty(mark, "scale", Vector2.One, 0.35).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            }
        }
        dial.QueueRedraw();
    }

    private static void Build(NRun run)
    {
        marks.Clear();
        shown = AlchemyPhase.None;
        dial = new Control { Position = Position, Size = new Vector2(Size, Size), MouseFilter = Control.MouseFilterEnum.Pass, Name = "AlchemyPhaseDial" };
        dial.Draw += () => Paint(dial);
        var centre = new Vector2(Size, Size) / 2;
        const float markSize = 34, radius = 43;
        for (int i = 0; i < Order.Length; i++)
        {
            var phase = Order[i];
            float angle = -Mathf.Pi / 2 + i * Mathf.Pi / 2;
            var at = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            var mark = new TextureRect
            {
                Size = new Vector2(markSize, markSize), Position = at - new Vector2(markSize, markSize) / 2,
                PivotOffset = new Vector2(markSize, markSize) / 2, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = Control.MouseFilterEnum.Ignore,
                Modulate = new Color(0.5f, 0.5f, 0.5f, 0.75f),
            };
            if (IconArt.Big("phase_" + phase.ToString().ToLowerInvariant()) is { } path)
                mark.Texture = ResourceLoader.Load<Texture2D>(path);
            dial.AddChild(mark);
            marks[phase] = mark;
        }
        run.AddChild(dial);
    }

    private static void Paint(Control c)
    {
        var centre = new Vector2(Size, Size) / 2;
        float r = Size / 2 - 2;
        c.DrawCircle(centre, r, new Color(0.06f, 0.08f, 0.1f, 0.88f));
        // Quarters centred on each mark: dim colour, the current one bright.
        for (int i = 0; i < Order.Length; i++)
        {
            var phase = Order[i];
            float mid = -Mathf.Pi / 2 + i * Mathf.Pi / 2;
            var colour = Colour(phase);
            colour.A = phase == shown ? 0.8f : 0.22f;
            var points = new List<Vector2> { centre };
            for (int k = 0; k <= 12; k++)
            {
                float a = mid - Mathf.Pi / 4 + k * Mathf.Pi / 2 / 12;
                points.Add(centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (r - 3));
            }
            c.DrawColoredPolygon(points.ToArray(), colour);
            if (marks.TryGetValue(phase, out var mark) && mark.Texture is null)
                c.DrawCircle(mark.Position + mark.Size / 2, 10, phase == shown ? Colour(phase).Lightened(0.3f) : Colour(phase).Darkened(0.4f));
        }
        var gold = shown == AlchemyPhase.None ? new Color("a89870") : new Color("f0c860");
        c.DrawArc(centre, r, 0, Mathf.Tau, 64, gold, 2.5f, true);
        // The face sigil from the key visual: double ring, triangle, inner circle.
        var sigilBack = new Color(0.06f, 0.08f, 0.1f, 0.95f);
        c.DrawCircle(centre, 22, sigilBack);
        c.DrawArc(centre, 21, 0, Mathf.Tau, 48, gold, 2f, true);
        c.DrawArc(centre, 17, 0, Mathf.Tau, 48, gold, 1.2f, true);
        var tri = new Vector2[4];
        for (int k = 0; k < 3; k++)
        {
            float a = -Mathf.Pi / 2 + k * Mathf.Tau / 3;
            tri[k] = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 15;
        }
        tri[3] = tri[0];
        c.DrawPolyline(tri, gold, 1.6f, true);
        c.DrawArc(centre, 6.5f, 0, Mathf.Tau, 32, gold, 1.4f, true);
    }
}
