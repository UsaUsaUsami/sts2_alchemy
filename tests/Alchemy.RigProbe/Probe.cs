using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Combat;

/// <summary>
/// Development probe, not shipped (scripts/rig-probe.ps1). Runs the game with rendering, puts a base game creature
/// rig on a transparent viewport, optionally rebuilds its skeleton data on a replacement atlas, plays animations and
/// saves frames as PNG so the result can be looked at instead of inferred from property values.
/// Output: ALCHEMY_PROBE_OUT (absolute directory). Log lines start with ALCHEMY_PROBE.
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class RigProbe
{
    public static void Initialize() => new Harmony("AlchemyRigProbe").PatchAll(Assembly.GetExecutingAssembly());
    private static void Log(string text) => GD.Print("ALCHEMY_PROBE " + text);
    private static string Out => System.Environment.GetEnvironmentVariable("ALCHEMY_PROBE_OUT") ?? "user://probe";

    [HarmonyPatch(typeof(OneTimeInitialization), nameof(OneTimeInitialization.ExecuteDeferred))]
    public static class Ready
    {
        public static void Postfix() => Callable.From(() => _ = Run()).CallDeferred();
    }

    private static async Task Run()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        try
        {
            DirAccess.MakeDirRecursiveAbsolute(Out);
            var viewport = new SubViewport
            {
                Size = new Vector2I(700, 700), TransparentBg = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
                Disable3D = true, OwnWorld3D = true,
            };
            tree.Root.AddChild(viewport);
            string mode = System.Environment.GetEnvironmentVariable("ALCHEMY_PROBE_MODE") ?? "";
            // "alchemist": the alchemist's own visuals, made the way combat makes them (CharacterModel.CreateVisuals).
            var visuals = mode == "alchemist"
                ? MegaCrit.Sts2.Core.Models.ModelDb.Character<Alchemy.AlchemistCharacter>().CreateVisuals()
                : ResourceLoader.Load<PackedScene>(SceneHelper.GetScenePath("creature_visuals/necrobinder")).Instantiate<NCreatureVisuals>(PackedScene.GenEditState.Disabled);
            visuals.Position = new Vector2(350, 620);
            viewport.AddChild(visuals);
            await Frames(tree, 3);
            var body = visuals.SpineBody ?? throw new Exception("no spine body");
            var spine = body.BoundObject;
            var data = spine.Call("get_skeleton_data_res").AsGodotObject();
            var atlas = data.Get("atlas_res").AsGodotObject();
            Log($"spine class {spine.GetClass()}, data {data.GetClass()}, atlas {atlas.GetClass()}");
            foreach (var cls in new[] { "SpineAtlasResource", "SpineSkeletonDataResource" })
                Log(cls + " methods: " + string.Join(",", ClassDB.ClassGetMethodList(cls, true).Select(m => (string)m["name"])));
            Log("atlas props: " + string.Join(",", atlas.GetPropertyList().Select(p => (string)p["name"])));
            Log("data props: " + string.Join(",", data.GetPropertyList().Select(p => (string)p["name"])));
            Log("animations: " + string.Join(",", new MegaSkeletonDataResource(data).GetAnimationNames()));

            if (mode == "alchemist")
            {
                visuals.Scale = Vector2.One * 1.25f;
                await Capture(tree, viewport, body, "alchemist", ["idle_loop", "attack", "hurt", "cast_mighty", "die"]);
            }
            else if (mode == "uvmap")
            {
                Swap(spine, data, atlas, recolour: false); // only to export the atlas text and pages for the scripts
                await UvMap(tree, viewport, visuals, body, spine);
            }
            else
            {
                await Capture(tree, viewport, body, "original", ["idle_loop", "attack", "hurt", "cast_mighty", "die"]);
                if (Swap(spine, data, atlas, recolour: true)) await Capture(tree, viewport, body, "swapped", ["idle_loop", "attack"]);
            }
        }
        catch (Exception ex) { Log("ERROR " + ex); }
        Log("DONE");
        tree.Quit();
    }

    /// Rebuilds the skeleton data on a copy of the atlas whose first page is recoloured, to see whether a page can be
    /// replaced at runtime from a file outside the game's pack.
    private static bool Swap(GodotObject spine, GodotObject data, GodotObject atlas, bool recolour)
    {
        try
        {
            // The atlas text is not exposed as a property; the imported resource (JSON with "atlas_data") is in the pack.
            string source = atlas.Get("resource_path").AsString();
            var import = new ConfigFile();
            import.Load(source + ".import");
            string imported = import.GetValue("remap", "path").AsString();
            var json = Json.ParseString(Godot.FileAccess.GetFileAsString(imported)).AsGodotDictionary();
            string atlasText = json["atlas_data"].AsString();
            Log($"atlas source {source} -> {imported}");
            var textures = atlas.Get("textures").AsGodotArray();
            Log($"atlas_data {atlasText.Length} chars, textures {textures.Count}");
            string dir = Out + "/rig";
            DirAccess.MakeDirRecursiveAbsolute(dir);
            for (int i = 0; i < textures.Count; i++)
            {
                var image = textures[i].As<Texture2D>().GetImage();
                if (image.IsCompressed()) image.Decompress();
                if (i == 0 && recolour)
                    for (int y = 0; y < image.GetHeight(); y++)
                        for (int x = 0; x < image.GetWidth(); x++)
                        {
                            var c = image.GetPixel(x, y);
                            image.SetPixel(x, y, new Color(c.G * 0.3f, c.R, c.B * 0.3f, c.A)); // green-shifted test page
                        }
                string page = i == 0 ? "necrobinder.png" : $"necrobinder_{i + 1}.png";
                image.SavePng($"{dir}/{page}");
            }
            File.WriteAllText(ProjectSettings.GlobalizePath($"{dir}/probe.atlas"), atlasText);
            var newAtlas = (GodotObject)ClassDB.Instantiate("SpineAtlasResource");
            var err = newAtlas.Call("load_from_atlas_file", $"{dir}/probe.atlas");
            Log($"load_from_atlas_file -> {err}, textures {newAtlas.Get("textures").AsGodotArray().Count}");
            var newData = (GodotObject)ClassDB.Instantiate("SpineSkeletonDataResource");
            newData.Set("skeleton_file_res", data.Get("skeleton_file_res"));
            newData.Set("atlas_res", newAtlas);
            Log($"new data loaded: {newData.Call("is_skeleton_data_loaded")}");
            if (recolour) spine.Call("set_skeleton_data_res", newData);
            return true;
        }
        catch (Exception ex) { Log("SWAP ERROR " + ex); return false; }
    }

    /// The same frozen pose twice: as drawn (pose.png) and with every pixel's atlas position on the first page encoded
    /// in its colour (uv.png: v = x*1024 + y + 1 in 24-bit RGB, 0 = nothing; the front-most part wins). scripts
    /// read the two together to know which atlas pixel shows where.
    private static async Task UvMap(SceneTree tree, SubViewport viewport, NCreatureVisuals visuals, MegaSprite body, GodotObject spine)
    {
        viewport.Size = new Vector2I(1400, 1400);
        visuals.Scale = Vector2.One * 3f;
        visuals.Position = new Vector2(700, 1250);
        for (int i = 0; i < 120 && body.TryGetAnimationState() is null; i++) await Frames(tree, 1);
        var state = body.GetAnimationState();
        state.SetAnimation("idle_loop");
        await Frames(tree, 2);
        state.SetTimeScale(0f);
        await Frames(tree, 4);
        viewport.GetTexture().GetImage().SavePng($"{Out}/pose.png");
        var page = body.BoundObject.Call("get_skeleton_data_res").AsGodotObject().Get("atlas_res").AsGodotObject()
            .Get("textures").AsGodotArray()[0].As<Texture2D>();
        var shader = new Shader { Code = """
            shader_type canvas_item;
            render_mode blend_disabled, unshaded;
            uniform vec2 page_size;
            void fragment() {
                vec4 t = texture(TEXTURE, UV);
                if (t.a < 0.5 || distance(1.0 / TEXTURE_PIXEL_SIZE, page_size) > 0.5) discard;
                vec2 px = floor(UV * page_size);
                float v = px.x * 1024.0 + px.y + 1.0;
                COLOR = vec4(floor(v / 65536.0) / 255.0, floor(mod(v, 65536.0) / 256.0) / 255.0, mod(v, 256.0) / 255.0, 1.0);
            }
            """ };
        var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("page_size", page.GetSize());
        Log($"page size {page.GetSize()}");
        body.SetNormalMaterial(material);
        await Frames(tree, 4);
        viewport.GetTexture().GetImage().SavePng($"{Out}/uv.png");
        Log("uv map saved");
    }

    private static async Task Capture(SceneTree tree, SubViewport viewport, MegaSprite body, string tag, string[] animations)
    {
        for (int i = 0; i < 120 && body.TryGetAnimationState() is null; i++) await Frames(tree, 1);
        foreach (var name in animations)
        {
            body.GetAnimationState().SetAnimation(name, loop: name.EndsWith("_loop"));
            for (int f = 0; f < 6; f++)
            {
                await Frames(tree, 8);
                viewport.GetTexture().GetImage().SavePng($"{Out}/{tag}_{name}_{f}.png");
            }
            Log($"captured {tag} {name}");
        }
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int i = 0; i < count; i++) await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
    }
}
