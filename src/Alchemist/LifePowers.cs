using Alchemist.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist;

/// <summary>
/// Game-side entry points of the life axis. The homunculus is a pet creature (v0.20): its HP bar is the
/// homunculus HP. LifeState holds the number for the rules; the only change made outside this class is the pet
/// taking hits for the alchemist, which State() folds back in before anything reads or changes it.
/// </summary>
public static class LifeAxis
{
    public static LifeState? State(Player? player)
    {
        if (player?.GetRelic<MaterialBox>()?.Combat?.Life is not { } life) return null;
        if (Pet(player) is { } pet) life.SyncHomunculus(pet.IsAlive ? pet.CurrentHp : 0);
        return life;
    }

    public static Creature? Pet(Player player) => player.PlayerCombatState?.GetPet<HomunculusPet>();

    /// The only way homunculus HP goes up: drain, and cards that say so.
    public static async Task GainHomunculus(PlayerChoiceContext c, Player player, int amount)
    {
        if (State(player) is not { } life || amount <= 0) return;
        life.GainHomunculus(amount);
        await PushToPet(c, player, life);
    }

    /// Fixed-cost exits: all or nothing. Spending thins the shield; spending the last of it fells the pet.
    public static async Task<bool> TrySpendHomunculus(PlayerChoiceContext c, Player player, int amount)
    {
        if (State(player) is not { } life || !life.TrySpendHomunculus(amount)) return false;
        await PushToPet(c, player, life);
        return true;
    }

    public static async Task<int> SpendAllHomunculus(PlayerChoiceContext c, Player player)
    {
        if (State(player) is not { } life) return 0;
        int spent = life.SpendAllHomunculus();
        await PushToPet(c, player, life);
        return spent;
    }

    /// Makes the pet match LifeState: summoned on the first gain, revived when HP returns after it fell,
    /// and felled at 0. Max HP only follows the number upward, so it is never a cap.
    private static async Task PushToPet(PlayerChoiceContext c, Player player, LifeState life)
    {
        int hp = life.HomunculusHp;
        var pet = Pet(player);
        if (pet is null)
        {
            if (hp <= 0) return;
            pet = await PlayerCmd.AddPet<HomunculusPet>(player);
            await PowerCmd.Apply<HomunculusPower>(c, pet, 1, null, null);
        }
        if (hp <= 0)
        {
            if (pet.IsAlive) await CreatureCmd.SetCurrentHp(pet, 0);
            return;
        }
        if (pet.IsDead || hp > pet.MaxHp) await CreatureCmd.SetMaxHp(pet, Math.Max(hp, pet.IsDead ? hp : pet.MaxHp));
        await CreatureCmd.SetCurrentHp(pet, hp);
    }

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
            if (lost > 0) await PushToPet(new ThrowingPlayerChoiceContext(), alchemist, life);
            foreach (var nourish in alchemist.Creature.Powers.OfType<NourishPower>().ToList())
                if (lost > 0) await nourish.OnDrained();
        }
        if (owner.IsAlive) await PowerCmd.Decrement(drain);
    }
}

/// <summary>
/// The homunculus on the field. It borrows Osty's visuals until the mod has its own art, and never acts.
/// </summary>
public sealed class HomunculusPet() : CustomPetModel(visibleHp: true), ILocalizationProvider
{
    private static string BorrowedVisuals => SceneHelper.GetScenePath("creature_visuals/osty");
    public override int MinInitialHp => 1;
    public override int MaxInitialHp => 1;
    public override IEnumerable<string> AssetPaths => [BorrowedVisuals];
    public override NCreatureVisuals? CreateCustomVisuals()
        => PreloadManager.Cache.GetScene(BorrowedVisuals).Instantiate<NCreatureVisuals>(PackedScene.GenEditState.Disabled);
    // Osty's skeleton names its animations differently from the defaults.
    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
        => SetupAnimationState(controller, "idle_loop", deadName: "die", hitName: "hurt", attackName: "attack", castName: "cast");
    public List<(string, string)> Localization => [("name", "ホムンクルス")];
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
/// Sits on the homunculus pet: attacks on the alchemist that get past block hit the homunculus instead, as
/// DieForYouPower does for Osty. It survives the pet's death so the pet can be revived in place.
/// </summary>
public sealed class HomunculusPower : AlchemyPower
{
    public override PowerStackType StackType => PowerStackType.Single;
    protected override PowerModel IconSource => ModelDb.Power<RegenPower>();
    public override List<(string,string)> Localization => new PowerLoc("ホムンクルス",
        "錬金術師への攻撃のうち、ブロックを超えた分を代わりに受ける。HPはドレインと、ホムンクルスHPを得ると書かれたカードでだけ増える。0になると倒れ、HPを得ると復活する。戦闘終了で消える。",
        "錬金術師への攻撃のうち、[gold]ブロック[/gold]を超えた分を代わりに受ける。[gold]ホムンクルスHP[/gold]は[gold]ドレイン[/gold]と、ホムンクルスHPを得ると書かれたカードでだけ増える。0になると倒れ、HPを得ると復活する。戦闘終了で消える。");
    // Same rule as DieForYouPower: only powered attacks, only while alive, overflow goes back to the alchemist.
    public override Creature ModifyUnblockedDamageTarget(Creature target, decimal _, ValueProp props, Creature? __)
        => target == Owner.PetOwner?.Creature && Owner.IsAlive && props.IsPoweredAttack() ? Owner : target;
    public override bool ShouldAllowHitting(Creature creature) => creature != Owner || creature.IsAlive;
    public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature) => creature != Owner;
    public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;
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

/// <summary>
/// NCombatRoom.AddCreature makes every pet except Osty non-interactable, which also hides its health bar and
/// stacks it on the player. The homunculus HP is the whole point of the pet, so it gets Osty's treatment:
/// health bar shown and placed to the player's right.
/// </summary>
[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.AddCreature))]
public static class HomunculusPetNodePatch
{
    public static void Postfix(NCombatRoom __instance, Creature creature)
    {
        if (creature.Monster is not HomunculusPet || creature.PetOwner is not { } owner) return;
        if (__instance.GetCreatureNode(creature) is not { } node || __instance.GetCreatureNode(owner.Creature) is not { } ownerNode) return;
        node.ToggleIsInteractable(true);
        node.Position = ownerNode.Position + Vector2.Right * ownerNode.Hitbox.Size.X * 0.5f + Osty.MinOffset;
    }
}
