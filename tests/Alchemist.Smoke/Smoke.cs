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
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

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
                bool mapsOk=true,everyRouteOk=true; int convertedFights=0,totalChosen=0,uncovered=0,midMiss=0,lateMiss=0;
                for(int seed=0;seed<100;seed++)
                {
                    var map=new StandardActMap(new Rng((ulong)seed),ModelDb.Act<Overgrowth>(),false,false);
                    int maxRow=map.GetRowCount()-1, restsBefore=map.GetAllMapPoints().Count(p=>p.PointType==MapPointType.RestSite);
                    var layout=WorkshopPlanner.SelectGuaranteed(WorkshopMap.BuildGraph(map),
                        new(map.StartingMapPoint.coord.col,map.StartingMapPoint.coord.row),
                        new(map.BossMapPoint.coord.col,map.BossMapPoint.coord.row),maxRow);
                    var chosen=layout.Coords;
                    if(!layout.MidGuaranteed || !layout.LateGuaranteed) uncovered++;
                    if(chosen.Count==0) continue;
                    var points=chosen.Select(p=>map.GetPoint(new MapCoord(p.Col,p.Row))!).ToArray();
                    totalChosen+=points.Length;
                    convertedFights+=points.Count(p=>p.PointType==MapPointType.Monster);
                    // Never spend a merchant, rest site or elite on a workshop.
                    mapsOk &= points.All(p=>p.CanBeModified && p.PointType is MapPointType.Unknown or MapPointType.Monster);
                    mapsOk &= chosen.All(p=>p.Row>=WorkshopPlanner.EarliestRow);
                    mapsOk &= map.GetAllMapPoints().Count(p=>p.PointType==MapPointType.RestSite)==restsBefore;
                    int midEnd=WorkshopPlanner.MidBandEndRow(maxRow);
                    bool midOk=BossUnreachableWithout(map,[..chosen.Where(p=>p.Row<=midEnd).Select(p=>new MapCoord(p.Col,p.Row))]);
                    bool lateOk=BossUnreachableWithout(map,[..chosen.Where(p=>p.Row>midEnd).Select(p=>new MapCoord(p.Col,p.Row))]);
                    if(!midOk) midMiss++;
                    if(!lateOk) lateMiss++;
                    // Coverage must hold wherever the planner claimed a guarantee.
                    everyRouteOk &= midOk || !layout.MidGuaranteed;
                    everyRouteOk &= lateOk || !layout.LateGuaranteed;
                    mapsOk &= chosen.Any(p=>p.Row<=midEnd) && chosen.Any(p=>p.Row>midEnd);
                }
                GD.Print($"ALCHEMIST_STAT workshopsPerAct={totalChosen/100.0:0.00} convertedFightsPerAct={convertedFights/100.0:0.00} uncoveredMaps={uncovered} midMiss={midMiss} lateMiss={lateMiss}");
                Check(mapsOk,"workshop placement valid across 100 real maps");
                Check(everyRouteOk,"claimed route coverage holds on 100 real maps");
                Check(uncovered<=2,"maps without full coverage stay rare and are reported");
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
    // A layer covers every route exactly when deleting it disconnects the boss from the start.
    private static bool BossUnreachableWithout(ActMap map,HashSet<MapCoord> removed)
    {
        var seen=new HashSet<MapCoord>();
        var queue=new Queue<MapPoint>([map.StartingMapPoint]);
        while(queue.Count>0)
        {
            var point=queue.Dequeue();
            foreach(var child in point.Children)
            {
                if(removed.Contains(child.coord) || !seen.Add(child.coord)) continue;
                if(child.coord.Equals(map.BossMapPoint.coord)) return false;
                queue.Enqueue(child);
            }
        }
        return true;
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
            await Until(()=>player.GetRelic<MaterialBox>()!.Inventory.WorkshopNodes.ContainsKey(run.CurrentActIndex),"workshops planned");
            var plannedNodes=player.GetRelic<MaterialBox>()!.Inventory.WorkshopNodes[run.CurrentActIndex];
            // Many points carry a workshop so that every branch has one, but any single route crosses
            // one in the mid band and one in the late band.
            int liveMidEnd=WorkshopPlanner.MidBandEndRow(run.Map.GetRowCount()-1);
            var liveRows=plannedNodes.Select(k=>int.Parse(k.Split(',')[1])).ToArray();
            Check(liveRows.Any(r=>r<=liveMidEnd) && liveRows.Any(r=>r>liveMidEnd),"live run places a mid and a late workshop band");
            var liveCoords=plannedNodes.Select(k=>k.Split(',').Select(int.Parse).ToArray()).Select(p=>new MapCoord(p[0],p[1])).ToArray();
            Check(BossUnreachableWithout(run.Map,[..liveCoords.Where(c=>c.row<=liveMidEnd)])
               && BossUnreachableWithout(run.Map,[..liveCoords.Where(c=>c.row>liveMidEnd)]),"live run route cannot skip either workshop band");
            await Task.Delay(100);
            Check(NMapScreen.Instance is not null && Descendants<Label>(NMapScreen.Instance).Count(x=>x.Text=="工房")==plannedNodes.Count,"workshops visible on map in advance");
            var mapPoints=Descendants<NNormalMapPoint>(NMapScreen.Instance!).ToArray();
            string IconOf(NNormalMapPoint p)=>p.GetNode<TextureRect>("%Icon").Texture?.ResourcePath ?? "";
            var workshopIcons=mapPoints.Where(p=>WorkshopMap.IsWorkshop(run,p.Point.coord)).Select(IconOf).ToArray();
            var restIcons=mapPoints.Where(p=>!WorkshopMap.IsWorkshop(run,p.Point.coord) && p.Point.PointType==MapPointType.RestSite).Select(IconOf).ToArray();
            Check(workshopIcons.Length==plannedNodes.Count && workshopIcons.All(x=>x.Contains("map_shop")) && restIcons.All(x=>!x.Contains("map_shop")),"workshop icon stays distinct from rest sites");
            await RunManager.Instance.EnterRoomDebug(RoomType.Monster,model:ModelDb.Encounter<BowlbugsWeak>().ToMutable(),showTransition:false);
            var box=player.GetRelic<MaterialBox>()!;
            await Until(()=>box.Combat != null && player.PlayerCombatState?.Hand.Cards.Count>0,"first battle ready");
            Check(box.Combat!.Phase==Alchemist.Core.Material.Iron,"real battle starts iron");
            await CreatureCmd.Kill(player.Creature.CombatState!.Enemies.ToArray(),true);
            await Until(()=>!CombatManager.Instance.IsInProgress && run.CurrentRoom is CombatRoom {IsPreFinished:true},"battle victory");
            Check(box.Inventory.Counts[0]==2,"real death hooks harvest two iron");
            var coordParts=plannedNodes[0].Split(',').Select(int.Parse).ToArray();
            await RunManager.Instance.EnterMapCoordDebug(new MapCoord(coordParts[0],coordParts[1]),RoomType.RestSite,MapPointType.RestSite,showTransition:false);
            await Until(()=>WorkshopUi.IsOpen,"map workshop opens");
            Check(run.CurrentRoom is RestSiteRoom { Options.Count: 0 },"workshop node offers no resting or upgrading");
            var uiOverlay=(Control?)AccessTools.Field(typeof(WorkshopUi),"overlay").GetValue(null);
            Check(uiOverlay is not null && uiOverlay.GetChildren().OfType<PanelContainer>().Any(),"workshop responsive frame created");
            var labels=Descendants<Label>(uiOverlay!).Select(x=>x.Text).ToArray();
            Check(labels.Any(x=>x.Contains("錬金工房")) && labels.Any(x=>x.Contains("容量")),"workshop material sidebar visible");
            labels=Descendants<Label>(uiOverlay!).Select(x=>x.Text).ToArray();
            var buttons=Descendants<Button>(uiOverlay!).ToArray();
            Check(labels.Any(x=>x.Contains("完成カードを選ぶ")) && buttons.Any(x=>x.Text.Contains("作成可能のみ")),"workshop card gallery visible");
            Check(Descendants<NGridCardHolder>(uiOverlay!).Count()==Recipes.All.Count(),"all completed cards rendered");
            // Overriding holder.Scale leaves cards stuck at SmallScale once hovered; the display scale
            // belongs on the parent node instead.
            Check(Descendants<NGridCardHolder>(uiOverlay!).All(h=>h.Scale.IsEqualApprox(NCardHolder.smallScale)),"card holders keep their own hover scale");
            var previewCards=(List<CardModel>)AccessTools.Field(typeof(WorkshopUi),"previewCards").GetValue(null)!;
            Check(previewCards.Count==Recipes.All.Count() && previewCards.All(c=>!run.ContainsCard(c)),"card previews do not enter run state");
            AccessTools.Field(typeof(WorkshopUi),"selectedRecipe").SetValue(null,Recipes.All[0]);
            AccessTools.Method(typeof(WorkshopUi),"Refresh").Invoke(null,null);
            labels=Descendants<Label>(uiOverlay!).Select(x=>x.Text).ToArray();
            buttons=Descendants<Button>(uiOverlay!).ToArray();
            Check(labels.Any(x=>x.Contains("このカードを錬成しますか")) && buttons.Any(x=>x.Text=="このカードを作る") && Descendants<NGridCardHolder>(uiOverlay!).Count()==1,"selected card confirmation visible");
            await (Task)AccessTools.Method(typeof(WorkshopUi),"Craft").Invoke(null,[Recipes.All[0]])!;
            Check(player.Deck.Cards.Count(c=>c is IronGuard)==1 && box.Inventory.Total==0,"real workshop crafts and consumes");
            box.Inventory.Grant("upgrade-iron",Alchemist.Core.Material.Iron);
            box.Inventory.Grant("upgrade-powder",Alchemist.Core.Material.Powder);
            AccessTools.Field(typeof(WorkshopUi),"upgradeMode").SetValue(null,true);
            AccessTools.Method(typeof(WorkshopUi),"Refresh").Invoke(null,null);
            Check(Descendants<Label>(uiOverlay!).Any(x=>x.Text.Contains("強化するカード")),"upgrade gallery visible");
            var strike=player.Deck.Cards.OfType<StrikeIronclad>().First(c=>c.IsUpgradable);
            AccessTools.Method(typeof(WorkshopUi),"UpgradeCard").Invoke(null,[strike]);
            Check(strike.IsUpgraded && box.Inventory.Total==0,"workshop upgrade consumes materials and upgrades card");
            await (Task)AccessTools.Method(typeof(WorkshopUi),"LeaveMapWorkshop").Invoke(null,null)!;
            await RunManager.Instance.EnterRoomDebug(RoomType.Monster,model:ModelDb.Encounter<BowlbugsWeak>().ToMutable(),showTransition:false);
            await Until(()=>box.Combat != null && player.PlayerCombatState?.Hand.Cards.Count>0,"second battle ready");
            Check(box.Combat!.Phase==Alchemist.Core.Material.Herb,"next battle continues material rotation");
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
                var hitTargets=forged.TargetType==TargetType.AllEnemies ? player.Creature.CombatState.Enemies.ToArray() : target is null ? [] : [target];
                if(f.Values.GetValueOrDefault("Hits")>1)
                    foreach(var enemy in hitTargets) { enemy.SetMaxHpInternal(999); enemy.SetCurrentHpInternal(999); }
                await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(),forged,target,skipCardPileVisuals:true);
                if(f.Values.TryGetValue("Fumes",out int fumes)) Check(player.Creature.GetPower<NoxiousFumesPower>()?.Amount==fumes,"culture power applied");
                if(f.Values.TryGetValue("ExhaustBlock",out int block)) Check(player.Creature.GetPower<FeelNoPainPower>()?.Amount==block && player.Creature.GetPower<DarkEmbracePower>()?.Amount==1,"recycling powers applied");
                if(f.Values.TryGetValue("StrengthPower",out int strength)) Check(player.Creature.GetPower<StrengthPower>()?.Amount==strength,"strength applied "+f.Id);
                if(f.Values.TryGetValue("DexterityPower",out int dexterity)) Check(player.Creature.GetPower<DexterityPower>()?.Amount==dexterity,"dexterity applied "+f.Id);
                if(f.Values.TryGetValue("Hits",out int hits) && hits>1)
                    Check(hitTargets.All(enemy=>999-enemy.CurrentHp>=f.Values["Damage"]*hits),"all multihit strikes land "+f.Id);
                Check(forged.Pile?.Type==(f.Exhaust?PileType.Exhaust:f.Kind==ForgeKind.Power?PileType.None:PileType.Discard) || f.Kind==ForgeKind.Power,"formula executes "+f.Id);
            }
            GD.Print("ALCHEMIST_LOOP_COMPLETE");
        }
        catch(Exception ex) { GD.PushError("ALCHEMIST_LOOP_FAIL "+ex); }
    }
    private static IEnumerable<T> Descendants<T>(Node node) where T : Node
    {
        foreach(var child in node.GetChildren())
        {
            if(child is T match) yield return match;
            foreach(var nested in Descendants<T>(child)) yield return nested;
        }
    }
}
