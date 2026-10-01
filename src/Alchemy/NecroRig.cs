using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Alchemy;

/// <summary>
/// The alchemist's combat body (2026-10-01, user: "ネクロ転用が好ましい"): the Necrobinder's Spine rig and animations,
/// repainted as the alchemist. Nothing of the base game is shipped. At first use the Necrobinder's atlas pages are
/// read from the game, each part is recoloured from its own shading (a gradient map, Recipes), the mod's own drawings
/// (art/character/rig/*.png: the staff, the face sigil, the robe band) are laid on top, and the result is written to
/// user://alchemy_rig and loaded as a new atlas under the game's own skeleton. scripts/rig-probe.ps1 renders it.
/// The head fire is a shader and particles in the scene, recoloured to the brass of the magic circle (Flames).
/// </summary>
public static class NecroRig
{
    private const string SceneName = "creature_visuals/necrobinder";
    private static readonly string CacheDir = "user://alchemy_rig";
    private static GodotObject? built;
    private static bool failed;

    public static string RigArtDirectory => Path.Combine(CharacterArt.ArtDirectory, "rig");
    /// The rig is used when the mod's drawings are installed; without them the alchemist keeps the still sprite.
    public static bool Available => !failed && Directory.Exists(RigArtDirectory);

    /// A gradient from the part's darkest to its lightest shade, or Clear: the part is dropped and only the overlay drawn.
    private readonly record struct Recipe(Color Dark, Color Light, bool Clear = false, int Trim = 0);

    private static readonly Color Gold = new("e2b552");
    private static Recipe Cloth(int trim = 0) => new(new Color("0e0d11"), new Color("46444c"), Trim: trim);
    private static Recipe ClothBack => new(new Color("08070a"), new Color("2a2930"));
    private static Recipe ClothFlat => new(new Color("121115"), new Color("2c2a31"));
    private static Recipe Leather => new(new Color("0c0907"), new Color("3c2c22"));
    private static Recipe Belt => new(new Color("2c1a0e"), new Color("9a6a3c"));
    private static Recipe Brass => new(new Color("4a300c"), new Color("ffe09a"));
    private static Recipe Glow => new(new Color("6a4310"), new Color("fff0b8"));
    private static Recipe Face => new(new Color("08080a"), new Color("2b2930"));
    private static readonly Recipe Clear = new(default, default, Clear: true);

    private static Recipe? For(string part) => part switch
    {
        "skirt top" or "sleeve_l_extended" or "bottom sleeve" or "l sleeve top" => Cloth(trim: 3),
        "skirt back" or "l sleeve bottom" or "l shoulder bottom" or "bottom shoulder" or "l shoulder" => ClothBack,
        "skirt flap" => Cloth(),
        "chest" or "spine" => ClothFlat,
        "belts" or "belt top" => Belt,
        "head" => Face,
        "back flame neck" => new(new Color("0c0b0e"), Gold),
        "back_flame_temp" => new(new Color("1c1408"), new Color("6a4a18")),
        "scythe" or "scythe_glow" or "sythe_dissolve" => Clear,
        "thumb1" or "thumb2" or "thumb3" or "wrist" or "webbing" => Brass,
        "shadow" => null,
        _ when part.StartsWith("death/") => part switch
        {
            "death/skull" => Face,
            "death/skull shadow" or "death/ribcage shadow" => null,
            "death/skirt" or "death/pink sleeve" => Cloth(trim: 2),
            _ => ClothBack,
        },
        _ when part.Contains("glow") || part.Contains("flame") || part.StartsWith("attack_slash") => Glow,
        // Bones of the arms, hands, fingers, legs and feet: gloves and boots.
        _ => Leather,
    };

    public static NCreatureVisuals? CreateVisuals()
    {
        if (!Available) return null;
        try
        {
            var scene = ResourceLoader.Load<PackedScene>(SceneHelper.GetScenePath(SceneName));
            var visuals = scene.Instantiate<NCreatureVisuals>(PackedScene.GenEditState.Disabled);
            var spine = visuals.GetNode("Visuals");
            var original = spine.Get("skeleton_data_res").AsGodotObject();
            if (Build(original) is { } data) spine.Set("skeleton_data_res", data);
            Flames(visuals);
            return visuals;
        }
        catch (Exception ex)
        {
            failed = true;
            GD.PushWarning($"[Alchemy] necrobinder rig skipped: {ex}");
            return null;
        }
    }

    private static GodotObject? Build(GodotObject original)
    {
        if (built is not null) return built;
        var atlas = original.Get("atlas_res").AsGodotObject();
        string text = AtlasText(atlas);
        var textures = atlas.Get("textures").AsGodotArray();
        var pages = ParsePages(text);
        if (pages.Count != textures.Count) throw new InvalidOperationException($"atlas pages {pages.Count} != textures {textures.Count}");
        DirAccess.MakeDirRecursiveAbsolute(CacheDir);
        for (int i = 0; i < pages.Count; i++)
        {
            var image = textures[i].As<Texture2D>().GetImage();
            if (image.IsCompressed()) image.Decompress();
            image.Convert(Image.Format.Rgba8);
            foreach (var region in pages[i].Regions) Paint(image, region);
            image.SavePng($"{CacheDir}/{pages[i].Name}");
        }
        string atlasPath = ProjectSettings.GlobalizePath($"{CacheDir}/alchemist.atlas");
        File.WriteAllText(atlasPath, text);
        var newAtlas = (GodotObject)ClassDB.Instantiate("SpineAtlasResource");
        var err = newAtlas.Call("load_from_atlas_file", atlasPath).AsInt32();
        if (err != 0) throw new InvalidOperationException($"load_from_atlas_file {err}");
        var data = (GodotObject)ClassDB.Instantiate("SpineSkeletonDataResource");
        data.Set("skeleton_file_res", original.Get("skeleton_file_res"));
        data.Set("atlas_res", newAtlas);
        data.Set("default_mix", original.Get("default_mix"));
        data.Set("animation_mixes", original.Get("animation_mixes"));
        if (!data.Call("is_skeleton_data_loaded").AsBool()) throw new InvalidOperationException("skeleton data did not load");
        GD.Print($"[Alchemy] necrobinder rig repainted ({pages.Sum(p => p.Regions.Count)} parts)");
        return built = data;
    }

    /// The atlas text is not a property of SpineAtlasResource; the imported resource it came from is JSON with it.
    private static string AtlasText(GodotObject atlas)
    {
        var import = new ConfigFile();
        import.Load(atlas.Get("resource_path").AsString() + ".import");
        var json = Json.ParseString(Godot.FileAccess.GetFileAsString(import.GetValue("remap", "path").AsString())).AsGodotDictionary();
        return json["atlas_data"].AsString();
    }

    public sealed record Region(string Name, Rect2I Packed, bool Rotated);
    public sealed record Page(string Name, List<Region> Regions);

    /// Spine 4 atlas text: blank-line separated pages; a page's first line is its file, then "key:value" lines; a
    /// region is a name line followed by its own "key:value" lines.
    public static List<Page> ParsePages(string text)
    {
        var pages = new List<Page>();
        foreach (var block in text.Replace("\r", "").Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var page = new Page(lines[0].Trim(), []);
            string? name = null;
            Rect2I bounds = default;
            bool rotated = false;
            void Flush() { if (name is not null) page.Regions.Add(new Region(name, bounds, rotated)); }
            foreach (var line in lines.Skip(1))
            {
                int colon = line.IndexOf(':');
                if (colon < 0) { Flush(); name = line.Trim(); bounds = default; rotated = false; continue; }
                if (name is null) continue; // page settings
                string key = line[..colon].Trim(), value = line[(colon + 1)..].Trim();
                if (key == "bounds")
                {
                    var n = value.Split(',').Select(int.Parse).ToArray();
                    bounds = new Rect2I(n[0], n[1], n[2], n[3]);
                }
                else if (key == "rotate") rotated = value is "90" or "true";
            }
            Flush();
            pages.Add(page);
        }
        return pages;
    }

    /// Recolours one part in place. Works on the part turned upright (as authored), so overlays and trims are drawn
    /// the way the part is seen; a rotated part is turned back before it is written.
    private static void Paint(Image page, Region region)
    {
        if (For(region.Name) is not { } recipe) return;
        var stored = new Rect2I(region.Packed.Position,
            region.Rotated ? new Vector2I(region.Packed.Size.Y, region.Packed.Size.X) : region.Packed.Size);
        var part = page.GetRegion(stored);
        if (region.Rotated) part.Rotate90(ClockDirection.Clockwise);
        if (recipe.Clear) part.Fill(new Color(0, 0, 0, 0));
        else GradientMap(part, recipe);
        Overlay(part, region.Name);
        if (region.Rotated) part.Rotate90(ClockDirection.Counterclockwise);
        page.BlitRect(part, new Rect2I(Vector2I.Zero, part.GetSize()), stored.Position);
    }

    private static void GradientMap(Image part, Recipe recipe)
    {
        int w = part.GetWidth(), h = part.GetHeight();
        float lo = 1, hi = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var c = part.GetPixel(x, y);
                if (c.A < 0.5f) continue;
                float l = c.R * 0.299f + c.G * 0.587f + c.B * 0.114f;
                lo = Math.Min(lo, l); hi = Math.Max(hi, l);
            }
        float span = Math.Max(hi - lo, 0.05f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var c = part.GetPixel(x, y);
                if (c.A <= 0) continue;
                float t = Math.Clamp((c.R * 0.299f + c.G * 0.587f + c.B * 0.114f - lo) / span, 0, 1);
                var mapped = recipe.Dark.Lerp(recipe.Light, t);
                // A gold hem: opaque pixels within Trim of the part's lower edge.
                if (recipe.Trim > 0 && c.A > 0.5f && y + recipe.Trim < h + 1)
                {
                    bool nearEdge = false;
                    for (int k = 1; k <= recipe.Trim && !nearEdge; k++)
                        nearEdge = y + k >= h || part.GetPixel(x, y + k).A < 0.5f;
                    if (nearEdge) mapped = Gold.Lerp(new Color("fff0b8"), t * 0.5f);
                }
                part.SetPixel(x, y, new Color(mapped, c.A));
            }
    }

    /// The mod's own drawing for the part, if any: art/character/rig/<part name with / and spaces as _>.png, drawn
    /// upright at the part's packed size.
    private static void Overlay(Image part, string name)
    {
        string path = Path.Combine(RigArtDirectory, name.Replace('/', '_').Replace(' ', '_') + ".png");
        if (!File.Exists(path) || Image.LoadFromFile(path) is not { } over || over.IsEmpty()) return;
        over.Convert(Image.Format.Rgba8);
        if (over.GetSize() != part.GetSize()) over.Resize(part.GetWidth(), part.GetHeight(), Image.Interpolation.Bilinear);
        part.BlendRect(over, new Rect2I(Vector2I.Zero, over.GetSize()), Vector2I.Zero);
    }

    /// The head fire and the scythe/slash effects are blue and pink in the scene; they take the brass of the magic
    /// circle (user, 2026-10-01).
    private static void Flames(Node visuals)
    {
        foreach (var node in Descendants(visuals))
        {
            if (node is CanvasItem { Material: ShaderMaterial material } item && node.Name != "Visuals")
            {
                item.Material = Brassy(material);
            }
            // Spine slot nodes (the slash) carry their material as the slot's normal_material, not CanvasItem.Material.
            if (node.GetClass() == "SpineSlotNode" && node.Get("normal_material").AsGodotObject() is ShaderMaterial slotMaterial)
                node.Set("normal_material", Brassy(slotMaterial));
            if (node is GpuParticles2D { ProcessMaterial: ParticleProcessMaterial process } particles)
            {
                var copy = (ParticleProcessMaterial)process.Duplicate();
                copy.ColorInitialRamp = null;
                copy.Color = new Color("f0c860");
                particles.ProcessMaterial = copy;
            }
        }
    }

    private static ShaderMaterial Brassy(ShaderMaterial material)
    {
        var copy = (ShaderMaterial)material.Duplicate();
        foreach (var (param, colour) in new[] { ("OuterColor", new Color("f0c860")), ("InnerColor", new Color("1a1004")), ("ColorParameter", new Color("e2b552")) })
            if (copy.GetShaderParameter(param).VariantType == Variant.Type.Color) copy.SetShaderParameter(param, colour);
        return copy;
    }

    private static IEnumerable<Node> Descendants(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            yield return child;
            foreach (var deeper in Descendants(child)) yield return deeper;
        }
    }
}
