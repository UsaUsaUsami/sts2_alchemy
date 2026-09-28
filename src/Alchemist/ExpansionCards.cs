using Alchemist.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist;

// v0.22 cards from the 2026-09-26 follow-up (design-axes.md 4.3). Names are tentative; numbers are prototype values.

/// <summary>F-1（仮名：自己培養）. One of the three forbidden cards that put a status on yourself (3.4).</summary>
public sealed class LifeSelfCultivation() : ElementCard(1,CardType.Power,CardRarity.Rare,TargetType.Self,AlchemyPhase.Water)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new PowerVar<SelfCultivationPower>(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<SelfCultivationPower>(),HoverTipFactory.FromPower<LifeDrainPower>()];
    public override List<(string,string)> Localization=>new CardLoc("自己培養","自分のターン開始時、自分に[gold]ドレイン[/gold]{SelfCultivationPower:diff()}を付与する。自分に付いた[gold]ドレイン[/gold]で得る[gold]ホムンクルスHP[/gold]は2倍になる。\n[gold]水相[/gold]");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<SelfCultivationPower>(c,DynamicVars["SelfCultivationPower"].BaseValue);
    // v0.23 (ユーザーレビュー): drain 1 halved straight back to 0 (v0.22.4), worth 1 HP for 2 homunculus HP a turn.
    // 2 (upgrade 3) settles at 3 HP for 6 (5 for 10); halving keeps the stack from piling up, so the upgrade
    // raises it instead of lowering the cost (design-axes 4.3 F-1 revised).
    protected override void OnUpgrade()=>DynamicVars["SelfCultivationPower"].UpgradeValueBy(1);
}

public sealed class SelfCultivationPower : AlchemyPower
{
    public const int SelfDrainMultiplier = 2;
    protected override PowerModel IconSource => ModelDb.Power<RegenPower>();
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<LifeDrainPower>()];
    public override List<(string,string)> Localization => new PowerLoc("自己培養",
        "自分のターン開始時、自分にドレインを付与する。自分に付いたドレインで得るホムンクルスHPは2倍になる。",
        "自分のターン開始時、自分に[gold]ドレイン[/gold]{Amount}を付与する。自分に付いた[gold]ドレイン[/gold]で得る[gold]ホムンクルスHP[/gold]は2倍になる。");
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;
        Flash();
        await PowerCmd.Apply<LifeDrainPower>(new ThrowingPlayerChoiceContext(), Owner, Amount, Owner, null);
    }
}

/// <summary>F-2（仮名：アルカヘスト）. Pours up to ten materials into one hit that grows with count and variety.</summary>
public sealed class AlchAlkahest() : ElementCard(2,CardType.Attack,CardRarity.Rare,TargetType.AnyEnemy,AlchemyPhase.Fire)
{
    public const int MaxMaterials = 10;
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DynamicVar("PerMaterial",3),new DynamicVar("MaxMaterials",MaxMaterials)];
    public override List<(string,string)> Localization=>new CardLoc("アルカヘスト",
        "通常素材を1〜{MaxMaterials}個選んで消費する。消費した素材の数×{PerMaterial:diff()}×消費した素材の種類数のダメージを与える。",
        ("selectionScreenPrompt","注ぎ込む素材を選択（最大10個）"));
    // design-axes 4.3 F-2 (未決, settled here): with no material the card cannot be played at all, rather than
    // spending two energy on nothing.
    protected override bool IsPlayable=>HasNormalMaterial;
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p)
    {
        if(Box is not { } box) return;
        List<(Material Material,CardModel Card)> units=[];
        foreach(var m in Enum.GetValues<Material>())
            for(int i=0;i<Math.Min(box.Inventory.Counts[(int)m],MaxMaterials);i++)
            {
                var unit=MaterialCards.Canonical(m).ToMutable(); unit.Owner=Owner; unit.AfterCreated();
                units.Add((m,unit));
            }
        if(units.Count==0) return;
        var chosen=(await CardSelectCmd.FromSimpleGrid(c,units.Select(u=>u.Card).ToList(),Owner,
            new CardSelectorPrefs(SelectionScreenPrompt,1,Math.Min(MaxMaterials,units.Count)))).ToHashSet();
        foreach(var u in units) u.Card.Owner=null!;
        var spent=units.Where(u=>chosen.Contains(u.Card)).GroupBy(u=>u.Material).Select(g=>(g.Key,Count:g.Count())).ToArray();
        int total=0;
        foreach(var (m,count) in spent) if(box.Inventory.TryConsume(m,count)) total+=count;
        int kinds=spent.Count(x=>x.Count>0);
        if(total>0) await Hit(c,p,total*DynamicVars["PerMaterial"].IntValue*kinds);
    }
    protected override void OnUpgrade()=>DynamicVars["PerMaterial"].UpgradeValueBy(1);
}

/// <summary>
/// F-3（仮名：四元の刃）. Cheaper for each kind of normal material held. Kinds, not count: counting units would
/// reward hoarding, against spending in combat (design-axes 6.1). v0.23 (ユーザーレビュー): 5 cost, so all four
/// normal kinds leave it at 1 and only holding a rare material as well brings it to 0.
/// </summary>
public sealed class AirElementBlade() : ElementCard(5,CardType.Attack,CardRarity.Uncommon,TargetType.AnyEnemy,AlchemyPhase.Air)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(18,ValueProp.Move)];
    public override List<(string,string)> Localization=>new CardLoc("四元の刃","{Damage:diff()}ダメージ。所持している通常素材の種類数だけコストが下がる。希少素材を持っていれば、さらに1下がる。\n[gold]風相[/gold]");
    private int KindsHeld=>Box?.Inventory is { } inventory ? inventory.Counts.Count(n=>n>0)+(inventory.RareCounts.Any(n=>n>0)?1:0) : 0;
    public override bool TryModifyEnergyCostInCombat(CardModel card,decimal originalCost,out decimal modifiedCost)
    {
        modifiedCost=originalCost;
        if(card!=this || KindsHeld==0) return false;
        modifiedCost=Math.Max(0,originalCost-KindsHeld);
        return true;
    }
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>Hit(c,p,DynamicVars.Damage.BaseValue);
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(6);
}

/// <summary>F-4（仮名：四相輪転）. See PhaseWheelPower.</summary>
public sealed class AlchPhaseWheel() : AlchemyCard(3,CardType.Power,CardRarity.Rare,TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<PhaseWheelPower>()];
    public override List<(string,string)> Localization=>new CardLoc("四相輪転","カードを使うたび、そのカードで[gold]相転移[/gold]が起きなかったなら、次の相へ進む（地→水→火→風→地）。");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<PhaseWheelPower>(c,1);
    protected override void OnUpgrade()=>EnergyCost.UpgradeBy(-1);
}

/// <summary>
/// Steps the phase after a card play that caused no transition of its own (design-axes 4.3 F-4). The card's own
/// element resolves first. The condition closes a loop: stepping after every card made each free card land a
/// transition, and every fourth one a draw (docs/test-results.md v0.22). MaterialBox applies it.
/// </summary>
public sealed class PhaseWheelPower : AlchemyPower
{
    public override PowerStackType StackType => PowerStackType.Single;
    protected override PowerModel IconSource => ModelDb.Power<EchoFormPower>();
    public override List<(string,string)> Localization => new PowerLoc("四相輪転",
        "カードを使うたび、そのカードで相転移が起きなかったなら、次の相へ進む。",
        "カードを使うたび、そのカードで相転移が起きなかったなら、次の相へ進む（地→水→火→風→地）。");
    internal void Pulse() => Flash();
}

/// <summary>
/// G-3（仮名：ダーヴの坩堝）. The alchemist's ancient card: Darv's Dusty Tome gives it, upgraded (BaseLib
/// ITomeCard). Ancient rarity keeps it out of rewards and the merchant.
/// </summary>
public sealed class DarvCrucibleCard() : AlchemyCard(2,CardType.Power,CardRarity.Ancient,TargetType.Self), ITomeCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips=>[HoverTipFactory.FromPower<CruciblePower>()];
    public override List<(string,string)> Localization=>new CardLoc("ダーヴの坩堝","自分のターン開始時、現在相に対応する素材を1個得る。素材ボックスが満杯なら得ない。");
    protected override Task OnPlay(PlayerChoiceContext c,CardPlay p)=>ApplySelf<CruciblePower>(c,1);
    protected override void OnUpgrade()=>EnergyCost.UpgradeBy(-1);
}

public sealed class CruciblePower : AlchemyPower
{
    public override PowerStackType StackType => PowerStackType.Single;
    protected override PowerModel IconSource => ModelDb.Power<RegenPower>();
    public override List<(string,string)> Localization => new PowerLoc("ダーヴの坩堝",
        "自分のターン開始時、現在相に対応する素材を1個得る。素材ボックスが満杯なら得ない。",
        "自分のターン開始時、現在相に対応する素材を1個得る。素材ボックスが満杯なら得ない。");
    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner) && Owner.Player is { } player && CrucibleTrickle.Grant(player, "crucible", combatState.RoundNumber)) Flash();
        return Task.CompletedTask;
    }
}
