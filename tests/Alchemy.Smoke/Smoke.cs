using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using System.Reflection;
using Alchemy;
using Alchemy.Core;
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
using MegaCrit.Sts2.Core.Models.CardPools;
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
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Events;

[ModInitializer(nameof(Initialize))]
public static class Smoke
{
    public static void Initialize() => new Harmony("AlchemistSmoke").PatchAll(Assembly.GetExecutingAssembly());
    private static void Check(bool ok, string name) { if(!ok) { GD.Print("ALCHEMIST_SMOKE_FAIL " + name); throw new Exception(name); } GD.Print("ALCHEMIST_SMOKE_PASS " + name); }
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
                Check(player.Deck.Cards.Count(c=>c is StrikeAlchemist)==3 && player.Deck.Cards.Count(c=>c is DefendAlchemist)==3
                    && player.Deck.Cards.Count(c=>c is EarthenGuard)==1 && player.Deck.Cards.Count(c=>c is SoothingMist)==1
                    && player.Deck.Cards.Count(c=>c is InstantAlchemy)==1,"deck composition 3/3/earth/water/instant");
                // v0.24: Neow's transform picks from the card's own pool; the borrowed Ironclad Strike gave Ironclad cards.
                var transformFrom=player.Deck.Cards.First(c=>c is StrikeAlchemist);
                var transformOptions=MegaCrit.Sts2.Core.Factories.CardFactory.GetDefaultTransformationOptions(transformFrom,false).ToArray();
                Check(transformOptions.Length>0 && transformOptions.All(c=>c.Pool is AlchemyCardPool) && transformFrom.Tags.Contains(CardTag.Strike),
                    $"transforming a starter Strike offers only alchemist cards ({transformOptions.Length} options)");
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
                box.Inventory.Grant("a",Alchemy.Core.Material.Iron);
                box.Inventory.Grant("b",Alchemy.Core.Material.Iron);
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
                // Card list for docs/card-list.md (scripts/card-list.py reads these lines): the game's own text and numbers.
                foreach(var canonical in ModelDb.CardPool<AlchemyCardPool>().AllCards)
                {
                    var shown=run.CreateCard(canonical,player);
                    string text=shown.GetDescriptionForPile(PileType.None);
                    string? upgradedText=null; int? upgradedCost=null;
                    if(shown.IsUpgradable)
                    {
                        var upgraded=run.CreateCard(canonical,player);
                        upgraded.UpgradeInternal();
                        upgradedText=upgraded.GetDescriptionForPile(PileType.None);
                        upgradedCost=upgraded.EnergyCost.GetWithModifiers(CostModifiers.None);
                    }
                    var recipe=Recipes.All.FirstOrDefault(r=>CraftedCards.ForRecipe(r.Id).Id==canonical.Id);
                    GD.Print("ALCHEMIST_CARDDUMP "+System.Text.Json.JsonSerializer.Serialize(new{
                        id=canonical.Id.Entry, cls=canonical.GetType().Name, title=shown.Title, type=canonical.Type.ToString(), rarity=canonical.Rarity.ToString(),
                        cost=canonical.EnergyCost.Canonical, costsX=canonical.EnergyCost.CostsX, upgradedCost,
                        element=(canonical as AlchemyCard)?.Element.ToString(), target=canonical.TargetType.ToString(),
                        keywords=canonical.Keywords.Select(k=>k.ToString()).ToArray(), text, upgradedText,
                        recipe=recipe?.Materials.Select(m=>Recipes.Name(m)).ToArray()}));
                }
                // Pool comparison (scripts/pool-balance.py): every character pool's numbers and text, base and upgraded.
                foreach(var (poolName,pool) in new (string,CardPoolModel)[]{("Alchemist",ModelDb.CardPool<AlchemyCardPool>()),
                    ("Ironclad",ModelDb.CardPool<IroncladCardPool>()),("Silent",ModelDb.CardPool<SilentCardPool>()),("Defect",ModelDb.CardPool<DefectCardPool>()),
                    ("Necrobinder",ModelDb.CardPool<NecrobinderCardPool>()),("Regent",ModelDb.CardPool<RegentCardPool>())})
                foreach(var canonical in pool.AllCards)
                {
                    try
                    {
                        var shown=run.CreateCard(canonical,player);
                        var upgraded=shown.IsUpgradable?run.CreateCard(canonical,player):null;
                        upgraded?.UpgradeInternal();
                        GD.Print("ALCHEMIST_POOLDUMP "+System.Text.Json.JsonSerializer.Serialize(new{
                            pool=poolName, id=canonical.Id.Entry, title=shown.Title, type=canonical.Type.ToString(), rarity=canonical.Rarity.ToString(),
                            cost=canonical.EnergyCost.Canonical, costsX=canonical.EnergyCost.CostsX,
                            upgradedCost=upgraded?.EnergyCost.GetWithModifiers(CostModifiers.None),
                            element=(canonical as AlchemyCard)?.Element.ToString(), target=canonical.TargetType.ToString(),
                            keywords=canonical.Keywords.Select(k=>k.ToString()).ToArray(),
                            vars=shown.DynamicVars.ToDictionary(v=>v.Key,v=>v.Value.BaseValue),
                            upgradedVars=upgraded?.DynamicVars.ToDictionary(v=>v.Key,v=>v.Value.BaseValue),
                            text=shown.GetDescriptionForPile(PileType.None), upgradedText=upgraded?.GetDescriptionForPile(PileType.None)}));
                    }
                    catch(Exception e){GD.Print($"ALCHEMIST_POOLDUMP_SKIP {poolName} {canonical.Id.Entry} {e.GetType().Name}");}
                }
                // v0.24: card art from art/cards (CardArt). scripts/smoke.ps1 copies a fixture PNG for the furnace card.
                foreach(var sample in new CardModel[]{ModelDb.Card<StrikeIronclad>(),ModelDb.Card<DefendIronclad>(),ModelDb.Card<Inflame>(),ModelDb.Card<Whistle>()})
                    GD.Print($"ALCHEMIST_STAT portraitSize {sample.Id.Entry} {sample.Rarity} {sample.Portrait?.GetSize()}");
                var arted=ModelDb.Card<FurnaceActivation>();
                Check(CardArt.For(arted) is { } artTexture && arted.Portrait==artTexture,
                    $"a PNG in art/cards replaces the borrowed portrait ({CardArt.ArtDirectory})");
                var unarted=ModelDb.CardPool<AlchemyCardPool>().AllCards.FirstOrDefault(c=>!File.Exists(Path.Combine(CardArt.ArtDirectory,CardArt.Slug(c)+".png")));
                Check(unarted is null || (CardArt.For(unarted) is null && unarted.Portrait is not null && ResourceLoader.Exists(unarted.PortraitPath)),
                    $"a card without a PNG keeps its borrowed portrait ({unarted?.Id.Entry})");
                Check(ResourceLoader.Exists(box.PackedIconPath),"relic portrait");
                Check(new[]{CardType.Attack,CardType.Skill,CardType.Power}.All(t=>ModelDb.CardPool<AlchemyCardPool>().AllCards
                    .Any(c=>c.Type==t && c.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare)),
                    "the pool has an attack, a skill and a power for the merchant");
                var craftedModels=Recipes.All.Select(r=>CraftedCards.ForRecipe(r.Id)).ToArray();
                Check(craftedModels.Length==20 && craftedModels.All(c=>c.Rarity==CardRarity.Event && ResourceLoader.Exists(c.PortraitPath)),
                    "twenty workshop cards resolve, use the Event rarity and have portraits");
                Check(craftedModels.Count(c=>c.Type==CardType.Power)==7 && craftedModels.OfType<DualElementCard>().Count()==6,"four core powers, three life powers and six dual-phase cards");
                var inscribed=run.CreateCard(ModelDb.Card<LavaShot>(),player);
                ((LavaShot)inscribed).AlchemistRareModifier="rare.mercury";
                var inscribedLoaded=(LavaShot)CardModel.FromSerializable(inscribed.ToSerializable());
                Check(inscribedLoaded.AlchemistRareModifier=="rare.mercury" && inscribedLoaded.Keywords.Contains(CardKeyword.Retain),
                    "a crafted card keeps its inscription through the game serializer");
                Check(inscribedLoaded.GetDescriptionForPile(PileType.None).Contains("保留"),"an inscribed crafted card shows its inscription text");
                var rewardPool=ModelDb.CardPool<AlchemyCardPool>().AllCards.Where(c=>c.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare).ToArray();
                GD.Print($"ALCHEMIST_STAT pool={rewardPool.Length} common={rewardPool.Count(c=>c.Rarity==CardRarity.Common)} uncommon={rewardPool.Count(c=>c.Rarity==CardRarity.Uncommon)} rare={rewardPool.Count(c=>c.Rarity==CardRarity.Rare)} attack={rewardPool.Count(c=>c.Type==CardType.Attack)} skill={rewardPool.Count(c=>c.Type==CardType.Skill)} power={rewardPool.Count(c=>c.Type==CardType.Power)}");
                // v0.22 (design-axes 4.3, 8): the 60 of v0.21 plus four follow-up cards.
                Check(rewardPool.Length==64 && rewardPool.Count(c=>c.Rarity==CardRarity.Common)==15 && rewardPool.Count(c=>c.Rarity==CardRarity.Uncommon)==26
                    && rewardPool.Count(c=>c.Rarity==CardRarity.Rare)==23,"reward pool is 64 cards: 15 common, 26 uncommon, 23 rare");
                Check(rewardPool.Count(c=>c.Type==CardType.Power)==12 && !rewardPool.Any(c=>c.Type==CardType.Power && c.Rarity==CardRarity.Common),
                    "twelve powers, none of them common, as in the base pools");
                int ElementCount(AlchemyPhase e)=>rewardPool.Count(c=>c is AlchemyCard a && a.Element==e);
                // v0.23: the three starter cards left the pool (Basic); 岩盤 (earth) and そよ風, 風の衣 (air) took their slots.
                Check(ElementCount(AlchemyPhase.Earth)==12 && ElementCount(AlchemyPhase.Water)==12 && ElementCount(AlchemyPhase.Fire)==15
                    && ElementCount(AlchemyPhase.Air)==14,"reward cards per element at 64: earth 12, water 12, fire 15, air 14");
                Check(new CardModel[]{ModelDb.Card<EarthenGuard>(),ModelDb.Card<SoothingMist>(),ModelDb.Card<InstantAlchemy>()}.All(c=>c.Rarity==CardRarity.Basic && !rewardPool.Contains(c)),
                    "the starter's own cards are Basic and never offered as rewards");
                // 原則4: every card that could fuel an in-turn loop has a profile the rule test searches.
                foreach(var loopCandidate in ModelDb.CardPool<AlchemyCardPool>().AllCards.OfType<AlchemyCard>().Where(c=>c is not MaterialCard))
                {
                    var upgradedCandidate=(AlchemyCard)run.CreateCard(loopCandidate,player);
                    int baseCost=upgradedCandidate.EnergyCost.GetResolved();
                    if(upgradedCandidate.IsUpgradable) upgradedCandidate.UpgradeInternal();
                    bool fuel=baseCost==0 || upgradedCandidate.EnergyCost.GetResolved()==0
                        || upgradedCandidate.DynamicVars.Keys.Any(k=>k is "Energy" or "Cards");
                    string name=loopCandidate.GetType().Name;
                    if(fuel) Check(LoopProfiles.All.Any(x=>x.Id==name),"loop profile covers "+name);
                }
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
                // v0.22.3 (design-axes 6.3 G-3): Darv's Dusty Tome gives the alchemist the crucible ancient card.
                var tome=(DustyTome)ModelDb.Relic<DustyTome>().ToMutable();
                tome.SetupForPlayer(player);
                Check(tome.AncientCard==ModelDb.Card<DarvCrucibleCard>().Id,"Darv's Dusty Tome picks the crucible for the alchemist");
                var ironclad=Player.CreateForNewRun(ModelDb.Character<Ironclad>(),UnlockState.all,1);
                RunState.CreateForNewRun([ironclad],ActModel.GetDefaultList().Select(a=>a.ToMutable()).ToList(),[],GameMode.Standard,0,"ALCHEMIST_SMOKE_02");
                var ironTome=(DustyTome)ModelDb.Relic<DustyTome>().ToMutable();
                ironTome.SetupForPlayer(ironclad);
                Check(ironTome.AncientCard!=ModelDb.Card<DarvCrucibleCard>().Id,"other characters' Dusty Tome never gives the crucible");
                Check(ModelDb.Card<DarvCrucibleCard>().Rarity==CardRarity.Ancient && !rewardPool.Contains(ModelDb.Card<DarvCrucibleCard>()),"the crucible is an ancient card, never a reward");
                foreach(var path in new[]{ModelDb.Relic<DarvCrucible>().PackedIconPath,ModelDb.Relic<RefinedMaterialBox>().PackedIconPath,
                    "res://images/relics/black_blood.png","res://images/relics/philosophers_stone.png",
                    "res://images/atlases/relic_outline_atlas.sprites/black_blood.tres","res://images/atlases/relic_outline_atlas.sprites/philosophers_stone.tres",
                    "res://images/enchantments/sharp.png",ModelDb.Enchantment<WorkshopInfusion>().IconPath})
                    Check(ResourceLoader.Exists(path),"borrowed art exists "+path);
                var touch=(TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable();
                Check(touch.SetupForPlayer(player) && touch.UpgradedRelic==ModelDb.Relic<RefinedMaterialBox>().Id,"Orobas refines the material box instead of turning it into a Circlet");
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
            Check(workshopIcons.Length==plannedNodes.Count && workshopIcons.All(x=>x.EndsWith("workshop_map_packed.ctex")) && restIcons.All(x=>!x.Contains("workshop_map")),"workshop icon stays distinct from rest sites");
            await RunManager.Instance.EnterRoomDebug(RoomType.Monster,model:ModelDb.Encounter<BowlbugsWeak>().ToMutable(),showTransition:false);
            var box=player.GetRelic<MaterialBox>()!;
            await Until(()=>box.Combat != null && player.PlayerCombatState?.Hand.Cards.Count>0,"first battle ready");
            Check(box.Combat!.Phases.Current==AlchemyPhase.None,"the ordinary box starts a real battle without an element");
            await Clear("first battle");
            Check(box.Inventory.Total==0,"enemy deaths grant no materials");
            var normalSet=new RewardsSet(player).WithRewardsFromRoom((CombatRoom)run.CurrentRoom!);
            await normalSet.GenerateWithoutOffering();
            Check(normalSet.Rewards.Any(r=>r is GoldReward) && normalSet.Rewards.Any(r=>r is CardReward),
                "normal combat keeps gold and restores the standard card reward");
            box.Inventory.Grant("workshop-fixture-iron-1",Alchemy.Core.Material.Iron);
            box.Inventory.Grant("workshop-fixture-iron-2",Alchemy.Core.Material.Iron);
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
            int ownedMaterialTypes=Enum.GetValues<Alchemy.Core.Material>().Count(m=>box.Inventory.Counts[(int)m]>0)
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
            Check(Descendants<Label>(uiOverlay!).Any(x=>x.Text.Contains("素材から錬成")) && buttons.Count(x=>x.Text.Contains("空きスロット"))==5,
                "material-first crafting grid opens with five empty slots (recipes take two to five materials)");
            AccessTools.Method(typeof(WorkshopUi),"AddCraftMaterial").Invoke(null,[Alchemy.Core.Material.Iron]);
            AccessTools.Method(typeof(WorkshopUi),"AddCraftMaterial").Invoke(null,[Alchemy.Core.Material.Iron]);
            labels=Descendants<Label>(uiOverlay!).Select(x=>x.Text).ToArray();
            Check(labels.Any(x=>x.Contains("鉄 ＋ 鉄 から作れるカード"))
                && Descendants<NGridCardHolder>(uiContent!).Count()==Recipes.FindAll(Alchemy.Core.Material.Iron,Alchemy.Core.Material.Iron).Count(),
                "two selected materials reveal every matching recipe without consuming them");
            Check(box.Inventory.Counts[(int)Alchemy.Core.Material.Iron]==2,"placing materials in the grid is a preview, not consumption");
            AccessTools.Field(typeof(WorkshopUi),"selectedRecipe").SetValue(null,Recipes.All[0]);
            AccessTools.Method(typeof(WorkshopUi),"Refresh").Invoke(null,null);
            labels=Descendants<Label>(uiOverlay!).Select(x=>x.Text).ToArray();
            buttons=Descendants<Button>(uiOverlay!).ToArray();
            Check(labels.Any(x=>x.Contains("このカードを錬成しますか")) && buttons.Any(x=>x.Text=="このカードを作る") && Descendants<NGridCardHolder>(uiContent!).Count()==1,"selected card confirmation visible");
            await (Task)AccessTools.Method(typeof(WorkshopUi),"Craft").Invoke(null,[Recipes.All[0]])!;
            Check(player.Deck.Cards.Count(c=>c is EarthCore)==1 && box.Inventory.Total==0,"real workshop crafts and consumes");
            box.Inventory.Grant("upgrade-herb",Alchemy.Core.Material.Herb);
            box.Inventory.Grant("upgrade-ether",Alchemy.Core.Material.Ether);
            AccessTools.Field(typeof(WorkshopUi),"upgradeMode").SetValue(null,true);
            AccessTools.Method(typeof(WorkshopUi),"Refresh").Invoke(null,null);
            Check(Descendants<Label>(uiOverlay!).Any(x=>x.Text.Contains("改造するカード")),"modification gallery visible");
            var elemental=player.Deck.Cards.OfType<EarthenGuard>().Single();
            AccessTools.Field(typeof(WorkshopUi),"selectedUpgrade").SetValue(null,elemental);
            AccessTools.Method(typeof(WorkshopUi),"Refresh").Invoke(null,null);
            Check(Descendants<Button>(uiOverlay!).Any(b=>b.Text.StartsWith("火薬を入れる（アタックのみ）") && b.Disabled),"powder cannot go into a skill");
            AccessTools.Method(typeof(WorkshopUi),"AddInfuseMaterial").Invoke(null,[elemental,Alchemy.Core.Material.Herb]);
            AccessTools.Method(typeof(WorkshopUi),"AddInfuseMaterial").Invoke(null,[elemental,Alchemy.Core.Material.Ether]);
            Check(box.Inventory.Total==2,"choosing materials for a modification is a preview, not consumption");
            AccessTools.Method(typeof(WorkshopUi),"UpgradeCard").Invoke(null,[elemental]);
            Check(elemental.Enchantment is WorkshopInfusion { AlchemistInfuseHerb: 1, AlchemistInfuseEther: 1, AlchemistInfuseIron: 0, AlchemistInfusePowder: 0, Amount: 2 }
                && box.Inventory.Total==0,"workshop modification puts one effect per material into the card and consumes them");
            var infusedLoaded=CardModel.FromSerializable(elemental.ToSerializable());
            Check(infusedLoaded.Enchantment is WorkshopInfusion { AlchemistInfuseHerb: 1, AlchemistInfuseEther: 1, Amount: 2 },"each material count survives the game's card serialization");
            string infusedText=elemental.Enchantment!.DynamicExtraCardText!.GetFormattedText();
            GD.Print("ALCHEMIST_STAT infusionText="+infusedText);
            Check(infusedText.Contains("ドレイン+1") && infusedText.Contains("励起+1") && !infusedText.Contains("鋭利") && !infusedText.Contains("ブロック"),
                $"the card text lists only the materials put in ({infusedText})");
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
            // v0.22.4: stardust also raises the cost by 1, on top of whatever an upgrade did, and only once.
            Check(rareCard.EnergyCost.GetWithModifiers(CostModifiers.None)==rareCard.InscriptionBaseCost+1 && rareCard.BaseReplayCount==1
                && restoredRare.EnergyCost.GetWithModifiers(CostModifiers.None)==rareCard.InscriptionBaseCost+1,
                $"stardust adds a replay and 1 cost, also after loading (cost {rareCard.EnergyCost.GetWithModifiers(CostModifiers.None)}, loaded {restoredRare.EnergyCost.GetWithModifiers(CostModifiers.None)})");
            var upgradedCore=(EarthCore)run.CreateCard(ModelDb.Card<EarthCore>(),player);
            upgradedCore.UpgradeInternal();
            int upgradedCoreCost=upgradedCore.EnergyCost.GetWithModifiers(CostModifiers.None);
            upgradedCore.AlchemistRareModifier="rare.stardust";
            var reloadedCore=(EarthCore)CardModel.FromSerializable(upgradedCore.ToSerializable());
            upgradedCore.DowngradeInternal();
            Check(upgradedCoreCost==upgradedCore.InscriptionBaseCost-1 && reloadedCore.EnergyCost.GetWithModifiers(CostModifiers.None)==upgradedCoreCost+1
                && upgradedCore.EnergyCost.GetWithModifiers(CostModifiers.None)==upgradedCore.InscriptionBaseCost+1,
                $"stardust on an upgraded card keeps the upgrade's -1, survives loading and a downgrade without stacking (upgraded {upgradedCoreCost}, loaded {reloadedCore.EnergyCost.GetWithModifiers(CostModifiers.None)}, downgraded {upgradedCore.EnergyCost.GetWithModifiers(CostModifiers.None)})");
            box.Inventory.Grant("second-craft-iron-1",Alchemy.Core.Material.Iron);
            box.Inventory.Grant("second-craft-iron-2",Alchemy.Core.Material.Iron);
            await (Task)AccessTools.Method(typeof(WorkshopUi),"Craft").Invoke(null,[Recipes.All[0]])!;
            Check(player.Deck.Cards.Count(c=>c is EarthCore)==1 && box.Inventory.Counts[(int)Alchemy.Core.Material.Iron]==2,
                "synthesis is limited to once per workshop visit even with materials left");
            int potionsBefore=player.Potions.Count();
            await (Task)AccessTools.Method(typeof(WorkshopUi),"BrewPotion").Invoke(null,[Alchemy.Core.Material.Iron])!;
            Check(player.Potions.Count(p=>p is EarthPhial)==1 && player.Potions.Count()==potionsBefore+1
                && box.Inventory.Counts[(int)Alchemy.Core.Material.Iron]==1,"brewing turns one iron into an earth phial in a potion slot");
            await (Task)AccessTools.Method(typeof(WorkshopUi),"BrewPotion").Invoke(null,[Alchemy.Core.Material.Iron])!;
            Check(player.Potions.Count()==potionsBefore+1 && box.Inventory.Counts[(int)Alchemy.Core.Material.Iron]==1,
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
            Check(box.Combat.Phases.Current==AlchemyPhase.Water && foe.GetPower<WeakPower>()?.Amount==1+PhaseRules.BaseAmount(AlchemyPhase.Water),
                $"earth to water adds the water transition's weak (weak {foe.GetPower<WeakPower>()?.Amount})");
            int hpBefore=foe.CurrentHp;
            await Play(cs.CreateCard<FlashPowder>(player),null);
            Check(box.Combat.Phases.Current==AlchemyPhase.Fire && hpBefore-foe.CurrentHp==6+PhaseRules.BaseAmount(AlchemyPhase.Fire),
                $"water to fire adds the fire transition's damage (dealt {hpBefore-foe.CurrentHp})");
            hpBefore=foe.CurrentHp;
            await Play(cs.CreateCard<FlashPowder>(player),null);
            Check(box.Combat.Phases.TransitionCount==2 && hpBefore-foe.CurrentHp==6,"a same-element card does not transition again");
            var tailwind=cs.CreateCard<AirBreeze>(player);
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
            // v0.22 (design-axes 7.3): powder is damage, iron block, herb drain and ether Preparation, one per material.
            var infusedStrike=cs.CreateCard<StrikeAlchemist>(player);
            var strikeInfusion=(WorkshopInfusion)ModelDb.Enchantment<WorkshopInfusion>().ToMutable();
            strikeInfusion.Add([1,1,1,1]);
            CardCmd.Enchant(strikeInfusion,infusedStrike,strikeInfusion.Amount);
            int infusedBlockBefore=player.Creature.Block, infusedDrainBefore=foe.GetPower<LifeDrainPower>()?.Amount ?? 0;
            if(player.Creature.GetPower<PreparationPower>() is { } leftoverPreparation) await PowerCmd.Remove(leftoverPreparation);
            foe.SetCurrentHpInternal(999);
            await Play(infusedStrike,foe);
            int infusedDealt=999-foe.CurrentHp;
            Check(player.Creature.Block-infusedBlockBefore==1 && (foe.GetPower<LifeDrainPower>()?.Amount ?? 0)==infusedDrainBefore+1
                && player.Creature.GetPower<PreparationPower>()?.Amount==1,
                $"an infused attack adds 1 block, 1 drain and 1 Preparation (block +{player.Creature.Block-infusedBlockBefore})");
            await PowerCmd.Remove(player.Creature.GetPower<PreparationPower>()!);
            foe.SetCurrentHpInternal(999);
            await Play(cs.CreateCard<StrikeAlchemist>(player),foe);
            int plainDealt=999-foe.CurrentHp;
            Check(infusedDealt==plainDealt+1,$"powder adds 1 damage to the attack (plain {plainDealt}, infused {infusedDealt})");
            foe.SetCurrentHpInternal(999);
            await PowerCmd.Remove(foe.GetPower<LifeDrainPower>()!); // The life-axis checks below start from a foe without drain.
            box.Inventory.Grant("instant-herb",Alchemy.Core.Material.Herb);
            int herbBefore=box.Inventory.Counts[(int)Alchemy.Core.Material.Herb];
            var picker=new PickSelector(c=>c is HerbImprovisation);
            using(CardSelectCmd.UseSelector(picker))
                await Play(cs.CreateCard<InstantAlchemy>(player),null);
            Check(picker.Offered.All(c=>box.Inventory.Counts[(int)(c switch{IronImprovisation=>Alchemy.Core.Material.Iron,HerbImprovisation=>Alchemy.Core.Material.Herb,PowderImprovisation=>Alchemy.Core.Material.Powder,_=>Alchemy.Core.Material.Ether})]>0 || c is HerbImprovisation),
                "instant alchemy offers only materials the box holds");
            Check(box.Inventory.Counts[(int)Alchemy.Core.Material.Herb]==herbBefore-1 && player.PlayerCombatState.Hand.Cards.OfType<HerbImprovisation>().Count()==1,
                "instant alchemy spends one run material for a temporary water card");
            await Play(cs.CreateCard<SoothingMist>(player),foe);
            var phial=player.Potions.OfType<EarthPhial>().First();
            int phialBlock=player.Creature.Block;
            await phial.OnUseWrapper(new ThrowingPlayerChoiceContext(),player.Creature);
            Check(player.Creature.Block-phialBlock==12 && !player.Potions.Contains(phial),"the brewed earth phial is usable in combat and is consumed");
            int turnTransitions=box.Combat.Phases.TransitionsThisTurn;
            foe.SetMaxHpInternal(999);foe.SetCurrentHpInternal(999);
            await Play(cs.CreateCard<AlchChainReaction>(player),foe);
            Check(turnTransitions>2 && 999-foe.CurrentHp==(turnTransitions-2)*20,$"chain reaction deals 20 per transition this turn after the second ({turnTransitions} transitions, {999-foe.CurrentHp} damage)");
            int transitionsBefore=box.Combat.Phases.TransitionCount;
            await Play(cs.CreateCard<LavaShot>(player),foe);
            Check(box.Combat.Phases.TransitionCount==transitionsBefore+2 && box.Combat.Phases.Current==AlchemyPhase.Fire && box.Combat.Phases.Previous==AlchemyPhase.Earth,
                "a dual-phase card enters its first then its second element, transitioning twice");
            // Cores are per element (design-axes.md 7.2) and the air draw ignores modifiers (原則5).
            await Play(cs.CreateCard<FlashPowder>(player),null);
            await ApplySelfPower<AirCorePower>(1);
            await ApplySelfPower<PreparationPower>(2);
            // Fixture: room in the hand and enough cards in the draw pile, so only the draw rule decides the count.
            foreach(var held in player.PlayerCombatState.Hand.Cards.ToArray()) await CardCmd.Exhaust(new ThrowingPlayerChoiceContext(),held);
            await CardPileCmd.AddGeneratedCardsToCombat([..Enumerable.Range(0,5).Select(_=>cs.CreateCard<EarthenGuard>(player))],PileType.Draw,player);
            GD.Print($"ALCHEMIST_STAT airFixture hand={player.PlayerCombatState.Hand.Cards.Count} draw={player.PlayerCombatState.DrawPile.Cards.Count} phase={box.Combat.Phases.Current}");
            var airCard=cs.CreateCard<AirBreeze>(player);
            await CardPileCmd.AddGeneratedCardsToCombat([airCard],PileType.Hand,player);
            int airDrawBefore=player.PlayerCombatState.DrawPile.Cards.Count;
            await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(),airCard,null,skipCardPileVisuals:true);
            Check(airDrawBefore-player.PlayerCombatState.DrawPile.Cards.Count==airCard.DynamicVars.Cards.IntValue+PhaseRules.BaseAmount(AlchemyPhase.Air),
                $"strengthening does not raise the air transition's draw (drew {airDrawBefore-player.PlayerCombatState.DrawPile.Cards.Count})");
            foe.SetCurrentHpInternal(999);
            await Play(cs.CreateCard<FlashPowder>(player),null);
            Check(999-foe.CurrentHp==6+PhaseRules.BaseAmount(AlchemyPhase.Fire)+1,$"the air core charges the next transition out of air (dealt {999-foe.CurrentHp})");
            await ApplySelfPower<WaterCorePower>(1);
            int vulnerableBefore=foe.GetPower<VulnerablePower>()?.Amount ?? 0;
            await Play(cs.CreateCard<SoothingMist>(player),foe);
            Check((foe.GetPower<VulnerablePower>()?.Amount ?? 0)==vulnerableBefore+1,"the water core adds vulnerable to the water transition");
            // Life axis (design-axes.md 3): drain, the homunculus and death.
            var life=box.Combat.Life;
            Check(!life.HomunculusAppeared && LifeAxis.Pet(player) is null,"no homunculus before any life card");
            foe.SetCurrentHpInternal(999);
            await PowerCmd.Apply<LifeDrainPower>(new ThrowingPlayerChoiceContext(),foe,8,player.Creature,null);
            var drain=foe.GetPower<LifeDrainPower>()!;
            await drain.AfterSideTurnStart(CombatSide.Player,[player.Creature],cs);
            Check(foe.CurrentHp==999 && drain.Amount==8,"drain waits for its owner's own turn start");
            await drain.AfterSideTurnStart(CombatSide.Enemy,[foe],cs);
            Check(foe.CurrentHp==991 && drain.Amount==4 && life.HomunculusHp==8 && life.HpDrainedThisCombat==8,
                $"drain takes its stacks as HP on the enemy turn start, feeds the homunculus and halves (rounded down) (hp {foe.CurrentHp}, stacks {drain.Amount}, homunculus {life.HomunculusHp})");
            var pet=LifeAxis.Pet(player);
            Check(pet is { IsAlive: true, CurrentHp: 8 } && pet.GetPower<HomunculusPower>() is not null && cs.Allies.Contains(pet),
                $"the homunculus steps onto the field as a pet with the drained HP (hp {pet?.CurrentHp})");
            Check(NCombatRoom.Instance?.GetCreatureNode(pet!) is not { } petNode || petNode.IsInteractable,
                "the homunculus keeps its health bar (other pets are made non-interactable, which hides it)");
            await Play(cs.CreateCard<LifeHarvest>(player),null);
            Check(foe.CurrentHp==991-7 && life.HomunculusHp==15 && foe.GetPower<LifeDrainPower>() is null,
                $"life harvest triggers drain three times, halving each time (4+2+1) (hp {foe.CurrentHp}, homunculus {life.HomunculusHp})");
            var greatWork=cs.CreateCard<LifeGreatWork>(player);
            await CardPileCmd.AddGeneratedCardsToCombat([greatWork],PileType.Hand,player);
            Check(life.HomunculusHp==15 && greatWork.EnergyCost.GetWithModifiers(CostModifiers.All)==4,$"the homunculus-scaled attack shows its lowered cost in hand ({greatWork.EnergyCost.GetWithModifiers(CostModifiers.All)})");
            // v0.22.4: attacks whose damage depends on the combat show what they would deal now, only in combat.
            var previewHuman=cs.CreateCard<HumanTransmutation>(player);
            var previewHellfire=cs.CreateCard<FireHellfire>(player);
            await CardPileCmd.AddGeneratedCardsToCombat([previewHuman,previewHellfire],PileType.Hand,player);
            foreach(var shown in new CardModel[]{previewHuman,previewHellfire}) shown.UpdateDynamicVarPreview(CardPreviewMode.Normal,null,shown.DynamicVars);
            decimal expectedHellfire=previewHellfire.DynamicVars.Damage.BaseValue+box.Combat.Phases.TransitionCount*previewHellfire.DynamicVars["Bonus"].BaseValue;
            string humanText=previewHuman.GetDescriptionForPile(PileType.Hand), libraryText=ModelDb.Card<HumanTransmutation>().GetDescriptionForPile(PileType.None);
            Check(previewHuman.DynamicVars[PreviewDamageVar.Key].EnchantedValue==life.HomunculusHp*3 && humanText.Contains("ダメージ）")
                && previewHellfire.DynamicVars[PreviewDamageVar.Key].EnchantedValue==expectedHellfire && !libraryText.Contains("ダメージ）"),
                $"variable attacks preview their damage in combat only (human {previewHuman.DynamicVars[PreviewDamageVar.Key].PreviewValue} for homunculus {life.HomunculusHp}, hellfire {previewHellfire.DynamicVars[PreviewDamageVar.Key].PreviewValue}/{expectedHellfire}; {humanText.Replace('\n',' ')})");
            foreach(var shown in new CardModel[]{previewHuman,previewHellfire}) await CardPileCmd.RemoveFromCombat(shown);
            int hpBeforeOffering=player.Creature.CurrentHp;
            await Play(cs.CreateCard<LifeOffering>(player),null);
            Check(player.Creature.CurrentHp==hpBeforeOffering-5 && life.HomunculusHp==25,"the offering trades 5 HP for 10 homunculus HP");
            foe.SetCurrentHpInternal(999);
            await Play(cs.CreateCard<HumanTransmutation>(player),foe);
            Check(999-foe.CurrentHp>=25*3 && player.Creature.GetPower<DeathMarkPower>() is not null && life.HomunculusHp==25,
                $"human transmutation hits for three times the homunculus and marks you for death (dealt {999-foe.CurrentHp})");
            await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(),player.Creature,2,foe,null);
            using(CardSelectCmd.UseSelector(new PickSelector(_=>false))) // spend nothing: the copy
                await Play(cs.CreateCard<DebuffTransferCard>(player),foe);
            Check(foe.GetPower<DeathMarkPower>() is not null && (foe.GetPower<WeakPower>()?.Amount ?? 0)>=2 && player.Creature.GetPower<DeathMarkPower>() is not null,
                "debuff transfer copies death and weak onto the enemy and keeps yours");
            var foeDeath=foe.GetPower<DeathMarkPower>()!;
            await foeDeath.AfterSideTurnStart(CombatSide.Player,[player.Creature],cs);
            Check(foe.IsAlive,"an enemy's death waits for the enemy turn");
            await PowerCmd.Remove(foeDeath); // Killing the only enemy here would end the battle the rest of this run needs.
            var fairy=await PotionCmd.TryToProcure<FairyInABottle>(player);
            Check(fairy.success,"fixture: a fairy in a bottle to prevent death");
            await player.Creature.GetPower<DeathMarkPower>()!.AfterSideTurnStart(CombatSide.Player,[player.Creature],cs);
            Check(player.Creature.IsAlive && player.Creature.GetPower<DeathMarkPower>() is null && !player.Potions.Contains(fairy.potion),
                "death resolves at your turn start through the normal kill, so a fairy in a bottle saves you and clears it");
            // v0.20: the homunculus soaks attacks that get past block, like Osty.
            player.Creature.LoseBlockInternal(player.Creature.Block);
            await CreatureCmd.GainBlock(player.Creature,4,ValueProp.Unpowered,null);
            int ownHpBeforeHit=player.Creature.CurrentHp;
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(),player.Creature,10,ValueProp.Move,null,null);
            Check(player.Creature.CurrentHp==ownHpBeforeHit && pet!.CurrentHp==25-6 && LifeAxis.State(player)!.HomunculusHp==19,
                $"an attack past block hits the homunculus, not the alchemist (pet {pet!.CurrentHp}, alchemist lost {ownHpBeforeHit-player.Creature.CurrentHp})");
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(),player.Creature,3,ValueProp.Unblockable|ValueProp.Unpowered,null,null);
            Check(player.Creature.CurrentHp==ownHpBeforeHit-3 && pet.CurrentHp==19,"HP loss that is not an attack stays with the alchemist");
            foe.SetCurrentHpInternal(999);
            await Play(cs.CreateCard<LifeBullet>(player),foe);
            Check(pet.CurrentHp==14 && 999-foe.CurrentHp>=15,$"the life bullet spends 5 of the homunculus for its big hit (pet {pet.CurrentHp}, dealt {999-foe.CurrentHp})");
            await Play(cs.CreateCard<LifeReclaim>(player),null);
            Check(LifeAxis.State(player)!.HomunculusHp==0 && pet.IsDead && LifeAxis.Pet(player)==pet,"reclaim spends the homunculus to 0 and it falls, staying on the field");
            ownHpBeforeHit=player.Creature.CurrentHp;
            player.Creature.LoseBlockInternal(player.Creature.Block);
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(),player.Creature,4,ValueProp.Move,null,null);
            Check(player.Creature.CurrentHp==ownHpBeforeHit-4,"a fallen homunculus no longer soaks attacks");
            await Play(cs.CreateCard<LifeFleshWall>(player),null);
            Check(pet.IsAlive && pet.CurrentHp==6 && LifeAxis.Pet(player)==pet,$"gaining homunculus HP revives the same pet (hp {pet.CurrentHp})");
            // v0.22.4: the starter relic only says what it does; 相転移 is a keyword-like tooltip on cards that mention it.
            string boxText=box.DynamicDescription.GetFormattedText();
            Check(boxText.Contains("炉の起動") && boxText.Length<60 && !boxText.Contains("エリート"),$"the material box text is short ({boxText})");
            var sparkTips=ModelDb.Card<FireSpark>().HoverTips.OfType<MegaCrit.Sts2.Core.HoverTips.HoverTip>().ToArray();
            var strikeTips=ModelDb.Card<EarthenGuard>().HoverTips.OfType<MegaCrit.Sts2.Core.HoverTips.HoverTip>().ToArray();
            Check(sparkTips.Any(t=>t.Title=="相転移" && t.Description.Contains("ブロック") && t.Description.Contains("違う相")) && strikeTips.All(t=>t.Title!="相転移"),
                $"a card that mentions 相転移 explains it, one that does not stays clean ({string.Join(" / ",sparkTips.Select(t=>t.Title+":"+t.Description))})");
            // 2026-09-29: the pet is shown as a golem, one still sprite (art/pets/golem.png) that GolemMotion animates.
            var golemBody=NCombatRoom.Instance?.GetCreatureNode(pet) is { } revivedNode ? revivedNode.Visuals.Body : null;
            Check(golemBody is Sprite2D { Texture: { } golemTex } && golemTex.GetHeight()==PetArt.GolemHeight && pet.Name=="ゴーレム",
                $"the homunculus is shown as the golem sprite and named ゴーレム (body {golemBody?.GetType().Name ?? "missing"}, name {pet.Name})");
            // 2026-09-30: the alchemist's own sprite and icons (CharacterArt) instead of the Ironclad's.
            var alchemistBody=NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.Visuals.Body;
            var character=player.Character;
            Check(alchemistBody is Sprite2D { Texture: { } bodyTex } && bodyTex.GetHeight()==CharacterArt.BodyHeight,
                $"the alchemist is shown as their own sprite (body {alchemistBody?.GetType().Name ?? "missing"})");
            Check(character.IconTexture==CharacterArt.Icon && character.IconOutlineTexture==CharacterArt.IconOutline && CharacterArt.Icon is not null,
                "the alchemist's head icon and outline replace the borrowed ones");
            Check(character.CharacterSelectIcon is { } selectIcon && selectIcon.GetWidth()==132 && selectIcon.GetHeight()==195
                && character.MapMarker is { } mapMarker && mapMarker.GetWidth()==49 && mapMarker==CharacterArt.MapMarker,
                $"the character select portrait and the map marker load from the mod's .ctex files ({character.CharacterSelectIcon?.GetSize()}, {character.MapMarker?.GetSize()})");
            // The select button and map load by path (ResourceLoader), not through the patched getters alone.
            string? selectPath=Traverse.Create(character).Property("CharacterSelectIconPath").GetValue<string>();
            string? markerPath=Traverse.Create(character).Property("MapMarkerPath").GetValue<string>();
            var byPath=selectPath is null ? null : ResourceLoader.Load<CompressedTexture2D>(selectPath);
            var markerByPath=markerPath is null ? null : ResourceLoader.Load<CompressedTexture2D>(markerPath);
            Check(byPath is { } bp && bp.GetWidth()==132 && bp.GetHeight()==195 && markerByPath is { } mp && mp.GetWidth()==49,
                $"the game's loader reads the alchemist's select portrait and map marker from their paths ({selectPath}, {byPath?.GetSize()}, {markerByPath?.GetSize()})");
            // 2026-10-01: the mod's own relic, enchantment and map icons (IconArt), loaded the way the game loads them.
            RelicModel[] ownRelics=[ModelDb.Relic<MaterialBox>(),ModelDb.Relic<RefinedMaterialBox>(),ModelDb.Relic<DarvCrucible>(),
                ModelDb.Relic<PhaseCompass>(),ModelDb.Relic<BloodChalice>(),ModelDb.Relic<PulsingCore>(),ModelDb.Relic<Quadrant>(),
                ModelDb.Relic<GreatCrucible>(),ModelDb.Relic<WardensFoundation>(),ModelDb.Relic<LargeMaterialBag>()];
            var badIcons=ownRelics.Where(r=>!(r.PackedIconPath.EndsWith("_packed.ctex") && r.Icon is { } i && i.GetWidth()==85
                && r.IconOutline is { } o && o.GetWidth()==85 && r.BigIcon is { } b && b.GetWidth()==256)).Select(r=>r.Id.Entry).ToArray();
            Check(badIcons.Length==0, $"the alchemist's relics show their own icons, small, outline and big (wrong: {string.Join(",",badIcons)})");
            string? modifyIcon=IconArt.Packed("workshop_modify"), mapIcon=IconArt.Packed("workshop_map");
            Check(modifyIcon is not null && ResourceLoader.Load<Texture2D>(modifyIcon)?.GetWidth()==64
                && mapIcon is not null && ResourceLoader.Load<Texture2D>(mapIcon)?.GetWidth()==128,
                $"the workshop's modification and map icons load from the mod's files ({modifyIcon}, {mapIcon})");
            // Shaped like the base game's AnimatedBg at 1920x1080: 2560x1200 at (-388,-80), scaled 1.1 about (1280, 600).
            var bgContainer=new Control{Position=new Vector2(-388,-80),Size=new Vector2(2560,1200),Scale=new Vector2(1.1f,1.1f),PivotOffset=new Vector2(1280,600)};
            bgContainer.AddChild(new Control{Name=character.Id.Entry+"_bg"});
            AlchemistSelectBg.Swap(bgContainer,character,retry:false);
            var bgChildren=bgContainer.GetChildren().ToArray();
            var selectPatched=Harmony.GetPatchInfo(AccessTools.Method(typeof(MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectScreen),"SelectCharacter"));
            Check(bgChildren.Length==1 && bgChildren[0] is TextureRect { Texture: { } bgTex } && bgTex.GetWidth()==1920
                && selectPatched?.Postfixes.Any(x=>x.PatchMethod.DeclaringType==typeof(AlchemistSelectBgPatch))==true,
                $"the character select background is swapped for the alchemist's own picture ({bgChildren.Length} children, {bgChildren.FirstOrDefault()?.GetType().Name})");
            // Where the picture lands on screen: the container's own transform applied to the picture's rect.
            Vector2 OnScreen(Vector2 p)=>bgContainer.Position+bgContainer.PivotOffset+(p-bgContainer.PivotOffset)*1.1f;
            var picture=bgChildren.FirstOrDefault() as Control;
            var shownFrom=picture is null ? Vector2.Zero : OnScreen(picture.Position);
            var shownTo=picture is null ? Vector2.Zero : OnScreen(picture.Position+picture.Size);
            Check(shownFrom.X<=0 && shownFrom.Y<=0 && shownTo.X>=1920 && shownTo.Y>=1080 && shownTo.X-shownFrom.X<1920*1.1f && shownTo.Y-shownFrom.Y<1080*1.1f,
                $"the background picture just covers the 1920x1080 screen instead of the oversized container (shown {shownFrom} to {shownTo})");
            bgContainer.Free();
            player.Creature.LoseBlockInternal(player.Creature.Block); // 盛り土 is earth: its transition gave block
            var overflowResults=(await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(),player.Creature,10,ValueProp.Move,null,null)).ToArray();
            Check(pet.IsDead && player.Creature.CurrentHp==ownHpBeforeHit-4-4,"damage past the homunculus's HP goes through to the alchemist");
            // ユーザー報告: the homunculus showed the whole hit, and the alchemist the overflow again.
            var petResult=overflowResults.Single(r=>r.Receiver==pet);
            var ownResult=overflowResults.Single(r=>r.Receiver==player.Creature);
            var numberCreate=AccessTools.Method(typeof(MegaCrit.Sts2.Core.Nodes.Vfx.NDamageNumVfx),"Create",[typeof(Creature),typeof(DamageResult)]);
            Check(HomunculusDamageNumberPatch.Shown(pet,petResult)==6 && HomunculusDamageNumberPatch.Shown(player.Creature,ownResult) is null
                && ownResult.UnblockedDamage+ownResult.OverkillDamage==4
                && Harmony.GetPatchInfo(numberCreate)?.Prefixes.Any(x=>x.PatchMethod.DeclaringType==typeof(HomunculusDamageNumberPatch))==true,
                $"an overflowing hit shows only what the homunculus lost over it (6, not {petResult.UnblockedDamage+petResult.OverkillDamage}) and the rest over the alchemist ({ownResult.UnblockedDamage})");
            // v0.21 workshop life cards.
            await Play(cs.CreateCard<LifeFleshWall>(player),null);
            await Play(cs.CreateCard<CraftMitosis>(player),null);
            Check(pet.IsAlive && pet.CurrentHp==12,$"mitosis doubles the homunculus (hp {pet.CurrentHp})");
            await Play(cs.CreateCard<EarthenGuard>(player),null); // enter earth first, so the armor itself causes no transition
            int armorBlockBefore=player.Creature.Block;
            await Play(cs.CreateCard<CraftFleshArmor>(player),null);
            Check(player.Creature.Block-armorBlockBefore==6,$"flesh armor gives half the homunculus as block (got {player.Creature.Block-armorBlockBefore})");
            await ApplySelfPower<PhilosophersBloodPower>(2);
            player.Creature.LoseBlockInternal(player.Creature.Block);
            int bloodDrainBefore=foe.GetPower<LifeDrainPower>()?.Amount ?? 0;
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(),player.Creature,3,ValueProp.Move,foe,null,null);
            Check(pet.CurrentHp<12 && (foe.GetPower<LifeDrainPower>()?.Amount ?? 0)==bloodDrainBefore+2,"philosopher's blood drains the attacker whose hit the homunculus soaked");
            await PowerCmd.Remove(player.Creature.GetPower<PhilosophersBloodPower>()!);
            await LifeAxis.GainHomunculus(new ThrowingPlayerChoiceContext(),player,10-pet.CurrentHp);
            foe.SetCurrentHpInternal(999);
            await Play(cs.CreateCard<CraftFusion>(player),foe);
            Check(pet.IsDead && 999-foe.CurrentHp>=30,$"fusion spends 10 of the homunculus for a 30 hit (dealt {999-foe.CurrentHp})");
            int transitionsBeforeTorrent=box.Combat.Phases.TransitionCount;
            var phaseBeforeTorrent=box.Combat.Phases.Current;
            await Play(cs.CreateCard<CraftThreePhaseTorrent>(player),foe);
            Check(box.Combat.Phases.Current==AlchemyPhase.Fire && box.Combat.Phases.TransitionCount-transitionsBeforeTorrent==(phaseBeforeTorrent==AlchemyPhase.Earth?2:3),
                $"EW&F passes earth, air and fire, up to three transitions ({box.Combat.Phases.TransitionCount-transitionsBeforeTorrent} from {phaseBeforeTorrent})");
            // v0.22 (design-axes 4.2): spending a material turns コペルニクスシフト into a swap of listed debuffs.
            foreach(var own in player.Creature.Powers.Where(x=>x.TypeForCurrentAmount==PowerType.Debuff).ToArray()) await PowerCmd.Remove(own);
            await ApplySelfPower<WeakPower>(2);
            if(foe.GetPower<VulnerablePower>() is null) await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(),foe,1,player.Creature,null);
            int foeVulnerable=foe.GetPower<VulnerablePower>()!.Amount, foeWeakBefore=foe.GetPower<WeakPower>()?.Amount ?? 0;
            box.Inventory.Pending.Clear(); box.Inventory.Counts=[1,0,0,0];
            using(CardSelectCmd.UseSelector(new PickSelector(_=>true)))
                await Play(cs.CreateCard<DebuffTransferCard>(player),foe);
            Check((player.Creature.GetPower<WeakPower>()?.Amount ?? 0)==foeWeakBefore && player.Creature.GetPower<VulnerablePower>()?.Amount==foeVulnerable
                && foe.GetPower<WeakPower>()?.Amount==2 && foe.GetPower<VulnerablePower>() is null && box.Inventory.Counts[0]==0,
                $"spending a material swaps the listed debuffs (enemy weak was {foeWeakBefore})");
            // With a receipt still pending nothing can be spent, so the material screen is not offered at all.
            box.Inventory.Counts=[1,0,0,0];
            box.Inventory.Pending.Add(new Harvest("smoke-pending",MaterialChoice.Normal(Alchemy.Core.Material.Herb)));
            var unsettledPicker=new PickSelector(_=>true);
            using(CardSelectCmd.UseSelector(unsettledPicker))
                await Play(cs.CreateCard<DebuffTransferCard>(player),foe);
            Check(unsettledPicker.Offered.Count==0 && box.Inventory.Counts[0]==1,"with a pending receipt the shift copies without asking for a material");
            box.Inventory.Pending.Clear(); box.Inventory.Counts=[0,0,0,0];
            foreach(var own in player.Creature.Powers.Where(x=>x.TypeForCurrentAmount==PowerType.Debuff).ToArray()) await PowerCmd.Remove(own);
            // F-1: self drain feeds your own homunculus, doubled.
            await ApplySelfPower<SelfCultivationPower>(ModelDb.Card<LifeSelfCultivation>().DynamicVars["SelfCultivationPower"].IntValue);
            await player.Creature.GetPower<SelfCultivationPower>()!.AfterSideTurnStart(CombatSide.Player,[player.Creature],cs);
            var selfDrain=player.Creature.GetPower<LifeDrainPower>();
            int selfHpBefore=player.Creature.CurrentHp, homunculusBeforeSelf=LifeAxis.State(player)!.HomunculusHp;
            await selfDrain!.AfterSideTurnStart(CombatSide.Player,[player.Creature],cs);
            Check(player.Creature.CurrentHp==selfHpBefore-2 && LifeAxis.State(player)!.HomunculusHp==homunculusBeforeSelf+4,
                $"self cultivation drains you 2 and your homunculus gains 4 (homunculus {homunculusBeforeSelf}→{LifeAxis.State(player)!.HomunculusHp})");
            await PowerCmd.Remove(player.Creature.GetPower<SelfCultivationPower>()!);
            if(player.Creature.GetPower<LifeDrainPower>() is { } leftoverSelfDrain) await PowerCmd.Remove(leftoverSelfDrain);
            // v0.22.1: 清流 gives drain 2 plus 1 per transition this combat.
            int streamTransitions=box.Combat.Phases.TransitionCount, streamDrainBefore=foe.GetPower<LifeDrainPower>()?.Amount ?? 0;
            await Play(cs.CreateCard<WaterClearStream>(player),foe);
            Check((foe.GetPower<LifeDrainPower>()?.Amount ?? 0)-streamDrainBefore==2+streamTransitions,
                $"clear stream drains 2 plus 1 per transition ({streamTransitions} transitions)");
            await PowerCmd.Remove(foe.GetPower<LifeDrainPower>()!);
            // F-2: count × 3 × kinds.
            box.Inventory.Counts=[2,1,0,0];
            foe.SetCurrentHpInternal(999);
            using(CardSelectCmd.UseSelector(new PickSelector(_=>true)))
                await Play(cs.CreateCard<AlchAlkahest>(player),foe);
            Check(999-foe.CurrentHp>=18 && box.Inventory.Counts.Sum()==0,$"alkahest pours three materials of two kinds into 3x3x2 (dealt {999-foe.CurrentHp})");
            // F-3: cost falls by the kinds held.
            box.Inventory.Counts=[1,1,1,0];
            var blade=cs.CreateCard<AirElementBlade>(player);
            await CardPileCmd.AddGeneratedCardsToCombat([blade],PileType.Hand,player);
            Check(blade.EnergyCost.GetWithModifiers(CostModifiers.All)==2,$"the element blade costs 5 minus three kinds held ({blade.EnergyCost.GetWithModifiers(CostModifiers.All)})");
            var rareBefore=box.Inventory.RareCounts.ToArray();
            box.Inventory.Counts=[1,1,1,1]; box.Inventory.RareCounts=[1,0,0];
            Check(blade.EnergyCost.GetWithModifiers(CostModifiers.All)==0,$"all four kinds and a rare material bring the blade to 0 ({blade.EnergyCost.GetWithModifiers(CostModifiers.All)})");
            box.Inventory.Counts=[0,0,0,0]; box.Inventory.RareCounts=rareBefore;
            // F-4: the wheel steps only after a play that caused no transition.
            await Play(cs.CreateCard<EarthenGuard>(player),null);
            await ApplySelfPower<PhaseWheelPower>(1);
            await Play(cs.CreateCard<EarthenGuard>(player),null);
            Check(box.Combat.Phases.Current==AlchemyPhase.Water,"a same-element card lets the wheel step earth to water");
            await Play(cs.CreateCard<FlashPowder>(player),null);
            Check(box.Combat.Phases.Current==AlchemyPhase.Fire,"a card that transitions itself does not step the wheel");
            await PowerCmd.Remove(player.Creature.GetPower<PhaseWheelPower>()!);
            // v0.23 (card review).
            int airEntries=box.Combat.Phases.TransitionsInto(AlchemyPhase.Air);
            var storm=cs.CreateCard<AirStormBlade>(player);
            await CardPileCmd.AddGeneratedCardsToCombat([storm],PileType.Hand,player);
            storm.UpdateDynamicVarPreview(CardPreviewMode.Normal,null,storm.DynamicVars);
            Check(airEntries>0 && airEntries<box.Combat.Phases.TransitionCount && storm.DynamicVars["HitCount"].PreviewValue==airEntries,
                $"the storm blade counts only transitions into air ({airEntries} of {box.Combat.Phases.TransitionCount})");
            await CardPileCmd.RemoveFromCombat(storm);
            if(foe.GetPower<LifeDrainPower>() is { } embraceLeftover) await PowerCmd.Remove(embraceLeftover);
            await PowerCmd.Apply<LifeDrainPower>(new ThrowingPlayerChoiceContext(),foe,5,player.Creature,null);
            await Play(cs.CreateCard<LifeCorrosiveEmbrace>(player),foe);
            Check(foe.GetPower<LifeDrainPower>()?.Amount==7,$"corrosive embrace adds 50% to drain, rounded down (5 to {foe.GetPower<LifeDrainPower>()?.Amount})");
            await PowerCmd.Remove(foe.GetPower<LifeDrainPower>()!);
            await ApplySelfPower<EarthKingPower>(20);
            player.Creature.LoseBlockInternal(player.Creature.Block);
            await CreatureCmd.GainBlock(player.Creature,30,ValueProp.Unpowered,null);
            await Traverse.Create(player.Creature).Method("ClearBlock").GetValue<Task>();
            Check(player.Creature.Block==10,$"the earth king keeps block and takes off only 20 at turn start (block {player.Creature.Block})");
            await Traverse.Create(player.Creature).Method("ClearBlock").GetValue<Task>();
            Check(player.Creature.Block==0,"the earth king never takes block below 0");
            await PowerCmd.Remove(player.Creature.GetPower<EarthKingPower>()!);
            foe.SetCurrentHpInternal(999);
            await ApplySelfPower<GateOfTruthPower>(8);
            await LifeAxis.GainHomunculus(new ThrowingPlayerChoiceContext(),player,Math.Max(0,GateOfTruthPower.Threshold-1-LifeAxis.State(player)!.HomunculusHp));
            await player.Creature.GetPower<GateOfTruthPower>()!.AfterSideTurnStart(CombatSide.Player,[player.Creature],cs);
            bool gateWaited=foe.CurrentHp==999 || LifeAxis.State(player)!.HomunculusHp>=GateOfTruthPower.Threshold;
            await LifeAxis.GainHomunculus(new ThrowingPlayerChoiceContext(),player,GateOfTruthPower.Threshold);
            int gateHomunculus=LifeAxis.State(player)!.HomunculusHp;
            foe.SetCurrentHpInternal(999);
            await player.Creature.GetPower<GateOfTruthPower>()!.AfterSideTurnStart(CombatSide.Player,[player.Creature],cs);
            Check(gateWaited && foe.CurrentHp==991 && LifeAxis.State(player)!.HomunculusHp==gateHomunculus,
                $"the gate of truth hits every enemy for 8 at 20 homunculus HP without spending it (hp {foe.CurrentHp})");
            await PowerCmd.Remove(player.Creature.GetPower<GateOfTruthPower>()!);
            // Random targets: count the damage over every enemy in the fight.
            var afterimageTargets=cs.HittableEnemies.ToArray();
            foreach(var enemy in afterimageTargets){enemy.SetMaxHpInternal(999);enemy.SetCurrentHpInternal(999);}
            await ApplySelfPower<WindAfterimagePower>(3);
            int afterimageTransitions=box.Combat.Phases.TransitionsThisTurn;
            await player.Creature.GetPower<WindAfterimagePower>()!.BeforeSideTurnEndEarly(new ThrowingPlayerChoiceContext(),CombatSide.Player,[player.Creature]);
            int afterimageDealt=afterimageTargets.Sum(e=>999-e.CurrentHp);
            Check(afterimageTransitions>0 && afterimageDealt==3*afterimageTransitions,
                $"wind afterimage hits for 3 per transition this turn ({afterimageTransitions} transitions, dealt {afterimageDealt} over {afterimageTargets.Length} enemies)");
            await PowerCmd.Remove(player.Creature.GetPower<WindAfterimagePower>()!);
            foe.SetCurrentHpInternal(999);
            await Play(cs.CreateCard<FireExplosion>(player),foe);
            Check(foe.IsStunned && 999-foe.CurrentHp>=35,$"the great explosion hits one enemy for 35 and stuns it (dealt {999-foe.CurrentHp})");
            async Task ApplySelfPower<T>(int amount) where T:PowerModel => await PowerCmd.Apply<T>(new ThrowingPlayerChoiceContext(),player.Creature,amount,player.Creature,null);
            // Every reward card once, in this already-running battle: debug room changes mid-combat leave
            // disposed card nodes in the headless node pool, so the sweep avoids a fresh room.
            // Fixture: a settled box with two of each material, so material-spending cards are playable.
            box.Inventory.Pending.Clear(); box.Inventory.Counts=[2,2,2,2];
            var sweepState=cs;
            var firstPick=new PickSelector(c=>c is not CraftedCard); // never exhaust the stardust card checked below
            using(CardSelectCmd.UseSelector(firstPick))
            foreach(var canonical in ModelDb.CardPool<AlchemyCardPool>().AllCards.Where(c=>c.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare))
            {
                foreach(var enemy in sweepState.Enemies.Where(e=>!e.IsDead)) { enemy.SetMaxHpInternal(99999); enemy.SetCurrentHpInternal(99999); } // 連鎖反応 late in a long turn can pass 999
                if(box.Inventory.Counts.Sum()<2) box.Inventory.Counts=[2,2,2,2];
                var played=sweepState.CreateCard(canonical,player);
                var target=played.TargetType==TargetType.AnyEnemy ? sweepState.Enemies.First(e=>!e.IsDead) : null;
                try { await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(),played,target,skipCardPileVisuals:true); }
                catch(Exception ex) { throw new Exception("reward card plays "+canonical.Id.Entry,ex); }
                // The wheel would step every later same-element card away from its own element.
                if(player.Creature.GetPower<PhaseWheelPower>() is { } sweptWheel) await PowerCmd.Remove(sweptWheel);
                if(played is AlchemyCard { Element: not AlchemyPhase.None } sweptElemental)
                    Check(box.Combat!.Phases.Current==sweptElemental.Element,"reward card plays and enters its element "+canonical.Id.Entry);
                else Check(true,"reward card plays "+canonical.Id.Entry);
            }
            GD.Print($"ALCHEMIST_STAT sweepTransitions={box.Combat!.Phases.TransitionCount}");
            // The sweep played 人体錬成 and デバフ転写; no turn passes here, but clear the marks before later rooms.
            foreach(var marked in sweepState.Creatures.Where(x=>x.GetPower<DeathMarkPower>() is not null).ToArray())
                await PowerCmd.Remove(marked.GetPower<DeathMarkPower>());
            // design-axes 6.2: each material card names its material and is much stronger when it can spend one.
            {
                // The sweep leaves the hand full, so these play straight from creation like the sweep does.
                Task PlayLoose(CardModel card,Creature? target)=>CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(),card,target,skipCardPileVisuals:true);
                var foe0=sweepState.Enemies.First(e=>!e.IsDead);
                foreach(var enemy in sweepState.Enemies.Where(e=>!e.IsDead)) { enemy.SetMaxHpInternal(99999); enemy.SetCurrentHpInternal(99999); } // 連鎖反応 late in a long turn can pass 999
                box.Inventory.Counts=[0,1,1,0];
                // Relative to the no-powder hit: strength and multipliers left over from the sweep scale both.
                int Health()=>foe0.CurrentHp+foe0.Block;
                int h0=Health();
                await PlayLoose(sweepState.CreateCard<AlchMaterialBomb>(player),null);
                int h1=Health();
                Check(box.Inventory.Counts[(int)Alchemy.Core.Material.Powder]==0,"material bomb spends the powder");
                await PlayLoose(sweepState.CreateCard<AlchMaterialBomb>(player),null);
                int h2=Health();
                Check((h0-h1)-(h1-h2)>=11,$"with powder the bomb hits at least 11 harder than without (dealt {h0-h1} then {h1-h2})");
                int ironBlockBefore=player.Creature.Block;
                box.Inventory.Counts[(int)Alchemy.Core.Material.Iron]=1;
                await PlayLoose(sweepState.CreateCard<EarthIronBulwark>(player),null);
                Check(player.Creature.Block-ironBlockBefore>=13 && box.Inventory.Counts[(int)Alchemy.Core.Material.Iron]==0,$"iron bulwark spends an iron for 13 block (block {player.Creature.Block})");
                int drainBefore=foe0.GetPower<LifeDrainPower>()?.Amount ?? 0;
                await PlayLoose(sweepState.CreateCard<WaterHerbDrip>(player),foe0);
                Check((foe0.GetPower<LifeDrainPower>()?.Amount ?? 0)==drainBefore+4 && box.Inventory.Counts[(int)Alchemy.Core.Material.Herb]==0,"herb drip spends a herb for drain 4");
                await PlayLoose(sweepState.CreateCard<WaterHerbDrip>(player),foe0);
                Check((foe0.GetPower<LifeDrainPower>()?.Amount ?? 0)==drainBefore+5,"without herb the drip gives drain 1");
                var catalysis=sweepState.CreateCard<AlchCatalysis>(player);
                box.Inventory.Counts=[1,1,1,0];
                Check(!catalysis.CanPlay(),"catalysis is unplayable without ether, even with other materials");
                box.Inventory.Counts=[1,0,0,1];
                Check(catalysis.CanPlay(),"catalysis is playable with an ether");
                await PlayLoose(catalysis,null);
                Check(box.Inventory.Counts[(int)Alchemy.Core.Material.Ether]==0 && box.Inventory.Counts[(int)Alchemy.Core.Material.Iron]==1,"catalysis spends the ether, not the iron");
            }
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
            Check(box.Inventory.Count(candidates[0])==chosenBefore+1 && box.Inventory.Offers.Count==0,"choosing grants the one chosen material");
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
                player.Creature.CombatState!.CreateCard<StrikeAlchemist>(player),
                player.Creature.CombatState.CreateCard<DefendAlchemist>(player)
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
            await Clear("furnace battle");
            // v0.22 (G-2): Orobas swaps the box for its refined form; the inventory must come along.
            box.Inventory.Counts=[1,2,3,0];
            var orobas=(TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable();
            orobas.SetupForPlayer(player);
            await RelicCmd.Obtain(orobas,player);
            var refinedBox=player.GetRelic<MaterialBox>();
            Check(refinedBox is RefinedMaterialBox && player.Relics.OfType<MaterialBox>().Count()==1 && refinedBox.Inventory.Counts.SequenceEqual([1,2,3,0]),
                "Orobas refines the box and the materials come with it");
            box=refinedBox!;
            await RunManager.Instance.EnterRoomDebug(RoomType.Monster,model:ModelDb.Encounter<BowlbugsWeak>().ToMutable(),showTransition:false);
            await Until(()=>box.Combat!=null && player.PlayerCombatState?.Hand.Cards.OfType<FurnaceActivation>().Count()==HarvestCombat.RefinedFurnaceLimit,"refined furnace battle ready");
            Check(box.Combat!.Limit==HarvestCombat.RefinedFurnaceLimit,"the refined box deals three furnace activations and allows three");
            Check(PhaseRules.IsElement(box.Combat.Phases.Current) && box.Combat.Phases.TransitionCount==0,"the refined box opens the battle in an element without a transition");
            // v0.22 (G-3/6.4): the crucible power adds one material of the current phase per turn, never past a full box.
            await PowerCmd.Apply<CruciblePower>(new ThrowingPlayerChoiceContext(),player.Creature,1,player.Creature,null);
            var crucible=player.Creature.GetPower<CruciblePower>()!;
            var turnState=player.Creature.CombatState!;
            var phaseMaterial=AlchemyPhaseState.MaterialFor(box.Combat.Phases.Current)!.Value;
            int beforeTrickle=box.Inventory.Counts[(int)phaseMaterial];
            turnState.RoundNumber+=10;
            await crucible.AfterSideTurnStart(CombatSide.Player,[player.Creature],turnState);
            await crucible.AfterSideTurnStart(CombatSide.Player,[player.Creature],turnState);
            Check(box.Inventory.Counts[(int)phaseMaterial]==beforeTrickle+1,"the crucible grants one material of the current phase once per turn");
            box.Inventory.Counts=[AlchemyState.BaseCapacity,0,0,0];
            turnState.RoundNumber++;
            await crucible.AfterSideTurnStart(CombatSide.Player,[player.Creature],turnState);
            Check(box.Inventory.Total==AlchemyState.BaseCapacity && box.Inventory.Pending.Count==0,"a full box receives nothing from the crucible and queues nothing");
            box.Inventory.Counts=[0,0,0,0];
            // 2026-10-01: the alchemist's own relics and potions replace the borrowed Ironclad pools.
            var alchemist=ModelDb.Character<AlchemistCharacter>();
            var poolRelics=alchemist.RelicPool.AllRelics.Select(r=>r.GetType()).ToHashSet();
            Type[] newRelics=[typeof(PhaseCompass),typeof(BloodChalice),typeof(PulsingCore),typeof(Quadrant),typeof(GreatCrucible),typeof(WardensFoundation),typeof(LargeMaterialBag)];
            Check(newRelics.All(poolRelics.Contains) && !poolRelics.Overlaps(ModelDb.RelicPool<IroncladRelicPool>().AllRelics.Select(r=>r.GetType()))
                && alchemist.PotionPool.AllPotions.Select(p=>p.GetType()).ToHashSet().SetEquals([typeof(PhaseTonic),typeof(LifeDrop),typeof(GolemElixir)]),
                $"the alchemist's relic and potion pools are their own ({string.Join(",",poolRelics.Select(t=>t.Name))})");
            Check(alchemist.RelicPool.AllRelics.Where(r=>newRelics.Contains(r.GetType())).Select(r=>r.Rarity).OrderBy(r=>r).SequenceEqual(
                    new[]{RelicRarity.Common,RelicRarity.Uncommon,RelicRarity.Uncommon,RelicRarity.Rare,RelicRarity.Rare,RelicRarity.Rare,RelicRarity.Shop}.OrderBy(r=>r)),
                "the new relics are Common 1, Uncommon 2, Rare 3, Shop 1 like a base game character's");
            async Task<T> Gain<T>() where T:RelicModel { var r=(T)ModelDb.Relic<T>().ToMutable(); await RelicCmd.Obtain(r,player); return player.GetRelic<T>()!; }
            var ctx=new ThrowingPlayerChoiceContext();
            var relicFoe=turnState.HittableEnemies[0];
            relicFoe.SetCurrentHpInternal(999);
            await Gain<LargeMaterialBag>();
            Check(box.Inventory.Capacity==AlchemyState.BaseCapacity+AlchemyState.LargeBagBonus,$"the large bag raises the box to 30 ({box.Inventory.Capacity})");
            await Gain<Quadrant>();
            var greatCrucible=await Gain<GreatCrucible>();
            // The furnace cards arrive before the turn's energy does; let the first turn start finish.
            await Until(()=>player.PlayerCombatState.Energy>0,"first turn energy");
            box.Combat.Phases.StartTurn();
            int energyBefore=player.PlayerCombatState.Energy;
            var start=box.Combat.Phases.Current;
            var others=new[]{AlchemyPhase.Earth,AlchemyPhase.Water,AlchemyPhase.Fire,AlchemyPhase.Air}.Where(p=>p!=start).ToArray();
            await PhaseTransitions.Enter(ctx,player,others[0]);
            await PhaseTransitions.Enter(ctx,player,others[1]);
            Check(player.PlayerCombatState.Energy==energyBefore,$"the quadrant waits for the third transition (energy {energyBefore}->{player.PlayerCombatState.Energy}, transitions {box.Combat.Phases.TransitionsThisTurn}, {start}->{string.Join(",",others)})");
            await PhaseTransitions.Enter(ctx,player,others[0]);
            Check(player.PlayerCombatState.Energy==energyBefore+1,"the third transition of a turn gives one energy");
            await PhaseTransitions.Enter(ctx,player,others[2]);
            Check(player.PlayerCombatState.Energy==energyBefore+1,"the fourth does not");
            player.Creature.LoseBlockInternal(player.Creature.Block);
            await greatCrucible.BeforeSideTurnEnd(ctx,CombatSide.Player,[player.Creature]);
            Check(box.Combat.Phases.KindsEnteredThisTurn==3 && player.Creature.Block==3*GreatCrucible.BlockPerKind,
                $"the great crucible gives block per distinct element entered this turn ({box.Combat.Phases.KindsEnteredThisTurn} kinds, {player.Creature.Block} block)");
            var pulsing=await Gain<PulsingCore>();
            int golemBefore=LifeAxis.State(player)!.HomunculusHp;
            await pulsing.AfterSideTurnStart(CombatSide.Player,[player.Creature],turnState);
            Check(LifeAxis.State(player)!.HomunculusHp==golemBefore+PulsingCore.Gain && LifeAxis.Pet(player) is { IsAlive: true },
                "the pulsing core adds golem HP at the turn start and brings the golem out");
            await Gain<WardensFoundation>();
            int foeDrainBefore=relicFoe.GetPower<LifeDrainPower>()?.Amount ?? 0;
            await LifeAxis.SpendAllHomunculus(ctx,player);
            Check(LifeAxis.Pet(player) is { IsDead: true } && (relicFoe.GetPower<LifeDrainPower>()?.Amount ?? 0)==foeDrainBefore+WardensFoundation.Drain,
                "the warden's foundation drains every enemy when the golem falls");
            var drop=await PotionCmd.TryToProcure<LifeDrop>(player);
            await drop.potion.OnUseWrapper(ctx,relicFoe);
            Check((relicFoe.GetPower<LifeDrainPower>()?.Amount ?? 0)==foeDrainBefore+WardensFoundation.Drain+LifeDrop.Drain,"the life drop drains one enemy");
            var elixir=await PotionCmd.TryToProcure<GolemElixir>(player);
            await elixir.potion.OnUseWrapper(ctx,player.Creature);
            Check(LifeAxis.State(player)!.HomunculusHp==GolemElixir.Gain && LifeAxis.Pet(player) is { IsAlive: true },"the golem elixir gives 20 golem HP and revives it");
            var tonic=await PotionCmd.TryToProcure<PhaseTonic>(player);
            var tonicTarget=box.Combat.Phases.Current==AlchemyPhase.Water ? AlchemyPhase.Air : AlchemyPhase.Water;
            int tonicTransitionsBefore=box.Combat.Phases.TransitionCount;
            using(CardSelectCmd.UseSelector(new PickSelector(c=>tonicTarget==AlchemyPhase.Water ? c is HerbMaterialCard : c is EtherMaterialCard)))
                await tonic.potion.OnUseWrapper(ctx,player.Creature);
            Check(box.Combat.Phases.Current==tonicTarget && box.Combat.Phases.TransitionCount==tonicTransitionsBefore+1,"the phase tonic moves to the chosen element as a transition");
            await Gain<PhaseCompass>();
            await box.BeforeCombatStart();
            Check(box.Combat!.Phases.TriggerFromNone,"with the compass, a new combat counts leaving the neutral phase as a transition");
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
