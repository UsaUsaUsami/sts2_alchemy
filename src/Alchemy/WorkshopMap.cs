using Alchemy.Core;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Alchemy;

public static class WorkshopMap
{
    public static string Key(MapCoord coord) => $"{coord.col},{coord.row}";
    public static bool IsWorkshop(RunState? run, MapCoord? coord)
    {
        if (run is null || coord is null || run.Players.Count != 1 || run.Players[0].Character is not AlchemistCharacter) return false;
        var box=run.Players[0].GetRelic<MaterialBox>();
        return box is not null && box.Inventory.WorkshopNodes.TryGetValue(run.CurrentActIndex,out var nodes) && nodes.Contains(Key(coord.Value));
    }
    public static bool IsCurrentWorkshop() => RunManager.Instance.DebugOnlyGetState() is { } run && IsWorkshop(run,run.CurrentMapCoord);

    private static WorkshopCandidate Point(MapPoint p) => new(p.coord.col,p.coord.row);
    // Merchants, rest sites, elites, the boss and anything the act protected are never converted; a
    // normal fight may be, but costs enough that "?" points are taken first.
    private static int CostOf(MapPoint p) => !p.CanBeModified ? WorkshopCut.Blocked : p.PointType switch
    {
        MapPointType.Unknown => 1,
        MapPointType.Monster => 4,
        _ => WorkshopCut.Blocked
    };
    public static List<CutNode> BuildGraph(ActMap map) =>
        [..map.GetAllMapPoints().Append(map.StartingMapPoint).Append(map.BossMapPoint).Distinct()
            .Select(p=>new CutNode(Point(p),
                p==map.StartingMapPoint || p==map.BossMapPoint ? WorkshopCut.Blocked : CostOf(p),
                [..p.Children.Select(Point)]))];

    public static void Plan(RunState run)
    {
        if (run.Players.Count != 1 || run.Players[0].Character is not AlchemistCharacter) return;
        var box=run.Players[0].GetRelic<MaterialBox>();
        if (box is null) return;
        var map=run.Map;
        if(!box.Inventory.WorkshopNodes.ContainsKey(run.CurrentActIndex))
        {
            var layout=WorkshopPlanner.SelectGuaranteed(BuildGraph(map),
                Point(map.StartingMapPoint),Point(map.BossMapPoint),map.GetRowCount()-1);
            if(!layout.MidGuaranteed || !layout.LateGuaranteed)
                GD.PushWarning($"[Alchemy] Act {run.CurrentActIndex}: 全経路を覆えませんでした（中盤={layout.MidGuaranteed} 終盤={layout.LateGuaranteed}）。該当帯は工房1個に縮退します。");
            box.Inventory.WorkshopNodes[run.CurrentActIndex]=layout.Coords.Select(p=>$"{p.Col},{p.Row}").ToList();
            box.Inventory.Revision++;
        }
        var workshopKeys=box.Inventory.WorkshopNodes[run.CurrentActIndex];
        foreach(var point in map.GetAllMapPoints().Where(p=>workshopKeys.Contains(Key(p.coord))))
            point.PointType=MapPointType.RestSite;
    }
}

[HarmonyPatch(typeof(RunManager),nameof(RunManager.GenerateMap))]
public static class WorkshopMapGenerationPatch
{
    public static void Postfix(RunManager __instance,ref Task __result) => __result=After(__result,__instance);
    private static async Task After(Task original,RunManager manager)
    {
        await original;
        if(manager.DebugOnlyGetState() is not { } run) return;
        WorkshopMap.Plan(run);
        NMapScreen.Instance?.SetMap(run.Map,run.Rng.Seed,clearDrawings:true);
    }
}

[HarmonyPatch(typeof(RunManager),"RollRoomTypeFor")]
public static class WorkshopRoomTypePatch
{
    public static bool Prefix(ref RoomType __result)
    {
        if(!WorkshopMap.IsCurrentWorkshop()) return true;
        __result=RoomType.RestSite;
        return false;
    }
}

[HarmonyPatch(typeof(RestSiteRoom),nameof(RestSiteRoom.EnterInternal))]
public static class WorkshopRoomEntryPatch
{
    /// The rest site rooms entered as workshops. Their exit is decided by the room, not by the current map
    /// coordinate: travelling on records the destination as current before the old room exits (2026-10-02: the exit
    /// then took the rest site path, waited forever for the rest site sync, and the screen stayed black).
    public static readonly System.Runtime.CompilerServices.ConditionalWeakTable<RestSiteRoom, object> Workshops = new();
    public static void Postfix(RestSiteRoom __instance, ref Task __result)
    {
        if(!WorkshopMap.IsCurrentWorkshop()) return;
        Workshops.AddOrUpdate(__instance, true);
        __result=After(__result);
    }
    private static async Task After(Task original)
    {
        await original;
        WorkshopUi.OpenMapWorkshop();
    }
}

// RestSiteSynchronizer only completes a player's rest site from BeforeLocalRestSiteExited when options
// remain, so a workshop node - which offers none - would hang forever awaiting AfterAllRestSitesCompleted.
// Singleplayer prototype: skip the barrier for workshop nodes only.
[HarmonyPatch(typeof(RestSiteRoom),nameof(RestSiteRoom.Exit))]
public static class WorkshopRoomExitPatch
{
    public static bool Prefix(RestSiteRoom __instance, ref Task __result)
    {
        if(!WorkshopRoomEntryPatch.Workshops.TryGetValue(__instance, out _) && !WorkshopMap.IsCurrentWorkshop()) return true;
        NRestSiteRoom.Instance?.BeforeExitingRoom();
        __result=Task.CompletedTask;
        return false;
    }
}

[HarmonyPatch(typeof(NNormalMapPoint),"_Ready")]
public static class WorkshopMapLabelPatch
{
    public static void Postfix(NNormalMapPoint __instance)
    {
        var run=RunManager.Instance.DebugOnlyGetState();
        if(!WorkshopMap.IsWorkshop(run,__instance.Point.coord)) return;
        // Below the icon, large enough to read, outlined so it holds on the parchment (UI review, 2026-10-05).
        var label=new Label { Text="工房",Position=new(-28,42),Size=new(80,30),HorizontalAlignment=HorizontalAlignment.Center,MouseFilter=Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size",20);
        label.AddThemeColorOverride("font_color",new Color("f4d796"));
        label.AddThemeColorOverride("font_outline_color",new Color("2a1c0c"));
        label.AddThemeConstantOverride("outline_size",6);
        __instance.AddChild(label);
    }
}

// UpdateIcon also runs from RefreshState, so patching _Ready alone let the rest site campfire come back
// as soon as the point changed state.
[HarmonyPatch(typeof(NNormalMapPoint),"UpdateIcon")]
public static class WorkshopMapIconPatch
{
    public static void Postfix(NNormalMapPoint __instance)
    {
        var run=RunManager.Instance.DebugOnlyGetState();
        if(!WorkshopMap.IsWorkshop(run,__instance.Point.coord)) return;
        var icon=__instance.GetNode<TextureRect>("%Icon");
        // Own ink symbol (IconArt) drawn like the other nodes; the borrowed shop icon is tinted gold to stand apart.
        string? own=IconArt.Packed("workshop_map");
        icon.Texture=ResourceLoader.Load<Texture2D>(own ?? ImageHelper.GetImagePath("atlases/ui_atlas.sprites/map/icons/map_shop.tres"));
        if(own is null) icon.SelfModulate=new Color("e6b86a");
    }
}

// The legend lists every node type but the workshop (UI review, 2026-10-05): one row is added to an alchemist's
// map, taken away again if the screen is reused by another character's run.
[HarmonyPatch(typeof(NMapScreen),nameof(NMapScreen.Open))]
public static class WorkshopMapLegendPatch
{
    private const string RowName="AlchemyWorkshopLegend";
    public static void Postfix(NMapScreen __instance)
    {
        var items=__instance.GetNodeOrNull<Control>("MapLegend/LegendItems");
        if(items is null) return;
        var existing=items.GetNodeOrNull(RowName);
        bool want=RunManager.Instance.DebugOnlyGetState() is { Players.Count: 1 } run && run.Players[0].Character is AlchemistCharacter;
        if(!want) { existing?.QueueFree(); return; }
        if(existing is not null) return;
        // Same look as the rest site row: its icon size and its label's font.
        var rest=items.GetNodeOrNull<Control>("RestSiteLegendItem");
        var restIcon=rest?.GetNodeOrNull<TextureRect>("Icon");
        var restLabel=rest?.FindChildren("*","Label",true,false).OfType<Label>().FirstOrDefault();
        var row=new HBoxContainer { Name=RowName,MouseFilter=Control.MouseFilterEnum.Ignore,CustomMinimumSize=rest?.CustomMinimumSize??new Vector2(0,48) };
        row.AddThemeConstantOverride("separation",10);
        string? own=IconArt.Packed("workshop_map");
        var icon=new TextureRect
        {
            Texture=ResourceLoader.Load<Texture2D>(own ?? ImageHelper.GetImagePath("atlases/ui_atlas.sprites/map/icons/map_shop.tres")),
            ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize=restIcon is { Size.X: > 0 } ? restIcon.Size : new Vector2(40,40),
            SelfModulate=own is null ? new Color("e6b86a") : Colors.White,
        };
        row.AddChild(new Control { CustomMinimumSize=new Vector2(18,0),MouseFilter=Control.MouseFilterEnum.Ignore });
        row.AddChild(icon);
        var text=new Label { Text="工房",VerticalAlignment=VerticalAlignment.Center };
        if(restLabel is not null)
        {
            text.AddThemeFontSizeOverride("font_size",restLabel.GetThemeFontSize("font_size"));
            text.AddThemeColorOverride("font_color",restLabel.GetThemeColor("font_color"));
            if(restLabel.GetThemeFont("font") is { } font) text.AddThemeFontOverride("font",font);
        }
        row.AddChild(text);
        items.AddChild(row);
        // The panel is sized for six rows; grow it by the row added (checked by screenshot, 2026-10-05).
        const float RowHeight=48;
        row.CustomMinimumSize=new Vector2(row.CustomMinimumSize.X,RowHeight);
        var legend=items.GetParent<Control>();
        legend.Size=new Vector2(legend.Size.X,legend.Size.Y+RowHeight+4);
        GD.Print($"[Alchemy] map legend: workshop row added (panel {legend.GetClass()} size {legend.Size})");
    }
}
