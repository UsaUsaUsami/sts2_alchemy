using Alchemist.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
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
    public override List<(string,string)> Localization => new RelicLoc("素材ボックス",
        "鉄→薬草→火薬→エーテルの順に素材相が循環する。戦闘開始時の敵を倒すと現在相の素材を得る。\n容量10。通常戦2個、エリート3個、ボス4個まで。\n画面左の「素材・工房」から確認する。", "倒す時機が、次の一枚を決める。");
    [SavedProperty]
    public string AlchemistState { get => Inventory.Save(); set => state = AlchemyState.Load(value); }
    protected override void AfterCloned() { base.AfterCloned(); state = null; Combat = null; }
    public override Task BeforeCombatStart()
    {
        var cs = Owner.Creature.CombatState!;
        int cap = cs.Encounter?.RoomType switch { RoomType.Elite => 3, RoomType.Boss => 4, _ => 2 };
        Combat = new(cs.Enemies.Where(e => e.CombatId.HasValue).Select(e => e.CombatId!.Value), cap);
        return Task.CompletedTask;
    }
    public override Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side, IReadOnlyList<Creature> participants, ICombatState cs)
    {
        if (side == CombatSide.Player) Combat?.BeginTurn(cs.RoundNumber);
        return Task.CompletedTask;
    }
    public override Task AfterDeath(PlayerChoiceContext context, Creature creature, bool wasRemovalPrevented, float length)
    {
        if (!wasRemovalPrevented && creature.IsDead && creature.CombatId is uint id && Combat?.Kill(id) is Material material)
        {
            Inventory.Grant($"{Owner.RunState.TotalFloor}:{Owner.RunState.CurrentRoom?.Id}:{id}", material);
            InvokeDisplayAmountChanged();
        }
        return Task.CompletedTask;
    }
    public override Task AfterCombatEnd(CombatRoom room) { Combat = null; return Task.CompletedTask; }
}
