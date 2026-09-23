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
        "属性カードを使うと地・水・火・風の相が変化する。異なる相へ移ると相転移効果が発動する。\n戦闘開始時は無相。炉の起動は常に手札へ加わり、現在相に対応する素材を採取する。\nエリート報酬は1枠、ボス報酬は2枠。希少素材は工房で恒久加工できる。", "相を変え、素材を選ぶ。",
        (RewardLocKey, "素材を選ぶ"));
    [SavedProperty]
    public string AlchemistState { get => Inventory.Save(); set => state = AlchemyState.Load(value); }
    protected override void AfterCloned() { base.AfterCloned(); state = null; Combat = null; }
    public override Task BeforeCombatStart()
    {
        Combat = new();
        return Task.CompletedTask;
    }
    public override async Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side, IReadOnlyList<Creature> participants, ICombatState cs)
    {
        if(side==CombatSide.Player && Combat is { FurnaceTokensGranted:false } combat)
        {
            combat.FurnaceTokensGranted=true;
            var cards=Enumerable.Range(0,2).Select(_=>cs.CreateCard<FurnaceActivation>(Owner)).ToArray();
            await CardPileCmd.AddGeneratedCardsToCombat(cards,PileType.Hand,Owner);
        }
    }
    public bool GrantFromFurnace()
    {
        if (Combat is null || Combat.FurnaceUsed is < 1 or > 2 || AlchemyPhaseState.MaterialFor(Combat.Phases.Current) is not { } material) return false;
        bool granted = Inventory.GrantHarvest($"{Owner.RunState.TotalFloor}:{Owner.RunState.CurrentRoom?.Id}:furnace{Combat.FurnaceUsed}", material);
        if (granted) InvokeDisplayAmountChanged();
        return granted;
    }
    public override Task AfterCombatEnd(CombatRoom room)
    {
        Combat = null;
        return Task.CompletedTask;
    }
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if(Combat is null || cardPlay.Player!=Owner || !cardPlay.IsFirstInSeries || cardPlay.Card is not AlchemyCard { Element:not AlchemyPhase.None } card) return;
        var change=Combat.Phases.Enter(card.Element);
        if(!change.Triggered) { Combat.LastTransition=$"現在相：{PhaseName(change.To)}"; return; }
        Combat.LastTransition=$"相転移：{PhaseName(change.From)} → {PhaseName(change.To)}";
        var target=cardPlay.Target?.Side==CombatSide.Enemy ? cardPlay.Target : card.CombatState?.HittableEnemies.FirstOrDefault();
        int amount=card.Enchantment is WorkshopTuning ? 2 : 1;
        switch(change.To)
        {
            case AlchemyPhase.Earth: await CreatureCmd.GainBlock(Owner.Creature,1+amount,ValueProp.Unpowered,cardPlay); break;
            case AlchemyPhase.Water when target is not null: await PowerCmd.Apply<WeakPower>(context,target,amount,Owner.Creature,card); break;
            case AlchemyPhase.Fire when target is not null: await CreatureCmd.Damage(context,target,2+amount,ValueProp.Unpowered,Owner.Creature,card,cardPlay); break;
            case AlchemyPhase.Air: await CardPileCmd.Draw(context,amount,Owner); break;
        }
    }
    public static string PhaseName(AlchemyPhase phase)=>phase switch { AlchemyPhase.Earth=>"地",AlchemyPhase.Water=>"水",AlchemyPhase.Fire=>"火",AlchemyPhase.Air=>"風",_=>"無相" };
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
