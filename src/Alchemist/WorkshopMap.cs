using Alchemist.Core;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes;
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

    public static void Plan(RunState run)
    {
        if (run.Players.Count != 1 || run.Players[0].Character is not AlchemistCharacter) return;
        var box=run.Players[0].GetRelic<MaterialBox>();
        if (box is null) return;
        var map=run.Map;
        if(!box.Inventory.WorkshopNodes.ContainsKey(run.CurrentActIndex))
        {
            var unknown=map.GetAllMapPoints().Where(p=>p.PointType==MapPointType.Unknown)
                .Select(p=>new WorkshopCandidate(p.coord.col,p.coord.row));
            int rests=map.GetAllMapPoints().Count(p=>p.PointType==MapPointType.RestSite);
            box.Inventory.WorkshopNodes[run.CurrentActIndex]=WorkshopPlanner.Select(unknown,rests,map.GetRowCount()-1)
                .Select(p=>$"{p.Col},{p.Row}").ToList();
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

[HarmonyPatch(typeof(NNormalMapPoint),"_Ready")]
public static class WorkshopMapIconPatch
{
    public static void Postfix(NNormalMapPoint __instance)
    {
        var run=RunManager.Instance.DebugOnlyGetState();
        if(!WorkshopMap.IsWorkshop(run,__instance.Point.coord)) return;
        var icon=__instance.GetNode<TextureRect>("%Icon");
        icon.Texture=ResourceLoader.Load<Texture2D>(ImageHelper.GetImagePath("atlases/ui_atlas.sprites/map/icons/map_shop.tres"));
        icon.SelfModulate=new Color("e6b86a");
        var label=new Label { Text="工房",Position=new(-24,34),Size=new(72,28),HorizontalAlignment=HorizontalAlignment.Center,MouseFilter=Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size",16);
        label.AddThemeColorOverride("font_color",new Color("f4d796"));
        __instance.AddChild(label);
    }
}
