using System.Reflection;
using Alchemist;
using Alchemist.Core;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Models.Powers;

[ModInitializer(nameof(Initialize))]
public static class Smoke
{
    public static void Initialize() => new Harmony("AlchemistSmoke").PatchAll(Assembly.GetExecutingAssembly());
    private static void Check(bool ok, string name) { if(!ok) throw new Exception(name); GD.Print("ALCHEMIST_SMOKE_PASS " + name); }
    [HarmonyPatch(typeof(OneTimeInitialization), nameof(OneTimeInitialization.ExecuteDeferred))]
    public static class Ready
    {
        public static void Postfix()
        {
            try
            {
                var character = ModelDb.Character<AlchemistCharacter>();
                Check(ModelDb.AllCharacters.Contains(character),"character registered");
                Check(character.Title.GetFormattedText() == "錬金術師","Japanese character localization");
                var player = Player.CreateForNewRun(character,UnlockState.all,1);
                var run = RunState.CreateForNewRun([player],ActModel.GetDefaultList().Select(a=>a.ToMutable()).ToList(),[],GameMode.Standard,0,"ALCHEMIST_SMOKE_01");
                Check(player.Deck.Cards.Count==9,"starting deck nine");
                Check(player.Deck.Cards.Count(c=>c is StrikeIronclad)==4 && player.Deck.Cards.Count(c=>c is DefendIronclad)==4 && player.Deck.Cards.Count(c=>c is PortableFurnace)==1,"deck composition 4/4/1");
                var box = player.GetRelic<MaterialBox>()!;
                Check(box.Inventory.Total==0,"starter inventory empty");
                box.Inventory.Grant("a",Alchemist.Core.Material.Iron);
                box.Inventory.Grant("b",Alchemist.Core.Material.Iron);
                var copy = (MaterialBox)RelicModel.FromSerializable(box.ToSerializable());
                Check(copy.Inventory.Total==2,"game relic serialization roundtrip");
                Check(copy.Inventory.Received.Contains("b"),"receipt survives game serializer");
                foreach(var canonical in new CardModel[]{ModelDb.Card<PortableFurnace>(),ModelDb.Card<FurnaceActivation>(),ModelDb.Card<IronGuard>(),ModelDb.Card<HerbalEdge>(),ModelDb.Card<AlchemicalBlast>(),ModelDb.Card<EtherLens>(),ModelDb.Card<HerbalGuard>()})
                {
                    Check(canonical.Pool is AlchemyCardPool,"card pool "+canonical.Id);
                    Check(ResourceLoader.Exists(canonical.PortraitPath),"portrait "+canonical.Id);
                    var mutable=run.CreateCard(canonical,player);
                    mutable.UpgradeInternal();
                    var restored=CardModel.FromSerializable(mutable.ToSerializable());
                    Check(restored.IsUpgraded && restored.Id==mutable.Id,"upgraded card save "+canonical.Id);
                }
                Check(ResourceLoader.Exists(box.PackedIconPath),"relic portrait");
                foreach(var f in ForgeCatalog.All)
                {
                    var card=run.CreateCard<ForgedCard>(player);
                    card.AlchemistFormula=f.Id;
                    Check(card.Title==f.Name && card.EnergyCost.Canonical==f.Cost,"formula identity and cost "+f.Id);
                    Check(card.GetDescriptionForPile(PileType.None).Contains(f.Describe().Split('。')[0]),"formula description "+f.Id);
                    Check(card.Keywords.Contains(CardKeyword.Retain)==f.Retain && card.Keywords.Contains(CardKeyword.Exhaust)==f.Exhaust,"formula keywords "+f.Id);
                    card.UpgradeInternal();
                    var loaded=(ForgedCard)CardModel.FromSerializable(card.ToSerializable());
                    var cloned=(ForgedCard)run.CloneCard(card);
                    Check(loaded.AlchemistFormula==f.Id && loaded.IsUpgraded && loaded.Type==card.Type && loaded.TargetType==card.TargetType,"formula save metadata "+f.Id);
                    Check(loaded.Keywords.SetEquals(card.Keywords) && loaded.EnergyCost.Canonical==f.Cost,"formula save cost keywords "+f.Id);
                    foreach(var (key,value) in f.Values) Check(loaded.DynamicVars[key].BaseValue==value+f.Upgrade.GetValueOrDefault(key) && cloned.DynamicVars[key].BaseValue==loaded.DynamicVars[key].BaseValue,"formula upgrade clone save "+f.Id+"/"+key);
                    loaded.DowngradeInternal();
                    Check(f.Values.All(x=>loaded.DynamicVars[x.Key].BaseValue==x.Value) && loaded.Keywords.Contains(CardKeyword.Exhaust)==f.Exhaust && loaded.Keywords.Contains(CardKeyword.Retain)==f.Retain,"formula downgrade retains identity "+f.Id);
                }
                GD.Print("ALCHEMIST_SMOKE_COMPLETE");
                _ = CombatLoop();
            }
            catch(Exception ex) { GD.PushError("ALCHEMIST_SMOKE_FAIL " + ex); }
        }
    }
    private static async Task Until(Func<bool> condition, string stage)
    {
        for(int i=0;i<200;i++) { if(condition()) return; await Task.Delay(100); }
        throw new TimeoutException(stage);
    }
    private static async Task CombatLoop()
    {
        try
        {
            await PreloadManager.LoadCommonAndMainMenuAssets();
            SaveManager.Instance.SetFtuesEnabled(false); // Isolated fixture only; no interactive tutorial in automated tests.
            var player=Player.CreateForNewRun<AlchemistCharacter>(UnlockState.all,1);
            var run=RunState.CreateForNewRun([player],ActModel.GetDefaultList().Select(a=>a.ToMutable()).ToList(),[],GameMode.Standard,0,"ALCHEMIST_LOOP_01");
            RunManager.Instance.SetUpNewSingleplayer(run,false);
            await (Task)AccessTools.Method(typeof(NGame),"StartRun").Invoke(NGame.Instance,[run])!;
            GD.Print("ALCHEMIST_LOOP run started");
            await RunManager.Instance.EnterRoomDebug(RoomType.Monster,model:ModelDb.Encounter<BowlbugsWeak>().ToMutable(),showTransition:false);
            var box=player.GetRelic<MaterialBox>()!;
            await Until(()=>box.Combat != null && player.PlayerCombatState?.Hand.Cards.Count>0,"first battle ready");
            Check(box.Combat!.Phase==Alchemist.Core.Material.Iron,"real battle starts iron");
            await CreatureCmd.Kill(player.Creature.CombatState!.Enemies.ToArray(),true);
            await Until(()=>!CombatManager.Instance.IsInProgress && run.CurrentRoom is CombatRoom {IsPreFinished:true},"battle victory");
            Check(box.Inventory.Counts[0]==2,"real death hooks harvest two iron");
            WorkshopUi.Tick(NRun.Instance!,1);
            WorkshopUi.Open();
            AccessTools.Field(typeof(WorkshopUi),"workshop").SetValue(null,true);
            await (Task)AccessTools.Method(typeof(WorkshopUi),"Craft").Invoke(null,[Recipes.All[0]])!;
            Check(player.Deck.Cards.Count(c=>c is IronGuard)==1 && box.Inventory.Total==0,"real workshop crafts and consumes");
            AccessTools.Method(typeof(WorkshopUi),"Close").Invoke(null,null);
            await RunManager.Instance.EnterRoomDebug(RoomType.Monster,model:ModelDb.Encounter<BowlbugsWeak>().ToMutable(),showTransition:false);
            await Until(()=>box.Combat != null && player.PlayerCombatState?.Hand.Cards.Count>0,"second battle ready");
            var card=player.PlayerCombatState!.AllCards.OfType<IronGuard>().Single();
            int before=player.Creature.Block;
            await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(),card,null,skipCardPileVisuals:true);
            Check(player.Creature.Block==before+16,"crafted card usable next battle");
            foreach(var f in ForgeCatalog.All)
            {
                await RunManager.Instance.EnterRoomDebug(RoomType.Monster,model:ModelDb.Encounter<BowlbugsWeak>().ToMutable(),showTransition:false);
                await Until(()=>box.Combat != null && player.PlayerCombatState?.Hand.Cards.Count>0,"formula battle ready");
                await Task.Delay(600);
                var forged=player.Creature.CombatState!.CreateCard<ForgedCard>(player);
                forged.AlchemistFormula=f.Id;
                var target=forged.TargetType==TargetType.AnyEnemy ? player.Creature.CombatState.Enemies[0] : null;
                await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(),forged,target,skipCardPileVisuals:true);
                if(f.Values.TryGetValue("Fumes",out int fumes)) Check(player.Creature.GetPower<NoxiousFumesPower>()?.Amount==fumes,"culture power applied");
                if(f.Values.TryGetValue("ExhaustBlock",out int block)) Check(player.Creature.GetPower<FeelNoPainPower>()?.Amount==block && player.Creature.GetPower<DarkEmbracePower>()?.Amount==1,"recycling powers applied");
                Check(forged.Pile?.Type==(f.Exhaust?PileType.Exhaust:f.Kind==ForgeKind.Power?PileType.None:PileType.Discard) || f.Kind==ForgeKind.Power,"formula executes "+f.Id);
            }
            GD.Print("ALCHEMIST_LOOP_COMPLETE");
        }
        catch(Exception ex) { GD.PushError("ALCHEMIST_LOOP_FAIL "+ex); }
    }
}
