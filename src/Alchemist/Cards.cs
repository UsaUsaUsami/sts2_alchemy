using Alchemist.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist;

[Pool(typeof(AlchemyCardPool))]
public abstract class AlchemyCard(int cost, CardType type, CardRarity rarity, TargetType target)
    : CustomCardModel(cost, type, rarity, target)
{
    public virtual AlchemyPhase Element => AlchemyPhase.None;
    // Placeholder art borrowed from base-game cards until the mod has its own.
    protected virtual CardModel Artwork => Type switch
    {
        CardType.Attack => ModelDb.Card<Thunderclap>(),
        CardType.Power => ModelDb.Card<Inflame>(),
        _ => ModelDb.Card<TrueGrit>()
    };
    public override string PortraitPath => Artwork.PortraitPath;
    public override string? CustomPortraitPath => Artwork.PortraitPath;
    public override string BetaPortraitPath => Artwork.BetaPortraitPath;

    // Shared vocabulary for card effects, so individual cards stay declarative.
    protected MaterialBox? Box => Owner.GetRelic<MaterialBox>();
    protected AlchemyPhase CurrentPhase => Box?.Combat?.Phases.Current ?? AlchemyPhase.None;
    protected int TransitionCount => Box?.Combat?.Phases.TransitionCount ?? 0;
    protected int TransitionsThisTurn => Box?.Combat?.Phases.TransitionsThisTurn ?? 0;
    /// Checked inside OnPlay, before MaterialBox moves the phase to this card's element.
    protected bool WillTransition => PhaseRules.IsElement(Element) && PhaseRules.IsElement(CurrentPhase) && CurrentPhase != Element;
    protected bool InOwnPhase => PhaseRules.IsElement(Element) && CurrentPhase == Element;
    protected bool HasNormalMaterial => Box?.Inventory is { Settled: true } inventory && inventory.Counts.Any(n => n > 0);

    protected async Task Hit(PlayerChoiceContext c, CardPlay p, decimal damage, int hits = 1)
    {
        for (int i = 0; i < hits; i++) await DamageCmd.Attack(damage).FromCard(this, p).Targeting(p.Target!).Execute(c);
    }
    protected async Task HitAll(PlayerChoiceContext c, CardPlay p, decimal damage, int hits = 1)
    {
        for (int i = 0; i < hits; i++) await DamageCmd.Attack(damage).FromCard(this, p).TargetingAllOpponents(CombatState!).Execute(c);
    }
    protected Task Block(CardPlay p, decimal amount) => CreatureCmd.GainBlock(Owner.Creature, amount, ValueProp.Move, p);
    /// Block from the card's own BlockVar, so upgrades and previews stay in sync.
    protected Task CardBlock(CardPlay p) => CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, p);
    protected Task ApplySelf<T>(PlayerChoiceContext c, decimal amount) where T : PowerModel
        => PowerCmd.Apply<T>(c, Owner.Creature, amount, Owner.Creature, this);
    protected Task ApplyTo<T>(PlayerChoiceContext c, Creature target, decimal amount) where T : PowerModel
        => PowerCmd.Apply<T>(c, target, amount, Owner.Creature, this);
    protected Task ApplyAll<T>(PlayerChoiceContext c, decimal amount) where T : PowerModel
        => PowerCmd.Apply<T>(c, CombatState!.HittableEnemies, amount, Owner.Creature, this);
    protected Task Draw(PlayerChoiceContext c, decimal count) => CardPileCmd.Draw(c, count, Owner);
    protected Task Energy(decimal amount) => PlayerCmd.GainEnergy(amount, Owner);

    /// Lets the player pick a normal material through the material-box cards. With ownedOnly, only
    /// materials the box holds are offered. Needs a "selectionScreenPrompt" entry in the card's loc.
    protected async Task<Material?> ChooseMaterial(PlayerChoiceContext c, bool ownedOnly)
    {
        var box = Box;
        if (box is null) return null;
        List<(Material Material, CardModel Card)> options = [];
        void Offer<T>(Material m) where T : CardModel
        {
            if (ownedOnly && box.Inventory.Counts[(int)m] <= 0) return;
            var card = ModelDb.Card<T>().ToMutable(); card.Owner = Owner; card.AfterCreated();
            options.Add((m, card));
        }
        Offer<IronMaterialCard>(Material.Iron); Offer<HerbMaterialCard>(Material.Herb);
        Offer<PowderMaterialCard>(Material.Powder); Offer<EtherMaterialCard>(Material.Ether);
        if (options.Count == 0) return null;
        var chosen = (await CardSelectCmd.FromSimpleGrid(c, options.Select(o => o.Card).ToList(), Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();
        foreach (var o in options) o.Card.Owner = null!;
        return options.FirstOrDefault(o => o.Card == chosen) is { Card: not null } picked ? picked.Material : null;
    }
    /// Picks an owned normal material and removes it from the run inventory.
    protected async Task<Material?> SpendMaterial(PlayerChoiceContext c)
        => await ChooseMaterial(c, ownedOnly: true) is { } m && Box!.Inventory.TryConsume(m) ? m : null;
}

public sealed class FurnaceActivation() : AlchemyCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];
    public override List<(string, string)> Localization => new CardLoc("炉の起動",
        $"手札1枚を廃棄し、現在相に対応する素材を{AlchemyState.YieldPerEvent}個得る。無相では使用できない。\n地：鉄、水：薬草、火：火薬、風：エーテル。戦闘全体で{HarvestCombat.FurnaceLimit}回まで。");
    // Unplayable rather than a silent no-op: playing it in the neutral phase, past the limit or with no
    // other card to feed the furnace would otherwise exhaust the token for nothing.
    protected override bool IsPlayable => Owner?.GetRelic<MaterialBox>()?.Combat is not { } combat
        || (combat.CanUseFurnace && Owner.PlayerCombatState?.Hand.Cards.Any(c => c != this) == true);
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var box = Owner.GetRelic<MaterialBox>();
        var combat = box?.Combat;
        if (combat is null || !combat.CanUseFurnace) return;
        var card = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1), null, this)).FirstOrDefault();
        if (card is null) return;
        await CardCmd.Exhaust(choiceContext, card);
        if (!combat.UseFurnace()) return;
        box!.GrantFromFurnace();
    }
    protected override void OnUpgrade() { }
}
