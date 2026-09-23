using Alchemist.Core;
using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist;

// Formula variants share execution, preview and persistence instead of one class per crafted card.
public sealed class ForgedCard() : AlchemyCard(2,CardType.Attack,CardRarity.Event,TargetType.AnyEnemy)
{
    private string formulaId = "greatblade.v1";
    private string rareModifierId = "";
    public ForgeFormula Formula => ForgeCatalog.Get(formulaId);
    public RareMaterialDefinition? RareModifier => rareModifierId.Length == 0 ? null : RareMaterials.Get(rareModifierId);
    [SavedProperty]
    public string AlchemistFormula
    {
        get => formulaId;
        set
        {
            AssertMutable();
            _ = ForgeCatalog.Get(value); // Validate before changing existing data.
            formulaId=value;
            ApplyConfiguration();
        }
    }
    [SavedProperty]
    public string AlchemistRareModifier
    {
        get => rareModifierId;
        set
        {
            AssertMutable();
            if(value.Length>0) _=RareMaterials.Get(value);
            rareModifierId=value;
            ApplyConfiguration();
        }
    }
    private void ApplyConfiguration()
    {
        foreach(var (key,var) in DynamicVars) var.BaseValue=Formula.Values.GetValueOrDefault(key);
        bool voided=RareModifier?.Material==RareMaterial.VoidCrystal;
        if(RareModifier?.Material==RareMaterial.Stardust && BaseReplayCount<1) BaseReplayCount=1;
        EnergyCost.SetCustomBaseCost(Math.Max(0,Formula.Cost-(voided?1:0)));
        RemoveKeyword(CardKeyword.Exhaust);
        RemoveKeyword(CardKeyword.Retain);
        if(Formula.Exhaust || voided) AddKeyword(CardKeyword.Exhaust);
        if(Formula.Retain || RareModifier?.Material==RareMaterial.Mercury) AddKeyword(CardKeyword.Retain);
    }
    public override string Title => Formula.Name + (IsUpgraded ? "+" : "");
    public override CardType Type => Formula.Kind switch { ForgeKind.Attack=>CardType.Attack,ForgeKind.Skill=>CardType.Skill,_=>CardType.Power };
    public override TargetType TargetType => Formula.Target switch { ForgeTarget.Enemy=>TargetType.AnyEnemy,ForgeTarget.AllEnemies=>TargetType.AllEnemies,_=>TargetType.Self };
    protected override int CanonicalEnergyCost => Math.Max(0,Formula.Cost-(RareModifier?.Material==RareMaterial.VoidCrystal?1:0));
    public override bool GainsBlock => Formula.Values.ContainsKey("Block");
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            if(Formula.Retain || RareModifier?.Material==RareMaterial.Mercury) yield return CardKeyword.Retain;
            if(Formula.Exhaust || RareModifier?.Material==RareMaterial.VoidCrystal) yield return CardKeyword.Exhaust;
        }
    }
    protected override CardModel Artwork => Formula.Kind switch { ForgeKind.Power=>ModelDb.Card<Inflame>(),ForgeKind.Skill=>ModelDb.Card<ShrugItOff>(),_=>ModelDb.Card<Thunderclap>() };
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(28,ValueProp.Move),new BlockVar(8,ValueProp.Move),new CardsVar(0),
        new PowerVar<PoisonPower>(0),new PowerVar<VulnerablePower>(0),
        new PowerVar<StrengthPower>(0),new PowerVar<DexterityPower>(0),new DynamicVar("Hits",0),
        new DynamicVar("ExhaustBlock",0),new DynamicVar("ExhaustDraw",0),new DynamicVar("Fumes",0),
        new DynamicVar("AdvancePhase",0)];
    public override List<(string,string)> Localization => [
        ("title","錬成カード"),("description",ForgeCatalog.Get("greatblade.v1").Text),
        ..ForgeCatalog.All.Select(f=>($"formula.{f.Id}.description",f.Text)),
        ..ForgeCatalog.All.SelectMany(f=>RareMaterials.All.Select(r=>($"formula.{f.Id}.{r.Id}.description",$"{f.Text}\n[gold]{r.EffectName}[/gold]：{r.Description}")))];
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            if(Formula.Values.ContainsKey("PoisonPower") || Formula.Values.ContainsKey("Fumes")) yield return HoverTipFactory.FromPower<PoisonPower>();
            if(Formula.Values.ContainsKey("VulnerablePower")) yield return HoverTipFactory.FromPower<VulnerablePower>();
            if(Formula.Values.ContainsKey("StrengthPower")) yield return HoverTipFactory.FromPower<StrengthPower>();
            if(Formula.Values.ContainsKey("DexterityPower")) yield return HoverTipFactory.FromPower<DexterityPower>();
            if(Formula.Values.ContainsKey("ExhaustBlock")) yield return HoverTipFactory.FromKeyword(CardKeyword.Exhaust);
        }
    }
    protected override void OnUpgrade()
    {
        foreach(var (key,value) in Formula.Upgrade) DynamicVars[key].UpgradeValueBy(value);
    }
    protected override void AfterDowngraded() => AlchemistFormula = formulaId;
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        if(DynamicVars.Vulnerable.BaseValue>0)
            await PowerCmd.Apply<VulnerablePower>(c,p.Target!,DynamicVars.Vulnerable.BaseValue,Owner.Creature,this);
        if(DynamicVars.Damage.BaseValue>0)
        {
            int hits=Math.Max(1,DynamicVars["Hits"].IntValue);
            for(int i=0;i<hits;i++)
            {
                var attack=DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this,p);
                if(TargetType==TargetType.AllEnemies) attack.TargetingAllOpponents(CombatState!);
                else attack.Targeting(p.Target!);
                await attack.Execute(c);
            }
        }
        if(DynamicVars.Poison.BaseValue>0)
        {
            if(TargetType==TargetType.AllEnemies) await PowerCmd.Apply<PoisonPower>(c,CombatState!.HittableEnemies,DynamicVars.Poison.BaseValue,Owner.Creature,this);
            else await PowerCmd.Apply<PoisonPower>(c,p.Target!,DynamicVars.Poison.BaseValue,Owner.Creature,this);
        }
        if(DynamicVars.Block.BaseValue>0) await CreatureCmd.GainBlock(Owner.Creature,DynamicVars.Block,p);
        if(DynamicVars.Cards.BaseValue>0) await CardPileCmd.Draw(c,DynamicVars.Cards.BaseValue,Owner);
        if(DynamicVars.Strength.BaseValue>0) await PowerCmd.Apply<StrengthPower>(c,Owner.Creature,DynamicVars.Strength.BaseValue,Owner.Creature,this);
        if(DynamicVars.Dexterity.BaseValue>0) await PowerCmd.Apply<DexterityPower>(c,Owner.Creature,DynamicVars.Dexterity.BaseValue,Owner.Creature,this);
        if(DynamicVars["ExhaustBlock"].BaseValue>0) await PowerCmd.Apply<FeelNoPainPower>(c,Owner.Creature,DynamicVars["ExhaustBlock"].BaseValue,Owner.Creature,this);
        if(DynamicVars["ExhaustDraw"].BaseValue>0) await PowerCmd.Apply<DarkEmbracePower>(c,Owner.Creature,DynamicVars["ExhaustDraw"].BaseValue,Owner.Creature,this);
        if(DynamicVars["Fumes"].BaseValue>0) await PowerCmd.Apply<NoxiousFumesPower>(c,Owner.Creature,DynamicVars["Fumes"].BaseValue,Owner.Creature,this);
        if(DynamicVars["AdvancePhase"].BaseValue>0)
            Owner.GetRelic<MaterialBox>()?.Combat?.AdvancePhase(DynamicVars["AdvancePhase"].IntValue);
    }
}

[HarmonyPatch(typeof(CardModel),nameof(CardModel.Description),MethodType.Getter)]
public static class ForgedDescriptionPatch
{
    public static bool Prefix(CardModel __instance,ref LocString __result)
    {
        if(__instance is not ForgedCard card) return true;
        string suffix=card.AlchemistRareModifier.Length==0 ? "" : $".{card.AlchemistRareModifier}";
        __result=new LocString("cards",$"{card.Id.Entry}.formula.{card.AlchemistFormula}{suffix}.description");
        return false;
    }
}
