using Alchemist.Core;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Alchemist;

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
                GD.PushWarning($"[Alchemist] Act {run.CurrentActIndex}: 全経路を覆えませんでした（中盤={layout.MidGuaranteed} 終盤={layout.LateGuaranteed}）。該当帯は工房1個に縮退します。");
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
    public static void Postfix(ref Task __result)
    {
        if(WorkshopMap.IsCurrentWorkshop()) __result=After(__result);
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
    public static bool Prefix(ref Task __result)
    {
        if(!WorkshopMap.IsCurrentWorkshop()) return true;
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
        var label=new Label { Text="工房",Position=new(-24,34),Size=new(72,28),HorizontalAlignment=HorizontalAlignment.Center,MouseFilter=Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size",16);
        label.AddThemeColorOverride("font_color",new Color("f4d796"));
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
        icon.Texture=ResourceLoader.Load<Texture2D>(ImageHelper.GetImagePath("atlases/ui_atlas.sprites/map/icons/map_shop.tres"));
        icon.SelfModulate=new Color("e6b86a");
    }
}
