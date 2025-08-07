using BepInEx;
using BepInEx.Logging;
using BlackMagicAPI.Helpers;
using BlackMagicAPI.Managers;
using HarmonyLib;
using MoreSpells.Modules;
using System.Reflection;
using UnityEngine;

namespace MoreSpells;

[BepInProcess("MageArena")]
[BepInDependency("com.d1gq.black.magic.api", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("com.magearena.modsync", BepInDependency.DependencyFlags.HardDependency)]
[BepInPlugin(MyGUID, PluginName, VersionString)]
public class MSPlugin : BaseUnityPlugin
{
    internal static MSPlugin Instance { get; private set; }
    private const string MyGUID = "com.d1gq.more.spells";
    internal const string PluginName = "MoreSpells";
    private const string VersionString = "1.1.1";

    private static Harmony? Harmony;
    internal static ManualLogSource Log => Instance._log;
    private ManualLogSource? _log;

    public static string modsync = "all";

    internal static AssetBundle? SpellsAssets;

    private void Awake()
    {
        _log = Logger;
        Instance = this;
        Harmony = new(MyGUID);
        Harmony.PatchAll();

        SpellsAssets = Assembly.GetExecutingAssembly().LoadAssetBundleFromResources("MoreSpells.Resources.spells");
        BlackMagicManager.RegisterSpell(this, typeof(MagicShieldData), typeof(MagicShieldLogic));
    }
}