using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Alchemy;

public sealed class AlchemistCharacter : PlaceholderCharacterModel
{
    public override Color NameColor => new("73d6b2");
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 75;
    public override CardPoolModel CardPool => ModelDb.CardPool<AlchemyCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<AlchemyRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<IroncladPotionPool>();
    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<StrikeAlchemist>(), ModelDb.Card<StrikeAlchemist>(),
        ModelDb.Card<StrikeAlchemist>(),
        ModelDb.Card<DefendAlchemist>(), ModelDb.Card<DefendAlchemist>(),
        ModelDb.Card<DefendAlchemist>(),
        ModelDb.Card<EarthenGuard>(), ModelDb.Card<SoothingMist>(), ModelDb.Card<InstantAlchemy>()];
    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<MaterialBox>()];
    // 2026-09-30: the alchemist's own look (CharacterArt). Rest site, merchant and character select background
    // still borrow the Ironclad's.
    public override NCreatureVisuals? CreateCustomVisuals()
        => CharacterArt.Body is { } body ? NodeFactory<NCreatureVisuals>.CreateFromResource(body) : null;
    // Same shape as the base game's scenes/ui/character_icons/*_icon.tscn: a full-rect TextureRect.
    public override Control? CustomIcon => CharacterArt.Icon is { } icon
        ? new TextureRect
        {
            Texture = icon, AnchorRight = 1, AnchorBottom = 1, MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        }
        : null;
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
    protected override RelicModel[] GenerateAllRelics() => ModelDb.RelicPool<IroncladRelicPool>().AllRelics.ToArray();
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
