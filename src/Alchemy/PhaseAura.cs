using Alchemy.Core;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Alchemy;

/// <summary>
/// The current phase on the battlefield (user, 2026-10-02: "like the Watcher's stances in StS1"): the screen's edges
/// glow in the phase's colour, and particles rise around the alchemist: earth, dust and grit drifting up slowly;
/// water, bubbles; fire, embers flickering up fast; air, streaks blowing past. A soft glow sits behind the alchemist.
/// Everything fades across a change of phase, and is gone in the neutral phase and outside combat.
/// The edge glow lies over the battlefield but under the combat UI and cards; the glow and particles are the first
/// child of the alchemist's creature node, so they sit behind the body. Nothing takes mouse input.
/// </summary>
public static class PhaseAura
{
    private const string LayerName = "AlchemyPhaseAura";
    private static Control? vignette;
    private static ShaderMaterial? vignetteMaterial;
    private static Node2D? around;
    private static Sprite2D? glow;
    private static readonly Dictionary<AlchemyPhase, CpuParticles2D> particles = [];
    private static AlchemyPhase shown = AlchemyPhase.None;
    private static Tween? fade;

    public static AlchemyPhase Shown => shown;
    public static Control? Vignette => GodotObject.IsInstanceValid(vignette) ? vignette : null;

    private const string VignetteShader = """
        shader_type canvas_item;
        uniform vec4 tint : source_color = vec4(1.0);
        uniform float strength = 0.0;
        void fragment() {
            vec2 d = abs(UV - 0.5) * 2.0;
            // Rounded-rectangle distance to the edge: 0 in the middle, 1 at the border.
            float edge = pow(pow(d.x, 4.0) + pow(d.y, 4.0), 0.25);
            float glow = smoothstep(0.72, 1.08, edge);
            float pulse = 0.85 + 0.15 * sin(TIME * 2.2);
            COLOR = vec4(tint.rgb, tint.a * glow * strength * pulse);
        }
        """;

    /// Called every frame from the run node, as the dial is.
    public static void Update(MaterialBox? box)
    {
        var combat = box?.Combat;
        var room = NCombatRoom.Instance;
        var phase = combat?.Phases.Current ?? AlchemyPhase.None;
        if (combat is null || room is null || !GodotObject.IsInstanceValid(room))
        {
            shown = AlchemyPhase.None;
            return;
        }
        var creature = room.GetCreatureNode(box!.Owner.Creature);
        if (creature is null || !GodotObject.IsInstanceValid(creature)) return;
        if (Vignette is null || vignette!.GetParent() != room || !GodotObject.IsInstanceValid(around) || around!.GetParent() != creature)
            Build(room, creature);
        if (phase == shown) return;
        Show(phase);
    }

    private static void Build(NCombatRoom room, Node creature)
    {
        if (GodotObject.IsInstanceValid(vignette)) vignette!.QueueFree();
        if (GodotObject.IsInstanceValid(around)) around!.QueueFree();
        particles.Clear();
        shown = AlchemyPhase.None;
        vignetteMaterial = new ShaderMaterial { Shader = new Shader { Code = VignetteShader } };
        vignette = new ColorRect { Name = LayerName, Material = vignetteMaterial, MouseFilter = Control.MouseFilterEnum.Ignore, Color = Colors.White };
        vignette.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        // Over the battlefield (the scene container: background and creatures) but under the combat UI and cards.
        room.AddChild(vignette);
        room.MoveChild(vignette, 1);
        // The glow and particles go under the alchemist's body: first child of its creature node.
        around = new Node2D { Name = LayerName + "Around" };
        creature.AddChild(around);
        creature.MoveChild(around, 0);
        glow = new Sprite2D { Texture = SoftDot(256), Scale = new Vector2(1.6f, 2.6f), Position = new Vector2(0, -170), Modulate = new Color(1, 1, 1, 0) };
        around.AddChild(glow);
        foreach (var phase in new[] { AlchemyPhase.Earth, AlchemyPhase.Water, AlchemyPhase.Fire, AlchemyPhase.Air })
        {
            var p = Particles(phase);
            around.AddChild(p);
            particles[phase] = p;
        }
        vignetteMaterial.SetShaderParameter("strength", 0f);
    }

    private static void Show(AlchemyPhase phase)
    {
        shown = phase;
        foreach (var (p, emitter) in particles) emitter.Emitting = p == phase;
        fade?.Kill();
        if (vignetteMaterial is null || glow is null) return;
        bool on = PhaseRules.IsElement(phase);
        var colour = PhaseDial.Colour(phase);
        if (on) vignetteMaterial.SetShaderParameter("tint", new Color(colour, 0.42f));
        fade = glow.CreateTween().SetParallel();
        fade.TweenMethod(Callable.From<float>(v => vignetteMaterial.SetShaderParameter("strength", v)),
            vignetteMaterial.GetShaderParameter("strength").AsSingle(), on ? 1f : 0f, 0.45);
        fade.TweenProperty(glow, "modulate", on ? new Color(colour.Lightened(0.15f), 0.5f) : new Color(colour, 0f), 0.45);
    }

    private static CpuParticles2D Particles(AlchemyPhase phase)
    {
        var p = new CpuParticles2D
        {
            Emitting = false, Texture = SoftDot(48), LocalCoords = false,
            EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle, EmissionRectExtents = new Vector2(120, 150),
            Position = new Vector2(0, -160), Gravity = Vector2.Zero,
        };
        // Earth's brown is too dark on the battlefield; its particles take a light ochre.
        var colour = phase == AlchemyPhase.Earth ? new Color("d9a066") : PhaseDial.Colour(phase);
        var ramp = new Gradient();
        ramp.SetColor(0, new Color(colour.Lightened(0.35f), 0f));
        ramp.SetColor(1, new Color(colour, 0f));
        ramp.AddPoint(0.2f, new Color(colour.Lightened(0.3f), 0.9f));
        p.ColorRamp = ramp;
        switch (phase)
        {
            case AlchemyPhase.Earth: // dust and grit drifting up, slowly, a little sideways
                p.Amount = 34; p.Lifetime = 2.6; p.Direction = new Vector2(0, -1); p.Spread = 25;
                p.InitialVelocityMin = 25; p.InitialVelocityMax = 55; p.ScaleAmountMin = 0.35f; p.ScaleAmountMax = 0.9f;
                p.Gravity = new Vector2(0, 8);
                break;
            case AlchemyPhase.Water: // bubbles rising and swaying
                p.Amount = 28; p.Lifetime = 2.4; p.Direction = new Vector2(0, -1); p.Spread = 12;
                p.InitialVelocityMin = 40; p.InitialVelocityMax = 80; p.ScaleAmountMin = 0.5f; p.ScaleAmountMax = 1.3f;
                p.TangentialAccelMin = -12; p.TangentialAccelMax = 12;
                break;
            case AlchemyPhase.Fire: // embers flickering up fast
                p.Amount = 50; p.Lifetime = 1.3; p.Direction = new Vector2(0, -1); p.Spread = 18;
                p.InitialVelocityMin = 100; p.InitialVelocityMax = 190; p.ScaleAmountMin = 0.3f; p.ScaleAmountMax = 0.7f;
                p.Gravity = new Vector2(0, -60); p.EmissionRectExtents = new Vector2(110, 90); p.Position = new Vector2(0, -90);
                break;
            case AlchemyPhase.Air: // streaks blowing past toward the enemies
                p.Amount = 30; p.Lifetime = 1.2; p.Direction = new Vector2(1, -0.1f); p.Spread = 6;
                p.InitialVelocityMin = 260; p.InitialVelocityMax = 380; p.ScaleAmountMin = 0.45f; p.ScaleAmountMax = 0.8f;
                p.ScaleAmountCurve = null;
                p.EmissionRectExtents = new Vector2(160, 170); p.Position = new Vector2(-80, -170);
                // Streaks: a long soft line turned along the motion.
                p.Texture = Streak(); p.ParticleFlagAlignY = true;
                break;
        }
        return p;
    }

    private static readonly Dictionary<int, Texture2D> dots = [];
    private static Texture2D? streak;

    /// A thin vertical line, soft at its ends and sides (the particles align it with their motion).
    private static Texture2D Streak()
    {
        if (streak is not null) return streak;
        const int w = 8, h = 72;
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float across = 1 - Math.Abs((x + 0.5f) / w * 2 - 1);
                float along = Mathf.Sin(Mathf.Pi * (y + 0.5f) / h);
                image.SetPixel(x, y, new Color(1, 1, 1, across * across * along));
            }
        return streak = ImageTexture.CreateFromImage(image);
    }

    /// A soft round dot (white, alpha falling off to the edge), tinted by the particles' colour ramp.
    private static Texture2D SoftDot(int size)
    {
        if (dots.TryGetValue(size, out var cached)) return cached;
        var g = new Gradient();
        g.SetColor(0, new Color(1, 1, 1, 1));
        g.SetColor(1, new Color(1, 1, 1, 0));
        var tex = new GradientTexture2D { Gradient = g, Width = size, Height = size, Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(0.5f, 0f) };
        return dots[size] = tex;
    }
}
