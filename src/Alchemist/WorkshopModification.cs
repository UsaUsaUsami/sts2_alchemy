using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist;

/// <summary>Saved by the game's native card-enchantment serialization.</summary>
public sealed class WorkshopTuning : CustomEnchantmentModel, ILocalizationProvider
{
    public override bool HasExtraCardText=>true;
    public override bool CanEnchant(CardModel card)=>base.CanEnchant(card) && card is AlchemyCard;
    public List<(string,string)> Localization=>[
        ("title","工房改造"),("description","このカードによる相転移の基本効果を1強化する。"),("extraCardText","[gold]工房改造[/gold]")];
}
