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

/// <summary>
/// The alchemist's own look (2026-09-30), from art/character next to the DLL (scripts/make-character-art.py):
/// the combat sprite cut from the key visual, the head icon and its outline, and two .ctex files for the character
/// select button and the map marker, which the game types as CompressedTexture2D. Any missing file keeps the
/// borrowed Ironclad asset for that spot.
/// </summary>
public static class CharacterArt
{
    /// On-screen height of the combat sprite in pixels. A trial value.
    public const int BodyHeight = 330;
    private static readonly Dictionary<string, Texture2D?> Loaded = [];

    public static string ArtDirectory
        => Path.Combine(Path.GetDirectoryName(typeof(CharacterArt).Assembly.Location) ?? "", "art", "character");

    public static bool HasBody => File.Exists(Path.Combine(ArtDirectory, "alchemist.png"));
    public static Texture2D? Body => Png("alchemist.png", BodyHeight);
    public static Texture2D? Icon => Png("icon.png");
    public static Texture2D? IconOutline => Png("icon_outline.png");
    public static Texture2D? SelectBg => Png("select_bg.png");
    public static CompressedTexture2D? CharacterSelect => Ctex("char_select.ctex");
    public static CompressedTexture2D? MapMarker => Ctex("map_marker.ctex");

    /// The file's absolute path for the game's ResourceLoader, or null without the file. The getters that hand out
    /// these textures only load a path, and a postfix on them did not reach the real select button (still the
    /// Ironclad, user report 2026-09-30), so the path itself is redirected.
    public static string? CtexPath(string file)
    {
        string path = Path.Combine(ArtDirectory, file);
        return File.Exists(path) ? path.Replace('\\', '/') : null;
    }

    private static Texture2D? Png(string file, int height = 0)
    {
        if (Loaded.TryGetValue(file, out var cached)) return cached;
        Texture2D? texture = null;
        string path = Path.Combine(ArtDirectory, file);
        try
        {
            if (File.Exists(path) && Image.LoadFromFile(path) is { } image && !image.IsEmpty())
            {
                if (height > 0) image.Resize(image.GetWidth() * height / image.GetHeight(), height, Image.Interpolation.Lanczos);
                texture = ImageTexture.CreateFromImage(image);
            }
        }
        catch (Exception ex) { GD.PushWarning($"[Alchemy] character art {path} skipped: {ex.Message}"); }
        return Loaded[file] = texture;
    }

    private static CompressedTexture2D? Ctex(string file)
    {
        if (Loaded.TryGetValue(file, out var cached)) return cached as CompressedTexture2D;
        CompressedTexture2D? texture = null;
        string path = Path.Combine(ArtDirectory, file);
        try
        {
            var candidate = new CompressedTexture2D();
            if (File.Exists(path) && candidate.Load(path) == Error.Ok) texture = candidate;
        }
        catch (Exception ex) { GD.PushWarning($"[Alchemy] character art {path} skipped: {ex.Message}"); }
        Loaded[file] = texture;
        return texture;
    }
}

/// <summary>
/// The mod's own icons (2026-10-01): .ctex files in art/icons next to the DLL, made by scripts/generate-icons.py and
/// scripts/import-icons.py. The game loads relic, power, enchantment and map icons from a path with ResourceLoader,
/// so these hand out the file's absolute path, the way the character select button already does (CharacterArt).
/// A missing file returns null and the caller keeps its borrowed base-game icon.
/// </summary>
public static class IconArt
{
    public static string ArtDirectory
        => Path.Combine(Path.GetDirectoryName(typeof(IconArt).Assembly.Location) ?? "", "art", "icons");

    /// The icon's slug for a model class: PhaseCompass -> phase_compass.
    public static string Slug(Type type) => Snake(type.Name);
    public static string Snake(string name)
        => string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));

    /// material_iron, material_void_crystal, ... for a normal or rare material.
    public static string MaterialSlug(Core.MaterialChoice m)
        => "material_" + Snake(m.Class == Core.MaterialClass.Normal ? m.NormalMaterial.ToString() : m.RareMaterial.ToString());

    public static string? Big(string slug) => Ctex(slug);
    public static string? Packed(string slug) => Ctex(slug + "_packed");
    public static string? Outline(string slug) => Ctex(slug + "_outline");

    private static string? Ctex(string name)
    {
        string path = Path.Combine(ArtDirectory, name + ".ctex");
        return File.Exists(path) ? path.Replace('\\', '/') : null;
    }
}

/// <summary>
/// The homunculus pet's sprite (2026-09-29: a stone golem, assets/concepts/homunculus-v1.png), a transparent PNG at
/// art/pets/golem.png made by scripts/import-pet.py. Without the file the pet keeps Osty's borrowed visuals.
/// </summary>
public static class PetArt
{
    /// On-screen height in pixels. A trial value: the alchemist is roughly 280 tall.
    public const int GolemHeight = 190;
    private static Texture2D? _golem;
    private static bool _loaded;

    public static string GolemPath
        => Path.Combine(Path.GetDirectoryName(typeof(PetArt).Assembly.Location) ?? "", "art", "pets", "golem.png");

    public static bool HasGolem => File.Exists(GolemPath);

    public static Texture2D? Golem
    {
        get
        {
            if (_loaded) return _golem;
            _loaded = true;
            try
            {
                if (HasGolem && Image.LoadFromFile(GolemPath) is { } image && !image.IsEmpty())
                {
                    image.Resize(image.GetWidth() * GolemHeight / image.GetHeight(), GolemHeight, Image.Interpolation.Lanczos);
                    _golem = ImageTexture.CreateFromImage(image);
                }
            }
            catch (Exception ex) { GD.PushWarning($"[Alchemy] pet art {GolemPath} skipped: {ex.Message}"); }
            return _golem;
        }
    }
}
