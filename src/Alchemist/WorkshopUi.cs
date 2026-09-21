using Alchemist.Core;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Alchemist;

// Standard Godot controls require no scene assets or generated script bindings.
public static class WorkshopUi
{
    private static NRun? runNode;
    private static Button? launcher;
    private static Control? overlay;
    private static VBoxContainer? sidebar;
    private static VBoxContainer? content;
    private static MaterialBox? box;
    private static double elapsed;
    private static bool busy;
    private static bool workshop;
    private static string status = "";
    private static bool craftableOnly;
    private static Recipe? selectedRecipe;
    private static readonly List<CardModel> previewCards = [];
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
        selectedRecipe = null;
        status = "";
        overlay = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade = new ColorRect { Color = new(0.015f,0.025f,0.03f,0.96f), MouseFilter = Control.MouseFilterEnum.Stop };
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        overlay.AddChild(shade);
        var frame = new PanelContainer();
        frame.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        frame.OffsetLeft = 70; frame.OffsetTop = 55; frame.OffsetRight = -70; frame.OffsetBottom = -55;
        frame.AddThemeStyleboxOverride("panel", Box(new Color("182027"), new Color("8b7045"), 2, 18));
        overlay.AddChild(frame);
        var margin = new MarginContainer();
        foreach (var side in new[] { "margin_left", "margin_top", "margin_right", "margin_bottom" }) margin.AddThemeConstantOverride(side, 24);
        frame.AddChild(margin);
        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 24);
        margin.AddChild(columns);
        var leftPanel = new PanelContainer { CustomMinimumSize = new(315,0) };
        leftPanel.AddThemeStyleboxOverride("panel", Box(new Color("10171d"), new Color("3d4a52"), 1, 12));
        columns.AddChild(leftPanel);
        var leftMargin = new MarginContainer();
        foreach (var side in new[] { "margin_left", "margin_top", "margin_right", "margin_bottom" }) leftMargin.AddThemeConstantOverride(side, 18);
        leftPanel.AddChild(leftMargin);
        sidebar = new VBoxContainer();
        sidebar.AddThemeConstantOverride("separation", 12);
        leftMargin.AddChild(sidebar);
        var scroll = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        columns.AddChild(scroll);
        content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(720,0) };
        content.AddThemeConstantOverride("separation", 14);
        scroll.AddChild(content);
        runNode!.AddChild(overlay);
        Refresh();
    }
    private static StyleBoxFlat Box(Color color, Color border, int width, int radius)
    {
        var style = new StyleBoxFlat { BgColor = color, BorderColor = border };
        style.SetBorderWidthAll(width);
        style.SetCornerRadiusAll(radius);
        return style;
    }
    private static Label Text(string text, int size = 23, Container? parent = null, Color? color = null)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeFontSizeOverride("font_size",size);
        if (color is Color tint) label.AddThemeColorOverride("font_color",tint);
        (parent ?? content)!.AddChild(label);
        return label;
    }
    private static Button Button(string text, Action action, bool disabled = false, Container? parent = null)
    {
        var b = new Button { Text = text, Disabled = disabled || busy, CustomMinimumSize = new(0,50) };
        b.AddThemeFontSizeOverride("font_size",22);
        b.Pressed += () => { if (!busy) action(); };
        (parent ?? content)!.AddChild(b);
        return b;
    }
    private static void Divider(Container parent)
    {
        var line = new HSeparator();
        line.AddThemeConstantOverride("separation", 8);
        parent.AddChild(line);
    }
    private static string MaterialSummary() => string.Join("\n", Enum.GetValues<Core.Material>()
        .Select(m => $"{Recipes.Name(m),-5}  {box!.Inventory.Counts[(int)m]} 個"));
    private static string Missing(Recipe recipe) => string.Join("、", new[] { recipe.First,recipe.Second }
        .GroupBy(m=>m).Select(g=>(Material:g.Key,Count:g.Count()-box!.Inventory.Counts[(int)g.Key]))
        .Where(x=>x.Count>0).Select(x=>$"{Recipes.Name(x.Material)} {x.Count}個"));
    private static CardModel CreatePreviewCard(Recipe recipe)
    {
        var card = (recipe.FormulaId is null ? Canonical(recipe) : ModelDb.Card<ForgedCard>()).ToMutable();
        card.Owner = box!.Owner;
        if (card is ForgedCard forged) forged.AlchemistFormula = recipe.FormulaId!;
        card.AfterCreated();
        previewCards.Add(card);
        return card;
    }
    private static void ClearPreviewCards()
    {
        foreach (var card in previewCards) card.Owner = null!;
        previewCards.Clear();
    }
    private static Control CardDisplay(Recipe recipe, float scale, bool clickable)
    {
        var wrapper = new VBoxContainer { CustomMinimumSize = clickable ? new(235,350) : new(390,510) };
        wrapper.Alignment = BoxContainer.AlignmentMode.Center;
        var stage = new Control { CustomMinimumSize = clickable ? new(225,285) : new(380,420) };
        wrapper.AddChild(stage);
        var node = NCard.Create(CreatePreviewCard(recipe));
        var holder = node is null ? null : NGridCardHolder.Create(node);
        if (holder is not null)
        {
            stage.AddChild(holder);
            holder.Position = stage.CustomMinimumSize / 2;
            holder.Scale = Vector2.One * scale;
            holder.SetClickable(clickable);
            if (clickable)
                holder.Connect(NCardHolder.SignalName.Pressed, Callable.From<NCardHolder>(_=> { selectedRecipe = recipe; Refresh(); }));
            node!.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
        }
        bool canCraft = box!.Inventory.CanCraft(recipe);
        Text($"{Recipes.Name(recipe.First)} ＋ {Recipes.Name(recipe.Second)}",18,wrapper,
            canCraft ? new Color("f2d18b") : new Color("90989c"));
        if (!canCraft) Text("不足："+Missing(recipe),16,wrapper,new Color("d88b82"));
        else if (clickable) Text("選んで詳細を確認",16,wrapper,new Color("aebbc0"));
        return wrapper;
    }
    private static void RecipeGallery(IEnumerable<Recipe> recipes)
    {
        var grid = new GridContainer { Columns = 3, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation",10);
        grid.AddThemeConstantOverride("v_separation",16);
        content!.AddChild(grid);
        foreach (var recipe in recipes) grid.AddChild(CardDisplay(recipe,0.62f,true));
    }
    private static void Confirmation(Recipe recipe)
    {
        bool canCraft = box!.Inventory.CanCraft(recipe);
        Text("このカードを錬成しますか？",32,content,new Color("f2d18b"));
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation",28);
        content!.AddChild(row);
        row.AddChild(CardDisplay(recipe,0.9f,false));
        var details = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        details.AddThemeConstantOverride("separation",12);
        row.AddChild(details);
        Text($"{recipe.Name}　【{recipe.Role}】",28,details,new Color("f2d18b"));
        Text(recipe.Plan,20,details,new Color("aebbc0"));
        Text(recipe.Preview,21,details);
        Text($"必要素材：{Recipes.Name(recipe.First)} ＋ {Recipes.Name(recipe.Second)}",22,details);
        if (!canCraft) Text("不足："+Missing(recipe),20,details,new Color("d88b82"));
        Button(canCraft ? "このカードを作る" : "素材が足りません",()=>_ = Craft(recipe),!canCraft,details);
        Button("一覧へ戻る",()=> { selectedRecipe=null; Refresh(); },parent:details);
    }
    private static void Refresh()
    {
        if (box is null || content is null || sidebar is null) return;
        ClearPreviewCards();
        foreach (var child in sidebar.GetChildren()) { sidebar.RemoveChild(child); child.QueueFree(); }
        foreach (var child in content.GetChildren()) { content.RemoveChild(child); child.QueueFree(); }
        Text(workshop ? "錬金工房" : "素材ボックス", 34, sidebar, new Color("f2d18b"));
        Text($"容量  {box.Inventory.Total} / {AlchemyState.Capacity}", 24, sidebar);
        Divider(sidebar);
        Text(MaterialSummary(), 22, sidebar);
        Divider(sidebar);
        Text("鉄：物理・防御\n薬草：毒・弱体\n火薬：高火力・全体\nエーテル：ドロー・循環",17,sidebar,new Color("aebbc0"));
        if (status.Length > 0)
        {
            Divider(sidebar);
            Text(status,19,sidebar,new Color("8fd6a5"));
        }
        if (box.Inventory.Pending.Count > 0 && !CombatManager.Instance.IsInProgress)
        {
            Text("未受領素材",32,content,new Color("f2d18b"));
            Text($"{Recipes.Name(box.Inventory.Pending[0].Material)}　（残り {box.Inventory.Pending.Count}個）\n次の部屋へ進む前に受け取りを決めてください。",23);
            if (box.Inventory.Total < AlchemyState.Capacity) Button("受け取る",()=>Resolve(true));
            else foreach (var material in Enum.GetValues<Core.Material>().Where(m=>box.Inventory.Counts[(int)m]>0))
                Button($"{Recipes.Name(material)}を1個手放して交換",()=>Resolve(true,material));
            Button("この素材の受け取りを辞退",()=>Resolve(false));
            return;
        }
        if (workshop)
        {
            if (selectedRecipe is not null) Confirmation(selectedRecipe);
            else
            {
                Text("完成カードを選ぶ",32,content,new Color("f2d18b"));
                Text("カードを選ぶと大きく表示し、効果を確認してから錬成できます。",19);
                Button(craftableOnly ? "作成可能のみ表示　｜　全カードへ" : "全カード表示　｜　作成可能のみへ",()=>{craftableOnly=!craftableOnly;Refresh();});
                var recipes = Recipes.All.Where(r=>!craftableOnly || box.Inventory.CanCraft(r)).OrderByDescending(box.Inventory.CanCraft).ThenBy(r=>r.Role=="デッキの軸"?0:r.Role=="切り札"?1:2).ToArray();
                RecipeGallery(recipes);
                if (recipes.Length==0) Text("現在の素材で作れるカードはありません。左の素材数を確認してください。",22);
            }
            Button("工房を退出する",()=>
            {
                box.Inventory.WorkshopClosedFloor = box.Owner.RunState.TotalFloor;
                Close();
            },parent:sidebar);
            Text("退出後、この戦闘では再入場できません。",16,sidebar,new Color("aebbc0"));
        }
        else
        {
            Text("素材の使い道",32,content,new Color("f2d18b"));
            Text("敵を倒した時の素材相に応じて素材を獲得します。現在・次・次々の相は、戦闘中に左上のボタンで確認できます。",22);
            Text("試作版では、戦闘勝利後に工房へ入れます。",20);
            Button("工房へ入る",()=> { workshop = true; craftableOnly = false; selectedRecipe = null; Refresh(); },!CanEnterWorkshop(box));
            Button("閉じる",Close,parent:sidebar);
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
        ClearPreviewCards();
        overlay?.QueueFree();
        overlay = null;
        content = null;
        sidebar = null;
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
            selectedRecipe = null;
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
