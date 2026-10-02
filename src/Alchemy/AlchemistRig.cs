using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Alchemy;

/// <summary>
/// The alchemist's own combat rig (2026-10-02, user: the repainted Necrobinder is too far from the character icon).
/// Parts drawn after the key visual (scripts/rig-parts.py, rig-cut.py) on a skeleton and animations written by
/// scripts/rig-build.py as Spine 4.2 JSON. It is loaded from plain files beside the DLL, as HelloSpire (MIT) loads its
/// rigs: SpineAtlasResource.load_from_atlas_file + SpineSkeletonFileResource.load_from_file. The skeleton file must
/// end in ".spine-json"; a ".json" is read as binary and refused (rig-probe, 2026-10-02).
/// The body sits in the Necrobinder's creature scene for its bounds and markers; that scene's own effects (head
/// fire, scythe sparks, slash) follow bones this rig does not have, so they are hidden.
/// Without the files, NecroRig (the repainted Necrobinder) is used.
/// </summary>
public static class AlchemistRig
{
    private const string DonorScene = "necrobinder";
    private static bool failed;

    public static string Directory => Path.Combine(CharacterArt.ArtDirectory, "rig", "alchemist");
    private static string AtlasPath => Path.Combine(Directory, "alchemist.atlas");
    private static string SkeletonPath => Path.Combine(Directory, "alchemist.spine-json");
    public static bool Available => !failed && File.Exists(AtlasPath) && File.Exists(SkeletonPath);

    /// The rig's animations (rig-build.py): idle_loop, relaxed_loop, attack, cast, hurt, die.
    public const string CastAnimation = "cast";

    private static readonly Dictionary<string, MegaSkeletonDataResource?> loaded = [];

    /// A rig written by our scripts: <dir>/<name>.atlas and <name>.spine-json, loaded once (null without the files
    /// or when loading failed; the failure is logged once).
    public static MegaSkeletonDataResource? LoadRig(string dir, string name)
    {
        string key = Path.Combine(dir, name);
        if (loaded.TryGetValue(key, out var cached)) return cached;
        string atlasPath = Path.Combine(dir, name + ".atlas"), skeletonPath = Path.Combine(dir, name + ".spine-json");
        MegaSkeletonDataResource? result = null;
        if (File.Exists(atlasPath) && File.Exists(skeletonPath))
        {
            try { result = Load(atlasPath, skeletonPath); }
            catch (Exception ex) { GD.PushWarning($"[Alchemy] rig {name} skipped: {ex}"); }
        }
        return loaded[key] = result;
    }

    /// The combat rig's skeleton data.
    public static MegaSkeletonDataResource? Data => failed ? null : LoadRig(Directory, "alchemist");

    /// Puts a rig on a base-game scene's Spine body (the merchant's or rest site's figure), scaled to that body's
    /// height, and plays `animation`. alignFront keeps the figure's front edge (toward the fire) where it was, so a
    /// wider rig (the alchemist with the golem) grows away from it. Returns false without the rig.
    public static bool Reskin(Node2D spine, string animation, MegaSkeletonDataResource? rig = null, bool alignFront = false)
    {
        if ((rig ?? Data) is not { } data) return false;
        var before = RestMerchantArt.FigureBounds(spine);
        var sprite = new MegaSprite(spine);
        sprite.SetSkeletonDataRes(data);
        spine.GetParent().RunWhenSpineReady(sprite, state =>
        {
            if (before is { } b && RestMerchantArt.FigureBounds(spine) is { } mine && mine.Size.Y > 1)
            {
                spine.Scale *= b.Size.Y / mine.Size.Y;
                if (alignFront && RestMerchantArt.FigureBounds(spine) is { } now)
                {
                    bool flipped = spine.Scale.X < 0;
                    float shift = flipped ? b.Position.X - now.Position.X : b.End.X - now.End.X;
                    spine.Position += new Vector2(shift, b.End.Y - now.End.Y);
                }
            }
            state.SetAnimation(animation, true);
        });
        return true;
    }

    public static NCreatureVisuals? CreateVisuals()
    {
        if (!Available) return null;
        try
        {
            if (Data is not { } data) return null;
            var scene = ResourceLoader.Load<PackedScene>(SceneHelper.GetScenePath("creature_visuals/" + DonorScene));
            var visuals = scene.Instantiate<NCreatureVisuals>(PackedScene.GenEditState.Disabled);
            var spine = visuals.GetNode<Node2D>("Visuals");
            new MegaSprite(spine).SetSkeletonDataRes(data);
            HideDonorEffects(spine);
            return visuals;
        }
        catch (Exception ex)
        {
            failed = true;
            GD.PushWarning($"[Alchemy] own rig skipped: {ex}");
            return null;
        }
    }

    private static MegaSkeletonDataResource Load(string atlasPath, string skeletonPath)
    {
        var atlas = (GodotObject)ClassDB.Instantiate("SpineAtlasResource");
        var err = atlas.Call("load_from_atlas_file", atlasPath).AsInt32();
        if (err != 0) throw new InvalidOperationException($"atlas {atlasPath}: {err}");
        var file = (GodotObject)ClassDB.Instantiate("SpineSkeletonFileResource");
        err = file.Call("load_from_file", skeletonPath).AsInt32();
        if (err != 0) throw new InvalidOperationException($"skeleton {skeletonPath}: {err}");
        var resource = (GodotObject)ClassDB.Instantiate("SpineSkeletonDataResource");
        resource.Set("atlas_res", atlas);
        resource.Set("skeleton_file_res", file);
        if (!resource.Call("is_skeleton_data_loaded").AsBool()) throw new InvalidOperationException("skeleton data did not load");
        GD.Print($"[Alchemy] rig loaded: {Path.GetFileName(skeletonPath)}");
        return new MegaSkeletonDataResource(resource);
    }

    /// The donor scene's slot and bone followers (head fire, scythe particles, slash) and loose effect sprites.
    /// The SpineMesh2D children are the rig's own drawing and stay.
    public static void HideDonorEffects(Node spine)
    {
        foreach (var child in spine.GetChildren())
        {
            // Transparent as well: the scene's flame script shows its fire again (rig-probe, 2026-10-02).
            if (child is CanvasItem item && child.GetClass() is "SpineSlotNode" or "SpineBoneNode" or "Sprite2D" or "GPUParticles2D")
            {
                item.Visible = false;
                item.Modulate = new Color(1, 1, 1, 0);
            }
        }
    }
}
