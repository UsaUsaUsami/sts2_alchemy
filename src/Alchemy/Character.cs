using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace Alchemy;

public sealed class AlchemistCharacter : PlaceholderCharacterModel
{
    public override Color NameColor => new("73d6b2");
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 75;
    public override CardPoolModel CardPool => ModelDb.CardPool<AlchemyCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<AlchemyRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<AlchemyPotionPool>();
    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<StrikeAlchemist>(), ModelDb.Card<StrikeAlchemist>(),
        ModelDb.Card<StrikeAlchemist>(),
        ModelDb.Card<DefendAlchemist>(), ModelDb.Card<DefendAlchemist>(),
        ModelDb.Card<DefendAlchemist>(),
        ModelDb.Card<EarthenGuard>(), ModelDb.Card<SoothingMist>(), ModelDb.Card<InstantAlchemy>()];
    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<MaterialBox>()];
    // 2026-09-30: the alchemist's own look (CharacterArt). Rest site and merchant: RestMerchantArt (2026-10-01).
    // 2026-10-01: the Necrobinder's rig repainted as the alchemist (NecroRig); the still sprite only without its art.
    public override NCreatureVisuals? CreateCustomVisuals()
        => NecroRig.CreateVisuals()
           ?? (CharacterArt.Body is { } body ? NodeFactory<NCreatureVisuals>.CreateFromResource(body) : null);
    // The donor's own mapping, so its animations play for the triggers the game sends.
    protected override List<(AnimState, string)> AnimationStates =>
    [
        (new AnimState("attack"), "Attack"),
        (new AnimState("hurt"), "Hit"),
        (new AnimState(NecroRig.CastAnimation), "PowerUp"),
        (new AnimState(NecroRig.CastAnimation), "Cast"),
    ];
    // Same shape as the base game's scenes/ui/character_icons/*_icon.tscn: a full-rect TextureRect.
    public override Control? CustomIcon => CharacterArt.Icon is { } icon
        ? new TextureRect
        {
            Texture = icon, AnchorRight = 1, AnchorBottom = 1, MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        }
        : null;
    public override string? CustomCharacterSelectIconPath
        => CharacterArt.CtexPath("char_select.ctex") ?? base.CustomCharacterSelectIconPath;
    public override string? CustomCharacterSelectLockedIconPath
        => CharacterArt.CtexPath("char_select.ctex") ?? base.CustomCharacterSelectLockedIconPath;
    public override string? CustomMapMarkerPath
        => CharacterArt.CtexPath("map_marker.ctex") ?? base.CustomMapMarkerPath;
    public override List<(string, string)> Localization => new CharacterLoc(
        "錬金術師", "錬金術師", "四元素の相を切り替えて戦い、現在相から素材を採取する。\n工房で錬成・改造・調薬・付与を行う旅人。\n【試作版：外見は仮】",
        "彼ら", "彼ら", "彼らの", "彼らの", "薬草と鉄", "次の相へ。", "炉の火が消えた。", "まだ火は残っている。", "次の工房に備えよう。", "錬金術師のカード", "錬金術師のカードを使う。");
}

public sealed class AlchemyCardPool : CustomCardPoolModel
{
    public override string Title => "Alchemist";
    public override string EnergyColorName => "ironclad";
    public override Color DeckEntryCardColor => new("73d6b2");
    public override bool IsColorless => false;
    public override Color ShaderColor => new("73d6b2");
    // Membership comes from the [Pool] attribute every AlchemyCard inherits; BaseLib adds those cards
    // itself, so GenerateAllCards stays empty (listing them again doubled every card, v0.15-v0.16).
    // Rewards and the merchant roll only Common/Uncommon/Rare: 16 cards per element plus 11 phase-less
    // ones (75). Workshop-only cards use Event and tokens use Token, so they are never offered.
}

public sealed class AlchemyRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => new("73d6b2");
    public override string EnergyColorName => "ironclad";
    // 2026-10-01: its own relics (AlchemyRelics.cs) through their [Pool] attributes, no longer the Ironclad's.
}

/// <summary>The alchemist's three potions (AlchemyPotions.cs), like each base game character's own three.</summary>
public sealed class AlchemyPotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => new("73d6b2");
    public override string EnergyColorName => "ironclad";
}

/// <summary>
/// The icon textures are plain getters on CharacterModel that BaseLib only lets a mod redirect by res:// path, and
/// the mod's art is loose files (CharacterArt). Each postfix swaps in the alchemist's texture when one loaded.
/// </summary>
[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.IconTexture), MethodType.Getter)]
public static class AlchemistIconTexturePatch
{
    public static void Postfix(CharacterModel __instance, ref Texture2D __result)
    {
        if (__instance is AlchemistCharacter && CharacterArt.Icon is { } icon) __result = icon;
    }
}

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.IconOutlineTexture), MethodType.Getter)]
public static class AlchemistIconOutlinePatch
{
    public static void Postfix(CharacterModel __instance, ref Texture2D __result)
    {
        if (__instance is AlchemistCharacter && CharacterArt.IconOutline is { } outline) __result = outline;
    }
}

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectIcon), MethodType.Getter)]
public static class AlchemistCharacterSelectIconPatch
{
    public static void Postfix(CharacterModel __instance, ref CompressedTexture2D __result)
    {
        if (__instance is AlchemistCharacter && CharacterArt.CharacterSelect is { } portrait) __result = portrait;
    }
}

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.MapMarker), MethodType.Getter)]
public static class AlchemistMapMarkerPatch
{
    public static void Postfix(CharacterModel __instance, ref CompressedTexture2D __result)
    {
        if (__instance is AlchemistCharacter && CharacterArt.MapMarker is { } marker) __result = marker;
    }
}

/// <summary>
/// The character select background. The game preloads every character's background scene by path, so the path stays
/// the Ironclad's and the instance it adds for the alchemist is swapped for one full-screen picture right after
/// (CharacterArt.SelectBg, 2026-09-30).
/// </summary>
public static class AlchemistSelectBg
{
    public static void Swap(Control? container, CharacterModel? character, bool retry = true)
    {
        if (container is null || character is not AlchemistCharacter || CharacterArt.SelectBg is not { } picture) return;
        string name = character.Id.Entry + "_bg";
        var olds = container.GetChildren().OfType<Control>().Where(c => c.Name == name).ToList();
        foreach (var old in olds)
        {
            container.RemoveChild(old);
            old.QueueFree();
        }
        // AddChildSafely defers when the container is busy; then the borrowed one arrives next frame.
        if (olds.Count == 0 && retry) Callable.From(() => Swap(container, character, false)).CallDeferred();
        var (position, size) = VisibleRect(container);
        var rect = new TextureRect
        {
            Name = name, Texture = picture, MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
        };
        // Only after IgnoreSize: before it, the texture's own 1920x1080 is the minimum size and Size cannot go below.
        rect.Position = position;
        rect.Size = size;
        container.AddChild(rect);
    }

    /// <summary>
    /// The screen area in the container's own coordinates, plus a small margin. The base game's container
    /// ("AnimatedBg") is larger than the screen (2560x1200 at 1920x1080), scaled 1.1 about its pivot, and drifts with
    /// the mouse: its backgrounds are painted for that bigger canvas. A picture stretched over the whole container
    /// was blown up and pushed the alchemist off the right edge (user report, 2026-09-30).
    /// </summary>
    public static (Vector2 Position, Vector2 Size) VisibleRect(Control container, Vector2? screenSize = null)
    {
        const float margin = 0.04f; // room for the mouse drift
        var screen = screenSize ?? (container.IsInsideTree() ? container.GetViewportRect().Size : new Vector2(1920, 1080));
        var scale = container.Scale.X == 0 ? 1f : container.Scale.X;
        Vector2 Local(Vector2 point) => container.PivotOffset + (point - container.Position - container.PivotOffset) / scale;
        var topLeft = Local(Vector2.Zero);
        var size = Local(screen) - topLeft;
        return (topLeft - size * margin, size * (1 + 2 * margin));
    }

    public static Control? Container(object screen) => Traverse.Create(screen).Field("_bgContainer").GetValue<Control>();
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
public static class AlchemistSelectBgPatch
{
    public static void Postfix(NCharacterSelectScreen __instance, CharacterModel characterModel)
        => AlchemistSelectBg.Swap(AlchemistSelectBg.Container(__instance), characterModel);
}

[HarmonyPatch(typeof(NCharacterSelectScreen), "OnLocalCharacterChangedForRandom")]
public static class AlchemistRandomSelectBgPatch
{
    public static void Postfix(NCharacterSelectScreen __instance, CharacterModel characterModel)
        => AlchemistSelectBg.Swap(AlchemistSelectBg.Container(__instance), characterModel);
}

[HarmonyPatch(typeof(NMultiplayerLoadGameScreen), "AfterMultiplayerStarted")]
public static class AlchemistLoadSelectBgPatch
{
    public static void Postfix(NMultiplayerLoadGameScreen __instance)
    {
        var container = AlchemistSelectBg.Container(__instance);
        if (container is null) return;
        foreach (var character in ModelDb.AllCharacters.OfType<AlchemistCharacter>())
            AlchemistSelectBg.Swap(container, character);
    }
}
