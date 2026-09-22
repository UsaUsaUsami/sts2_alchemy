using System.Text.Json;

namespace Alchemist.Core;

public enum Material { Iron, Herb, Powder, Ether }
public sealed record Harvest(string Id, MaterialChoice Material);
// Materials is unordered and allows repeats (e.g. two Iron): recipes key on the material multiset, not on
// slot position. Commons consume 2, uncommons 3, rares 4 (AGENTS.md 4.8 base grammar, extended past pairs).
public sealed record Recipe(string Id, IReadOnlyList<Material> Materials, string Name, string Preview,
    string Role = "基礎強化", string Plan = "", string? FormulaId = null);

public static class Recipes
{
    // Curated M1 recipes cover all ten pairs with multiple build directions; full base x material grammar remains M3.
    public static readonly Recipe[] All = [
        new("iron_guard.v1", [Material.Iron, Material.Iron], "鍛鉄の護り", "1コスト / スキル / 自身\n16ブロック、1枚ドロー。強化後21ブロック。", "守りと循環", "大きく守りながら、切り札を引きに行く。"),
        new("herbal_edge.v1", [Material.Iron, Material.Herb], "薬刃", "1コスト / アタック / 敵1体\n12ダメージ、弱体2。強化後16ダメージ、弱体3。", "攻撃の準備", "弱体を付け、大剣や全体攻撃の火力を伸ばす。"),
        new("blast.v1", [Material.Powder, Material.Powder], "炸裂弾", "1コスト / アタック / 敵全体\n14ダメージ。強化後19。", "集団戦", "廃棄せず繰り返せる全体攻撃。"),
        new("ether_lens.v1", [Material.Ether, Material.Ether], "エーテルレンズ", "1コスト / スキル / 自身\n3枚ドロー、8ブロック。強化後12ブロック。", "守りと循環", "手札を増やし、パワーや切り札を早く引く。"),
        new("herbal_guard.v1", [Material.Herb, Material.Herb], "薬草の被膜", "1コスト / スキル / 自身\n12ブロック、敵全体に脱力2。強化後16ブロック。", "守りと循環", "敵全体の攻撃を弱め、毒が回る時間を作る。"),
        ..ForgeCatalog.All.Select(f=>new Recipe(f.Id,f.Materials,f.Name,f.Preview,f.Role,f.Plan,f.Id))];
    private static bool SameMultiset(IReadOnlyList<Material> a, IReadOnlyList<Material> b)
        => a.Count == b.Count && a.OrderBy(m => m).SequenceEqual(b.OrderBy(m => m));
    public static IEnumerable<Recipe> FindAll(IReadOnlyList<Material> materials) => All.Where(r => SameMultiset(r.Materials, materials));
    public static IEnumerable<Recipe> FindAll(Material a, Material b) => FindAll([a, b]);
    public static Recipe? Find(Material a, Material b) => FindAll(a,b).FirstOrDefault();
    public static string Name(Material m) => m switch {
        Material.Iron => "鉄", Material.Herb => "薬草", Material.Powder => "火薬", Material.Ether => "エーテル", _ => throw new ArgumentOutOfRangeException(nameof(m)) };
}

public sealed class AlchemyState
{
    public const int Capacity = 20;
    // Standard combat card rewards are disabled for this character (see MaterialBox.TryModifyRewards);
    // this is the compensating yield. Each harvest event grants this many units of the same material.
    public const int YieldPerEvent = 2;
    // 1: harvest-only. 2: normal reward slots. 3: rare inventory and mixed reward slots.
    public const int CurrentSchema = 3;
    public int Schema { get; set; } = CurrentSchema;
    public int[] Counts { get; set; } = new int[4];
    public int[] RareCounts { get; set; } = new int[3];
    public List<Harvest> Pending { get; set; } = [];
    /// Reward slots that have been offered but not yet taken or declined.
    public List<MaterialOffer> Offers { get; set; } = [];
    public HashSet<string> Received { get; set; } = [];
    public HashSet<string> Committed { get; set; } = [];
    public int WorkshopClosedFloor { get; set; } = -1;
    public Material NextCombatMaterial { get; set; } = Material.Iron;
    public Dictionary<int,List<string>> WorkshopNodes { get; set; } = [];
    public int Total => Counts.Sum() + RareCounts.Sum();
    public int Revision { get; set; }

    public bool Grant(string id, Material material)
        => Grant(id, MaterialChoice.Normal(material));
    public bool GrantRare(string id, RareMaterial material)
        => Grant(id, MaterialChoice.Rare(material));
    public bool Grant(string id, MaterialChoice material)
    {
        if (!material.IsValid) throw new ArgumentOutOfRangeException(nameof(material));
        if (!Received.Add(id)) return false;
        if (Total < Capacity && Pending.Count == 0) Add(material, 1);
        else Pending.Add(new(id, material));
        Revision++;
        return true;
    }
    /// Grants YieldPerEvent units of one material from a single harvest event. The first unit uses `id`
    /// itself, preserving the exact Received/Pending bookkeeping other code already relies on (the
    /// reward-slot re-entry guard checks Received.Contains(id)); additional units use suffixed ids so a
    /// full box queues each extra unit through the existing single-unit pending/exchange flow instead of
    /// requiring a multi-unit exchange UI.
    public bool GrantHarvest(string id, MaterialChoice material)
    {
        bool granted = Grant(id, material);
        for (int i = 1; i < YieldPerEvent; i++) Grant($"{id}:bonus{i}", material);
        return granted;
    }
    public bool GrantHarvest(string id, Material material) => GrantHarvest(id, MaterialChoice.Normal(material));

    public bool HasOffer(string id) => Offers.Any(o => o.Id == id);

    /// Records a reward slot. Returns false when the slot was already offered or already resolved, so a
    /// hook that fires twice cannot hand out the same slot twice.
    public bool Offer(string id, IReadOnlyList<Material> candidates)
        => Offer(id, candidates.Select(MaterialChoice.Normal).ToArray());
    public bool Offer(string id, IReadOnlyList<MaterialChoice> candidates)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentException("報酬枠のIDが空です。", nameof(id));
        if (candidates.Count != MaterialOffers.CandidateCount)
            throw new ArgumentException($"候補は{MaterialOffers.CandidateCount}個必要です。", nameof(candidates));
        if (candidates.Any(m => !m.IsValid) || candidates.Distinct().Count() != candidates.Count)
            throw new ArgumentException("候補が不正か重複しています。", nameof(candidates));
        if (Received.Contains(id) || HasOffer(id)) return false;
        Offers.Add(new(id, [.. candidates]));
        Revision++;
        return true;
    }

    /// Takes one candidate. Capacity is handled by the shared receipt path, so a full box defers the
    /// material to Pending instead of dropping it.
    public void TakeOffer(string id, Material choice) => TakeOffer(id, MaterialChoice.Normal(choice));
    public void TakeOffer(string id, MaterialChoice choice)
    {
        var offer = Offers.FirstOrDefault(o => o.Id == id)
            ?? throw new InvalidOperationException("その報酬枠はすでに解決済みです。");
        if (!offer.Candidates.Contains(choice)) throw new InvalidOperationException("候補にない素材です。");
        Offers.Remove(offer);
        GrantHarvest(id, choice);
    }

    public void DeclineOffer(string id)
    {
        var offer = Offers.FirstOrDefault(o => o.Id == id)
            ?? throw new InvalidOperationException("その報酬枠はすでに解決済みです。");
        Offers.Remove(offer);
        // Recorded as received so the slot can never be offered again on a re-entry or reload.
        Received.Add(id);
        Revision++;
    }

    public void Resolve(bool accept, Material? exchange = null)
        => Resolve(accept, exchange is null ? null : MaterialChoice.Normal(exchange.Value));
    public void Resolve(bool accept, MaterialChoice? exchange)
    {
        if (Pending.Count == 0) throw new InvalidOperationException("未受領素材がありません。");
        if (accept)
        {
            if (Total >= Capacity)
            {
                if (exchange is null || !exchange.Value.IsValid || Count(exchange.Value) <= 0)
                    throw new InvalidOperationException("交換する素材を選んでください。");
                Add(exchange.Value, -1);
            }
            Add(Pending[0].Material, 1);
        }
        Pending.RemoveAt(0);
        Revision++;
    }
    /// Unresolved receipts and reward slots both block spending, so materials cannot be burned while the
    /// player still owes a decision on what they are about to receive.
    public bool Settled => Pending.Count == 0 && Offers.Count == 0;
    private bool HasMaterials(IReadOnlyList<Material> materials)
        => materials.GroupBy(m => m).All(g => Counts[(int)g.Key] >= g.Count());
    public bool CanCraft(Recipe r) => Settled && HasMaterials(r.Materials);
    public void Commit(Recipe r, string operation, Action addCard, Action rollbackCard)
    {
        if (Committed.Contains(operation)) return;
        if (!Recipes.All.Contains(r) || !CanCraft(r)) throw new InvalidOperationException("素材が不足しているか、未解決の受け取り・報酬枠があります。");
        int[] before = (int[])Counts.Clone();
        try
        {
            addCard();
            foreach (var m in r.Materials) Counts[(int)m]--;
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
        if (!Recipes.All.Contains(r) || !CanCraft(r)) throw new InvalidOperationException("素材が不足しているか、未解決の受け取り・報酬枠があります。");
        int[] before = (int[])Counts.Clone();
        try
        {
            await addCard();
            foreach (var m in r.Materials) Counts[(int)m]--;
            Committed.Add(operation);
            Revision++;
        }
        catch { Counts = before; rollbackCard(); throw; }
    }
    public bool CanSpend(Material first, Material second) => Settled
        && Counts[(int)first] >= (first == second ? 2 : 1) && Counts[(int)second] >= 1;
    public void CommitUpgrade(Material first, Material second, string operation, Action upgrade)
    {
        if (Committed.Contains(operation)) return;
        if (!CanSpend(first,second)) throw new InvalidOperationException("強化に必要な素材が不足しているか、未解決の受け取り・報酬枠があります。");
        upgrade();
        Counts[(int)first]--;
        Counts[(int)second]--;
        Committed.Add(operation);
        Revision++;
    }
    public bool CanApplyRare(RareMaterial material) => Settled && RareCounts[(int)material] > 0;
    public void CommitRare(RareMaterial material, string operation, Action apply, Action rollback)
    {
        if (Committed.Contains(operation)) return;
        if (!Enum.IsDefined(material) || !CanApplyRare(material))
            throw new InvalidOperationException("希少素材が不足しているか、未解決の受け取り・報酬枠があります。");
        try
        {
            apply();
            RareCounts[(int)material]--;
            Committed.Add(operation);
            Revision++;
        }
        catch { rollback(); throw; }
    }
    public int Count(MaterialChoice material) => material.Class == MaterialClass.Normal
        ? Counts[(int)material.NormalMaterial] : RareCounts[(int)material.RareMaterial];
    private void Add(MaterialChoice material, int amount)
    {
        if (material.Class == MaterialClass.Normal) Counts[(int)material.NormalMaterial] += amount;
        else RareCounts[(int)material.RareMaterial] += amount;
    }
    public string Save() => JsonSerializer.Serialize(this);
    public static AlchemyState Load(string json)
    {
        using var document = JsonDocument.Parse(json);
        int schema = document.RootElement.TryGetProperty(nameof(Schema), out var schemaNode) ? schemaNode.GetInt32() : 1;
        AlchemyState s;
        if (schema is 1 or 2)
        {
            var legacy = JsonSerializer.Deserialize<LegacyState>(json) ?? throw new InvalidDataException("錬金術のセーブが空です。");
            s = new AlchemyState {
                Counts = legacy.Counts, Pending = legacy.Pending.Select(x=>new Harvest(x.Id,MaterialChoice.Normal(x.Material))).ToList(),
                Offers = legacy.Offers.Select(x=>new MaterialOffer(x.Id,[..x.Candidates.Select(MaterialChoice.Normal)])).ToList(),
                Received = legacy.Received, Committed = legacy.Committed, WorkshopClosedFloor = legacy.WorkshopClosedFloor,
                NextCombatMaterial = legacy.NextCombatMaterial, WorkshopNodes = legacy.WorkshopNodes, Revision = legacy.Revision
            };
        }
        else s = JsonSerializer.Deserialize<AlchemyState>(json) ?? throw new InvalidDataException("錬金術のセーブが空です。");
        if (schema is not (1 or 2 or 3) || s.Counts is not { Length: 4 } || s.RareCounts is not { Length: 3 }
            || s.Counts.Any(n => n < 0 || n > Capacity) || s.RareCounts.Any(n => n < 0 || n > Capacity)
            || s.Total > Capacity || s.Pending is null || s.Received is null || s.Committed is null || s.WorkshopNodes is null
            || s.Offers is null
            || !Enum.IsDefined(s.NextCombatMaterial)
            || s.Pending.Any(p => p is null || !p.Material.IsValid || !s.Received.Contains(p.Id))
            || s.Pending.Select(p => p.Id).Distinct().Count() != s.Pending.Count
            || s.Offers.Any(o => o is null || string.IsNullOrEmpty(o.Id) || s.Received.Contains(o.Id)
                || o.Candidates is not { Length: MaterialOffers.CandidateCount }
                || o.Candidates.Any(m => !m.IsValid)
                || o.Candidates.Distinct().Count() != o.Candidates.Length)
            || s.Offers.Select(o => o.Id).Distinct().Count() != s.Offers.Count)
            throw new InvalidDataException("非対応または破損した錬金術セーブです。データは初期化しません。");
        // Schema 1 predates reward slots; its empty Offers list is already the correct migration.
        s.Schema = CurrentSchema;
        return s;
    }

    private sealed record LegacyHarvest(string Id, Material Material);
    private sealed record LegacyOffer(string Id, Material[] Candidates);
    private sealed class LegacyState
    {
        public int[] Counts { get; set; } = new int[4];
        public List<LegacyHarvest> Pending { get; set; } = [];
        public List<LegacyOffer> Offers { get; set; } = [];
        public HashSet<string> Received { get; set; } = [];
        public HashSet<string> Committed { get; set; } = [];
        public int WorkshopClosedFloor { get; set; } = -1;
        public Material NextCombatMaterial { get; set; } = Material.Iron;
        public Dictionary<int,List<string>> WorkshopNodes { get; set; } = [];
        public int Revision { get; set; }
    }
}

public sealed class HarvestCombat(Material startingMaterial = Material.Iron)
{
    public int Turn { get; private set; } = 1;
    public int FurnaceUsed { get; private set; }
    public bool FurnaceActive { get; set; }
    public Material StartingMaterial { get; } = startingMaterial;
    public Material Phase => (Material)(((int)StartingMaterial + Turn - 1) % 4);
    public Material NextPhase => (Material)(((int)Phase + 1) % 4);
    public void BeginTurn(int turn) { if (turn > Turn) Turn = turn; }
    public bool UseFurnace()
    {
        if (!FurnaceActive || FurnaceUsed >= 2) return false;
        FurnaceUsed++;
        return true;
    }
}

public readonly record struct WorkshopCandidate(int Col, int Row);
public static class WorkshopPlanner
{
    // AGENTS.md 4.5: one to two workshops per ordinary act.
    public const int PerAct = 2;
    // Leaves a few fights before the first workshop so there is something to spend.
    public const int EarliestRow = 3;
    // Where the mid band ends, as a fraction of the rows available for workshops.
    private const double MidBandEnd = 0.45;

    public static int MidBandEndRow(int maxRow) => EarliestRow + (int)((maxRow - EarliestRow) * MidBandEnd);

    /// Places a workshop layer every route must cross, once in the mid act and once late enough that
    /// materials won just before the boss are still spendable. A band whose routes cannot all be covered
    /// without converting a merchant, rest site or elite falls back to a single workshop and reports
    /// itself as not guaranteed, rather than taking one of those rooms to compensate.
    public static WorkshopLayout SelectGuaranteed(IReadOnlyList<CutNode> graph,
        WorkshopCandidate start, WorkshopCandidate goal, int maxRow)
    {
        int midEnd = MidBandEndRow(maxRow);
        List<WorkshopCandidate> result = [];
        var guaranteed = new bool[2];
        var bands = new[] { (Low: EarliestRow, High: midEnd), (Low: midEnd + 1, High: maxRow) };
        for (int i = 0; i < bands.Length; i++)
        {
            bool InBand(CutNode n) => n.At.Row >= bands[i].Low && n.At.Row <= bands[i].High;
            var cut = WorkshopCut.Minimum([.. graph.Select(n => InBand(n) ? n : n with { Cost = WorkshopCut.Blocked })], start, goal);
            guaranteed[i] = cut.Count > 0;
            result.AddRange(cut.Count > 0 ? cut
                : graph.Where(n => InBand(n) && n.Cost != WorkshopCut.Blocked)
                    .OrderBy(n => n.At.Row).ThenBy(n => n.At.Col).Select(n => n.At).TakeLast(1));
        }
        return new([.. result.Distinct().OrderBy(p => p.Row).ThenBy(p => p.Col)], guaranteed[0], guaranteed[1]);
    }
}
