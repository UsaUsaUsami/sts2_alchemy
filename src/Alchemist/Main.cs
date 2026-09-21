using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace Alchemist;

[ModInitializer(nameof(Initialize))]
public static class Main
{
    public static void Initialize()
    {
        new Harmony("syouh.Alchemist").PatchAll(Assembly.GetExecutingAssembly());
        GD.Print("[Alchemist] Initialized 0.2.0; target StS2 v0.111.0 / BaseLib 3.4.5; singleplayer prototype");
    }
}
