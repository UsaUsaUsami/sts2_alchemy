using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist;

/// <summary>
/// Workshop modification, saved by the game's native card-enchantment serialization. PhaseTransitions only
/// asks the source card's enchantment, so this strengthens transitions this card causes and no others.
/// Further modification kinds are new enchantments implementing the same transition hooks.
/// </summary>
public sealed class WorkshopTuning : CustomEnchantmentModel, ILocalizationProvider, IPhaseTransitionModifier
{
    public const int Bonus = 1;
    public override bool HasExtraCardText=>true;
    public override bool CanEnchant(CardModel card)=>base.CanEnchant(card) && card is AlchemyCard { Element: not Core.AlchemyPhase.None };
    public void ModifyPhaseTransition(PhaseTransitionContext transition)=>transition.Amount+=Bonus;
    public List<(string,string)> Localization=>[
        ("title","工房改造"),("description","このカードによる相転移の効果を1強化する。"),("extraCardText","[gold]工房改造[/gold]")];
}
