using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace Alchemy;

/// <summary>
/// The alchemist at the rest site and the merchant (2026-10-01): the base game shows a Spine animation there, which
/// the mod borrows from the Ironclad. The Ironclad's skeleton is kept (the scene's hitbox and thought bubbles are
/// placed around it) but made transparent, and one still picture (scripts/rest-merchant-art.py) is fitted into its
/// figure bounds: same height, same bottom, same centre. Without the picture the Ironclad stays.
/// </summary>
public static class RestMerchantArt
{
    public const string NodeName = "AlchemistSceneArt";

    /// Puts `picture` over the scene's Spine figure under `root`; returns the new node, or null without a figure.
    /// alignFront: line up the picture's front edge (the side the figure faces, toward the rest site fire) with the
    /// Ironclad's instead of centring it, so a wider picture (the golem beside the alchemist) grows away from the fire.
    public static Sprite2D? Apply(Node2D root, Texture2D? picture, bool alignFront = false)
    {
        if (picture is null || root.GetChildren().OfType<Node2D>().FirstOrDefault(n => n.GetClass() == "SpineSprite") is not { } spine)
            return null;
        if (root.GetNodeOrNull<Sprite2D>(NodeName) is { } existing) return existing;
        var sprite = new Sprite2D { Name = NodeName, Texture = picture, Centered = false, Visible = false };
        root.AddChild(sprite);
        root.MoveChild(sprite, spine.GetIndex() + 1);
        // Transparent rather than hidden, so the skeleton keeps updating and its bounds stay readable.
        spine.SelfModulate = new Color(1, 1, 1, 0);
        root.RunWhenSpineReady(new MegaSprite(spine), _ => Fit(sprite, spine, alignFront));
        return sprite;
    }

    /// The figure's box in the root's coordinates, as the base game computes it in NMonsterDeathVfx.
    public static Rect2? FigureBounds(Node2D spine)
    {
        var skeleton = new MegaSprite(spine).GetSkeleton();
        if (skeleton is null) return null;
        var b = skeleton.GetBounds();
        var a = spine.Position + b.Position * spine.Scale;
        var c = spine.Position + b.End * spine.Scale;
        var min = new Vector2(Math.Min(a.X, c.X), Math.Min(a.Y, c.Y));
        var max = new Vector2(Math.Max(a.X, c.X), Math.Max(a.Y, c.Y));
        return max.Y - min.Y > 1 ? new Rect2(min, max - min) : null;
    }

    private static void Fit(Sprite2D sprite, Node2D spine, bool alignFront)
    {
        if (!GodotObject.IsInstanceValid(sprite) || sprite.Texture is not { } tex) return;
        var box = FigureBounds(spine) ?? new Rect2(-150, -330, 300, 330);
        float scale = box.Size.Y / tex.GetHeight();
        sprite.Scale = new Vector2(spine.Scale.X < 0 ? -scale : scale, scale);
        float width = tex.GetWidth() * scale;
        bool flipped = spine.Scale.X < 0;
        float centreX = box.Position.X + box.Size.X / 2;
        // A mirrored sprite draws leftward from its position, so its position is its right edge.
        float left = alignFront ? (flipped ? box.Position.X : box.End.X - width) : centreX - width / 2;
        sprite.Position = new Vector2(flipped ? left + width : left, box.End.Y - box.Size.Y);
        sprite.Visible = true;
        GD.Print($"[Alchemy] scene art fitted to {box} (scale {scale:0.###})");
    }

    public static bool IsAlchemist(Player? player) => player?.Character is AlchemistCharacter;
}

[HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._Ready))]
public static class AlchemistRestSitePatch
{
    public static void Postfix(NRestSiteCharacter __instance)
    {
        if (!RestMerchantArt.IsAlchemist(__instance.Player)) return;
        try { RestMerchantArt.Apply(__instance, CharacterArt.RestSite, alignFront: true); }
        catch (Exception ex) { GD.PushWarning($"[Alchemy] rest site art skipped: {ex.Message}"); }
    }
}

// FlipX mirrors the Spine nodes for every second player; the picture follows on its next fit.
[HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.FlipX))]
public static class AlchemistRestSiteFlipPatch
{
    public static void Postfix(NRestSiteCharacter __instance)
    {
        if (__instance.GetNodeOrNull<Sprite2D>(RestMerchantArt.NodeName) is { } sprite)
        {
            sprite.Scale = new Vector2(-sprite.Scale.X, sprite.Scale.Y);
            sprite.Position = new Vector2(-sprite.Position.X, sprite.Position.Y);
        }
    }
}

[HarmonyPatch(typeof(NMerchantRoom), "AfterRoomIsLoaded")]
public static class AlchemistMerchantPatch
{
    public static void Postfix(NMerchantRoom __instance)
    {
        try
        {
            // The room adds one visual per player, in the order of its (reordered) player list.
            var players = Traverse.Create(__instance).Field<List<Player>>("_players").Value;
            var visuals = __instance.PlayerVisuals;
            for (int i = 0; i < Math.Min(players.Count, visuals.Count); i++)
                if (RestMerchantArt.IsAlchemist(players[i])) RestMerchantArt.Apply(visuals[i], CharacterArt.Merchant);
        }
        catch (Exception ex) { GD.PushWarning($"[Alchemy] merchant art skipped: {ex.Message}"); }
    }
}
