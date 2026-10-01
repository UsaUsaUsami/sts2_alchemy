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
            var scene = ResourceLoader.Load<PackedScene>(SceneHelper.GetScenePath("creature_visuals/necrobinder"));
            var visuals = scene.Instantiate<NCreatureVisuals>(PackedScene.GenEditState.Disabled);
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

            await Capture(tree, viewport, body, "original", ["idle_loop", "attack", "hurt", "cast_mighty", "die"]);

            if (Swap(spine, data, atlas)) await Capture(tree, viewport, body, "swapped", ["idle_loop", "attack"]);
        }
        catch (Exception ex) { Log("ERROR " + ex); }
        Log("DONE");
        tree.Quit();
    }

    /// Rebuilds the skeleton data on a copy of the atlas whose first page is recoloured, to see whether a page can be
    /// replaced at runtime from a file outside the game's pack.
    private static bool Swap(GodotObject spine, GodotObject data, GodotObject atlas)
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
                if (i == 0)
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
            spine.Call("set_skeleton_data_res", newData);
            return true;
        }
        catch (Exception ex) { Log("SWAP ERROR " + ex); return false; }
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
