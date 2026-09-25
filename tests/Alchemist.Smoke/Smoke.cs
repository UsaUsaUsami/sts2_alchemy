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
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Creatures;

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
                Check(player.Deck.Cards.Count(c=>c is StrikeIronclad)==3 && player.Deck.Cards.Count(c=>c is DefendIronclad)==3
                    && player.Deck.Cards.Count(c=>c is EarthenGuard)==1 && player.Deck.Cards.Count(c=>c is SoothingMist)==1
                    && player.Deck.Cards.Count(c=>c is InstantAlchemy)==1,"deck composition 3/3/earth/water/instant");
                Check(ModelDb.Potion<EarthPhial>() is not null && ModelDb.Potion<WaterPhial>() is not null
                    && ModelDb.Potion<FirePhial>() is not null && ModelDb.Potion<AirPhial>() is not null,
                    "workshop-only potion models registered without a random-drop pool");
                foreach(var potion in new PotionModel[]{ModelDb.Potion<EarthPhial>(),ModelDb.Potion<WaterPhial>(),ModelDb.Potion<FirePhial>(),ModelDb.Potion<AirPhial>()})
                {
                    Check(potion.Pool is TokenPotionPool,"workshop potion sits in the token pool "+potion.Id.Entry);
                    var owned=potion.ToMutable(); owned.Owner=player;
                    Check(!string.IsNullOrWhiteSpace(owned.DynamicDescription.GetFormattedText()),"workshop potion description resolves "+potion.Id.Entry);
                }
                Check(!ModelDb.PotionPool<IroncladPotionPool>().AllPotions.Any(p=>p is EarthPhial or WaterPhial or FirePhial or AirPhial),"workshop potions never enter the random-drop pool");
                var box = player.GetRelic<MaterialBox>()!;
                Check(box.Inventory.Total==0,"starter inventory empty");
                box.Inventory.Grant("a",Alchemist.Core.Material.Iron);
                box.Inventory.Grant("b",Alchemist.Core.Material.Iron);
                var copy = (MaterialBox)RelicModel.FromSerializable(box.ToSerializable());
                Check(copy.Inventory.Total==2,"game relic serialization roundtrip");
                Check(copy.Inventory.Received.Contains("b"),"receipt survives game serializer");
                foreach(var canonical in new CardModel[]{ModelDb.Card<FurnaceActivation>()}.Concat(Recipes.All.Select(r=>CraftedCards.ForRecipe(r.Id))))
                {
                    Check(canonical.Pool is AlchemyCardPool,"card pool "+canonical.Id);
                    Check(ResourceLoader.Exists(canonical.PortraitPath),"portrait "+canonical.Id);
                    var mutable=run.CreateCard(canonical,player);
                    mutable.UpgradeInternal();
                    var restored=CardModel.FromSerializable(mutable.ToSerializable());
                    Check(restored.IsUpgraded && restored.Id==mutable.Id,"upgraded card save "+canonical.Id);
                }
                Check(ResourceLoader.Exists(box.PackedIconPath),"relic portrait");
                var resonance=ModelDb.Power<PhaseResonancePower>();
                Check(ResourceLoader.Exists(resonance.CustomPackedIconPath!) && ResourceLoader.Exists(resonance.CustomBigIconPath!),"phase resonance power icons exist");
                Check(new[]{CardType.Attack,CardType.Skill,CardType.Power}.All(t=>ModelDb.CardPool<AlchemyCardPool>().AllCards
                    .Any(c=>c.Type==t && c.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare)),
                    "the pool has an attack, a skill and a power for the merchant");
                var craftedModels=Recipes.All.Select(r=>CraftedCards.ForRecipe(r.Id)).ToArray();
                Check(craftedModels.Length==10 && craftedModels.All(c=>c.Rarity==CardRarity.Event && ResourceLoader.Exists(c.PortraitPath)),
                    "ten workshop cards resolve, use the Event rarity and have portraits");
                Check(craftedModels.Count(c=>c.Type==CardType.Power)==4 && craftedModels.OfType<DualElementCard>().Count()==6,"four core powers and six dual-phase cards");
                var inscribed=run.CreateCard(ModelDb.Card<LavaShot>(),player);
                ((LavaShot)inscribed).AlchemistRareModifier="rare.mercury";
                var inscribedLoaded=(LavaShot)CardModel.FromSerializable(inscribed.ToSerializable());
                Check(inscribedLoaded.AlchemistRareModifier=="rare.mercury" && inscribedLoaded.Keywords.Contains(CardKeyword.Retain),
                    "a crafted card keeps its inscription through the game serializer");
                Check(inscribedLoaded.GetDescriptionForPile(PileType.None).Contains("保留"),"an inscribed crafted card shows its inscription text");
                var rewardPool=ModelDb.CardPool<AlchemyCardPool>().AllCards.Where(c=>c.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare).ToArray();
                GD.Print($"ALCHEMIST_STAT pool={rewardPool.Length} common={rewardPool.Count(c=>c.Rarity==CardRarity.Common)} uncommon={rewardPool.Count(c=>c.Rarity==CardRarity.Uncommon)} rare={rewardPool.Count(c=>c.Rarity==CardRarity.Rare)} attack={rewardPool.Count(c=>c.Type==CardType.Attack)} skill={rewardPool.Count(c=>c.Type==CardType.Skill)} power={rewardPool.Count(c=>c.Type==CardType.Power)}");
                Check(rewardPool.Length==76 && rewardPool.Count(c=>c.Rarity==CardRarity.Common)==20 && rewardPool.Count(c=>c.Rarity==CardRarity.Uncommon)==33
                    && rewardPool.Count(c=>c.Rarity==CardRarity.Rare)==23,"reward pool is 76 cards: 20 common, 33 uncommon, 23 rare");
                Check(rewardPool.Count(c=>c.Type==CardType.Power)==14 && !rewardPool.Any(c=>c.Type==CardType.Power && c.Rarity==CardRarity.Common),
                    "fourteen powers, none of them common, as in the base pools");
                Check(PhaseRules.Elements.All(e=>rewardPool.Count(c=>c is AlchemyCard a && a.Element==e)==16),"sixteen reward cards per element");
                Check(rewardPool.All(c=>ResourceLoader.Exists(c.PortraitPath)),"every reward card has a portrait");
                foreach(var power in ModelDb.AllPowers.OfType<AlchemyPower>())
                    Check(ResourceLoader.Exists(power.CustomPackedIconPath!) && ResourceLoader.Exists(power.CustomBigIconPath!),"power icons "+power.Id.Entry);
                foreach(var power in new PowerModel[]{ModelDb.Power<WeakPower>(),ModelDb.Power<VulnerablePower>(),ModelDb.Power<PoisonPower>(),ModelDb.Power<StrengthPower>(),
                    ModelDb.Power<PlatingPower>(),ModelDb.Power<ThornsPower>(),ModelDb.Power<VigorPower>(),ModelDb.Power<BlurPower>(),ModelDb.Power<BarricadePower>(),
                    ModelDb.Power<AfterimagePower>(),ModelDb.Power<NoxiousFumesPower>(),ModelDb.Power<BlockNextTurnPower>(),ModelDb.Power<EnergyNextTurnPower>(),ModelDb.Power<DrawCardsNextTurnPower>()})
                    GD.Print($"ALCHEMIST_POWER_TITLE {power.Id.Entry}={power.Title.GetFormattedText()}");
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
    // Elite and boss encounters can arrive in waves, so killing the enemies captured once is not enough.
    private static async Task Clear(string stage)
    {
        var player=RunManager.Instance.DebugOnlyGetState()!.Players[0];
        for(int wave=0;wave<8 && CombatManager.Instance.IsInProgress;wave++)
        {
            var alive=player.Creature.CombatState?.Enemies.Where(e=>!e.IsDead).ToArray() ?? [];
            if(alive.Length>0) await CreatureCmd.Kill(alive,true);
            await Task.Delay(300);
        }
        if(CombatManager.Instance.IsInProgress)
        {
            var left=player.Creature.CombatState?.Enemies.Select(e=>$"{e.CombatId}:{e.CurrentHp}") ?? [];
            throw new TimeoutException($"{stage} victory (残: {string.Join(",",left)})");
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
            Check(box.Combat!.Phases.Current==AlchemyPhase.None,"real battle starts without an element");
            await Clear("first battle");
            Check(box.Inventory.Total==0,"enemy deaths grant no materials");
            var normalSet=new RewardsSet(player).WithRewardsFromRoom((CombatRoom)run.CurrentRoom!);
            await normalSet.GenerateWithoutOffering();
            Check(normalSet.Rewards.Any(r=>r is GoldReward) && normalSet.Rewards.Any(r=>r is CardReward),
                "normal combat keeps gold and restores the standard card reward");
            box.Inventory.Grant("workshop-fixture-iron-1",Alchemist.Core.Material.Iron);
            box.Inventory.Grant("workshop-fixture-iron-2",Alchemist.Core.Material.Iron);
            var coordParts=plannedNodes[0].Split(',').Select(int.Parse).ToArray();
            await RunManager.Instance.EnterMapCoordDebug(new MapCoord(coordParts[0],coordParts[1]),RoomType.RestSite,MapPointType.RestSite,showTransition:false);
            await Until(()=>WorkshopUi.IsOpen,"map workshop opens");
            Check(run.CurrentRoom is RestSiteRoom { Options.Count: 0 },"workshop node offers no resting or upgrading");
            var uiOverlay=(Control?)AccessTools.Field(typeof(WorkshopUi),"overlay").GetValue(null);
            // MaterialCardRow mounts card-style visuals for owned materials in the sidebar, distinct from
            // the recipe gallery in content; card-count checks below scope to content to avoid conflating
            // the two now that both use NGridCardHolder.
            var uiContent=(Control?)AccessTools.Field(typeof(WorkshopUi),"content").GetValue(null);
            Check(uiOverlay is not null && uiOverlay.GetChildren().OfType<PanelContainer>().Any(),"workshop responsive frame created");
            var labels=Descendants<Label>(uiOverlay!).Select(x=>x.Text).ToArray();
            Check(labels.Any(x=>x.Contains("錬金工房")) && labels.Any(x=>x.Contains("容量")),"workshop material sidebar visible");
            labels=Descendants<Label>(uiOverlay!).Select(x=>x.Text).ToArray();
            var buttons=Descendants<Button>(uiOverlay!).ToArray();
            Check(labels.Any(x=>x.Contains("完成カードを選ぶ")) && buttons.Any(x=>x.Text.Contains("作成可能のみ")),"workshop card gallery visible");
            Check(Descendants<NGridCardHolder>(uiContent!).Count()==Recipes.All.Count(),"all completed cards rendered");
            int ownedMaterialTypes=Enum.GetValues<Alchemist.Core.Material>().Count(m=>box.Inventory.Counts[(int)m]>0)
                +Enum.GetValues<RareMaterial>().Count(m=>box.Inventory.RareCounts[(int)m]>0);
            Check(Descendants<NGridCardHolder>(uiOverlay!).Count()==Recipes.All.Count()+ownedMaterialTypes,
                "the sidebar's MaterialCardRow adds one card per owned material type on top of the gallery");
            await Task.Delay(200);
            var descriptionField=AccessTools.Field(typeof(NCard),"_descriptionLabel");
            var recipeNodes=Descendants<NCard>(uiOverlay!).ToArray();
            Check(recipeNodes.All(n=>n.Visibility==ModelVisibility.Visible),"all recipe cards are explicitly visible");
            Check(recipeNodes.Select(n=>((MegaRichTextLabel)descriptionField.GetValue(n)!).Text)
                .All(t=>!string.IsNullOrWhiteSpace(t) && !t.Contains("If you can read this",StringComparison.OrdinalIgnoreCase)),
                "all recipe descriptions replace the scene diagnostic placeholder on first open");
            // Overriding holder.Scale leaves cards stuck at SmallScale once hovered; the display scale
            // belongs on the parent node instead.
            Check(Descendants<NGridCardHolder>(uiOverlay!).All(h=>h.Scale.IsEqualApprox(NCardHolder.smallScale)),"card holders keep their own hover scale");
            var previewCards=(List<CardModel>)AccessTools.Field(typeof(WorkshopUi),"previewCards").GetValue(null)!;
            Check(previewCards.Count==Recipes.All.Count()+ownedMaterialTypes && previewCards.All(c=>!run.ContainsCard(c)),"card previews do not enter run state");
            AccessTools.Field(typeof(WorkshopUi),"materialCraftMode").SetValue(null,true);
            AccessTools.Method(typeof(WorkshopUi),"Refresh").Invoke(null,null);
            buttons=Descendants<Button>(uiOverlay!).ToArray();
            Check(Descendants<Label>(uiOverlay!).Any(x=>x.Text.Contains("素材から錬成")) && buttons.Count(x=>x.Text.Contains("空きスロット"))==2,
                "material-first crafting grid opens with two empty slots");
            AccessTools.Method(typeof(WorkshopUi),"AddCraftMaterial").Invoke(null,[Alchemist.Core.Material.Iron]);
            AccessTools.Method(typeof(WorkshopUi),"AddCraftMaterial").Invoke(null,[Alchemist.Core.Material.Iron]);
            labels=Descendants<Label>(uiOverlay!).Select(x=>x.Text).ToArray();
            Check(labels.Any(x=>x.Contains("鉄 ＋ 鉄 から作れるカード"))
                && Descendants<NGridCardHolder>(uiContent!).Count()==Recipes.FindAll(Alchemist.Core.Material.Iron,Alchemist.Core.Material.Iron).Count(),
                "two selected materials reveal every matching recipe without consuming them");
            Check(box.Inventory.Counts[(int)Alchemist.Core.Material.Iron]==2,"placing materials in the grid is a preview, not consumption");
            AccessTools.Field(typeof(WorkshopUi),"selectedRecipe").SetValue(null,Recipes.All[0]);
            AccessTools.Method(typeof(WorkshopUi),"Refresh").Invoke(null,null);
            labels=Descendants<Label>(uiOverlay!).Select(x=>x.Text).ToArray();
            buttons=Descendants<Button>(uiOverlay!).ToArray();
            Check(labels.Any(x=>x.Contains("このカードを錬成しますか")) && buttons.Any(x=>x.Text=="このカードを作る") && Descendants<NGridCardHolder>(uiContent!).Count()==1,"selected card confirmation visible");
            await (Task)AccessTools.Method(typeof(WorkshopUi),"Craft").Invoke(null,[Recipes.All[0]])!;
            Check(player.Deck.Cards.Count(c=>c is EarthCore)==1 && box.Inventory.Total==0,"real workshop crafts and consumes");
            box.Inventory.Grant("upgrade-herb",Alchemist.Core.Material.Herb);
            box.Inventory.Grant("upgrade-ether",Alchemist.Core.Material.Ether);
            AccessTools.Field(typeof(WorkshopUi),"upgradeMode").SetValue(null,true);
            AccessTools.Method(typeof(WorkshopUi),"Refresh").Invoke(null,null);
            Check(Descendants<Label>(uiOverlay!).Any(x=>x.Text.Contains("改造するカード")),"modification gallery visible");
            var elemental=player.Deck.Cards.OfType<EarthenGuard>().Single();
            AccessTools.Method(typeof(WorkshopUi),"UpgradeCard").Invoke(null,[elemental]);
            Check(elemental.Enchantment is WorkshopTuning && box.Inventory.Total==0,"workshop modification consumes materials and persists on the card");
            var rareCard=(LavaShot)run.CreateCard(ModelDb.Card<LavaShot>(),player);
            var rareAdded=await CardPileCmd.Add(rareCard,PileType.Deck,skipVisuals:true);
            rareCard=(LavaShot)rareAdded.cardAdded;
            box.Inventory.GrantRare("smoke-stardust",RareMaterial.Stardust);
            AccessTools.Field(typeof(WorkshopUi),"upgradeMode").SetValue(null,false);
            AccessTools.Field(typeof(WorkshopUi),"rareMode").SetValue(null,true);
            AccessTools.Method(typeof(WorkshopUi),"Refresh").Invoke(null,null);
            Check(Descendants<Label>(uiOverlay!).Any(x=>x.Text.Contains("希少加工する錬成カード")),"rare processing gallery visible");
            AccessTools.Method(typeof(WorkshopUi),"ApplyRare").Invoke(null,[rareCard,RareMaterials.Get(RareMaterial.Stardust)]);
            Check(rareCard.AlchemistRareModifier=="rare.stardust" && box.Inventory.RareCounts[(int)RareMaterial.Stardust]==0,
                "workshop permanently applies and consumes stardust");
            var restoredRare=(LavaShot)CardModel.FromSerializable(rareCard.ToSerializable());
            Check(restoredRare.AlchemistRareModifier=="rare.stardust","rare modifier survives card serialization");
            box.Inventory.Grant("second-craft-iron-1",Alchemist.Core.Material.Iron);
            box.Inventory.Grant("second-craft-iron-2",Alchemist.Core.Material.Iron);
            await (Task)AccessTools.Method(typeof(WorkshopUi),"Craft").Invoke(null,[Recipes.All[0]])!;
            Check(player.Deck.Cards.Count(c=>c is EarthCore)==1 && box.Inventory.Counts[(int)Alchemist.Core.Material.Iron]==2,
                "synthesis is limited to once per workshop visit even with materials left");
            int potionsBefore=player.Potions.Count();
            await (Task)AccessTools.Method(typeof(WorkshopUi),"BrewPotion").Invoke(null,[Alchemist.Core.Material.Iron])!;
            Check(player.Potions.Count(p=>p is EarthPhial)==1 && player.Potions.Count()==potionsBefore+1
                && box.Inventory.Counts[(int)Alchemist.Core.Material.Iron]==1,"brewing turns one iron into an earth phial in a potion slot");
            await (Task)AccessTools.Method(typeof(WorkshopUi),"BrewPotion").Invoke(null,[Alchemist.Core.Material.Iron])!;
            Check(player.Potions.Count()==potionsBefore+1 && box.Inventory.Counts[(int)Alchemist.Core.Material.Iron]==1,
                "brewing is limited to once per workshop visit");
            AccessTools.Field(typeof(WorkshopUi),"rareMode").SetValue(null,false);
            AccessTools.Method(typeof(WorkshopUi),"Refresh").Invoke(null,null);
            var facilityButtons=Descendants<Button>(uiOverlay!).Select(b=>b.Text).ToArray();
            Check(new[]{"錬成","改造","調薬","付与"}.All(f=>facilityButtons.Any(t=>t.StartsWith(f+"\n") && t.Contains("使用済み"))),
                "all four facilities were usable in one visit and now read as used");
            await (Task)AccessTools.Method(typeof(WorkshopUi),"LeaveMapWorkshop").Invoke(null,null)!;
            await RunManager.Instance.EnterRoomDebug(RoomType.Monster,model:ModelDb.Encounter<BowlbugsWeak>().ToMutable(),showTransition:false);
            await Until(()=>box.Combat != null && player.PlayerCombatState?.Hand.Cards.Count>0,"second battle ready");
            Check(box.Combat!.Phases.Current==AlchemyPhase.None,"next battle resets the elemental phase");
            // Wait for the opening draw and the relic's tokens to finish; playing earlier races the turn-start draw.
            await Until(()=>player.PlayerCombatState!.Hand.Cards.OfType<FurnaceActivation>().Count()==HarvestCombat.FurnaceLimit
                && player.PlayerCombatState.Hand.Cards.Count>=5+HarvestCombat.FurnaceLimit,"opening hand and furnace tokens dealt");
            await Task.Delay(600);
            var tokens=player.PlayerCombatState!.Hand.Cards.OfType<FurnaceActivation>().ToArray();
            Check(tokens.Length==HarvestCombat.FurnaceLimit && tokens.All(t=>!t.CanPlay()),
                "the starter relic deals the furnace every combat, unplayable in the neutral phase");
            var card=player.PlayerCombatState!.AllCards.OfType<EarthCore>().Single();
            int before=player.Creature.Block;
            await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(),card,null,skipCardPileVisuals:true);
            Check(player.Creature.GetPower<EarthCorePower>()?.Amount==3 && player.Creature.Block==before && box.Combat.Phases.Current==AlchemyPhase.Earth,
                "crafted core power usable next battle, and entering the first element triggers nothing");
            Check(tokens.All(t=>t.CanPlay()),"furnace becomes playable once an element is active");
            var cs=player.Creature.CombatState!;
            var foe=cs.Enemies[0];
            foe.SetMaxHpInternal(999);foe.SetCurrentHpInternal(999);
            async Task Play(CardModel c,Creature? target)
            {
                await CardPileCmd.AddGeneratedCardsToCombat([c],PileType.Hand,player);
                await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(),c,target,skipCardPileVisuals:true);
            }
            await Play(cs.CreateCard<SoothingMist>(player),foe);
            Check(box.Combat.Phases.Current==AlchemyPhase.Water && foe.GetPower<WeakPower>()?.Amount==2+PhaseRules.BaseAmount(AlchemyPhase.Water),
                $"earth to water adds the water transition's weak (weak {foe.GetPower<WeakPower>()?.Amount})");
            int hpBefore=foe.CurrentHp;
            await Play(cs.CreateCard<Alchemist.Ignition>(player),foe);
            Check(box.Combat.Phases.Current==AlchemyPhase.Fire && hpBefore-foe.CurrentHp==10+PhaseRules.BaseAmount(AlchemyPhase.Fire),
                $"water to fire adds the fire transition's damage (dealt {hpBefore-foe.CurrentHp})");
            hpBefore=foe.CurrentHp;
            await Play(cs.CreateCard<FlashPowder>(player),null);
            Check(box.Combat.Phases.TransitionCount==2 && hpBefore-foe.CurrentHp==6,"a same-element card does not transition again");
            var tailwind=cs.CreateCard<Tailwind>(player);
            await CardPileCmd.AddGeneratedCardsToCombat([tailwind],PileType.Hand,player);
            // Count the draw pile: an exhausting card can still sit in the hand when AutoPlay returns.
            int drawBefore=player.PlayerCombatState.DrawPile.Cards.Count;
            await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(),tailwind,null,skipCardPileVisuals:true);
            int drawn=drawBefore-player.PlayerCombatState.DrawPile.Cards.Count;
            Check(box.Combat.Phases.Current==AlchemyPhase.Air && drawn==tailwind.DynamicVars.Cards.IntValue+PhaseRules.BaseAmount(AlchemyPhase.Air),
                $"fire to air adds the air transition's draw (drew {drawn})");
            var tuned=cs.CreateCard<EarthenGuard>(player);
            Check(CardCmd.Enchant<WorkshopTuning>(tuned,1) is not null,"workshop tuning attaches to an elemental card");
            int blockBefore=player.Creature.Block;
            await Play(tuned,null);
            Check(player.Creature.Block-blockBefore==8+PhaseRules.BaseAmount(AlchemyPhase.Earth)+WorkshopTuning.Bonus+3,
                $"tuning and the earth core both strengthen the transition into earth (block {player.Creature.Block-blockBefore})");
            box.Inventory.Grant("instant-herb",Alchemist.Core.Material.Herb);
            int herbBefore=box.Inventory.Counts[(int)Alchemist.Core.Material.Herb];
            var picker=new PickSelector(c=>c is HerbImprovisation);
            using(CardSelectCmd.UseSelector(picker))
                await Play(cs.CreateCard<InstantAlchemy>(player),null);
            Check(picker.Offered.All(c=>box.Inventory.Counts[(int)(c switch{IronImprovisation=>Alchemist.Core.Material.Iron,HerbImprovisation=>Alchemist.Core.Material.Herb,PowderImprovisation=>Alchemist.Core.Material.Powder,_=>Alchemist.Core.Material.Ether})]>0 || c is HerbImprovisation),
                "instant alchemy offers only materials the box holds");
            Check(box.Inventory.Counts[(int)Alchemist.Core.Material.Herb]==herbBefore-1 && player.PlayerCombatState.Hand.Cards.OfType<HerbImprovisation>().Count()==1,
                "instant alchemy spends one run material for a temporary water card");
            await Play(cs.CreateCard<PhaseResonance>(player),null);
            int resonanceBefore=player.Creature.Block;
            await Play(cs.CreateCard<SoothingMist>(player),foe);
            Check(player.Creature.Block-resonanceBefore==2,$"phase resonance reacts to a transition through the listener hook (block {player.Creature.Block-resonanceBefore})");
            var phial=player.Potions.OfType<EarthPhial>().First();
            int phialBlock=player.Creature.Block;
            await phial.OnUseWrapper(new ThrowingPlayerChoiceContext(),player.Creature);
            Check(player.Creature.Block-phialBlock==12 && !player.Potions.Contains(phial),"the brewed earth phial is usable in combat and is consumed");
            int turnTransitions=box.Combat.Phases.TransitionsThisTurn;
            foe.SetMaxHpInternal(999);foe.SetCurrentHpInternal(999);
            await Play(cs.CreateCard<AlchChainReaction>(player),foe);
            Check(turnTransitions>0 && 999-foe.CurrentHp==turnTransitions*10,$"chain reaction deals 10 per transition this turn ({turnTransitions} transitions, {999-foe.CurrentHp} damage)");
            int transitionsBefore=box.Combat.Phases.TransitionCount;
            await Play(cs.CreateCard<LavaShot>(player),foe);
            Check(box.Combat.Phases.TransitionCount==transitionsBefore+2 && box.Combat.Phases.Current==AlchemyPhase.Fire && box.Combat.Phases.Previous==AlchemyPhase.Earth,
                "a dual-phase card enters its first then its second element, transitioning twice");
            // Every reward card once, in this already-running battle: debug room changes mid-combat leave
            // disposed card nodes in the headless node pool, so the sweep avoids a fresh room.
            // Fixture: a settled box with two of each material, so material-spending cards are playable.
            box.Inventory.Pending.Clear(); box.Inventory.Counts=[2,2,2,2];
            var sweepState=cs;
            var firstPick=new PickSelector(c=>c is not CraftedCard); // never exhaust the stardust card checked below
            using(CardSelectCmd.UseSelector(firstPick))
            foreach(var canonical in ModelDb.CardPool<AlchemyCardPool>().AllCards.Where(c=>c.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare))
            {
                foreach(var enemy in sweepState.Enemies.Where(e=>!e.IsDead)) { enemy.SetMaxHpInternal(999); enemy.SetCurrentHpInternal(999); }
                if(box.Inventory.Counts.Sum()<2) box.Inventory.Counts=[2,2,2,2];
                var played=sweepState.CreateCard(canonical,player);
                var target=played.TargetType==TargetType.AnyEnemy ? sweepState.Enemies.First(e=>!e.IsDead) : null;
                try { await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(),played,target,skipCardPileVisuals:true); }
                catch(Exception ex) { throw new Exception("reward card plays "+canonical.Id.Entry,ex); }
                if(played is AlchemyCard { Element: not AlchemyPhase.None } sweptElemental)
                    Check(box.Combat!.Phases.Current==sweptElemental.Element,"reward card plays and enters its element "+canonical.Id.Entry);
                else Check(true,"reward card plays "+canonical.Id.Entry);
            }
            GD.Print($"ALCHEMIST_STAT sweepTransitions={box.Combat!.Phases.TransitionCount}");
            box.Inventory.Counts=[0,0,0,0]; // Leave room in the box for the reward and merchant checks below.
            var replayCard=player.PlayerCombatState.AllCards.OfType<LavaShot>().Single(c=>c.AlchemistRareModifier=="rare.stardust");
            var replayTarget=player.Creature.CombatState!.Enemies[0];
            replayTarget.SetMaxHpInternal(999);replayTarget.SetCurrentHpInternal(999);
            await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(),replayCard,replayTarget,skipCardPileVisuals:true);
            Check(replayTarget.CurrentHp<=999-replayCard.DynamicVars.Damage.BaseValue*2,"stardust replays the card exactly once");
            // Material reward slots are additional to furnace harvesting and to the normal rewards.
            // Passing an encounter model would override the room type with that encounter's own, so the room
            // type alone selects the elite and boss encounters here.
            var eliteRoom=(CombatRoom)await RunManager.Instance.EnterRoomDebug(RoomType.Elite,showTransition:false);
            await Until(()=>box.Combat!=null && player.PlayerCombatState?.Hand.Cards.Count>0,"elite battle ready");
            Check(eliteRoom.RoomType==RoomType.Elite,"elite room entered");
            int harvestBefore=box.Inventory.Total;
            await Clear("elite");
            int harvested=box.Inventory.Total-harvestBefore;
            var eliteSet=new RewardsSet(player).WithRewardsFromRoom(eliteRoom);
            await eliteSet.GenerateWithoutOffering();
            var eliteSlots=eliteSet.Rewards.OfType<MaterialReward>().ToArray();
            Check(eliteSlots.Length==MaterialOffers.EliteSlots,"elite rewards carry one material slot");
            Check(eliteSet.Rewards.Any(r=>r is GoldReward) && eliteSet.Rewards.Any(r=>r is RelicReward) && eliteSet.Rewards.Any(r=>r is CardReward),
                "material slot is added without removing gold, relic, or card rewards");
            Check(eliteSlots[0].Description.GetFormattedText()=="素材を選ぶ","material slot label localized");
            var candidates=box.Inventory.Offers.Single(o=>o.Id==eliteSlots[0].OfferId).Candidates;
            Check(candidates.Length==3 && candidates.Distinct().Count()==3,"the slot offers three distinct materials");
            Check(box.Inventory.Total==harvestBefore+harvested,"the slot does not grant a material before it is chosen");
            // Reopening the screen must neither duplicate the slot nor reroll it.
            var eliteAgain=new RewardsSet(player).WithRewardsFromRoom(eliteRoom);
            await eliteAgain.GenerateWithoutOffering();
            Check(eliteAgain.Rewards.OfType<MaterialReward>().Count()==1 && box.Inventory.Offers.Count(o=>o.Id==eliteSlots[0].OfferId)==1
                && box.Inventory.Offers.Single(o=>o.Id==eliteSlots[0].OfferId).Candidates.SequenceEqual(candidates),"reopening neither duplicates nor rerolls the slot");
            var choosing=WorkshopUi.ChooseOffer(box,eliteSlots[0].OfferId);
            await Until(()=>WorkshopUi.IsOpen,"material choice opens");
            var chooseOverlay=(Control?)AccessTools.Field(typeof(WorkshopUi),"overlay").GetValue(null);
            var choiceButtons=Descendants<Button>(chooseOverlay!).Select(x=>x.Text).ToArray();
            Check(Descendants<Label>(chooseOverlay!).Any(x=>x.Text.Contains("素材を1つ選ぶ")),"material choice screen visible");
            Check(candidates.All(m=>choiceButtons.Any(t=>t.StartsWith(RareMaterials.Name(m)))) && choiceButtons.Any(t=>t.Contains("辞退")),
                "three candidates and a decline are offered");
            int chosenBefore=box.Inventory.Count(candidates[0]);
            AccessTools.Method(typeof(WorkshopUi),"TakeOffer").Invoke(null,[eliteSlots[0].OfferId,candidates[0]]);
            Check(await choosing,"the rewards screen is released once the slot is resolved");
            Check(box.Inventory.Count(candidates[0])==chosenBefore+AlchemyState.YieldPerEvent && box.Inventory.Offers.Count==0,"choosing grants the doubled harvest yield");
            var eliteAfterTake=new RewardsSet(player).WithRewardsFromRoom(eliteRoom);
            await eliteAfterTake.GenerateWithoutOffering();
            Check(!eliteAfterTake.Rewards.OfType<MaterialReward>().Any(),"a taken slot is never offered again");
            var bossRoom=(CombatRoom)await RunManager.Instance.EnterRoomDebug(RoomType.Boss,showTransition:false);
            await Until(()=>box.Combat!=null && player.PlayerCombatState?.Hand.Cards.Count>0,"boss battle ready");
            Check(bossRoom.RoomType==RoomType.Boss,"boss room entered");
            await Clear("boss");
            var bossSet=new RewardsSet(player).WithRewardsFromRoom(bossRoom);
            await bossSet.GenerateWithoutOffering();
            var bossSlots=bossSet.Rewards.OfType<MaterialReward>().ToArray();
            Check(bossSlots.Length==MaterialOffers.BossSlots,"boss rewards carry two material slots");
            Check(bossSlots.Select(s=>s.OfferId).Distinct().Count()==2 && bossSlots.All(s=>box.Inventory.HasOffer(s.OfferId)),"boss slots are distinct and recorded");
            var bossOffers=bossSlots.Select(s=>box.Inventory.Offers.Single(o=>o.Id==s.OfferId)).ToArray();
            Check(bossOffers.Count(o=>o.Candidates.All(x=>x.Class==MaterialClass.Rare))==1
                && bossOffers.Single(o=>o.Candidates.All(x=>x.Class==MaterialClass.Rare)).Candidates.Select(x=>x.RareMaterial).ToHashSet().SetEquals(Enum.GetValues<RareMaterial>()),
                "one boss slot guarantees all three rare materials");
            Check(bossSet.Rewards.Any(r=>r is GoldReward) && bossSet.Rewards.Any(r=>r is CardReward),"boss keeps its gold and standard card reward");
            // The real rewards screen lives on the overlay stack, so the picker has to be parented there
            // and added after it, or it would draw behind. Offer() is not awaited: it completes only once
            // every reward has been taken.
            _ = bossSet.Offer();
            await Until(()=>GodotObject.IsInstanceValid(NOverlayStack.Instance)
                && Descendants<NRewardButton>(NOverlayStack.Instance).Any(b=>b.Reward is MaterialReward),"rewards screen shows the material slots");
            Check(Descendants<NRewardButton>(NOverlayStack.Instance!).Count(b=>b.Reward is MaterialReward)==MaterialOffers.BossSlots,
                "both boss slots appear as reward buttons");
            var rewardsScreen=NOverlayStack.Instance!.GetChildren().OfType<NRewardsScreen>().Last();
            var choosingBoss=WorkshopUi.ChooseOffer(box,bossSlots[0].OfferId);
            await Until(()=>WorkshopUi.IsOpen,"boss material choice opens");
            var bossOverlay=(Control?)AccessTools.Field(typeof(WorkshopUi),"overlay").GetValue(null);
            Check(bossOverlay is not null && bossOverlay.GetParent()==NOverlayStack.Instance
                && bossOverlay.GetIndex()>rewardsScreen.GetIndex(),"the picker draws above the rewards screen");
            // Taking one slot must leave the other intact across a save and reload of the relic state.
            AccessTools.Method(typeof(WorkshopUi),"TakeOffer").Invoke(null,[bossSlots[0].OfferId,box.Inventory.Offers.Single(o=>o.Id==bossSlots[0].OfferId).Candidates[0]]);
            Check(await choosingBoss,"the boss rewards screen is released once its slot is resolved");
            var reloadedBox=(MaterialBox)RelicModel.FromSerializable(box.ToSerializable());
            Check(reloadedBox.Inventory.Offers.Count==1 && reloadedBox.Inventory.HasOffer(bossSlots[1].OfferId)
                && !reloadedBox.Inventory.Settled,"a half-claimed boss reward survives serialization");
            AccessTools.Method(typeof(WorkshopUi),"DeclineOffer").Invoke(null,[bossSlots[1].OfferId]);
            Check(box.Inventory.Offers.Count==0 && box.Inventory.Settled,"declining clears the remaining slot");
            // Merchant material purchase: a custom panel gated to the shop room, not the game's own
            // character-card slots (those complete a purchase by adding straight to the deck).
            if(WorkshopUi.IsOpen) AccessTools.Method(typeof(WorkshopUi),"Close").Invoke(null,null);
            var shopRoom=(MerchantRoom)await RunManager.Instance.EnterRoomDebug(RoomType.Shop,showTransition:false);
            Check(shopRoom.RoomType==RoomType.Shop,"merchant room entered");
            await Task.Delay(300);
            var merchantNode=Descendants<NMerchantRoom>(NRun.Instance!).Last();
            merchantNode.OpenInventory();
            await Until(()=>merchantNode.Inventory.IsOpen,"native merchant inventory opens");
            var materialEntry=merchantNode.Inventory.GetNodeOrNull<Button>("AlchemistMaterialShopButton");
            Check(materialEntry is not null && materialEntry.Text.Contains("錬金素材"),"material entry is mounted on the visible merchant inventory");
            materialEntry!.EmitSignal(BaseButton.SignalName.Pressed);
            await Until(()=>WorkshopUi.IsOpen,"merchant material shop opens from its visible button");
            var shopOverlay=(Control?)AccessTools.Field(typeof(WorkshopUi),"overlay").GetValue(null);
            await PlayerCmd.GainGold(500,player);
            AccessTools.Method(typeof(WorkshopUi),"Refresh").Invoke(null,null);
            var shopLabels=Descendants<Label>(shopOverlay!).Select(x=>x.Text).ToArray();
            var shopButtons=Descendants<Button>(shopOverlay!).ToArray();
            var shopOffers=(MerchantMaterialOffer[])AccessTools.Property(typeof(WorkshopUi),"CurrentMerchantOffers").GetValue(null)!;
            Check(shopOffers.Length==MerchantMaterialOffers.Slots && shopOffers.All(o=>shopLabels.Any(t=>t.Contains($"{Recipes.Name(o.Material)}　{MerchantMaterialOffers.Price}G"))),
                "three deterministic material slots are priced and offered");
            Check(shopButtons.Count(b=>b.Text=="購入する")==MerchantMaterialOffers.Slots,"enough gold makes every stocked material purchasable");
            int goldBefore=player.Gold;
            var bought=shopOffers[0];
            int materialBefore=box.Inventory.Counts[(int)bought.Material];
            await (Task)AccessTools.Method(typeof(WorkshopUi),"BuyMaterial").Invoke(null,[bought])!;
            Check(player.Gold==goldBefore-MerchantMaterialOffers.Price && box.Inventory.Counts[(int)bought.Material]==materialBefore+1,
                "buying a material spends gold once and grants exactly one unit");
            int goldAfter=player.Gold;
            await (Task)AccessTools.Method(typeof(WorkshopUi),"BuyMaterial").Invoke(null,[bought])!;
            Check(player.Gold==goldAfter && box.Inventory.Received.Contains(bought.Id),"a sold slot cannot be purchased twice");
            Check(Descendants<NGridCardHolder>(shopOverlay!).Any(),"material box renders card-style visuals for owned materials");
            AccessTools.Field(typeof(WorkshopUi),"merchantMode").SetValue(null,false);
            AccessTools.Method(typeof(WorkshopUi),"Close").Invoke(null,null);
            await RunManager.Instance.EnterRoomDebug(RoomType.Monster,model:ModelDb.Encounter<BowlbugsWeak>().ToMutable(),showTransition:false);
            await Until(()=>box.Combat != null && player.PlayerCombatState?.Hand.Cards.Count>0,"post-shop battle ready");
            await Clear("post-shop battle");
            WorkshopUi.Open();
            await Until(()=>WorkshopUi.IsOpen,"material box opens outside the merchant room");
            var nonShopOverlay=(Control?)AccessTools.Field(typeof(WorkshopUi),"overlay").GetValue(null);
            Check(!Descendants<Button>(nonShopOverlay!).Any(b=>b.Text.Contains("商人で素材を買う")),"merchant purchase entry hidden away from the shop room");
            AccessTools.Method(typeof(WorkshopUi),"Close").Invoke(null,null);
            await RunManager.Instance.EnterRoomDebug(RoomType.Monster,model:ModelDb.Encounter<BowlbugsWeak>().ToMutable(),showTransition:false);
            await Until(()=>box.Combat!=null && player.PlayerCombatState?.Hand.Cards.OfType<FurnaceActivation>().Count()==HarvestCombat.FurnaceLimit,"furnace battle ready");
            await Task.Delay(600);
            var sacrifices=new CardModel[] {
                player.Creature.CombatState!.CreateCard<StrikeIronclad>(player),
                player.Creature.CombatState.CreateCard<DefendIronclad>(player)
            };
            await CardPileCmd.AddGeneratedCardsToCombat(sacrifices,PileType.Hand,player);
            box.Combat!.Phases.Enter(AlchemyPhase.Earth);
            var activations=player.PlayerCombatState!.Hand.Cards.OfType<FurnaceActivation>().Take(2).ToArray();
            Check(activations.Length==2,"the relic's two furnace activations are in hand");
            var selector=new TestCardSelector();
            using(CardSelectCmd.UseSelector(selector))
            {
                for(int i=0;i<2;i++)
                {
                    var material=AlchemyPhaseState.MaterialFor(box.Combat!.Phases.Current)!.Value;
                    int beforeMaterial=box.Inventory.Counts[(int)material];
                    selector.PrepareToSelect([sacrifices[i]]);
                    await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(),activations[i],null,skipCardPileVisuals:true);
                    Check(sacrifices[i].Pile?.Type==PileType.Exhaust,"furnace activation exhausts the selected card");
                    Check(box.Inventory.Counts[(int)material]==beforeMaterial+AlchemyState.YieldPerEvent,
                        "successful furnace exhaust grants the current-phase material immediately");
                }
            }
            Check(box.Combat!.FurnaceUsed==2,"furnace harvesting is capped at two per combat");
            GD.Print("ALCHEMIST_LOOP_COMPLETE");
        }
        catch(Exception ex) { GD.PushError("ALCHEMIST_LOOP_FAIL "+ex); }
    }
    private sealed class PickSelector(Func<CardModel,bool> pick) : ICardSelector
    {
        public List<CardModel> Offered { get; } = [];
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options,int minSelect,int maxSelect)
        {
            var current=options.ToArray();
            Offered.AddRange(current);
            return Task.FromResult<IEnumerable<CardModel>>(current.Where(pick).Take(maxSelect).ToArray());
        }
        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options,IReadOnlyList<CardRewardAlternative> alternatives)
            => throw new NotSupportedException();
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
