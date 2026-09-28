using Alchemy.Core;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemy;

/// <summary>
/// v0.15-v0.21 workshop modification ("this card's transitions +1"). No longer applied; kept so cards in
/// older saves still load and work.
/// </summary>
public sealed class WorkshopTuning : CustomEnchantmentModel, ILocalizationProvider, IPhaseTransitionModifier
{
    public const int Bonus = 1;
    public override bool HasExtraCardText=>true;
    public override bool CanEnchant(CardModel card)=>base.CanEnchant(card) && card is AlchemyCard { Element: not (Core.AlchemyPhase.None or Core.AlchemyPhase.Air) };
    public void ModifyPhaseTransition(PhaseTransitionContext transition)=>transition.Amount+=Bonus;
    public List<(string,string)> Localization=>[
        ("title","工房改造（旧）"),("description","このカードによる相転移の効果を1強化する。"),("extraCardText","[gold]工房改造[/gold]")];
}

/// <summary>
/// Workshop modification since v0.22 (design-axes.md 7.3): one effect per material put in, mixed freely, four
/// per card. The four counts are separate saved properties, which the game's enchantment serialization keeps
/// (docs/research.md 2026-09-26). It takes the card's single enchantment slot, like any other enchantment.
/// </summary>
public sealed class WorkshopInfusion : CustomEnchantmentModel, ILocalizationProvider
{
    private int iron, herb, powder, ether;
    [SavedProperty] public int AlchemistInfuseIron { get => iron; set { iron = value; Sync(); } }
    [SavedProperty] public int AlchemistInfuseHerb { get => herb; set { herb = value; Sync(); } }
    [SavedProperty] public int AlchemistInfusePowder { get => powder; set { powder = value; Sync(); } }
    [SavedProperty] public int AlchemistInfuseEther { get => ether; set { ether = value; Sync(); } }

    /// Counts indexed by Material, the shape InfusionRules works with.
    public int[] Counts => [iron, herb, powder, ether];
    public void Add(IReadOnlyList<int> add)
    {
        AlchemistInfuseIron += add[(int)Material.Iron];
        AlchemistInfuseHerb += add[(int)Material.Herb];
        AlchemistInfusePowder += add[(int)Material.Powder];
        AlchemistInfuseEther += add[(int)Material.Ether];
        Amount = Counts.Sum();
    }

    public override bool HasExtraCardText => true;
    public override bool ShowAmount => true;
    protected override string? CustomIconPath => "res://images/enchantments/sharp.png";
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Iron", 0m), new DynamicVar("Herb", 0m), new DynamicVar("Powder", 0m), new DynamicVar("Ether", 0m)];
    // design-axes 7.3: iron, herb and ether go into any card; powder only into attacks (checked per material).
    public override bool CanEnchant(CardModel card) => base.CanEnchant(card) && card.Type is CardType.Attack or CardType.Skill or CardType.Power;
    public override void RecalculateValues() => Sync();
    private void Sync()
    {
        if (!IsMutable) return;
        DynamicVars["Iron"].BaseValue = iron;
        DynamicVars["Herb"].BaseValue = herb;
        DynamicVars["Powder"].BaseValue = powder;
        DynamicVars["Ether"].BaseValue = ether;
    }

    public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
        => props.IsPoweredAttack() ? powder : 0m;

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        var owner = Card.Owner.Creature;
        if (iron > 0) await CreatureCmd.GainBlock(owner, iron, ValueProp.Move, cardPlay);
        // design-axes 7.3 (未決, settled here): a card without a target drains the same enemy a transition's
        // weak or damage would pick, the first hittable one.
        if (herb > 0 && (cardPlay?.Target is { IsDead: false, Side: CombatSide.Enemy } target ? target
                : owner.CombatState?.HittableEnemies.FirstOrDefault()) is { } drained)
            await PowerCmd.Apply<LifeDrainPower>(choiceContext, drained, herb, owner, Card);
        if (ether > 0) await PowerCmd.Apply<PreparationPower>(choiceContext, owner, ether, owner, Card);
    }

    public List<(string,string)> Localization => [
        ("title", "工房改造"),
        ("description", "素材1個につき効果+1。火薬：ダメージ+1（アタックのみ）。鉄：使用時にブロック+1。薬草：使用時にドレイン+1。エーテル：使用時に励起+1。"),
        ("extraCardText", "[gold]工房改造[/gold]{Powder:cond:>0? 鋭利{Powder}|}{Iron:cond:>0? ブロック+{Iron}|}{Herb:cond:>0? ドレイン+{Herb}|}{Ether:cond:>0? 励起+{Ether}|}")];
}
