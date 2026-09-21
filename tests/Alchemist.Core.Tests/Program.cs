using Alchemist.Core;

int passed = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); passed++; Console.WriteLine($"PASS {name}"); }
void Reject(Action action, string name) { try { action(); } catch (InvalidOperationException) { Check(true,name); return; } catch (InvalidDataException) { Check(true,name); return; } throw new Exception(name); }
var combat = new HarvestCombat([1,2,3,4], 3);
Check(combat.Phase == Material.Iron,"turn one iron");
foreach (int turn in Enumerable.Range(2,4)) { combat.BeginTurn(turn); Check(combat.Phase == (Material)((turn-1)%4),$"phase turn {turn}"); }
combat.BeginTurn(5); combat.BeginTurn(3);
Check(combat.Turn == 5,"duplicate and old turn ignored");
combat.BeginTurn(6);
Check(combat.Kill(1) == Material.Herb && combat.Kill(2) == Material.Herb,"simultaneous/enemy-turn kills share phase");
Check(combat.Kill(1) is null,"death redelivery/revival cannot harvest twice");
Check(combat.Kill(99) is null,"summoned enemy excluded");
Check(combat.Kill(3) == Material.Herb && combat.Kill(4) is null,"harvest cap enforced");
Check(!combat.UseFurnace(),"inactive furnace denied");
combat.FurnaceActive = true;
Check(combat.UseFurnace() && combat.UseFurnace() && !combat.UseFurnace(),"furnace shared cap two");
combat.FurnaceActive = true;
Check(!combat.UseFurnace(),"reapplication cannot reset furnace");
Check(new HarvestCombat([1],2).FurnaceUsed == 0,"new combat resets furnace");
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
var pending = new AlchemyState(); for(int i=0;i<12;i++) pending.Grant($"p{i}",Material.Iron);
var reload = AlchemyState.Load(pending.Save());
Check(reload.Pending.Count==2 && !reload.CanCraft(recipe),"pending saved and blocks storage abuse");
Check(Recipes.All.Length==12 && Recipes.All.Select(r=>r.Id).Distinct().Count()==12,"twelve stable recipe ids");
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
Check(Recipes.FindAll(Material.Iron,Material.Iron).Count()==2,"iron offers attack or defense");
Check(Recipes.FindAll(Material.Herb,Material.Herb).Count()==2,"herbs offer engine or defense");
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
Console.WriteLine($"{passed} checks passed.");
