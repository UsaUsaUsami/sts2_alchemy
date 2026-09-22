using Alchemist.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Alchemist;

[Pool(typeof(AlchemyRelicPool))]
public sealed class MaterialBox : CustomRelicModel
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
    public override List<(string,string)> Localization => new RelicLoc("素材ボックス",
        "鉄→薬草→火薬→エーテルの順に素材相が循環する。[gold]炉の起動[/gold]でカードを廃棄すると現在相の素材を1個得る。\n炉の起動は戦闘全体で2回まで。容量10。\nこれとは別に、エリートの報酬で1枠、ボスの報酬で2枠、3候補から素材を1個選べる。\n画面左の「素材・工房」から確認する。", "廃棄する時機が、次の一枚を決める。",
        (RewardLocKey, "素材を選ぶ"));
    [SavedProperty]
    public string AlchemistState { get => Inventory.Save(); set => state = AlchemyState.Load(value); }
    protected override void AfterCloned() { base.AfterCloned(); state = null; Combat = null; }
    public override Task BeforeCombatStart()
    {
        Combat = new(Inventory.NextCombatMaterial);
        return Task.CompletedTask;
    }
    public override Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side, IReadOnlyList<Creature> participants, ICombatState cs)
    {
        if (side == CombatSide.Player) Combat?.BeginTurn(cs.RoundNumber);
        return Task.CompletedTask;
    }
    public bool GrantFromFurnace(Material material)
    {
        if (Combat is null || Combat.FurnaceUsed is < 1 or > 2) return false;
        bool granted = Inventory.Grant($"{Owner.RunState.TotalFloor}:{Owner.RunState.CurrentRoom?.Id}:furnace{Combat.FurnaceUsed}", material);
        if (granted) InvokeDisplayAmountChanged();
        return granted;
    }
    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (Combat is not null)
        {
            Inventory.NextCombatMaterial = Combat.NextPhase;
            Inventory.Revision++;
        }
        Combat = null;
        return Task.CompletedTask;
    }
    // These slots are additional to furnace harvesting and never replace the normal gold, card, relic
    // or potion rewards.
    private string OfferId(AbstractRoom room, int slot) => $"{Owner.RunState.TotalFloor}:{room.Id}:reward{slot}";
    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || room is not CombatRoom) return false;
        int slots = room.RoomType switch
        {
            RoomType.Elite => MaterialOffers.EliteSlots,
            RoomType.Boss => MaterialOffers.BossSlots,
            _ => 0
        };
        bool modified = false;
        for (int slot = 0; slot < slots; slot++)
        {
            string id = OfferId(room, slot);
            // Already taken or declined: a re-entered rewards screen must not offer the slot again.
            if (Inventory.Received.Contains(id)) continue;
            if (!Inventory.HasOffer(id)) Inventory.Offer(id, MaterialOffers.Roll(Owner.RunState.Rng.Seed, id));
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
