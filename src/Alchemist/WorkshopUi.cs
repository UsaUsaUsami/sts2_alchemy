using Alchemist.Core;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
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
    private static bool mapWorkshop;
    private static bool upgradeMode;
    private static bool rareMode;
    private static string status = "";
    private static bool craftableOnly;
    private static bool materialCraftMode;
    private static bool browsing;
    private static bool merchantMode;
    private static string? offerId;
    private static TaskCompletionSource<bool>? offerResult;
    private static Recipe? selectedRecipe;
    private static CardModel? selectedUpgrade;
    private static ForgedCard? selectedRareCard;
    private static readonly List<Core.Material> craftInputs = [];
    private static readonly List<CardModel> previewCards = [];
    public static bool IsOpen => GodotObject.IsInstanceValid(overlay);
    public static bool IsBusy => busy;

    public static MaterialBox? CurrentBox()
    {
        var state = RunManager.Instance.DebugOnlyGetState();
        return state?.Players.Count == 1 && state.Players[0].Character is AlchemistCharacter
            ? state.Players[0].GetRelic<MaterialBox>() : null;
    }
    public static bool CanEnterWorkshop(MaterialBox b) => mapWorkshop && !CombatManager.Instance.IsInProgress
        && WorkshopMap.IsCurrentWorkshop();

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
            launcher.Pressed += () => { if (!IsOpen) { mapWorkshop=false; Open(); } };
            node.AddChild(launcher);
        }
        var combat = box.Combat;
        launcher!.Text = combat is null ? $"素材・工房  {box.Inventory.Total}/{AlchemyState.Capacity}"
            : $"素材 {box.Inventory.Total}/{AlchemyState.Capacity}  炉 {combat.FurnaceUsed}/2\n{Recipes.Name(combat.Phase)} → {Recipes.Name((Core.Material)(((int)combat.Phase+1)%4))} → {Recipes.Name((Core.Material)(((int)combat.Phase+2)%4))}";
        // Unresolved reward slots are reached from the rewards screen button, so only overflow receipts
        // open this screen on their own.
        if (box.Inventory.Pending.Count > 0 && !CombatManager.Instance.IsInProgress && !IsOpen) Open();
    }

    public static void Open(Node? parent = null)
    {
        if (box is null || !GodotObject.IsInstanceValid(runNode) || IsOpen) return;
        workshop = false;
        browsing = false;
        merchantMode = false;
        selectedRecipe = null;
        selectedUpgrade = null;
        selectedRareCard = null;
        materialCraftMode = false;
        craftInputs.Clear();
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
        (parent ?? runNode!).AddChild(overlay);
        Refresh();
    }

    public static void OpenMerchantShop(NMerchantInventory parent)
    {
        box = CurrentBox();
        runNode = NRun.Instance;
        if (box is null || !InMerchantRoom || !GodotObject.IsInstanceValid(parent)) return;
        if (IsOpen) Close();
        Open(parent);
        merchantMode = true;
        Refresh();
    }

    /// Called by the rewards screen entry. The picker is parented to the overlay stack that holds the
    /// rewards screen and added after it, so it draws above rather than behind.
    public static Task<bool> ChooseOffer(MaterialBox b, string id)
    {
        box = b;
        runNode = NRun.Instance;
        // Resolved already (double click, or a reward restored after the choice was made).
        if (!b.Inventory.HasOffer(id)) return Task.FromResult(true);
        if (!GodotObject.IsInstanceValid(runNode)) return Task.FromResult(false);
        offerResult?.TrySetResult(false);
        if (IsOpen) Close();
        offerResult = new();
        offerId = id;
        Open(GodotObject.IsInstanceValid(NOverlayStack.Instance) ? NOverlayStack.Instance : null);
        return offerResult.Task;
    }
    public static void OpenMapWorkshop()
    {
        box=CurrentBox();
        runNode=NRun.Instance;
        if(box is null || !GodotObject.IsInstanceValid(runNode)) return;
        mapWorkshop=true;
        Open();
        workshop=true;
        upgradeMode=false;
        rareMode=false;
        craftableOnly=false;
        materialCraftMode=false;
        craftInputs.Clear();
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
        .Select(m => $"{Recipes.Name(m),-5}  {box!.Inventory.Counts[(int)m]} 個")
        .Concat(Enum.GetValues<RareMaterial>().Select(m=>$"{RareMaterials.Get(m).Name,-5}  {box!.Inventory.RareCounts[(int)m]} 個")));
    private static string Missing(Recipe recipe) => string.Join("、", recipe.Materials
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
    private static CardModel CreateMaterialPreviewCard(CardModel canonical)
    {
        var card = canonical.ToMutable();
        card.Owner = box!.Owner;
        card.AfterCreated();
        previewCards.Add(card);
        return card;
    }
    // A row of small material cards next to the numeric summary: same visual identity as the crafted
    // cards, but AlchemyState.Counts/RareCounts stay the only real inventory. These are mounted through
    // the same preview-only pattern as recipe cards, so they never enter a pile or RunState.
    private static void MaterialCardRow()
    {
        var owned = Enum.GetValues<Core.Material>().Where(m => box!.Inventory.Counts[(int)m] > 0).ToArray();
        var ownedRare = Enum.GetValues<RareMaterial>().Where(m => box!.Inventory.RareCounts[(int)m] > 0).ToArray();
        if (owned.Length == 0 && ownedRare.Length == 0) return;
        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 4);
        sidebar!.AddChild(row);
        foreach (var m in owned) MaterialCardSlot(row, MaterialCards.Canonical(m), Recipes.Name(m), box!.Inventory.Counts[(int)m]);
        foreach (var m in ownedRare) MaterialCardSlot(row, MaterialCards.Canonical(m), RareMaterials.Get(m).Name, box!.Inventory.RareCounts[(int)m]);
    }
    private static void MaterialCardSlot(Container parent, CardModel canonical, string name, int count)
    {
        var wrapper = new VBoxContainer { CustomMinimumSize = new(68, 96) };
        wrapper.Alignment = BoxContainer.AlignmentMode.Center;
        var stage = new Control { CustomMinimumSize = new(64, 80) };
        wrapper.AddChild(stage);
        var holder = MountCard(stage, CreateMaterialPreviewCard(canonical), 0.28f, false);
        if (holder is not null) UpdateCardWhenReady(holder, PileType.None, CardPreviewMode.Normal);
        Text($"{name} ×{count}", 14, wrapper, new Color("f2d18b"));
        parent.AddChild(wrapper);
    }
    // NCardHolder tweens its own scale between SmallScale and HoverScale, so the display scale belongs on
    // a parent node. Setting holder.Scale directly leaves every hovered card stuck at SmallScale afterwards.
    private static NGridCardHolder? MountCard(Control stage, CardModel card, float scale, bool clickable)
    {
        var node = NCard.Create(card);
        if (node is null) return null;
        var holder = NGridCardHolder.Create(node);
        if (holder is null) return null;
        var pivot = new Control { Scale = Vector2.One * (scale / NCardHolder.smallScale.X) };
        stage.AddChild(pivot);
        pivot.AddChild(holder);
        holder.Position = stage.CustomMinimumSize / 2 / pivot.Scale;
        holder.SetClickable(clickable);
        return holder;
    }
    private static void UpdateCardWhenReady(NGridCardHolder holder,PileType pile,CardPreviewMode preview)
    {
        var cardNode=holder.CardNode!;
        cardNode.Visibility=ModelVisibility.Visible;
        cardNode.UpdateVisuals(pile,preview);
        // NCard scenes come from a pool. On the first frame some nested labels are not ready yet and keep
        // the scene's diagnostic placeholder until another refresh (upgrade/reload). Refresh once after the
        // node has entered the tree so every recipe gets its real description immediately.
        Callable.From(()=>
        {
            if(GodotObject.IsInstanceValid(cardNode))
            {
                cardNode.Visibility=ModelVisibility.Visible;
                cardNode.UpdateVisuals(pile,preview);
            }
        }).CallDeferred();
    }
    private static Control CardDisplay(Recipe recipe, float scale, bool clickable)
    {
        var wrapper = new VBoxContainer { CustomMinimumSize = clickable ? new(235,350) : new(390,510) };
        wrapper.Alignment = BoxContainer.AlignmentMode.Center;
        var stage = new Control { CustomMinimumSize = clickable ? new(225,285) : new(380,420) };
        wrapper.AddChild(stage);
        var holder = MountCard(stage, CreatePreviewCard(recipe), scale, clickable);
        if (holder is not null)
        {
            if (clickable)
                holder.Connect(NCardHolder.SignalName.Pressed, Callable.From<NCardHolder>(_=> { selectedRecipe = recipe; Refresh(); }));
            UpdateCardWhenReady(holder,PileType.None,CardPreviewMode.Normal);
        }
        bool canCraft = box!.Inventory.CanCraft(recipe);
        Text($"{ForgeCatalog.BaseName(recipe.Base)}　｜　{string.Join(" ＋ ", recipe.Materials.Select(Recipes.Name))}",18,wrapper,
            canCraft ? new Color("f2d18b") : new Color("90989c"));
        if (!canCraft) Text("不足："+Missing(recipe),16,wrapper,new Color("d88b82"));
        else if (clickable) Text("選んで詳細を確認",16,wrapper,new Color("aebbc0"));
        return wrapper;
    }
    private static (Core.Material First,Core.Material Second) UpgradeCost(CardModel card) => card.Type switch
    {
        CardType.Attack => (Core.Material.Iron,Core.Material.Powder),
        CardType.Skill => (Core.Material.Herb,Core.Material.Ether),
        _ => (Core.Material.Powder,Core.Material.Ether)
    };
    private static CardModel UpgradePreview(CardModel source)
    {
        var preview=CardModel.FromSerializable(source.ToSerializable());
        preview.Owner=box!.Owner;
        preview.UpgradeInternal();
        previewCards.Add(preview);
        return preview;
    }
    private static Control DeckCardDisplay(CardModel card,float scale,bool clickable,bool upgraded=false)
    {
        var wrapper=new VBoxContainer { CustomMinimumSize=clickable?new(235,340):new(330,480) };
        wrapper.Alignment=BoxContainer.AlignmentMode.Center;
        var stage=new Control { CustomMinimumSize=clickable?new(225,285):new(320,405) };
        wrapper.AddChild(stage);
        var holder=MountCard(stage,upgraded?UpgradePreview(card):card,scale,clickable);
        if(holder is not null)
        {
            if(clickable) holder.Connect(NCardHolder.SignalName.Pressed,Callable.From<NCardHolder>(_=>{selectedUpgrade=card;Refresh();}));
            UpdateCardWhenReady(holder,PileType.Deck,upgraded?CardPreviewMode.Upgrade:CardPreviewMode.Normal);
        }
        if(clickable)
        {
            var cost=UpgradeCost(card);
            bool affordable=box!.Inventory.CanUpgradeAt(box.Owner.RunState.TotalFloor,cost.First,cost.Second);
            Text($"{Recipes.Name(cost.First)} ＋ {Recipes.Name(cost.Second)}",17,wrapper,affordable?new Color("f2d18b"):new Color("90989c"));
            if(!affordable) Text("素材不足",15,wrapper,new Color("d88b82"));
        }
        return wrapper;
    }
    private static void RecipeBrowser(string title, string help)
    {
        Text(title,32,content,new Color("f2d18b"));
        Text(help,19);
        Button(craftableOnly ? "作成可能のみ表示　｜　全カードへ" : "全カード表示　｜　作成可能のみへ",()=>{craftableOnly=!craftableOnly;Refresh();});
        var recipes = Recipes.All.Where(r=>!craftableOnly || box!.Inventory.CanCraft(r))
            .OrderByDescending(box!.Inventory.CanCraft)
            .ThenBy(r=>r.Base).ThenBy(r=>r.Materials[0]).ThenBy(r=>r.Materials[1]).ToArray();
        RecipeGallery(recipes);
        if (recipes.Length==0) Text("現在の素材で作れるカードはありません。左の素材数を確認してください。",22);
    }
    private static void RecipeGallery(IEnumerable<Recipe> recipes)
    {
        var grid = new GridContainer { Columns = 3, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation",10);
        grid.AddThemeConstantOverride("v_separation",16);
        content!.AddChild(grid);
        foreach (var recipe in recipes) grid.AddChild(CardDisplay(recipe,0.62f,true));
    }
    // Every recipe uses an unordered pair. Once both materials are placed, the weapon, remedy and device
    // results appear together so the same inventory can be approached from either direction.
    private const int MinCraftSlots = 2;
    private const int MaxCraftSlots = 2;
    private static void MaterialCrafting()
    {
        Text("素材から錬成",32,content,new Color("f2d18b"));
        Text("素材を2個置くと、その組み合わせから作れる武器・薬・装置が表示されます。順番は問いません。",19);
        var slots=new HBoxContainer();slots.AddThemeConstantOverride("separation",18);content!.AddChild(slots);
        for(int i=0;i<MaxCraftSlots;i++)
        {
            int index=i;
            string label=i<craftInputs.Count ? Recipes.Name(craftInputs[i]) : "空きスロット";
            Button(label,()=>RemoveCraftMaterial(index),i>=craftInputs.Count,slots);
            if(i<MaxCraftSlots-1) Text("＋",30,slots,new Color("f2d18b"));
        }
        Text("入れる素材を選ぶ（同じ素材を2個置くこともできます）",20);
        var materials=new GridContainer { Columns=4,SizeFlagsHorizontal=Control.SizeFlags.ExpandFill };
        materials.AddThemeConstantOverride("h_separation",10);content.AddChild(materials);
        foreach(var material in Enum.GetValues<Core.Material>())
        {
            int selected=craftInputs.Count(x=>x==material);
            int owned=box!.Inventory.Counts[(int)material];
            Button($"{Recipes.Name(material)}\n所持 {owned} / 配置 {selected}",()=>AddCraftMaterial(material),
                craftInputs.Count>=MaxCraftSlots || selected>=owned,materials);
        }
        if(craftInputs.Count==0) return;
        Button("素材をすべて戻す",()=>{craftInputs.Clear();Refresh();});
        if(craftInputs.Count<MinCraftSlots)
        {
            Text("もう1個素材を置いてください。",22,content,new Color("aebbc0"));
            return;
        }
        var recipes=Recipes.FindAll(craftInputs).ToArray();
        Text($"{string.Join(" ＋ ", craftInputs.Select(Recipes.Name))} から作れるカード",26,content,new Color("f2d18b"));
        RecipeGallery(recipes);
    }
    private static void AddCraftMaterial(Core.Material material)
    {
        if(craftInputs.Count>=MaxCraftSlots || craftInputs.Count(x=>x==material)>=box!.Inventory.Counts[(int)material]) return;
        craftInputs.Add(material);Refresh();
    }
    private static void RemoveCraftMaterial(int index)
    {
        if(index<0 || index>=craftInputs.Count) return;
        craftInputs.RemoveAt(index);Refresh();
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
        Text($"{recipe.Name}　【{ForgeCatalog.BaseName(recipe.Base)}／{recipe.Role}】",28,details,new Color("f2d18b"));
        Text(recipe.Plan,20,details,new Color("aebbc0"));
        Text(recipe.Preview,21,details);
        Text($"必要素材：{string.Join(" ＋ ", recipe.Materials.Select(Recipes.Name))}",22,details);
        if (!canCraft) Text("不足："+Missing(recipe),20,details,new Color("d88b82"));
        if (workshop) Button(canCraft ? "このカードを作る" : "素材が足りません",()=>_ = Craft(recipe),!canCraft,details);
        else Text("錬成はマップ上の工房で行います。",20,details,new Color("aebbc0"));
        Button("一覧へ戻る",()=> { selectedRecipe=null; Refresh(); },parent:details);
    }
    private static void UpgradeGallery()
    {
        var cards=box!.Owner.Deck.Cards.Where(c=>c.IsUpgradable).ToArray();
        Text("強化するカードを選ぶ",32,content,new Color("f2d18b"));
        Text("1つの工房につき1枚だけ強化できます。アタックは鉄＋火薬、スキルは薬草＋エーテル、パワーは火薬＋エーテルを消費します。",19);
        if(box.Inventory.WorkshopUpgradeFloor==box.Owner.RunState.TotalFloor)
            Text("この工房での強化は使用済みです。カード錬成は引き続き行えます。",20,content,new Color("d8c082"));
        var grid=new GridContainer { Columns=3,SizeFlagsHorizontal=Control.SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation",10);grid.AddThemeConstantOverride("v_separation",16);
        content!.AddChild(grid);
        foreach(var card in cards) grid.AddChild(DeckCardDisplay(card,0.62f,true));
        if(cards.Length==0) Text("強化できるカードはありません。",22);
    }
    private static void UpgradeConfirmation(CardModel card)
    {
        var cost=UpgradeCost(card);
        bool can=card.IsUpgradable && box!.Owner.Deck.Cards.Contains(card)
            && box.Inventory.CanUpgradeAt(box.Owner.RunState.TotalFloor,cost.First,cost.Second);
        Text("このカードを強化しますか？",32,content,new Color("f2d18b"));
        var row=new HBoxContainer(); row.AddThemeConstantOverride("separation",12); content!.AddChild(row);
        var current=new VBoxContainer(); row.AddChild(current); Text("現在",21,current); current.AddChild(DeckCardDisplay(card,0.72f,false));
        Text("→",34,row,new Color("f2d18b"));
        var upgraded=new VBoxContainer(); row.AddChild(upgraded); Text("強化後",21,upgraded); upgraded.AddChild(DeckCardDisplay(card,0.72f,false,true));
        var actions=new VBoxContainer { SizeFlagsHorizontal=Control.SizeFlags.ExpandFill }; row.AddChild(actions);
        Text($"必要素材\n{Recipes.Name(cost.First)} ＋ {Recipes.Name(cost.Second)}",22,actions);
        if(!can) Text("素材が不足しています。",19,actions,new Color("d88b82"));
        Button(can?"このカードを強化する":"強化できません",()=>UpgradeCard(card),!can,actions);
        Button("一覧へ戻る",()=>{selectedUpgrade=null;Refresh();},parent:actions);
    }
    private static void RareGallery()
    {
        var cards=box!.Owner.Deck.Cards.OfType<ForgedCard>().Where(c=>c.AlchemistRareModifier.Length==0).ToArray();
        Text("希少加工する錬成カードを選ぶ",32,content,new Color("d8b4ff"));
        Text("希少素材を1個消費し、カードに解除不能の特殊効果を1つ刻みます。1枚につき1回までです。",19);
        var grid=new GridContainer { Columns=3,SizeFlagsHorizontal=Control.SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation",10);grid.AddThemeConstantOverride("v_separation",16);
        content!.AddChild(grid);
        foreach(var card in cards) grid.AddChild(RareCardDisplay(card));
        if(cards.Length==0) Text("加工できる未加工の錬成カードがありません。",22);
    }
    private static Control RareCardDisplay(ForgedCard card)
    {
        var wrapper=new VBoxContainer { CustomMinimumSize=new(235,340),Alignment=BoxContainer.AlignmentMode.Center };
        var stage=new Control { CustomMinimumSize=new(225,285) };wrapper.AddChild(stage);
        var holder=MountCard(stage,card,0.62f,true);
        if(holder is not null)
        {
            holder.Connect(NCardHolder.SignalName.Pressed,Callable.From<NCardHolder>(_=>{selectedRareCard=card;Refresh();}));
            UpdateCardWhenReady(holder,PileType.Deck,CardPreviewMode.Normal);
        }
        Text("選んで希少効果を確認",16,wrapper,new Color("d8b4ff"));
        return wrapper;
    }
    private static void RareConfirmation(ForgedCard card)
    {
        bool valid=box!.Owner.Deck.Cards.Contains(card) && card.AlchemistRareModifier.Length==0;
        Text("刻む希少効果を選ぶ",32,content,new Color("d8b4ff"));
        Text($"対象：{card.Title}\n加工後は取り外し・上書きできません。",21);
        foreach(var rare in RareMaterials.All)
        {
            bool meaningful=rare.Material switch
            {
                RareMaterial.Mercury => !card.Keywords.Contains(CardKeyword.Retain),
                RareMaterial.VoidCrystal => card.Formula.Cost>0 || !card.Keywords.Contains(CardKeyword.Exhaust),
                _ => true
            };
            bool can=valid && meaningful && box.Inventory.CanApplyRare(rare.Material);
            string owned=$"所持 {box.Inventory.RareCounts[(int)rare.Material]}個";
            Button($"{rare.Name}【{rare.EffectName}】　{rare.Description}\n{owned}",()=>ApplyRare(card,rare),!can);
        }
        Button("カード一覧へ戻る",()=>{selectedRareCard=null;Refresh();});
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
        MaterialCardRow();
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
            Text($"{RareMaterials.Name(box.Inventory.Pending[0].Material)}　（残り {box.Inventory.Pending.Count}個）\n次の部屋へ進む前に受け取りを決めてください。",23);
            if (box.Inventory.Total < AlchemyState.Capacity) Button("受け取る",()=>Resolve(true));
            else foreach (var material in OwnedMaterials())
                Button($"{RareMaterials.Name(material)}を1個手放して交換",()=>Resolve(true,material));
            Button("この素材の受け取りを辞退",()=>Resolve(false));
            return;
        }
        if (box.Inventory.Offers.Count > 0 && !CombatManager.Instance.IsInProgress)
        {
            var offer = box.Inventory.Offers.FirstOrDefault(o=>o.Id==offerId) ?? box.Inventory.Offers[0];
            Text("素材を1つ選ぶ",32,content,new Color("f2d18b"));
            Text($"3つの候補から1個だけ受け取れます。未解決の報酬枠は残り {box.Inventory.Offers.Count} 個です。",23);
            if (box.Inventory.Total >= AlchemyState.Capacity)
                Text("素材ボックスが満杯です。受け取ると、交換か辞退を続けて選びます。",20,content,new Color("d8c082"));
            foreach (var material in offer.Candidates)
                Button($"{RareMaterials.Name(material)}　—　{MaterialHint(material)}",()=>TakeOffer(offer.Id,material));
            Button("この報酬枠を辞退する",()=>DeclineOffer(offer.Id));
            Text("辞退した枠は戻りません。次の部屋へ進む前に決めてください。",18,content,new Color("aebbc0"));
            return;
        }
        if (workshop)
        {
            var modes=new HBoxContainer(); sidebar.AddChild(modes);
            Button("カード錬成",()=>{upgradeMode=false;rareMode=false;selectedUpgrade=null;selectedRareCard=null;Refresh();},!upgradeMode&&!rareMode,modes);
            Button("既存カード強化",()=>{upgradeMode=true;rareMode=false;selectedRecipe=null;selectedRareCard=null;Refresh();},upgradeMode,modes);
            Button("希少加工",()=>{upgradeMode=false;rareMode=true;selectedRecipe=null;selectedUpgrade=null;Refresh();},rareMode,modes);
            if(rareMode && selectedRareCard is not null) RareConfirmation(selectedRareCard);
            else if(rareMode) RareGallery();
            else if(upgradeMode && selectedUpgrade is not null) UpgradeConfirmation(selectedUpgrade);
            else if(upgradeMode) UpgradeGallery();
            else if (selectedRecipe is not null) Confirmation(selectedRecipe);
            else
            {
                var craftModes=new HBoxContainer();content!.AddChild(craftModes);
                Button("カードから探す",()=>{materialCraftMode=false;craftInputs.Clear();Refresh();},!materialCraftMode,craftModes);
                Button("素材から作る",()=>{materialCraftMode=true;craftableOnly=false;Refresh();},materialCraftMode,craftModes);
                if(materialCraftMode) MaterialCrafting();
                else RecipeBrowser("完成カードを選ぶ","カードを選ぶと大きく表示し、効果を確認してから錬成できます。");
            }
            Button("工房を退出する",()=>_ = LeaveMapWorkshop(),parent:sidebar);
            Text("退出するとマップへ戻ります。",16,sidebar,new Color("aebbc0"));
        }
        else if (browsing)
        {
            if (selectedRecipe is not null) Confirmation(selectedRecipe);
            else RecipeBrowser("レシピ一覧","素材の組み合わせと完成カードを確認できます。錬成はマップ上の工房で行います。");
            Button("素材ボックスへ戻る",()=>{browsing=false;selectedRecipe=null;Refresh();},parent:sidebar);
        }
        else if (merchantMode)
        {
            MerchantMaterialShop();
            Button("素材ボックスへ戻る",()=>{merchantMode=false;Refresh();},parent:sidebar);
        }
        else
        {
            Text("素材の使い道",32,content,new Color("f2d18b"));
            Text("《炉の起動》でカードを廃棄した時の素材相に応じて素材を獲得します。現在・次・次々の相は、戦闘中に左上のボタンで確認できます。",22);
            Text("工房はマップ上の専用ノードから利用できます。",20);
            Button("レシピ一覧を見る",()=>{browsing=true;craftableOnly=false;Refresh();},parent:sidebar);
            if (InMerchantRoom) Button("商人で素材を買う",()=>{merchantMode=true;Refresh();},parent:sidebar);
            Button("閉じる",Close,parent:sidebar);
        }
    }
    // Buying materials does not reuse the game's own character-card merchant slots: those are backed by
    // CardFactory/RunState.CreateCard and complete a purchase by adding the created card straight to the
    // deck (MerchantCardEntry.OnTryPurchase), which has no hook for redirecting into a material grant
    // without patching that core purchase path. This panel is the same kind of custom, scene-free overlay
    // WorkshopUi already uses everywhere else, gated to the merchant room instead of the workshop node.
    private static bool InMerchantRoom => box?.Owner.RunState.CurrentRoom is MerchantRoom;
    private static string MerchantVisitId => $"{box!.Owner.RunState.TotalFloor}:{box.Owner.RunState.CurrentRoom?.Id}";
    private static MerchantMaterialOffer[] CurrentMerchantOffers =>
        MerchantMaterialOffers.Roll(box!.Owner.RunState.Rng.Seed,MerchantVisitId);
    private static void MerchantMaterialShop()
    {
        Text("商人で素材を買う",32,content,new Color("f2d18b"));
        if (!InMerchantRoom)
        {
            Text("商人のいる部屋でのみ購入できます。",22,content,new Color("d88b82"));
            return;
        }
        Text($"所持ゴールド　{box!.Owner.Gold}G",20);
        Text($"この訪問では3枠だけ入荷します。各枠は1回限り、1個{MerchantMaterialOffers.Price}Gです。",19);
        var grid = new GridContainer { Columns = 3, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation",10);grid.AddThemeConstantOverride("v_separation",16);
        content!.AddChild(grid);
        foreach (var offer in CurrentMerchantOffers) MerchantMaterialSlot(grid,offer);
    }
    private static void MerchantMaterialSlot(Container parent, MerchantMaterialOffer offer)
    {
        var wrapper = new VBoxContainer { CustomMinimumSize = new(160,235) };
        wrapper.Alignment = BoxContainer.AlignmentMode.Center;
        var stage = new Control { CustomMinimumSize = new(150,190) };
        wrapper.AddChild(stage);
        var holder = MountCard(stage, CreateMaterialPreviewCard(MaterialCards.Canonical(offer.Material)), 0.62f, false);
        if (holder is not null) UpdateCardWhenReady(holder, PileType.None, CardPreviewMode.Normal);
        bool sold = box!.Inventory.Received.Contains(offer.Id);
        bool full = box.Inventory.Total >= AlchemyState.Capacity || !box.Inventory.Settled;
        bool afford = box.Owner.Gold >= MerchantMaterialOffers.Price;
        Text($"{Recipes.Name(offer.Material)}　{MerchantMaterialOffers.Price}G",20,wrapper,!sold&&afford&&!full?new Color("f2d18b"):new Color("90989c"));
        string label = sold ? "売り切れ" : full ? "ボックスを整理してください" : afford ? "購入する" : "ゴールド不足";
        Button(label,()=>_=BuyMaterial(offer),sold||full||!afford,wrapper);
        parent.AddChild(wrapper);
    }
    private static async Task BuyMaterial(MerchantMaterialOffer offer)
    {
        if (box is null || busy || !InMerchantRoom || box.Owner.Gold < MerchantMaterialOffers.Price
            || box.Inventory.Total >= AlchemyState.Capacity || !box.Inventory.Settled
            || box.Inventory.Received.Contains(offer.Id)
            || !CurrentMerchantOffers.Contains(offer)) return;
        busy = true;
        Refresh();
        try
        {
            await PlayerCmd.LoseGold(MerchantMaterialOffers.Price, box.Owner, GoldLossType.Spent);
            if(!box.Inventory.Grant(offer.Id,offer.Material)) throw new InvalidOperationException("この素材は売り切れです。");
            status = $"{Recipes.Name(offer.Material)}を購入しました。";
        }
        catch (Exception ex) { status = $"購入できませんでした：{ex.Message}"; GD.PushError(ex.ToString()); }
        finally { busy = false; if (IsOpen) Refresh(); }
    }
    private static IEnumerable<MaterialChoice> OwnedMaterials() =>
        Enum.GetValues<Core.Material>().Select(MaterialChoice.Normal).Where(m=>box!.Inventory.Count(m)>0)
        .Concat(Enum.GetValues<RareMaterial>().Select(MaterialChoice.Rare).Where(m=>box!.Inventory.Count(m)>0));
    private static void Resolve(bool accept, MaterialChoice? exchange = null)
    {
        box!.Inventory.Resolve(accept,exchange);
        Refresh();
    }
    private static string MaterialHint(MaterialChoice material) => material.Class == MaterialClass.Rare
        ? RareMaterials.Get(material.RareMaterial).Description
        : material.NormalMaterial switch
    {
        Core.Material.Iron => "物理・防御", Core.Material.Herb => "毒・弱体",
        Core.Material.Powder => "高火力・全体", _ => "ドロー・循環"
    };
    private static void TakeOffer(string id, MaterialChoice material)
    {
        if (box is null || busy) return;
        try { box.Inventory.TakeOffer(id,material); status = $"{RareMaterials.Name(material)}を受け取りました。"; }
        catch (Exception ex) { status = $"受け取れませんでした：{ex.Message}"; GD.PushError(ex.ToString()); }
        FinishOffer(id);
    }
    private static void DeclineOffer(string id)
    {
        if (box is null || busy) return;
        try { box.Inventory.DeclineOffer(id); status = "この報酬枠を辞退しました。"; }
        catch (Exception ex) { status = $"辞退できませんでした：{ex.Message}"; GD.PushError(ex.ToString()); }
        FinishOffer(id);
    }
    // Releases the rewards screen only once the slot really left the state, so a rejected take leaves the
    // button in place instead of consuming the reward.
    private static void FinishOffer(string id)
    {
        if (box!.Inventory.HasOffer(id)) { if (IsOpen) Refresh(); return; }
        if (offerId == id)
        {
            offerId = null;
            var waiting = offerResult;
            offerResult = null;
            waiting?.TrySetResult(true);
        }
        if (box.Inventory.Offers.Count == 0 && box.Inventory.Pending.Count == 0 && !workshop && !browsing) Close();
        else if (IsOpen) Refresh();
    }
    private static void Close()
    {
        if (busy) return;
        ClearPreviewCards();
        overlay?.QueueFree();
        overlay = null;
        content = null;
        sidebar = null;
        mapWorkshop = false;
        offerId = null;
        // Closing without a choice leaves the slot unresolved: the reward button stays, and the travel
        // guard reopens this screen before the next room.
        var waiting = offerResult;
        offerResult = null;
        waiting?.TrySetResult(false);
    }
    private static async Task LeaveMapWorkshop()
    {
        if(busy) return;
        Close();
        await RunManager.Instance.EnterRoom(new MapRoom());
    }
    private static void UpgradeCard(CardModel card)
    {
        if(box is null || busy || !workshop || !CanEnterWorkshop(box) || !card.IsUpgradable || !box.Owner.Deck.Cards.Contains(card)) return;
        busy=true;
        try
        {
            var cost=UpgradeCost(card);
            box.Inventory.CommitUpgrade(box.Owner.RunState.TotalFloor,cost.First,cost.Second,Guid.NewGuid().ToString("N"),()=>CardCmd.Upgrade(card,CardPreviewStyle.EventLayout));
            status=$"{card.Title}を強化しました。";
            selectedUpgrade=null;
        }
        catch(Exception ex) { status=$"強化できませんでした：{ex.Message}"; GD.PushError(ex.ToString()); }
        finally { busy=false;if(IsOpen)Refresh(); }
    }
    private static void ApplyRare(ForgedCard card,RareMaterialDefinition rare)
    {
        if(box is null || busy || !workshop || !CanEnterWorkshop(box) || !box.Owner.Deck.Cards.Contains(card)
            || card.AlchemistRareModifier.Length>0) return;
        busy=true;
        string before=card.AlchemistRareModifier;
        try
        {
            box.Inventory.CommitRare(rare.Material,Guid.NewGuid().ToString("N"),
                ()=>card.AlchemistRareModifier=rare.Id,()=>card.AlchemistRareModifier=before);
            status=$"{card.Title}に{rare.Name}の「{rare.EffectName}」を刻みました。";
            selectedRareCard=null;
        }
        catch(Exception ex) { status=$"希少加工できませんでした：{ex.Message}";GD.PushError(ex.ToString()); }
        finally { busy=false;if(IsOpen)Refresh(); }
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
            if(materialCraftMode) craftInputs.Clear();
        }
        catch (Exception ex) { status = $"錬成できませんでした：{ex.Message}"; GD.PushError(ex.ToString()); }
        finally { busy = false; if (IsOpen) Refresh(); }
    }
}

[HarmonyPatch(typeof(NMerchantInventory),nameof(NMerchantInventory.Open))]
public static class MerchantMaterialButtonPatch
{
    public static void Postfix(NMerchantInventory __instance)
    {
        if(WorkshopUi.CurrentBox() is null || __instance.GetNodeOrNull<Button>("AlchemistMaterialShopButton") is not null) return;
        var button = new Button { Name="AlchemistMaterialShopButton", Text="錬金素材を見る" };
        button.AnchorLeft=1; button.AnchorRight=1;
        button.OffsetLeft=-330; button.OffsetRight=-30; button.OffsetTop=24; button.OffsetBottom=86;
        button.AddThemeFontSizeOverride("font_size",22);
        button.Pressed += ()=>WorkshopUi.OpenMerchantShop(__instance);
        __instance.AddChild(button);
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
        if (b.Inventory.Settled && !WorkshopUi.IsBusy && !WorkshopUi.IsOpen) return true;
        WorkshopUi.Open();
        __result = Task.CompletedTask;
        return false;
    }
}
