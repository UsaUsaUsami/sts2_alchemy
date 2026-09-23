using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace Alchemist;

public sealed class AlchemistCharacter : PlaceholderCharacterModel
{
    public override Color NameColor => new("73d6b2");
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 75;
    public override CardPoolModel CardPool => ModelDb.CardPool<AlchemyCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<AlchemyRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<IroncladPotionPool>();
    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<StrikeIronclad>(), ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<DefendIronclad>(), ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<EarthenGuard>(), ModelDb.Card<SoothingMist>(), ModelDb.Card<InstantAlchemy>()];
    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<MaterialBox>()];
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
    protected override CardModel[] GenerateAllCards() => [
        ModelDb.Card<EarthenGuard>(),ModelDb.Card<StoneEdge>(),ModelDb.Card<SoothingMist>(),ModelDb.Card<TidalGuard>(),
        ModelDb.Card<Ignition>(),ModelDb.Card<FlashPowder>(),ModelDb.Card<Tailwind>(),ModelDb.Card<Slipstream>(),
        ModelDb.Card<InstantAlchemy>()];
}

public sealed class AlchemyRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => new("73d6b2");
    public override string EnergyColorName => "ironclad";
    protected override RelicModel[] GenerateAllRelics() => ModelDb.RelicPool<IroncladRelicPool>().AllRelics.ToArray();
}
