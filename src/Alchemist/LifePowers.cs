using Alchemist.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist;

/// <summary>
/// Game-side entry points of the life axis. LifeState (in HarvestCombat) is the source of truth; the
/// homunculus power only displays it.
/// </summary>
public static class LifeAxis
{
    public static LifeState? State(Player? player) => player?.GetRelic<MaterialBox>()?.Combat?.Life;

    /// The only way homunculus HP goes up: drain, and cards that say so.
    public static async Task GainHomunculus(PlayerChoiceContext c, Player player, int amount)
    {
        if (State(player) is not { } life || amount <= 0) return;
        life.GainHomunculus(amount);
        await ShowHomunculus(c, player);
    }

    public static bool TrySpendHomunculus(Player player, int amount)
    {
        if (State(player) is not { } life || !life.TrySpendHomunculus(amount)) return false;
        Refresh(player);
        return true;
    }

    public static int SpendAllHomunculus(Player player)
    {
        int spent = State(player)?.SpendAllHomunculus() ?? 0;
        Refresh(player);
        return spent;
    }

    /// The vessel appears on the first gain and stays for the rest of the combat, at 0 too.
    private static async Task ShowHomunculus(PlayerChoiceContext c, Player player)
    {
        if (player.Creature.GetPower<HomunculusPower>() is { } shown) { shown.Refresh(); return; }
        await PowerCmd.Apply<HomunculusPower>(c, player.Creature, 1, player.Creature, null);
    }
    private static void Refresh(Player player) => player.Creature.GetPower<HomunculusPower>()?.Refresh();

    /// Who receives the HP a drain takes: whoever applied it, else the first alchemist in the fight.
    public static Player? DrainOwner(PowerModel drain)
        => drain.Applier?.Player is { } applier && applier.GetRelic<MaterialBox>() is not null ? applier
            : drain.Owner.CombatState?.Players.FirstOrDefault(p => p.GetRelic<MaterialBox>() is not null);

    /// One drain trigger: the owner loses HP equal to the stacks, the alchemist's homunculus gains what was
    /// actually lost, then one stack decays. Shared by the turn-start trigger and 生命の収穫.
    public static async Task TriggerDrain(LifeDrainPower drain)
    {
        var owner = drain.Owner;
        if (owner.IsDead || drain.Amount <= 0) return;
        var results = await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), owner, DrainRules.Damage(drain.Amount),
            ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        int lost = results.Sum(r => DrainRules.HpLost(r.UnblockedDamage, r.OverkillDamage));
        if (DrainOwner(drain) is { } alchemist && State(alchemist) is { } life)
        {
            life.RecordDrain(lost);
            if (lost > 0) await ShowHomunculus(new ThrowingPlayerChoiceContext(), alchemist);
        }
        if (owner.IsAlive) await PowerCmd.Decrement(drain);
    }
}

/// <summary>ドレイン（仮称）: like poison, but what it takes goes to the homunculus.</summary>
public sealed class LifeDrainPower : AlchemyPower
{
    public override PowerType Type => PowerType.Debuff;
    protected override PowerModel IconSource => ModelDb.Power<PoisonPower>();
    public override List<(string,string)> Localization => new PowerLoc("ドレイン",
        "ターン開始時、この数値分のHPを失う。失ったHPは錬金術師のホムンクルスHPになる。その後1減る。",
        "ターン開始時、{Amount}のHPを失い、同じ量を[gold]ホムンクルスHP[/gold]として奪われる。その後1減る。");
    // Same hook and participant check as the base game's PoisonPower.
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner)) await LifeAxis.TriggerDrain(this);
    }
}

/// <summary>
/// The homunculus vessel. Its amount is pinned at 1 so the game never removes it at zero; the label shows the
/// homunculus HP held in LifeState instead.
/// </summary>
public sealed class HomunculusPower : AlchemyPower
{
    protected override PowerModel IconSource => ModelDb.Power<RegenPower>();
    public override int DisplayAmount => LifeAxis.State(Owner.Player)?.HomunculusHp ?? 0;
    public override List<(string,string)> Localization => new PowerLoc("ホムンクルス",
        "生命を溜める器。ドレインと、ホムンクルスHPを得るカードでだけ増える。攻撃を受けず、戦闘終了で消える。",
        "生命を溜める器。[gold]ホムンクルスHP[/gold]は{Amount}。[gold]ドレイン[/gold]と、ホムンクルスHPを得ると書かれたカードでだけ増える。攻撃を受けず、戦闘終了で消える。");
    public void Refresh() => InvokeDisplayAmountChanged();
}

/// <summary>死亡 (人体錬成): a visible debuff, so potions, relics and cards can interact with it.</summary>
public sealed class DeathMarkPower : AlchemyPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override PowerModel IconSource => ModelDb.Power<DoomPower>();
    public override List<(string,string)> Localization => new PowerLoc("死亡",
        "次の自分のターン開始時に死亡する。",
        "次の自分のターン開始時に死亡する。");
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        var turn = side == CombatSide.Player ? DebuffSide.Player : DebuffSide.Enemy;
        var mine = Owner.Side == CombatSide.Player ? DebuffSide.Player : DebuffSide.Enemy;
        if (!participants.Contains(Owner) || !DeathMarkRules.TriggersAt(mine, turn) || Owner.IsDead) return;
        Flash();
        // Not forced: Fairy in a Bottle and other death prevention get their say.
        await CreatureCmd.Kill(Owner);
        if (Owner.IsAlive) await PowerCmd.Remove(this);
    }
}

/// <summary>手本B: at the start of each of your turns, drain every enemy.</summary>
public sealed class DrainMiasmaPower : AlchemyPower
{
    protected override PowerModel IconSource => ModelDb.Power<NoxiousFumesPower>();
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<LifeDrainPower>()];
    public override List<(string,string)> Localization => new PowerLoc("吸精の瘴気",
        "自分のターン開始時、敵全体にドレインを与える。",
        "自分のターン開始時、敵全体に[gold]ドレイン[/gold]{Amount}を与える。");
    // Same shape as the base game's NoxiousFumesPower.
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;
        Flash();
        await PowerCmd.Apply<LifeDrainPower>(new ThrowingPlayerChoiceContext(), Owner.CombatState!.HittableEnemies, Amount, Owner, null);
    }
}
