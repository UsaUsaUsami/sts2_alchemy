namespace Alchemist.Core;

/// A map point as seen by the placement planner. Cost is what converting this point into a workshop
/// costs the act; Blocked means it must never be converted.
public sealed record CutNode(WorkshopCandidate At, int Cost, IReadOnlyList<WorkshopCandidate> Children);

/// MidGuaranteed / LateGuaranteed say whether every route really crosses that band's workshops, or
/// whether the band fell back to a single point because the routes could not be covered.
public sealed record WorkshopLayout(IReadOnlyList<WorkshopCandidate> Coords, bool MidGuaranteed, bool LateGuaranteed);

/// Every route through an act passes exactly one point per row, so guaranteeing a workshop on all
/// routes means converting a vertex cut between the start and the boss. Solved as a min cut so the
/// act keeps its merchants, rest sites and elites, which are Blocked here.
public static class WorkshopCut
{
    public const int Blocked = int.MaxValue;
    private const long Infinite = long.MaxValue / 4;

    public static IReadOnlyList<WorkshopCandidate> Minimum(IReadOnlyList<CutNode> nodes, WorkshopCandidate start, WorkshopCandidate goal)
    {
        var index = new Dictionary<WorkshopCandidate,int>();
        for (int i = 0; i < nodes.Count; i++) index[nodes[i].At] = i;
        if (!index.TryGetValue(start, out int startIndex) || !index.TryGetValue(goal, out int goalIndex)) return [];

        int size = nodes.Count * 2;
        var capacity = new long[size, size];
        for (int i = 0; i < nodes.Count; i++)
        {
            capacity[i * 2, i * 2 + 1] = nodes[i].Cost == Blocked ? Infinite : nodes[i].Cost;
            foreach (var child in nodes[i].Children)
                if (index.TryGetValue(child, out int j)) capacity[i * 2 + 1, j * 2] = Infinite;
        }
        int source = startIndex * 2 + 1, sink = goalIndex * 2;

        var parent = new int[size];
        long flow = 0;
        while (true)
        {
            Array.Fill(parent, -1);
            parent[source] = source;
            Queue<int> queue = new([source]);
            while (queue.Count > 0 && parent[sink] < 0)
            {
                int u = queue.Dequeue();
                for (int v = 0; v < size; v++)
                    if (parent[v] < 0 && capacity[u, v] > 0) { parent[v] = u; queue.Enqueue(v); }
            }
            if (parent[sink] < 0) break;
            long added = Infinite;
            for (int v = sink; v != source; v = parent[v]) added = Math.Min(added, capacity[parent[v], v]);
            for (int v = sink; v != source; v = parent[v]) { capacity[parent[v], v] -= added; capacity[v, parent[v]] += added; }
            flow += added;
            if (flow >= Infinite) return [];
        }

        var reachable = new bool[size];
        reachable[source] = true;
        Queue<int> walk = new([source]);
        while (walk.Count > 0)
        {
            int u = walk.Dequeue();
            for (int v = 0; v < size; v++)
                if (!reachable[v] && capacity[u, v] > 0) { reachable[v] = true; walk.Enqueue(v); }
        }
        return [.. nodes.Where((n,i) => n.Cost != Blocked && reachable[i * 2] && !reachable[i * 2 + 1]).Select(n => n.At)
            .OrderBy(p => p.Row).ThenBy(p => p.Col)];
    }
}
