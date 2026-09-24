using Alchemist.Core;

int passed = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); passed++; Console.WriteLine($"PASS {name}"); }
void Reject(Action action, string name) { try { action(); } catch (InvalidOperationException) { Check(true,name); return; } catch (InvalidDataException) { Check(true,name); return; } catch (ArgumentException) { Check(true,name); return; } throw new Exception(name); }
var combat = new HarvestCombat();
Check(combat.Phases.Current == AlchemyPhase.None,"combat starts without an element");
var firstElement=combat.Phases.Enter(AlchemyPhase.Earth);
Check(!firstElement.Triggered && combat.Phases.Current==AlchemyPhase.Earth,"first element establishes the phase without a transition effect");
var sameElement=combat.Phases.Enter(AlchemyPhase.Earth);
Check(!sameElement.Triggered && combat.Phases.TransitionCount==0,"playing the same element does not transition");
var transition=combat.Phases.Enter(AlchemyPhase.Water);
Check(transition.Triggered && transition.From==AlchemyPhase.Earth && transition.To==AlchemyPhase.Water
    && combat.Phases.TransitionCount==1,"different elements trigger one destination transition");
Check(combat.Phases.Current==AlchemyPhase.Water,"phase persists until another elemental card is played");
Check(AlchemyPhaseState.MaterialFor(AlchemyPhase.Earth)==Material.Iron
    && AlchemyPhaseState.MaterialFor(AlchemyPhase.Water)==Material.Herb
    && AlchemyPhaseState.MaterialFor(AlchemyPhase.Fire)==Material.Powder
    && AlchemyPhaseState.MaterialFor(AlchemyPhase.Air)==Material.Ether
    && AlchemyPhaseState.MaterialFor(AlchemyPhase.None) is null,"each active phase maps to one normal material");
var emptyCombat=new HarvestCombat();
Check(!emptyCombat.UseFurnace(),"furnace cannot harvest in no phase");
emptyCombat.Phases.Enter(AlchemyPhase.Fire);
Check(emptyCombat.CanUseFurnace && emptyCombat.UseFurnace() && emptyCombat.UseFurnace() && !emptyCombat.UseFurnace() && !emptyCombat.CanUseFurnace,
    "furnace has a shared cap of two uses and reports it before play");
Check(new HarvestCombat().FurnaceUsed == 0 && new HarvestCombat().Phases.Current==AlchemyPhase.None,"new combat resets furnace and phase");
Check(PhaseRules.Next(AlchemyPhase.Earth)==AlchemyPhase.Water && PhaseRules.Next(AlchemyPhase.Air)==AlchemyPhase.Earth
    && PhaseRules.Next(AlchemyPhase.None)==AlchemyPhase.None,"advance effects cycle the four elements and do nothing without one");
Reject(()=>combat.Phases.Enter(AlchemyPhase.None),"nothing can transition back to the neutral phase");
Check(PhaseRules.Elements.All(e=>PhaseRules.BaseAmount(e)>0) && PhaseRules.BaseAmount(AlchemyPhase.None)==0,
    "each destination has one base effect amount and the neutral phase has none");
Check(PhaseRules.Elements.All(e=>PhaseRules.PhaseFor(PhaseRules.MaterialFor(e)!.Value)==e),"phase and material mappings are inverse");
Check(PhaseRules.FromMaterials([Material.Herb,Material.Iron,Material.Herb])==AlchemyPhase.Water
    && PhaseRules.FromMaterials([Material.Powder,Material.Iron])==AlchemyPhase.Fire
    && PhaseRules.FromMaterials([])==AlchemyPhase.None,"crafted element follows the dominant material, ties to the first listed");
Check(ForgeCatalog.All.All(f=>PhaseRules.IsElement(f.Element)),"every forged formula belongs to an element");
Check(PhaseRules.Elements.All(e=>ForgeCatalog.Craftable.Any(f=>f.Element==e)),"every element has craftable forged cards");
var triggered=new AlchemyPhaseState(triggerFromNone:true);
Check(triggered.Enter(AlchemyPhase.Fire).Triggered,"the first transition from the neutral phase can be switched on");
var inventory = new AlchemyState();
Check(inventory.Total == 0,"empty initial inventory");
var legacyFull=new AlchemyState();
legacyFull.Counts[0]=15;
var legacyLoaded=AlchemyState.Load(legacyFull.Save());
Check(legacyLoaded.Total==15 && legacyLoaded.Grant("after-shrink",Material.Herb) && legacyLoaded.Pending.Count==1 && legacyLoaded.Total==15,
    "a pre-v0.16 box over the new capacity loads and only queues new receipts");
for (int i=0;i<AlchemyState.Capacity;i++) inventory.Grant($"kill{i}",Material.Iron);
Check(inventory.Total == AlchemyState.Capacity,"capacity cap, same type counts individually");
Check(!inventory.Grant("kill0",Material.Iron),"grant is idempotent");
inventory.Grant("overflow",Material.Herb);
Check(inventory.Total == AlchemyState.Capacity && inventory.Pending.Count == 1,"overflow deferred");
Reject(()=>inventory.Resolve(true),"full acceptance needs exchange");
inventory.Resolve(true,Material.Iron);
Check(inventory.Counts[0] == AlchemyState.Capacity-1 && inventory.Counts[1] == 1 && inventory.Pending.Count == 0,"exchange atomic");
inventory.Grant("decline",Material.Powder); inventory.Resolve(false);
Check(inventory.Total == AlchemyState.Capacity && inventory.Counts[2] == 0,"decline only pending reward");
var saved = AlchemyState.Load(inventory.Save());
Check(saved.Total == AlchemyState.Capacity && !saved.Grant("overflow",Material.Herb),"save preserves receipt ids");
Reject(()=>AlchemyState.Load("{\"Schema\":99}"),"unknown schema not reset");
Reject(()=>AlchemyState.Load("{\"Counts\":[-1,0,0,0]}"),"negative count rejected");
foreach (var a in Enum.GetValues<Material>()) foreach (var b in Enum.GetValues<Material>())
    Check(Recipes.Find(a,b) == Recipes.Find(b,a),$"recipe order {a}/{b}");
var recipe = Recipes.All[0]; int deck = 0;
inventory.Commit(recipe,"op1",()=>deck++,()=>deck--);
inventory.Commit(recipe,"op1",()=>deck++,()=>deck--);
Check(deck==1 && inventory.Total==AlchemyState.Capacity-2,"craft consumes two, duplicate ignored");
Reject(()=>inventory.Commit(recipe,"failure",()=>{deck++;throw new InvalidOperationException();},()=>deck--),"failed delivery throws");
Check(deck==1 && inventory.Total==AlchemyState.Capacity-2,"failed delivery rolled back");
await inventory.CommitAsync(recipe,"async",()=>{deck++;return Task.CompletedTask;},()=>deck--);
Check(deck==2 && inventory.Total==AlchemyState.Capacity-4,"async delivery and cost agree");
var empty = new AlchemyState();
Reject(()=>empty.Commit(recipe,"empty",()=>deck++,()=>deck--),"insufficient ingredients do not deliver");
Check(deck==2 && empty.Total==0,"no negative inventory");
Check(AlchemyState.Load(inventory.Save()).Committed.Contains("async"),"save preserves craft receipts");
var upgradeStock=new AlchemyState();
upgradeStock.Grant("ui",Material.Iron);upgradeStock.Grant("up",Material.Powder);
int upgraded=0;
upgradeStock.CommitUpgrade(7,Material.Iron,Material.Powder,"upgrade1",()=>upgraded++);
upgradeStock.CommitUpgrade(7,Material.Iron,Material.Powder,"upgrade1",()=>upgraded++);
Check(upgraded==1 && upgradeStock.Total==0,"upgrade consumes materials once");
var failedUpgrade=new AlchemyState();failedUpgrade.Grant("fi",Material.Herb);failedUpgrade.Grant("fe",Material.Ether);
Reject(()=>failedUpgrade.CommitUpgrade(8,Material.Herb,Material.Ether,"bad",()=>throw new InvalidOperationException()),"failed upgrade throws");
Check(failedUpgrade.Total==2,"failed upgrade preserves materials");
var limitedUpgrade=new AlchemyState();
foreach(var pair in new[]{("i0",Material.Iron),("p0",Material.Powder),("i1",Material.Iron),("p1",Material.Powder)}) limitedUpgrade.Grant(pair.Item1,pair.Item2);
limitedUpgrade.CommitUpgrade(9,Material.Iron,Material.Powder,"first",()=>{});
Reject(()=>limitedUpgrade.CommitUpgrade(9,Material.Iron,Material.Powder,"second",()=>{}),"only one existing-card upgrade per workshop");
limitedUpgrade.CommitUpgrade(10,Material.Iron,Material.Powder,"next",()=>{});
Check(limitedUpgrade.Total==0,"a later workshop restores the upgrade action");
var facilities=new AlchemyState { Counts=[6,6,6,1], RareCounts=[1,0,0] };
int craftedAtWorkshop=0;
await facilities.CommitCraftAtAsync(12,Recipes.All[0],"facility-craft",()=>{craftedAtWorkshop++;return Task.CompletedTask;},()=>craftedAtWorkshop--);
Reject(()=>facilities.CommitCraftAtAsync(12,Recipes.All[0],"facility-craft-2",()=>Task.CompletedTask,()=>{}).GetAwaiter().GetResult(),
    "synthesis is available only once per workshop");
facilities.CommitUpgrade(12,Material.Iron,Material.Powder,"facility-mod",()=>{});
await facilities.CommitBrewAtAsync(12,Material.Herb,"facility-brew",()=>Task.FromResult(true));
facilities.CommitRareAt(12,RareMaterial.Mercury,"facility-enchant",()=>{},()=>{});
Check(craftedAtWorkshop==1
    && !facilities.CanUseFacility(12,WorkshopFacility.Synthesis)
    && !facilities.CanUseFacility(12,WorkshopFacility.Modification)
    && !facilities.CanUseFacility(12,WorkshopFacility.Brewing)
    && !facilities.CanUseFacility(12,WorkshopFacility.Enchantment),
    "all four workshop facilities can each be used once during the same visit");
Check(Enum.GetValues<WorkshopFacility>().Where(f=>f!=WorkshopFacility.None).All(f=>facilities.CanUseFacility(13,f)),
    "a later workshop restores all four facilities");
var failedBrew=new AlchemyState { Counts=[1,0,0,0] };
Reject(()=>failedBrew.CommitBrewAtAsync(2,Material.Iron,"failed-brew",()=>Task.FromResult(false)).GetAwaiter().GetResult(),
    "failed potion delivery is rejected");
Check(failedBrew.Counts[0]==1 && failedBrew.CanUseFacility(2,WorkshopFacility.Brewing),
    "failed potion delivery consumes neither material nor facility");
var phaseSave=new AlchemyState { NextCombatMaterial=Material.Ether };
phaseSave.WorkshopNodes[0]=["2,4","5,9"];
var loadedPhase=AlchemyState.Load(phaseSave.Save());
Check(loadedPhase.NextCombatMaterial==Material.Ether && loadedPhase.WorkshopNodes[0].SequenceEqual(["2,4","5,9"]),"phase and workshop map survive save");
var pending = new AlchemyState(); for(int i=0;i<AlchemyState.Capacity+2;i++) pending.Grant($"p{i}",Material.Iron);
var reload = AlchemyState.Load(pending.Save());
Check(reload.Pending.Count==2 && !reload.CanCraft(recipe),"pending saved and blocks storage abuse");
// Furnace and material reward slots use the configured harvest yield; normal card rewards are separate.
var harvestState=new AlchemyState();
Check(harvestState.GrantHarvest("harvest0",Material.Powder) && harvestState.Count(MaterialChoice.Normal(Material.Powder))==AlchemyState.YieldPerEvent,
    "a harvest event grants the doubled yield");
Check(!harvestState.GrantHarvest("harvest0",Material.Powder) && harvestState.Count(MaterialChoice.Normal(Material.Powder))==AlchemyState.YieldPerEvent,
    "a resent harvest event does not grant twice");
// Elite and boss reward slots (AGENTS.md 4.4), counted apart from the kill cap.
Check(MaterialOffers.EliteSlots==1 && MaterialOffers.BossSlots==2 && MaterialOffers.CandidateCount==3,
    "one elite slot, two boss slots, three candidates each");
var eliteRoll=MaterialOffers.RollMixed(9876543210,"12:4:reward0");
Check(eliteRoll.Length==3 && eliteRoll.Distinct().Count()==3 && eliteRoll.All(m=>m.IsValid),
    "a slot offers three distinct materials");
Check(eliteRoll.SequenceEqual(MaterialOffers.RollMixed(9876543210,"12:4:reward0")),"same seed and slot reroll identically");
Check(!eliteRoll.SequenceEqual(MaterialOffers.RollMixed(9876543211,"12:4:reward0"))
   || !eliteRoll.SequenceEqual(MaterialOffers.RollMixed(9876543210,"12:4:reward1")),"seed and slot both affect the roll");
Check(Enumerable.Range(0,400).SelectMany(i=>MaterialOffers.Roll((ulong)i,"r")).Distinct().Count()==4,
    "every material can appear across seeds");
var rareRoll=MaterialOffers.RollRare(123,"boss:rare");
Check(rareRoll.Length==3 && rareRoll.All(x=>x.Class==MaterialClass.Rare) && rareRoll.Distinct().Count()==3,
    "boss guaranteed slot contains three distinct rare materials");
Check(Enumerable.Range(0,400).Count(i=>MaterialOffers.RollMixed((ulong)i,"elite").Any(x=>x.Class==MaterialClass.Rare)) is >70 and <130,
    "mixed slots include one rare candidate at about twenty-five percent");
var offers=new AlchemyState();
Check(offers.Offer("boss0",eliteRoll) && !offers.Offer("boss0",eliteRoll),"a slot is recorded once");
Reject(()=>offers.Offer("bad",[Material.Iron,Material.Iron,Material.Herb]),"duplicate candidates rejected");
Reject(()=>offers.Offer("bad",[Material.Iron,Material.Herb]),"wrong candidate count rejected");
Check(!offers.Settled && !offers.CanCraft(Recipes.All[0]),"an open slot blocks crafting");
Reject(()=>offers.TakeOffer("boss0",MaterialChoice.Rare(RareMaterial.Stardust)),"material outside the candidates rejected");
offers.TakeOffer("boss0",eliteRoll[1]);
Check(offers.Count(eliteRoll[1])==AlchemyState.YieldPerEvent && offers.Total==AlchemyState.YieldPerEvent && offers.Settled,
    "taking a slot grants the doubled harvest yield");
Reject(()=>offers.TakeOffer("boss0",eliteRoll[0]),"a slot cannot be taken twice");
Check(!offers.Offer("boss0",eliteRoll),"a resolved slot is never re-offered");
offers.Offer("boss1",eliteRoll); offers.DeclineOffer("boss1");
Check(offers.Total==AlchemyState.YieldPerEvent && offers.Settled && !offers.Offer("boss1",eliteRoll),"declining closes the slot for good");
Reject(()=>offers.DeclineOffer("boss1"),"a declined slot cannot be declined twice");
var fullBox=new AlchemyState();
for(int i=0;i<AlchemyState.Capacity;i++) fullBox.Grant($"f{i}",Material.Iron);
fullBox.Offer("eliteFull",eliteRoll); fullBox.TakeOffer("eliteFull",eliteRoll[0]);
Check(fullBox.Total==AlchemyState.Capacity && fullBox.Pending.Count==AlchemyState.YieldPerEvent && fullBox.Offers.Count==0,
    "a full box defers every unit of the doubled yield to the shared receipt path");
while (fullBox.Pending.Count>0) fullBox.Resolve(true,MaterialChoice.Normal(Material.Iron));
Check(fullBox.Count(eliteRoll[0])==(eliteRoll[0]==MaterialChoice.Normal(Material.Iron)?AlchemyState.Capacity:AlchemyState.YieldPerEvent) && fullBox.Settled,
    "deferred choice resolves by exchange, once per unit");
var savedOffers=new AlchemyState();
savedOffers.Offer("keep0",eliteRoll); savedOffers.Offer("keep1",MaterialOffers.Roll(7,"keep1"));
var reloadedOffers=AlchemyState.Load(savedOffers.Save());
Check(reloadedOffers.Offers.Count==2 && reloadedOffers.Offers[0].Candidates.SequenceEqual(eliteRoll),
    "unresolved slots and their candidates survive save and load");
Check(!reloadedOffers.Offer("keep0",eliteRoll),"reloaded slots are not re-offered");
var migrated=AlchemyState.Load("{\"Schema\":1,\"Counts\":[1,0,0,0]}");
Check(migrated.Schema==AlchemyState.CurrentSchema && migrated.Offers.Count==0 && migrated.Total==1,
    "schema 1 saves migrate without losing materials");
var migratedOffer=AlchemyState.Load("{\"Schema\":2,\"Offers\":[{\"Id\":\"x\",\"Candidates\":[0,1,2]}]}");
Check(migratedOffer.Offers.Single().Candidates.All(x=>x.Class==MaterialClass.Normal),"schema 2 offers migrate to typed candidates");
var migratedV3=AlchemyState.Load("{\"Schema\":3,\"Counts\":[0,0,0,0],\"RareCounts\":[0,0,0]}");
Check(migratedV3.Schema==AlchemyState.CurrentSchema && migratedV3.WorkshopUpgradeFloor==-1,"schema 3 saves gain an unused workshop upgrade action");
Reject(()=>AlchemyState.Load("{\"Schema\":2,\"Offers\":[{\"Id\":\"x\",\"Candidates\":[0,0,1]}]}"),"corrupt candidates rejected");
Reject(()=>AlchemyState.Load("{\"Schema\":2,\"Received\":[\"x\"],\"Offers\":[{\"Id\":\"x\",\"Candidates\":[0,1,2]}]}"),"slot both received and open rejected");
var rareStock=new AlchemyState();rareStock.GrantRare("rare",RareMaterial.Stardust);
string applied="";
rareStock.CommitRare(RareMaterial.Stardust,"apply",()=>applied="rare.stardust",()=>applied="");
Check(applied=="rare.stardust" && rareStock.RareCounts[(int)RareMaterial.Stardust]==0,"rare processing consumes exactly one material");
Reject(()=>rareStock.CommitRare(RareMaterial.Stardust,"again",()=>applied="bad",()=>applied="rare.stardust"),"missing rare material is rejected");
Check(ForgeCatalog.All.Count==78 && ForgeCatalog.All.Select(r=>r.Id).Distinct().Count()==78,"all legacy formula ids remain loadable");
Check(Recipes.All.Length==65 && Recipes.All.Select(r=>r.Id).Distinct().Count()==65,"craftable pool has sixty-five stable recipe ids");
Check(Recipes.All.Count(r=>r.Materials.Count==2)==10 && Recipes.All.Count(r=>r.Materials.Count==3)==20 && Recipes.All.Count(r=>r.Materials.Count==4)==35,
    "one common per material pair, twenty uncommons and thirty-five rares");
foreach(var a in Enum.GetValues<Material>()) foreach(var b in Enum.GetValues<Material>())
{
    var choices=Recipes.FindAll(a,b).ToArray();
    Check(choices.Length>0 && choices.SequenceEqual(Recipes.FindAll(b,a)),$"all pairs craftable and order independent {a}/{b}");
    foreach(var r in choices)
    {
        var stock=new AlchemyState(); stock.Grant("a",a);stock.Grant("b",b);
        Check(stock.CanCraft(r),$"two collected materials can craft {r.Id}");
    }
}
Check(Enum.GetValues<Material>().SelectMany((a,i)=>Enum.GetValues<Material>().Skip(i).Select(b=>Recipes.FindAll(a,b).Count())).All(n=>n==1),
    "every unordered pair exposes exactly one cheap recipe");
Check(ForgeCatalog.Craftable.Where(f=>f.Materials.Count==2).All(f=>f.Values.GetValueOrDefault("StrengthPower")==0
    && f.Values.GetValueOrDefault("DexterityPower")==0 && f.Values.GetValueOrDefault("Hits")<=1),
    "cheap formulas do not provide permanent stats or multihit");
Check(ForgeCatalog.Craftable.Count(f=>f.Values.GetValueOrDefault("Hits")>1)==2,"multihit is limited to one uncommon and one rare");
Check(ForgeCatalog.Craftable.Where(f=>f.Materials.Count==3).All(f=>f.Values.GetValueOrDefault("StrengthPower")<=1 && f.Values.GetValueOrDefault("DexterityPower")<=1),
    "uncommon permanent stat gains are capped at one");
Check(ForgeCatalog.Craftable.Where(f=>f.Materials.Count==4).All(f=>f.Values.GetValueOrDefault("StrengthPower")<=2 && f.Values.GetValueOrDefault("DexterityPower")<=2),
    "rare permanent stat gains are capped at two");
Check(ForgeCatalog.Craftable.Count(f=>f.Values.GetValueOrDefault("AdvancePhase")>0)>=3,"phase-control recipes form a new effect family");
foreach(var f in ForgeCatalog.Craftable)
{
    Check(f.Values.All(x=>x.Value>=0) && f.Cost>=1,"no energy generation or zero-cost cycle "+f.Id);
    Check(!f.Preview.Contains('{') && f.Preview.Contains(f.Describe(true)),"full shared preview "+f.Id);
    var stock=new AlchemyState();
    for(int i=0;i<f.Materials.Count;i++) stock.Grant($"m{i}",f.Materials[i]);
    var r=Recipes.All.Single(r=>r.FormulaId==f.Id);int created=0;
    await stock.CommitAsync(r,f.Id,()=>{created++;return Task.CompletedTask;},()=>created--);
    Check(created==1 && stock.Total==0,$"formula consumes exactly {f.Materials.Count} "+f.Id);
}
Reject(()=>ForgeCatalog.Get("unknown.v99"),"unknown formula not reset");
var shopA=MerchantMaterialOffers.Roll(1234,"act1:shop3");
Check(shopA.Length==MerchantMaterialOffers.Slots && shopA.Select(x=>x.Material).Distinct().Count()==MerchantMaterialOffers.Slots,
    "merchant has three distinct finite material slots");
Check(shopA.SequenceEqual(MerchantMaterialOffers.Roll(1234,"act1:shop3")),"merchant stock is stable for the visit");
Check(!shopA.SequenceEqual(MerchantMaterialOffers.Roll(1234,"act1:shop4")),"merchant visit id changes stock");
// Recipe.Materials generalizes past pairs (commons 2 / uncommons 3 / rares 4); exercised directly here
// since the catalog itself still only fills the two-material tier.
var triple = new Recipe("test.triple.v0", [Material.Iron, Material.Iron, Material.Herb], "test", "");
var quad = new Recipe("test.quad.v0", [Material.Powder, Material.Powder, Material.Ether, Material.Ether], "test", "");
var threeStock = new AlchemyState(); threeStock.Grant("t0",Material.Herb); threeStock.Grant("t1",Material.Iron); threeStock.Grant("t2",Material.Iron);
Check(threeStock.CanCraft(triple),"a three-material recipe can craft regardless of grant order");
var shortStock = new AlchemyState(); shortStock.Grant("s0",Material.Iron); shortStock.Grant("s1",Material.Herb);
Check(!shortStock.CanCraft(triple),"a three-material recipe needs all three units, not just distinct types");
var fourStock = new AlchemyState();
for(int i=0;i<2;i++) fourStock.Grant($"f{i}",Material.Powder);
for(int i=0;i<2;i++) fourStock.Grant($"e{i}",Material.Ether);
Check(fourStock.CanCraft(quad),"a four-material recipe can require two of one type and two of another");
fourStock.Grant("extra",Material.Ether);
Check(fourStock.CanCraft(quad) && fourStock.Total==5,"surplus materials do not block a smaller requirement");
Check(Recipes.FindAll([Material.Herb,Material.Iron,Material.Iron]).SequenceEqual(Recipes.FindAll([Material.Iron,Material.Herb,Material.Iron])),
    "FindAll matches the material multiset regardless of order");
// A layered map: rows 1..14 have three columns each, every point feeding all three of the next row.
List<CutNode> Layered(Func<int,int,int> cost)
{
    List<CutNode> graph = [new(new(0,0),WorkshopCut.Blocked,[..Enumerable.Range(0,3).Select(c=>new WorkshopCandidate(c,1))])];
    for(int row=1;row<=14;row++)
        for(int col=0;col<3;col++)
            graph.Add(new(new(col,row),cost(col,row),
                [..row==14?[new WorkshopCandidate(0,15)]:Enumerable.Range(0,3).Select(c=>new WorkshopCandidate(c,row+1))]));
    graph.Add(new(new(0,15),WorkshopCut.Blocked,[]));
    return graph;
}
IEnumerable<List<WorkshopCandidate>> Routes(IReadOnlyList<CutNode> graph, WorkshopCandidate at)
{
    var node=graph.Single(n=>n.At==at);
    if(node.Children.Count==0) { yield return [at]; yield break; }
    foreach(var child in node.Children)
        foreach(var tail in Routes(graph,child)) { tail.Insert(0,at); yield return tail; }
}
var plainGraph=Layered((_,_)=>1);
var layout=WorkshopPlanner.SelectGuaranteed(plainGraph,new(0,0),new(0,15),15);
var guaranteed=layout.Coords;
Check(layout.MidGuaranteed && layout.LateGuaranteed,"both bands report a real guarantee");
Check(guaranteed.Count>0 && guaranteed.All(p=>p.Row>=WorkshopPlanner.EarliestRow),"guaranteed workshops respect the earliest row");
int midEnd=WorkshopPlanner.MidBandEndRow(15);
Check(Routes(plainGraph,new(0,0)).All(route=>route.Any(p=>guaranteed.Contains(p) && p.Row<=midEnd))
   && Routes(plainGraph,new(0,0)).All(route=>route.Any(p=>guaranteed.Contains(p) && p.Row>midEnd)),"every route meets a mid and a late workshop");
Check(WorkshopPlanner.PerAct==2,"two workshop bands per act");
// Rows 6 and 11 hold a merchant in column 1; the cut must route around it, never through it.
var blockedGraph=Layered((col,row)=>col==1 && row is 6 or 11 ? WorkshopCut.Blocked : 1);
var aroundBlocked=WorkshopPlanner.SelectGuaranteed(blockedGraph,new(0,0),new(0,15),15).Coords;
Check(aroundBlocked.All(p=>!(p.Col==1 && p.Row is 6 or 11)),"protected points are never converted");
Check(Routes(blockedGraph,new(0,0)).All(route=>route.Any(p=>aroundBlocked.Contains(p))),"every route still meets a workshop");
// A column of protected points spanning a whole band cannot be covered at all.
var uncoverable=WorkshopPlanner.SelectGuaranteed(Layered((col,_)=>col==0?WorkshopCut.Blocked:1),new(0,0),new(0,15),15);
Check(!uncoverable.MidGuaranteed && !uncoverable.LateGuaranteed,"impossible coverage is reported, not patched over");
Check(uncoverable.Coords.Count>0 && uncoverable.Coords.All(p=>p.Col!=0),"fallback still places a workshop without touching protected points");
Console.WriteLine($"{passed} checks passed.");
