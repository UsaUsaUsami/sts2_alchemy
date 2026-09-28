using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace Alchemy;

[ModInitializer(nameof(Initialize))]
public static class Main
{
    public static void Initialize()
    {
        new Harmony("syouh.Alchemy").PatchAll(Assembly.GetExecutingAssembly());
        GD.Print("[Alchemy] Initialized 0.24.0; target StS2 v0.111.0 / BaseLib 3.4.5; singleplayer prototype");
    }
}
