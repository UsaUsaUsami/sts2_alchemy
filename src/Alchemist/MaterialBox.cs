using Alchemist.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist;

[Pool(typeof(AlchemyRelicPool))]
public class MaterialBox : CustomRelicModel
{
    private AlchemyState? state;
    public AlchemyState Inventory => state ??= new();
    public HarvestCombat? Combat { get; private set; }
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool ShowCounter => true;
    public override int DisplayAmount => Inventory.Total;
    public override string PackedIconPath => ModelDb.Relic<BurningBlood>().PackedIconPath;
    protected override string PackedIconOutlinePath => "res://images/atlases/relic_outline_atlas.sprites/burning_blood.tres";
    protected override string BigIconPath => "res://images/relics/burning_blood.png";
    public const string RewardLocKey = "materialReward";
    public const string RewardIconPath = "res://images/relics/burning_blood.png";
    /// Furnace activations dealt at the start of each combat, which is also that combat's cap.
    public virtual int FurnaceTokens => HarvestCombat.FurnaceLimit;
    public override List<(string,string)> Localization => BoxLoc("素材ボックス", "相を変え、素材を選ぶ。");
    protected List<(string,string)> BoxLoc(string title, string flavor) => new RelicLoc(title,
        $"属性カードを使うと地・水・火・風の相が変化する。異なる相へ移ると相転移効果が発動する。\n戦闘開始時はランダムな相に入る（相転移ではない）。各戦闘の最初に炉の起動が{FurnaceTokens}枚手札へ加わり、現在相に対応する素材を採取する（戦闘全体で{FurnaceTokens}回まで）。\nエリート報酬は1枠、ボス報酬は2枠。希少素材は工房で恒久加工できる。", flavor,
        (RewardLocKey, "素材を選ぶ"),
        ("phaseShift.Earth", "地相へ転移"), ("phaseShift.Water", "水相へ転移"),
        ("phaseShift.Fire", "火相へ転移"), ("phaseShift.Air", "風相へ転移"),
        ("boxFull", "素材ボックスが満杯"));
    // Orobas (Touch of Orobas) refines the starter relic. Without this BaseLib falls back to the base game's
    // Circlet, which would take the whole material box with it (v0.21 bug).
    public override RelicModel? GetUpgradeReplacement() => ModelDb.Relic<RefinedMaterialBox>();
    [SavedProperty]
    public string AlchemistState { get => Inventory.Save(); set => state = AlchemyState.Load(value); }
    protected override void AfterCloned() { base.AfterCloned(); state = null; Combat = null; }
    public override Task BeforeCombatStart()
    {
        Combat = new(FurnaceTokens);
        // design-axes 6.3 G-1: open in a random element, without a transition effect or a count.
        Combat.Phases.Open(PhaseRules.Opening(Owner.RunState.Rng.Seed, $"{Owner.RunState.TotalFloor}:{Owner.RunState.CurrentRoom?.Id}:opening"));
        return Task.CompletedTask;
    }
    /// RelicCmd.Replace hands over no state, so MaterialBoxReplacePatch copies the inventory across when
    /// Orobas swaps the box for its refined form.
    internal void TakeInventoryFrom(MaterialBox other) => AlchemistState = other.AlchemistState;
    internal void RefreshCount() => InvokeDisplayAmountChanged();
    public override async Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side, IReadOnlyList<Creature> participants, ICombatState cs)
    {
        if(side==CombatSide.Player) Combat?.Phases.StartTurn();
        if(side==CombatSide.Player && Combat is { FurnaceTokensGranted:false } combat)
        {
            combat.FurnaceTokensGranted=true;
            var cards=Enumerable.Range(0,combat.Limit).Select(_=>cs.CreateCard<FurnaceActivation>(Owner)).ToArray();
            await CardPileCmd.AddGeneratedCardsToCombat(cards,PileType.Hand,Owner);
        }
    }
    public bool GrantFromFurnace()
    {
        if (Combat is null || Combat.FurnaceUsed < 1 || Combat.FurnaceUsed > Combat.Limit || AlchemyPhaseState.MaterialFor(Combat.Phases.Current) is not { } material) return false;
        bool granted = Inventory.GrantHarvest($"{Owner.RunState.TotalFloor}:{Owner.RunState.CurrentRoom?.Id}:furnace{Combat.FurnaceUsed}", material);
        if (granted) InvokeDisplayAmountChanged();
        return granted;
    }
    public override Task AfterCombatEnd(CombatRoom room)
    {
        Combat = null;
        return Task.CompletedTask;
    }
    // Cards only declare an element; PhaseTransitions decides whether that is a transition and what it does.
    // Replays of the same play (IsFirstInSeries false) do not move the phase again.
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if(Combat is null || cardPlay.Player!=Owner || !cardPlay.IsFirstInSeries || cardPlay.Card is not AlchemyCard { Element:not AlchemyPhase.None } card) return;
        await PhaseTransitions.Enter(context,Owner,card.Element,card,cardPlay.Target,cardPlay);
    }
    public static string PhaseName(AlchemyPhase phase)=>PhaseRules.Name(phase);
    // Material slots are additional to normal card, gold, relic and potion rewards.
    private string OfferId(AbstractRoom room, int slot) => $"{Owner.RunState.TotalFloor}:{room.Id}:reward{slot}";
    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || room is not CombatRoom) return false;
        bool modified = false;
        int slots = room.RoomType switch
        {
            RoomType.Elite => MaterialOffers.EliteSlots,
            RoomType.Boss => MaterialOffers.BossSlots,
            _ => 0
        };
        for (int slot = 0; slot < slots; slot++)
        {
            string id = OfferId(room, slot);
            // Already taken or declined: a re-entered rewards screen must not offer the slot again.
            if (Inventory.Received.Contains(id)) continue;
            if (!Inventory.HasOffer(id))
            {
                var candidates = room.RoomType == RoomType.Boss && slot == 1
                    ? MaterialOffers.RollRare(Owner.RunState.Rng.Seed,id)
                    : MaterialOffers.RollMixed(Owner.RunState.Rng.Seed,id);
                Inventory.Offer(id,candidates);
            }
            if (rewards.OfType<MaterialReward>().Any(r => r.OfferId == id)) continue;
            rewards.Add(new MaterialReward(player, this, id));
            modified = true;
        }
        return modified;
    }

    // The workshop reuses RestSiteRoom for floor progression only; resting and upgrading there would
    // hand out a rest site the act never spent. NRestSiteRoom shows Proceed on its own when empty.
    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner || options.Count == 0 || !WorkshopMap.IsCurrentWorkshop()) return false;
        options.Clear();
        return true;
    }
}
