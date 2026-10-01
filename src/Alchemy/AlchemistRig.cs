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
    private static MegaSkeletonDataResource? data;
    private static bool failed;

    public static string Directory => Path.Combine(CharacterArt.ArtDirectory, "rig", "alchemist");
    private static string AtlasPath => Path.Combine(Directory, "alchemist.atlas");
    private static string SkeletonPath => Path.Combine(Directory, "alchemist.spine-json");
    public static bool Available => !failed && File.Exists(AtlasPath) && File.Exists(SkeletonPath);

    /// The rig's animations (rig-build.py): idle_loop, attack, cast, hurt, die.
    public const string CastAnimation = "cast";

    public static NCreatureVisuals? CreateVisuals()
    {
        if (!Available) return null;
        try
        {
            data ??= Load();
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

    private static MegaSkeletonDataResource Load()
    {
        var atlas = (GodotObject)ClassDB.Instantiate("SpineAtlasResource");
        var err = atlas.Call("load_from_atlas_file", AtlasPath).AsInt32();
        if (err != 0) throw new InvalidOperationException($"atlas {AtlasPath}: {err}");
        var file = (GodotObject)ClassDB.Instantiate("SpineSkeletonFileResource");
        err = file.Call("load_from_file", SkeletonPath).AsInt32();
        if (err != 0) throw new InvalidOperationException($"skeleton {SkeletonPath}: {err}");
        var resource = (GodotObject)ClassDB.Instantiate("SpineSkeletonDataResource");
        resource.Set("atlas_res", atlas);
        resource.Set("skeleton_file_res", file);
        if (!resource.Call("is_skeleton_data_loaded").AsBool()) throw new InvalidOperationException("skeleton data did not load");
        GD.Print("[Alchemy] own rig loaded");
        return new MegaSkeletonDataResource(resource);
    }

    /// The donor scene's slot and bone followers (head fire, scythe particles, slash) and loose effect sprites.
    /// The SpineMesh2D children are the rig's own drawing and stay.
    private static void HideDonorEffects(Node spine)
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
