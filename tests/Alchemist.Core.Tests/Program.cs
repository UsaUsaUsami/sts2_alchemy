using Alchemist.Core;

int passed = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); passed++; Console.WriteLine($"PASS {name}"); }
void Reject(Action action, string name) { try { action(); } catch (InvalidOperationException) { Check(true,name); return; } catch (InvalidDataException) { Check(true,name); return; } catch (ArgumentException) { Check(true,name); return; } throw new Exception(name); }
var combat = new HarvestCombat();
Check(combat.Phase == Material.Iron,"turn one iron");
foreach (int turn in Enumerable.Range(2,4)) { combat.BeginTurn(turn); Check(combat.Phase == (Material)((turn-1)%4),$"phase turn {turn}"); }
combat.BeginTurn(5); combat.BeginTurn(3);
Check(combat.Turn == 5,"duplicate and old turn ignored");
combat.BeginTurn(6);
Check(!combat.UseFurnace(),"inactive furnace denied");
combat.FurnaceActive = true;
Check(combat.UseFurnace() && combat.UseFurnace() && !combat.UseFurnace(),"furnace shared cap two");
combat.FurnaceActive = true;
Check(!combat.UseFurnace(),"reapplication cannot reset furnace");
Check(new HarvestCombat().FurnaceUsed == 0,"new combat resets furnace");
var continuous = new HarvestCombat(Material.Powder);
Check(continuous.Phase==Material.Powder,"combat starts from saved material");
continuous.BeginTurn(2);
Check(continuous.Phase==Material.Ether && continuous.NextPhase==Material.Iron,"continuous phase advances and wraps");
var inventory = new AlchemyState();
Check(inventory.Total == 0,"empty initial inventory");
for (int i=0;i<10;i++) inventory.Grant($"kill{i}",Material.Iron);
Check(inventory.Total == 10,"capacity ten, same type counts individually");
Check(!inventory.Grant("kill0",Material.Iron),"grant is idempotent");
inventory.Grant("overflow",Material.Herb);
Check(inventory.Total == 10 && inventory.Pending.Count == 1,"overflow deferred");
Reject(()=>inventory.Resolve(true),"full acceptance needs exchange");
inventory.Resolve(true,Material.Iron);
Check(inventory.Counts[0] == 9 && inventory.Counts[1] == 1 && inventory.Pending.Count == 0,"exchange atomic");
inventory.Grant("decline",Material.Powder); inventory.Resolve(false);
Check(inventory.Total == 10 && inventory.Counts[2] == 0,"decline only pending reward");
var saved = AlchemyState.Load(inventory.Save());
Check(saved.Total == 10 && !saved.Grant("overflow",Material.Herb),"save preserves receipt ids");
Reject(()=>AlchemyState.Load("{\"Schema\":99}"),"unknown schema not reset");
Reject(()=>AlchemyState.Load("{\"Counts\":[-1,0,0,0]}"),"negative count rejected");
foreach (var a in Enum.GetValues<Material>()) foreach (var b in Enum.GetValues<Material>())
    Check(Recipes.Find(a,b) == Recipes.Find(b,a),$"recipe order {a}/{b}");
var recipe = Recipes.All[0]; int deck = 0;
inventory.Commit(recipe,"op1",()=>deck++,()=>deck--);
inventory.Commit(recipe,"op1",()=>deck++,()=>deck--);
Check(deck==1 && inventory.Total==8,"craft consumes two, duplicate ignored");
Reject(()=>inventory.Commit(recipe,"failure",()=>{deck++;throw new InvalidOperationException();},()=>deck--),"failed delivery throws");
Check(deck==1 && inventory.Total==8,"failed delivery rolled back");
await inventory.CommitAsync(recipe,"async",()=>{deck++;return Task.CompletedTask;},()=>deck--);
Check(deck==2 && inventory.Total==6,"async delivery and cost agree");
var empty = new AlchemyState();
Reject(()=>empty.Commit(recipe,"empty",()=>deck++,()=>deck--),"insufficient ingredients do not deliver");
Check(deck==2 && empty.Total==0,"no negative inventory");
Check(AlchemyState.Load(inventory.Save()).Committed.Contains("async"),"save preserves craft receipts");
var upgradeStock=new AlchemyState();
upgradeStock.Grant("ui",Material.Iron);upgradeStock.Grant("up",Material.Powder);
int upgraded=0;
upgradeStock.CommitUpgrade(Material.Iron,Material.Powder,"upgrade1",()=>upgraded++);
upgradeStock.CommitUpgrade(Material.Iron,Material.Powder,"upgrade1",()=>upgraded++);
Check(upgraded==1 && upgradeStock.Total==0,"upgrade consumes materials once");
var failedUpgrade=new AlchemyState();failedUpgrade.Grant("fi",Material.Herb);failedUpgrade.Grant("fe",Material.Ether);
Reject(()=>failedUpgrade.CommitUpgrade(Material.Herb,Material.Ether,"bad",()=>throw new InvalidOperationException()),"failed upgrade throws");
Check(failedUpgrade.Total==2,"failed upgrade preserves materials");
var phaseSave=new AlchemyState { NextCombatMaterial=Material.Ether };
phaseSave.WorkshopNodes[0]=["2,4","5,9"];
var loadedPhase=AlchemyState.Load(phaseSave.Save());
Check(loadedPhase.NextCombatMaterial==Material.Ether && loadedPhase.WorkshopNodes[0].SequenceEqual(["2,4","5,9"]),"phase and workshop map survive save");
var pending = new AlchemyState(); for(int i=0;i<12;i++) pending.Grant($"p{i}",Material.Iron);
var reload = AlchemyState.Load(pending.Save());
Check(reload.Pending.Count==2 && !reload.CanCraft(recipe),"pending saved and blocks storage abuse");
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
Check(offers.Count(eliteRoll[1])==1 && offers.Total==1 && offers.Settled,"taking a slot grants exactly one material");
Reject(()=>offers.TakeOffer("boss0",eliteRoll[0]),"a slot cannot be taken twice");
Check(!offers.Offer("boss0",eliteRoll),"a resolved slot is never re-offered");
offers.Offer("boss1",eliteRoll); offers.DeclineOffer("boss1");
Check(offers.Total==1 && offers.Settled && !offers.Offer("boss1",eliteRoll),"declining closes the slot for good");
Reject(()=>offers.DeclineOffer("boss1"),"a declined slot cannot be declined twice");
var fullBox=new AlchemyState();
for(int i=0;i<10;i++) fullBox.Grant($"f{i}",Material.Iron);
fullBox.Offer("eliteFull",eliteRoll); fullBox.TakeOffer("eliteFull",eliteRoll[0]);
Check(fullBox.Total==10 && fullBox.Pending.Count==1 && fullBox.Offers.Count==0,
    "a full box defers the chosen material to the shared receipt path");
fullBox.Resolve(true,MaterialChoice.Normal(Material.Iron));
Check(fullBox.Count(eliteRoll[0])==(eliteRoll[0]==MaterialChoice.Normal(Material.Iron)?10:1) && fullBox.Settled,"deferred choice resolves by exchange");
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
Reject(()=>AlchemyState.Load("{\"Schema\":2,\"Offers\":[{\"Id\":\"x\",\"Candidates\":[0,0,1]}]}"),"corrupt candidates rejected");
Reject(()=>AlchemyState.Load("{\"Schema\":2,\"Received\":[\"x\"],\"Offers\":[{\"Id\":\"x\",\"Candidates\":[0,1,2]}]}"),"slot both received and open rejected");
var rareStock=new AlchemyState();rareStock.GrantRare("rare",RareMaterial.Stardust);
string applied="";
rareStock.CommitRare(RareMaterial.Stardust,"apply",()=>applied="rare.stardust",()=>applied="");
Check(applied=="rare.stardust" && rareStock.RareCounts[(int)RareMaterial.Stardust]==0,"rare processing consumes exactly one material");
Reject(()=>rareStock.CommitRare(RareMaterial.Stardust,"again",()=>applied="bad",()=>applied="rare.stardust"),"missing rare material is rejected");
Check(Recipes.All.Length==28 && Recipes.All.Select(r=>r.Id).Distinct().Count()==28,"twenty-eight stable recipe ids");
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
Check(Recipes.FindAll(Material.Iron,Material.Iron).Count()>=4,"iron offers attack defense multihit and scaling");
Check(Recipes.FindAll(Material.Herb,Material.Herb).Count()>=3,"herbs offer engine defense and dexterity");
Check(ForgeCatalog.All.Count(f=>f.Values.GetValueOrDefault("Hits")>1)>=6,"multiple multihit recipes available");
Check(ForgeCatalog.All.Count(f=>f.Values.GetValueOrDefault("StrengthPower")>0)>=5,"multiple strength recipes available");
Check(ForgeCatalog.All.Count(f=>f.Values.GetValueOrDefault("DexterityPower")>0)>=4,"multiple dexterity recipes available");
foreach(var f in ForgeCatalog.All)
{
    Check(f.Values.All(x=>x.Value>=0) && f.Cost>=1,"no energy generation or zero-cost cycle "+f.Id);
    Check(!f.Preview.Contains('{') && f.Preview.Contains(f.Describe(true)),"full shared preview "+f.Id);
    var stock=new AlchemyState(); stock.Grant("x",f.First);stock.Grant("y",f.Second);
    var r=Recipes.All.Single(r=>r.FormulaId==f.Id);int created=0;
    await stock.CommitAsync(r,f.Id,()=>{created++;return Task.CompletedTask;},()=>created--);
    Check(created==1 && stock.Total==0,"formula consumes exactly two "+f.Id);
}
Reject(()=>ForgeCatalog.Get("unknown.v99"),"unknown formula not reset");
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
