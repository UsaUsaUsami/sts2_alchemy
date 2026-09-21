using Alchemist.Core;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Alchemist;

// Standard Godot controls require no scene assets or generated script bindings.
public static class WorkshopUi
{
    private static NRun? runNode;
    private static Button? launcher;
    private static Control? overlay;
    private static VBoxContainer? content;
    private static MaterialBox? box;
    private static double elapsed;
    private static bool busy;
    private static bool workshop;
    private static string status = "";
    private static bool craftableOnly;
    public static bool IsOpen => GodotObject.IsInstanceValid(overlay);
    public static bool IsBusy => busy;

    public static MaterialBox? CurrentBox()
    {
        var state = RunManager.Instance.DebugOnlyGetState();
        return state?.Players.Count == 1 && state.Players[0].Character is AlchemistCharacter
            ? state.Players[0].GetRelic<MaterialBox>() : null;
    }
    public static bool CanEnterWorkshop(MaterialBox b) => !CombatManager.Instance.IsInProgress
        && b.Owner.RunState.CurrentRoom is CombatRoom { IsPreFinished: true }
        && b.Inventory.WorkshopClosedFloor != b.Owner.RunState.TotalFloor;

    public static void Tick(NRun node, double delta)
    {
        elapsed += delta;
        if (elapsed < 0.25) return;
        elapsed = 0;
        box = CurrentBox();
        if (box is null) return;
        if (runNode != node || !GodotObject.IsInstanceValid(launcher))
        {
            runNode = node;
            overlay = null;
            busy = false;
            workshop = false;
            launcher = new Button { Position = new(16, 125), Size = new(330, 76) };
            launcher.AddThemeFontSizeOverride("font_size", 20);
            launcher.Pressed += () => { if (!IsOpen) Open(); };
            node.AddChild(launcher);
        }
        var combat = box.Combat;
        launcher!.Text = combat is null ? $"素材・工房  {box.Inventory.Total}/10"
            : $"素材 {box.Inventory.Total}/10  炉 {combat.FurnaceUsed}/2\n{Recipes.Name(combat.Phase)} → {Recipes.Name((Core.Material)(((int)combat.Phase+1)%4))} → {Recipes.Name((Core.Material)(((int)combat.Phase+2)%4))}";
        if (box.Inventory.Pending.Count > 0 && !CombatManager.Instance.IsInProgress && !IsOpen) Open();
    }

    public static void Open()
    {
        if (box is null || !GodotObject.IsInstanceValid(runNode) || IsOpen) return;
        workshop = false;
        status = "";
        overlay = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade = new ColorRect { Color = new(0.015f,0.025f,0.03f,0.96f), MouseFilter = Control.MouseFilterEnum.Stop };
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        overlay.AddChild(shade);
        var scroll = new ScrollContainer { Position = new(130,85), Size = new(1130,810) };
        overlay.AddChild(scroll);
        content = new VBoxContainer { CustomMinimumSize = new(1050,0) };
        content.AddThemeConstantOverride("separation", 12);
        scroll.AddChild(content);
        runNode!.AddChild(overlay);
        Refresh();
    }
    private static void Text(string text, int size = 23)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeFontSizeOverride("font_size",size);
        content!.AddChild(label);
    }
    private static void Button(string text, Action action, bool disabled = false)
    {
        var b = new Button { Text = text, Disabled = disabled || busy, CustomMinimumSize = new(0,50) };
        b.AddThemeFontSizeOverride("font_size",22);
        b.Pressed += () => { if (!busy) action(); };
        content!.AddChild(b);
    }
    private static void Refresh()
    {
        if (box is null || content is null) return;
        foreach (var child in content.GetChildren()) { content.RemoveChild(child); child.QueueFree(); }
        Text(workshop ? "錬金工房 / 試作" : "素材ボックス", 36);
        Text($"容量 {box.Inventory.Total}/10   —   " + string.Join("    ",Enum.GetValues<Core.Material>().Select(m=>$"{Recipes.Name(m)} {box.Inventory.Counts[(int)m]}")));
        Text("鉄：物理・防御  /  薬草：弱体・脱力  /  火薬：全体攻撃  /  エーテル：ドロー",18);
        Text("入手元：戦闘開始時の敵を撃破。死亡が確定した時の素材相を参照します。",18);
        if (status.Length > 0) Text(status);
        if (box.Inventory.Pending.Count > 0 && !CombatManager.Instance.IsInProgress)
        {
            Text($"未受領 {box.Inventory.Pending.Count}個：{Recipes.Name(box.Inventory.Pending[0].Material)}\n次の部屋へ進む前に受け取りを解決してください。");
            if (box.Inventory.Total < AlchemyState.Capacity) Button("受け取る",()=>Resolve(true));
            else foreach (var material in Enum.GetValues<Core.Material>().Where(m=>box.Inventory.Counts[(int)m]>0))
                Button($"{Recipes.Name(material)}を1個手放して交換",()=>Resolve(true,material));
            Button("この素材の受け取りを辞退",()=>Resolve(false));
            return;
        }
        if (workshop)
        {
            Text("通常素材2個を消費してマスターデッキに1枚追加。材料がある限り繰り返せます。",19);
            Text("主役を選ぶ：弱体＋大剣 ／ 猛毒培養槽＋防御 ／ 循環錬成炉＋廃棄。全10組み合わせにレシピがあります。",19);
            Button(craftableOnly ? "今作れるものを表示中 → 全レシピを見る" : "全レシピを表示中 → 今作れるものだけ見る",()=>{craftableOnly=!craftableOnly;Refresh();});
            var recipes = Recipes.All.Where(r=>!craftableOnly || box.Inventory.CanCraft(r)).OrderByDescending(box.Inventory.CanCraft).ThenBy(r=>r.Role=="デッキの軸"?0:r.Role=="切り札"?1:2);
            foreach (var recipe in recipes)
            {
                Text($"【{recipe.Role}】{recipe.Name} — {Recipes.Name(recipe.First)} ＋ {Recipes.Name(recipe.Second)}\n{recipe.Plan}\n{recipe.Preview}",21);
                if(!box.Inventory.CanCraft(recipe)) Text("不足："+string.Join("、",new[]{recipe.First,recipe.Second}.GroupBy(m=>m).Select(g=>(Material:g.Key,Count:g.Count()-box.Inventory.Counts[(int)g.Key])).Where(x=>x.Count>0).Select(x=>$"{Recipes.Name(x.Material)} {x.Count}個")),18);
                Button("この内容で錬成する",()=>_ = Craft(recipe),!box.Inventory.CanCraft(recipe));
            }
            Button("工房を退出する（この戦闘後は再入場不可）",()=>
            {
                box.Inventory.WorkshopClosedFloor = box.Owner.RunState.TotalFloor;
                Close();
            });
        }
        else
        {
            Text("試作版の工房は戦闘勝利後に入れます。専用マップノードは今後追加予定です。",20);
            Button("開発用工房に入る",()=> { workshop = true; Refresh(); },!CanEnterWorkshop(box));
            Button("閉じる",Close);
        }
    }
    private static void Resolve(bool accept, Core.Material? exchange = null)
    {
        box!.Inventory.Resolve(accept,exchange);
        Refresh();
    }
    private static void Close()
    {
        if (busy) return;
        overlay?.QueueFree();
        overlay = null;
        content = null;
    }
    private static CardModel Canonical(Recipe recipe) => recipe.Id switch {
        "iron_guard.v1" => ModelDb.Card<IronGuard>(), "herbal_edge.v1" => ModelDb.Card<HerbalEdge>(),
        "blast.v1" => ModelDb.Card<AlchemicalBlast>(), "ether_lens.v1" => ModelDb.Card<EtherLens>(),
        "herbal_guard.v1" => ModelDb.Card<HerbalGuard>(), _ => throw new InvalidOperationException("未対応レシピ") };

    private static async Task Craft(Recipe recipe)
    {
        if (box is null || busy || !workshop || !CanEnterWorkshop(box)) return;
        busy = true;
        Refresh();
        CardModel? created = null;
        CardModel? added = null;
        var b = box;
        try
        {
            await b.Inventory.CommitAsync(recipe,Guid.NewGuid().ToString("N"),async () =>
            {
                created = b.Owner.RunState.CreateCard(recipe.FormulaId is null ? Canonical(recipe) : ModelDb.Card<ForgedCard>(),b.Owner);
                if(created is ForgedCard forged) forged.AlchemistFormula=recipe.FormulaId!;
                var result = await CardPileCmd.Add(created,PileType.Deck,skipVisuals:true);
                added = result.cardAdded;
                if (!result.success || !b.Owner.Deck.Cards.Contains(added)) throw new InvalidOperationException("カードの追加が拒否されました。");
            },() =>
            {
                foreach (var card in new[] { created, added }.OfType<CardModel>().Distinct())
                {
                    if (b.Owner.Deck.Cards.Contains(card)) b.Owner.Deck.RemoveInternal(card);
                    b.Owner.RunState.RemoveCard(card);
                }
            });
            status = $"{recipe.Name}をデッキに追加しました。";
        }
        catch (Exception ex) { status = $"錬成できませんでした：{ex.Message}"; GD.PushError(ex.ToString()); }
        finally { busy = false; if (IsOpen) Refresh(); }
    }
}

[HarmonyPatch(typeof(NRun), nameof(NRun._Process))]
public static class AlchemistHudPatch
{
    public static void Postfix(NRun __instance, double delta) => WorkshopUi.Tick(__instance, delta);
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterMapCoord))]
public static class PendingHarvestTravelPatch
{
    public static bool Prefix(ref Task __result)
    {
        if (WorkshopUi.CurrentBox() is not { } b) return true;
        if (b.Inventory.Pending.Count == 0 && !WorkshopUi.IsBusy && !WorkshopUi.IsOpen) return true;
        WorkshopUi.Open();
        __result = Task.CompletedTask;
        return false;
    }
}
