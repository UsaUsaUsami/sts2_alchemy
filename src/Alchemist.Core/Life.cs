namespace Alchemist.Core;

/// <summary>
/// Combat-only state of the life axis (design-axes.md 3). The homunculus stands on the field as the
/// alchemist's pet (v0.20, like the Necrobinder's Osty) and its HP is this number: it appears the first time
/// the number is gained, soaks attacks, and is thrown away with the combat. Only drain and cards that say so
/// add to it; there is deliberately no shared "gain on damage" rule.
/// </summary>
public sealed class LifeState
{
    public int HomunculusHp { get; private set; }
    /// Set by the first gain and never cleared within the combat, so the vessel stays on screen at 0.
    public bool HomunculusAppeared { get; private set; }
    /// HP enemies lost to drain this combat. Cards that scale with drain read this, not the homunculus,
    /// so spending the homunculus does not undo their progress.
    public int HpDrainedThisCombat { get; private set; }

    public void GainHomunculus(int amount)
    {
        if (amount <= 0) return;
        HomunculusHp += amount;
        HomunculusAppeared = true;
    }

    /// The pet took damage for the alchemist: the game creature's HP is authoritative for that loss.
    public void SyncHomunculus(int hp) => HomunculusHp = Math.Max(0, hp);

    /// Fixed-cost exits: all or nothing, so a card never half-resolves on too little HP.
    public bool TrySpendHomunculus(int amount)
    {
        if (amount <= 0 || HomunculusHp < amount) return false;
        HomunculusHp -= amount;
        return true;
    }

    /// Spend-all exits. Returns what was spent.
    public int SpendAllHomunculus()
    {
        int spent = HomunculusHp;
        HomunculusHp = 0;
        return spent;
    }

    /// One drain trigger took hpLost from an enemy; the homunculus gains the same amount.
    public void RecordDrain(int hpLost)
    {
        if (hpLost <= 0) return;
        HpDrainedThisCombat += hpLost;
        GainHomunculus(hpLost);
    }
}

public static class DrainRules
{
    /// A trigger deals damage equal to the stacks, then loses one stack, on the same turn-start timing as
    /// the base game's poison.
    public static int Damage(int stacks) => Math.Max(0, stacks);
    public static int StacksAfterTrigger(int stacks) => Math.Max(0, stacks - 1);

    /// HP actually lost: damage past the target's remaining HP is not drained into the homunculus.
    public static int HpLost(int unblockedDamage, int overkillDamage) => Math.Max(0, unblockedDamage - overkillDamage);

    /// Total HP a stack pile deals when triggered `times` times in a row (e.g. 生命の収穫), each trigger
    /// decaying as usual. Stops once the stacks run out.
    public static int TotalOverTriggers(int stacks, int times)
    {
        int total = 0;
        for (int i = 0; i < times && stacks > 0; i++) { total += Damage(stacks); stacks = StacksAfterTrigger(stacks); }
        return total;
    }

    /// 手本C: cost falls by one per `perStep` HP drained this combat, never below zero.
    public static int ReducedCost(int baseCost, int hpDrained, int perStep)
        => perStep <= 0 ? baseCost : Math.Max(0, baseCost - Math.Max(0, hpDrained) / perStep);
}

public enum DebuffSide { Player, Enemy }

/// <summary>
/// 死亡 (from 人体錬成): the owner dies at the start of its own side's next turn. Between the player's turn and
/// the player's next turn the enemy side's turn starts first, which is what lets drain and a copied death
/// finish an enemy before the player's own death resolves.
/// </summary>
public static class DeathMarkRules
{
    public static bool TriggersAt(DebuffSide owner, DebuffSide turnStarting) => owner == turnStarting;
}

/// <summary>A debuff as seen by デバフ転写: which power, how much, and whether it is currently a debuff.</summary>
public readonly record struct StatusCopy(string PowerId, int Amount, bool IsDebuff);

public static class DebuffTransfer
{
    /// What デバフ転写 copies onto one enemy: every debuff the player currently has, at the same amount.
    /// Buffs, and powers whose current amount is zero, are left alone. The player keeps their own debuffs.
    public static IReadOnlyList<StatusCopy> Plan(IEnumerable<StatusCopy> playerPowers)
        => [.. playerPowers.Where(p => p.IsDebuff && p.Amount != 0)];
}
