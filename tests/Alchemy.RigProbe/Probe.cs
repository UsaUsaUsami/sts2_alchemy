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
    private static int Zoom => int.TryParse(System.Environment.GetEnvironmentVariable("ALCHEMY_PROBE_ZOOM"), out var z) && z > 0 ? z : 1;
    private static string Out => System.Environment.GetEnvironmentVariable("ALCHEMY_PROBE_OUT") ?? "user://probe";

    [HarmonyPatch(typeof(OneTimeInitialization), nameof(OneTimeInitialization.ExecuteDeferred))]
    public static class Ready
    {
        public static void Postfix() => Callable.From(() => { _ = Run(); }).CallDeferred();
    }

    private static async Task Run()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        try
        {
            DirAccess.MakeDirRecursiveAbsolute(Out);
            var viewport = new SubViewport
            {
                // ALCHEMY_PROBE_ZOOM (default 1): a bigger frame and figure, to look at joints up close.
                Size = new Vector2I(700, 700) * Zoom, TransparentBg = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
                Disable3D = true, OwnWorld3D = true,
            };
            tree.Root.AddChild(viewport);
            string mode = System.Environment.GetEnvironmentVariable("ALCHEMY_PROBE_MODE") ?? "";
            // ALCHEMY_PROBE_RIG picks the rig donor (necrobinder / silent) for side-by-side comparisons.
            // ALCHEMY_PROBE_RIDERS: "slot,file,x,y,rotation,scale;..." to try pictures on the rig without rebuilding.
            if (System.Environment.GetEnvironmentVariable("ALCHEMY_PROBE_RIDERS") is { Length: > 0 } riders)
            {
                Alchemy.NecroRig.Riders.Clear();
                foreach (var spec in riders.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var f = spec.Split(',');
                    Alchemy.NecroRig.Riders.Add(new(f[0], f[1], new Vector2(float.Parse(f[2]), float.Parse(f[3])), float.Parse(f[4]), float.Parse(f[5])));
                }
            }
            if (System.Environment.GetEnvironmentVariable("ALCHEMY_PROBE_RIG") is { Length: > 0 } rigName)
                Alchemy.NecroRig.Donor = Enum.Parse<Alchemy.NecroRig.RigDonor>(rigName, ignoreCase: true);
            // "alchemist": the alchemist's own visuals, made the way combat makes them (CharacterModel.CreateVisuals).
            var visuals = mode == "alchemist"
                ? MegaCrit.Sts2.Core.Models.ModelDb.Character<Alchemy.AlchemistCharacter>().CreateVisuals()
                : ResourceLoader.Load<PackedScene>(SceneHelper.GetScenePath("creature_visuals/" + (System.Environment.GetEnvironmentVariable("ALCHEMY_PROBE_DONOR") is { Length: > 0 } donor ? donor : "necrobinder"))).Instantiate<NCreatureVisuals>(PackedScene.GenEditState.Disabled);
            visuals.Position = new Vector2(350, 620) * Zoom;
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

            if (mode == "api")
            {
                foreach (var cls in ClassDB.GetClassList().Where(c => c.StartsWith("Spine")).OrderBy(c => c))
                    Log($"class {cls}: " + string.Join(",", ClassDB.ClassGetMethodList(cls, true).Select(m => (string)m["name"])));
                foreach (var boneObj in data.Call("get_bones").AsGodotArray())
                {
                    var bone = boneObj.AsGodotObject();
                    var parent = bone.Call("get_parent").AsGodotObject();
                    Log($"bone {bone.Call("get_bone_name")} parent={parent?.Call("get_bone_name")} len={bone.Call("get_length")} sx={bone.Call("get_scale_x")} sy={bone.Call("get_scale_y")} x={bone.Call("get_x")} y={bone.Call("get_y")} rot={bone.Call("get_rotation")}");
                }
                foreach (var slotObj in data.Call("get_slots").AsGodotArray())
                {
                    var slot = slotObj.AsGodotObject();
                    Log($"slot {slot.Call("get_name")} bone={slot.Call("get_bone_data").AsGodotObject()?.Call("get_bone_name")} attachment={slot.Call("get_attachment_name")}");
                }
            }
            else if (mode == "alchemist")
            {
                visuals.Scale = Vector2.One * 1.25f * Zoom;
                DumpTree(visuals, 0);
                await Capture(tree, viewport, body, "alchemist", ["idle_loop", "attack", "hurt", Alchemy.AlchemistRig.Available ? Alchemy.AlchemistRig.CastAnimation : Alchemy.NecroRig.CastAnimation, "die"]);
            }
            else if (mode == "combat")
            {
                // A real alchemist run and battle (as the smoke loop starts one), the whole window captured once
                // per phase: for the HUD layout and the phase effects (PhaseAura).
                viewport.QueueFree();
                visuals.QueueFree();
                await MegaCrit.Sts2.Core.Assets.PreloadManager.LoadCommonAndMainMenuAssets();
                MegaCrit.Sts2.Core.Saves.SaveManager.Instance.SetFtuesEnabled(false);
                var player = MegaCrit.Sts2.Core.Entities.Players.Player.CreateForNewRun<Alchemy.AlchemistCharacter>(MegaCrit.Sts2.Core.Unlocks.UnlockState.all, 1);
                var run = MegaCrit.Sts2.Core.Runs.RunState.CreateForNewRun([player], MegaCrit.Sts2.Core.Models.ActModel.GetDefaultList().Select(a => a.ToMutable()).ToList(), [], MegaCrit.Sts2.Core.Runs.GameMode.Standard, 0, "ALCHEMY_PROBE_01");
                MegaCrit.Sts2.Core.Runs.RunManager.Instance.SetUpNewSingleplayer(run, false);
                await (Task)AccessTools.Method(typeof(MegaCrit.Sts2.Core.Nodes.NGame), "StartRun").Invoke(MegaCrit.Sts2.Core.Nodes.NGame.Instance, [run])!;
                await Frames(tree, 30);
                await MegaCrit.Sts2.Core.Runs.RunManager.Instance.EnterRoomDebug(MegaCrit.Sts2.Core.Rooms.RoomType.Monster,
                    model: MegaCrit.Sts2.Core.Models.ModelDb.Encounter<MegaCrit.Sts2.Core.Models.Encounters.BowlbugsWeak>().ToMutable(), showTransition: false);
                var box = player.GetRelic<Alchemy.MaterialBox>()!;
                for (int i = 0; i < 600 && (box.Combat is null || player.PlayerCombatState?.Hand.Cards.Count is not > 0); i++) await Frames(tree, 1);
                await Frames(tree, 30);
                // The main menu's early-access notice is still up in a fresh profile; take it down.
                MegaCrit.Sts2.Core.Saves.SaveManager.Instance.SettingsSave.SeenEaDisclaimer = true;
                foreach (var modal in tree.Root.FindChildren("*", "NEarlyAccessDisclaimer", true, false)) modal.QueueFree();
                if (MegaCrit.Sts2.Core.Nodes.CommonUi.NModalContainer.Instance is { } modals)
                    foreach (var child in modals.GetChildren()) child.QueueFree();
                await Frames(tree, 60);
                tree.Root.GetTexture().GetImage().SavePng($"{Out}/combat_none.png");
                if (MegaCrit.Sts2.Core.Nodes.Rooms.NCombatRoom.Instance is { } combatRoom)
                    foreach (var child in combatRoom.GetChildren())
                        Log($"room child {child.GetIndex()} {child.GetClass()} {child.Name} {(child is CanvasItem ci ? ci.ZIndex.ToString() : "")}");
                foreach (var phase in new[] { Alchemy.Core.AlchemyPhase.Earth, Alchemy.Core.AlchemyPhase.Water, Alchemy.Core.AlchemyPhase.Fire, Alchemy.Core.AlchemyPhase.Air })
                {
                    box.Combat!.Phases.Enter(phase);
                    await Frames(tree, 70);
                    var shot = tree.Root.GetTexture().GetImage();
                    shot.SavePng($"{Out}/combat_{phase.ToString().ToLowerInvariant()}.png");
                    shot.GetRegion(new Rect2I(250, 300, 500, 500)).SavePng($"{Out}/close_{phase.ToString().ToLowerInvariant()}.png");
                    Log($"captured combat {phase}");
                }
            }
            else if (mode == "golem")
            {
                // The golem as combat makes it (HomunculusPet), beside the alchemist for scale.
                visuals.QueueFree();
                visuals = MegaCrit.Sts2.Core.Models.ModelDb.Character<Alchemy.AlchemistCharacter>().CreateVisuals();
                viewport.AddChild(visuals);
                await Frames(tree, 3);
                body = visuals.SpineBody!;
                for (int i = 0; i < 120 && body.TryGetAnimationState() is null; i++) await Frames(tree, 1);
                visuals.Scale = Vector2.One * 1.25f * Zoom;
                visuals.Position = new Vector2(230, 620) * Zoom;
                var pet = MegaCrit.Sts2.Core.Models.ModelDb.Monster<Alchemy.HomunculusPet>().CreateCustomVisuals()!;
                pet.Scale = Vector2.One * 1.25f * Zoom;
                pet.Position = new Vector2(480, 620) * Zoom;
                viewport.AddChild(pet);
                await Frames(tree, 3);
                var petBody = pet.SpineBody ?? throw new Exception("golem has no spine body");
                for (int i = 0; i < 120 && petBody.TryGetAnimationState() is null; i++) await Frames(tree, 1);
                body.GetAnimationState().SetAnimation("idle_loop", loop: true);
                await Capture(tree, viewport, petBody, "golem", ["idle_loop", "attack", "cast", "hurt", "die", "dead_loop", "revive"]);
            }
            else if (mode == "rest")
            {
                // The rest site's figure: the Ironclad's scene Spine node alone (the scene script needs a run).
                var scene = ResourceLoader.Load<PackedScene>(SceneHelper.GetScenePath("rest_site/characters/ironclad_rest_site"));
                var root = scene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
                var spineNode = root.GetChildren().OfType<Node2D>().First(n => n.GetClass() == "SpineSprite");
                root.RemoveChild(spineNode);
                var holder = new Node2D { Position = new Vector2(600, 450) * Zoom, Scale = Vector2.One * 0.6f * Zoom };
                viewport.Size = new Vector2I(1200, 900) * Zoom;
                visuals.Visible = false;
                viewport.AddChild(holder);
                holder.AddChild(spineNode);
                var ironSprite = new MegaSprite(spineNode);
                holder.RunWhenSpineReady(ironSprite, s => s.SetAnimation("overgrowth_loop", true));
                await Frames(tree, 30);
                viewport.GetTexture().GetImage().SavePng($"{Out}/rest_ironclad.png");
                Log($"rest bounds ironclad {Alchemy.RestMerchantArt.FigureBounds(spineNode)} pos {spineNode.Position} scale {spineNode.Scale}");
                Log($"rest reskin {Alchemy.AlchemistRig.Reskin(spineNode, "overgrowth_loop", Alchemy.RestMerchantArt.RestRig, alignFront: true)}");
                for (int f = 0; f < 6; f++)
                {
                    await Frames(tree, 25);
                    viewport.GetTexture().GetImage().SavePng($"{Out}/rest_alchemist_{f}.png");
                }
                Log($"rest bounds alchemist {Alchemy.RestMerchantArt.FigureBounds(spineNode)} pos {spineNode.Position} scale {spineNode.Scale}");
            }
            else if (mode == "merchant")
            {
                // The merchant room's figure: the Ironclad's merchant scene as the game builds it, then reskinned.
                var scene = ResourceLoader.Load<PackedScene>(SceneHelper.GetScenePath("merchant/characters/ironclad_merchant"));
                var figure = scene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
                figure.Position = new Vector2(350, 600) * Zoom;
                figure.Scale = Vector2.One * Zoom;
                visuals.Visible = false;
                viewport.AddChild(figure);
                await Frames(tree, 30);
                viewport.GetTexture().GetImage().SavePng($"{Out}/merchant_ironclad.png");
                var spineNode = (Node2D)figure.GetChild(0);
                Log($"merchant reskin {Alchemy.AlchemistRig.Reskin(spineNode, "relaxed_loop")}");
                for (int f = 0; f < 4; f++)
                {
                    await Frames(tree, 20);
                    viewport.GetTexture().GetImage().SavePng($"{Out}/merchant_alchemist_{f}.png");
                }
            }
            else if (mode == "custom")
            {
                // A rig written by scripts (ALCHEMY_PROBE_SKEL: the .json or .skel; its .atlas beside it) on the
                // donor scene, the way HelloSpire loads its own rigs (plain files, no import).
                string skel = System.Environment.GetEnvironmentVariable("ALCHEMY_PROBE_SKEL") ?? "";
                var customAtlas = (GodotObject)ClassDB.Instantiate("SpineAtlasResource");
                Log($"custom atlas load {customAtlas.Call("load_from_atlas_file", Path.ChangeExtension(skel, ".atlas"))}");
                var file = (GodotObject)ClassDB.Instantiate("SpineSkeletonFileResource");
                foreach (var ext in new[] { ".spine-json", ".spjson" })
                    if (File.Exists(Path.ChangeExtension(skel, ext)))
                        Log($"custom skel try {ext} {((GodotObject)ClassDB.Instantiate("SpineSkeletonFileResource")).Call("load_from_file", Path.ChangeExtension(skel, ext))}");
                Log($"custom skel load {file.Call("load_from_file", skel)}");
                var customData = (GodotObject)ClassDB.Instantiate("SpineSkeletonDataResource");
                customData.Set("atlas_res", customAtlas);
                customData.Set("skeleton_file_res", file);
                Log($"custom loaded {customData.Call("is_skeleton_data_loaded")} animations: "
                    + string.Join(",", new MegaSkeletonDataResource(customData).GetAnimationNames()));
                body.SetSkeletonDataRes(new MegaSkeletonDataResource(customData));
                await Frames(tree, 3);
                visuals.Scale = Vector2.One * 1.25f * Zoom;
                var names = (System.Environment.GetEnvironmentVariable("ALCHEMY_PROBE_ANIMS") ?? "idle_loop,attack,hurt,cast,die").Split(',');
                await Capture(tree, viewport, body, "custom", names);
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
                string page = Alchemy.NecroRig.ParsePages(atlasText)[i].Name;
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

    /// The scene under the visuals: class, name, the slot or bone a Spine node follows, texture and shader parameters.
    private static void DumpTree(Node node, int depth)
    {
        var line = $"tree {new string(' ', depth * 2)}{node.GetClass()} {node.Name}";
        foreach (var prop in new[] { "slot_name", "bone_name" })
            if (node.Get(prop).VariantType == Variant.Type.String) line += $" {prop}={node.Get(prop)}";
        if (node is CanvasItem item)
        {
            line += $" visible={item.Visible} modulate={item.Modulate}";
            if (item.Material is ShaderMaterial sm)
                line += $" shader={sm.Shader?.ResourcePath} params=" + string.Join("|",
                    RenderingServer.GetShaderParameterList(sm.Shader!.GetRid()).Select(p => (string)p["name"]).Select(n => $"{n}:{sm.GetShaderParameter(n)}"));
        }
        if (node.Get("texture").AsGodotObject() is Texture2D tex) line += $" texture={tex.ResourcePath}";
        if (node.GetClass() == "SpineSlotNode" && node.Get("normal_material").AsGodotObject() is ShaderMaterial nm)
            line += $" normal_material={nm.Shader?.ResourcePath} params=" + string.Join("|",
                RenderingServer.GetShaderParameterList(nm.Shader!.GetRid()).Select(p => (string)p["name"]).Select(n => $"{n}:{nm.GetShaderParameter(n)}"));
        if (node is GpuParticles2D p) line += $" particles texture={p.Texture?.ResourcePath}";
        Log(line);
        foreach (var child in node.GetChildren()) DumpTree(child, depth + 1);
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
                // Where the riders' slot nodes are on the frame, to place pictures by frame pixels.
                foreach (var rider in viewport.FindChildren("Alchemist_*", owned: false).OfType<Node2D>())
                    Log($"rider {name}_{f} {rider.Name} transform={rider.GetGlobalTransform()}");
                // Bones near the staff's tip, in frame pixels, to see which one the drawn tip keeps to.
                if (body.BoundObject.Call("get_skeleton").AsGodotObject() is { } sk)
                    foreach (var boneName in new[] { "scythe_twist", "scythe_twist_counter", "scythe_vfx_attach_2", "scythe_slide" })
                        if (sk.Call("find_bone", boneName).AsGodotObject() is { } b)
                            Log($"bone {name}_{f} {boneName} at={b.Call("get_global_transform").AsTransform2D().Origin}");
                // The scythe part's mesh deform (the spin is drawn by deforming it, not by its bone).
                if (body.BoundObject.Call("get_skeleton").AsGodotObject()?.Call("find_slot", "scythe").AsGodotObject() is { } slot)
                {
                    var deform = slot.Call("get_deform").AsFloat32Array();
                    string range = deform.Length < 2 ? "" : $" x {Enumerable.Range(0, deform.Length / 2).Min(i => deform[2 * i]):0}..{Enumerable.Range(0, deform.Length / 2).Max(i => deform[2 * i]):0} y {Enumerable.Range(0, deform.Length / 2).Min(i => deform[2 * i + 1]):0}..{Enumerable.Range(0, deform.Length / 2).Max(i => deform[2 * i + 1]):0}";
                    Log($"deform {name}_{f} n={deform.Length}{range} attachment={slot.Call("get_attachment").AsGodotObject()?.Call("get_attachment_name")}");
                }
            }
            Log($"captured {tag} {name}");
        }
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int i = 0; i < count; i++) await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
    }
}
