using System.Text.Json;

namespace Alchemist.Core;

public enum Material { Iron, Herb, Powder, Ether }
public sealed record Harvest(string Id, Material Material);
public sealed record Recipe(string Id, Material First, Material Second, string Name, string Preview,
    string Role = "基礎強化", string Plan = "", string? FormulaId = null);

public static class Recipes
{
    // Curated M1 recipes cover all ten pairs with multiple build directions; full base x material grammar remains M3.
    public static readonly Recipe[] All = [
        new("iron_guard.v1", Material.Iron, Material.Iron, "鍛鉄の護り", "1コスト / スキル / 自身\n16ブロック、1枚ドロー。強化後21ブロック。", "守りと循環", "大きく守りながら、切り札を引きに行く。"),
        new("herbal_edge.v1", Material.Iron, Material.Herb, "薬刃", "1コスト / アタック / 敵1体\n12ダメージ、弱体2。強化後16ダメージ、弱体3。", "攻撃の準備", "弱体を付け、大剣や全体攻撃の火力を伸ばす。"),
        new("blast.v1", Material.Powder, Material.Powder, "炸裂弾", "1コスト / アタック / 敵全体\n14ダメージ。強化後19。", "集団戦", "廃棄せず繰り返せる全体攻撃。"),
        new("ether_lens.v1", Material.Ether, Material.Ether, "エーテルレンズ", "1コスト / スキル / 自身\n3枚ドロー、8ブロック。強化後12ブロック。", "守りと循環", "手札を増やし、パワーや切り札を早く引く。"),
        new("herbal_guard.v1", Material.Herb, Material.Herb, "薬草の被膜", "1コスト / スキル / 自身\n12ブロック、敵全体に脱力2。強化後16ブロック。", "守りと循環", "敵全体の攻撃を弱め、毒が回る時間を作る。"),
        ..ForgeCatalog.All.Select(f=>new Recipe(f.Id,f.First,f.Second,f.Name,f.Preview,f.Role,f.Plan,f.Id))];
    public static IEnumerable<Recipe> FindAll(Material a, Material b) => All.Where(r =>
        r.First == a && r.Second == b || r.First == b && r.Second == a);
    public static Recipe? Find(Material a, Material b) => FindAll(a,b).FirstOrDefault();
    public static string Name(Material m) => m switch {
        Material.Iron => "鉄", Material.Herb => "薬草", Material.Powder => "火薬", Material.Ether => "エーテル", _ => throw new ArgumentOutOfRangeException(nameof(m)) };
}

public sealed class AlchemyState
{
    public const int Capacity = 10;
    public int Schema { get; set; } = 1;
    public int[] Counts { get; set; } = new int[4];
    public List<Harvest> Pending { get; set; } = [];
    public HashSet<string> Received { get; set; } = [];
    public HashSet<string> Committed { get; set; } = [];
    public int WorkshopClosedFloor { get; set; } = -1;
    public int Total => Counts.Sum();
    public int Revision { get; set; }

    public bool Grant(string id, Material material)
    {
        if (!Enum.IsDefined(material)) throw new ArgumentOutOfRangeException(nameof(material));
        if (!Received.Add(id)) return false;
        if (Total < Capacity && Pending.Count == 0) Counts[(int)material]++;
        else Pending.Add(new(id, material));
        Revision++;
        return true;
    }
    public void Resolve(bool accept, Material? exchange = null)
    {
        if (Pending.Count == 0) throw new InvalidOperationException("未受領素材がありません。");
        if (accept)
        {
            if (Total >= Capacity)
            {
                if (exchange is null || !Enum.IsDefined(exchange.Value) || Counts[(int)exchange] <= 0)
                    throw new InvalidOperationException("交換する素材を選んでください。");
                Counts[(int)exchange]--;
            }
            Counts[(int)Pending[0].Material]++;
        }
        Pending.RemoveAt(0);
        Revision++;
    }
    public bool CanCraft(Recipe r) => Pending.Count == 0 && Counts[(int)r.First] >= (r.First == r.Second ? 2 : 1) && Counts[(int)r.Second] >= 1;
    public void Commit(Recipe r, string operation, Action addCard, Action rollbackCard)
    {
        if (Committed.Contains(operation)) return;
        if (!Recipes.All.Contains(r) || !CanCraft(r)) throw new InvalidOperationException("素材が不足しているか、未受領素材があります。");
        int[] before = (int[])Counts.Clone();
        try
        {
            addCard();
            Counts[(int)r.First]--;
            Counts[(int)r.Second]--;
            Committed.Add(operation);
            Revision++;
        }
        catch
        {
            Counts = before;
            rollbackCard();
            throw;
        }
    }
    public async Task CommitAsync(Recipe r, string operation, Func<Task> addCard, Action rollbackCard)
    {
        if (Committed.Contains(operation)) return;
        if (!Recipes.All.Contains(r) || !CanCraft(r)) throw new InvalidOperationException("素材が不足しているか、未受領素材があります。");
        int[] before = (int[])Counts.Clone();
        try
        {
            await addCard();
            Counts[(int)r.First]--;
            Counts[(int)r.Second]--;
            Committed.Add(operation);
            Revision++;
        }
        catch { Counts = before; rollbackCard(); throw; }
    }
    public string Save() => JsonSerializer.Serialize(this);
    public static AlchemyState Load(string json)
    {
        var s = JsonSerializer.Deserialize<AlchemyState>(json) ?? throw new InvalidDataException("錬金術のセーブが空です。");
        if (s.Schema != 1 || s.Counts is not { Length: 4 } || s.Counts.Any(n => n < 0 || n > Capacity)
            || s.Total > Capacity || s.Pending is null || s.Received is null || s.Committed is null
            || s.Pending.Any(p => p is null || !Enum.IsDefined(p.Material) || !s.Received.Contains(p.Id))
            || s.Pending.Select(p => p.Id).Distinct().Count() != s.Pending.Count)
            throw new InvalidDataException("非対応または破損した錬金術セーブです。データは初期化しません。");
        return s;
    }
}

public sealed class HarvestCombat(IEnumerable<uint> initialEnemies, int cap)
{
    private readonly HashSet<uint> eligible = initialEnemies.ToHashSet();
    private readonly HashSet<uint> harvested = [];
    public int Turn { get; private set; } = 1;
    public int FurnaceUsed { get; private set; }
    public bool FurnaceActive { get; set; }
    public Material Phase => (Material)((Turn - 1) % 4);
    public void BeginTurn(int turn) { if (turn > Turn) Turn = turn; }
    public Material? Kill(uint enemy)
    {
        if (harvested.Count >= cap || !eligible.Contains(enemy) || !harvested.Add(enemy)) return null;
        return Phase;
    }
    public bool UseFurnace()
    {
        if (!FurnaceActive || FurnaceUsed >= 2) return false;
        FurnaceUsed++;
        return true;
    }
}
