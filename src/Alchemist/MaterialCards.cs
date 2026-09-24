using Alchemist.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Alchemist;

// Visual-only cards for the material box: mounted with NCard.Create like the recipe previews in
// WorkshopUi (CreatePreviewCard), never added to a pile or RunState, and never actually played.
// AlchemyState.Counts/RareCounts stay the source of truth; these only give the box a card-like
// look. One small class per material keeps each card's art and text fixed, instead of one generic
// class needing per-instance dynamic localization.
// Type/Rarity match every other card already proven through this exact NCard.Create preview path
// (the workshop cards use Event too); CardType.Status and
// CardRarity.Token were never exercised through that pipeline and stalled it out in practice.
public abstract class MaterialCard() : AlchemyCard(0, CardType.Skill, CardRarity.Event, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;
    protected override void OnUpgrade() { }
}

[Pool(typeof(AlchemyCardPool))]
public sealed class IronMaterialCard() : MaterialCard
{
    protected override CardModel Artwork => ModelDb.Card<TrueGrit>();
    public override List<(string, string)> Localization => new CardLoc("鉄", "物理攻撃・防御を形作る基本素材。工房でカードへ錬成する。");
}
[Pool(typeof(AlchemyCardPool))]
public sealed class HerbMaterialCard() : MaterialCard
{
    protected override CardModel Artwork => ModelDb.Card<ShrugItOff>();
    public override List<(string, string)> Localization => new CardLoc("薬草", "毒・弱体を形作る基本素材。工房でカードへ錬成する。");
}
[Pool(typeof(AlchemyCardPool))]
public sealed class PowderMaterialCard() : MaterialCard
{
    protected override CardModel Artwork => ModelDb.Card<Thunderclap>();
    public override List<(string, string)> Localization => new CardLoc("火薬", "高火力・全体攻撃を形作る基本素材。工房でカードへ錬成する。");
}
[Pool(typeof(AlchemyCardPool))]
public sealed class EtherMaterialCard() : MaterialCard
{
    protected override CardModel Artwork => ModelDb.Card<Inflame>();
    public override List<(string, string)> Localization => new CardLoc("エーテル", "ドロー・循環を形作る基本素材。工房でカードへ錬成する。");
}
[Pool(typeof(AlchemyCardPool))]
public sealed class MercuryMaterialCard() : MaterialCard
{
    protected override CardModel Artwork => ModelDb.Card<ShrugItOff>();
    public override List<(string, string)> Localization => new CardLoc("水銀", "保留を刻む希少素材。工房で錬成カードへ恒久加工する。");
}
[Pool(typeof(AlchemyCardPool))]
public sealed class StardustMaterialCard() : MaterialCard
{
    protected override CardModel Artwork => ModelDb.Card<Inflame>();
    public override List<(string, string)> Localization => new CardLoc("星砂", "リプレイを刻む希少素材。工房で錬成カードへ恒久加工する。");
}
[Pool(typeof(AlchemyCardPool))]
public sealed class VoidCrystalMaterialCard() : MaterialCard
{
    protected override CardModel Artwork => ModelDb.Card<TrueGrit>();
    public override List<(string, string)> Localization => new CardLoc("虚無結晶", "コスト減少と廃棄を刻む希少素材。工房で錬成カードへ恒久加工する。");
}

public static class MaterialCards
{
    public static CardModel Canonical(Material m) => m switch
    {
        Material.Iron => ModelDb.Card<IronMaterialCard>(),
        Material.Herb => ModelDb.Card<HerbMaterialCard>(),
        Material.Powder => ModelDb.Card<PowderMaterialCard>(),
        Material.Ether => ModelDb.Card<EtherMaterialCard>(),
        _ => throw new ArgumentOutOfRangeException(nameof(m))
    };
    public static CardModel Canonical(RareMaterial m) => m switch
    {
        RareMaterial.Mercury => ModelDb.Card<MercuryMaterialCard>(),
        RareMaterial.Stardust => ModelDb.Card<StardustMaterialCard>(),
        RareMaterial.VoidCrystal => ModelDb.Card<VoidCrystalMaterialCard>(),
        _ => throw new ArgumentOutOfRangeException(nameof(m))
    };
}
