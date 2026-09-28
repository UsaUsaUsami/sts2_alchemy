using Godot;
using MegaCrit.Sts2.Core.Models;

namespace Alchemy;

/// <summary>
/// Card art shipped as plain PNG files next to the mod's DLL (art/cards/&lt;slug&gt;.png) and loaded at runtime, so
/// no Godot .pck export is needed (v0.24). A card without a file keeps its borrowed base-game art. The slug is the
/// card id without the mod prefix, lower case, like the base game's own portrait files (strike_ironclad.png).
/// Files are produced by scripts/art-prompts.py (what to draw) and scripts/import-art.py (size and name).
/// </summary>
public static class CardArt
{
    public const string Folder = "art/cards";
    private static readonly Dictionary<string, Texture2D?> Loaded = [];

    public static string ArtDirectory
        => Path.Combine(Path.GetDirectoryName(typeof(CardArt).Assembly.Location) ?? "", "art", "cards");

    public static string Slug(CardModel card)
    {
        string entry = card.Id.Entry;
        int dash = entry.IndexOf('-');
        return (dash >= 0 ? entry[(dash + 1)..] : entry).ToLowerInvariant();
    }

    /// The card's own art, or null when there is no file for it (the caller then keeps its fallback).
    public static Texture2D? For(CardModel card)
    {
        string slug = Slug(card);
        if (Loaded.TryGetValue(slug, out var cached)) return cached;
        Texture2D? texture = null;
        string path = Path.Combine(ArtDirectory, slug + ".png");
        try
        {
            if (File.Exists(path) && Image.LoadFromFile(path) is { } image && !image.IsEmpty())
                texture = ImageTexture.CreateFromImage(image);
        }
        catch (Exception ex) { GD.PushWarning($"[Alchemy] card art {path} skipped: {ex.Message}"); }
        Loaded[slug] = texture;
        return texture;
    }
}
