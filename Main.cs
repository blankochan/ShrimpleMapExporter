using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
[assembly: MelonInfo(typeof(GizmoTest.MapExporter), "MapExporter", "0.0.2", "ZabelTheBanal")]


namespace GizmoTest;

public class MapExporter : MelonMod
{
    public override void OnEarlyInitializeMelon()
    {

        if (Application.buildGUID is "4c29d92a2ace4dd58a608f435fe214bd" or "49bf2d45f34840c3a6b67ebe9e56799a")
            // the maps in beta 5 dont load right, this covers the first build and the hotfix
            this.Unregister("Beta 5 maps dont export correctly to beta 4 and are unsupported");
    }
    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        if (sceneName is not "Splashes") return;

        var locator = Addressables.ResourceLocators.Cast<Il2CppSystem.Linq.Enumerable.WhereSelectListIterator<ResourceLocatorInfo, IResourceLocator>>().source.ToArray()[0].Locator.Cast<ResourceLocationMap>();
        var maps = locator.Locations["Maps"];
        int mapsLength = maps.Cast<Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<IResourceLocation>>().Count;
        for (int mapIndex = 0; mapIndex < mapsLength; mapIndex++)
        {
            var map = maps[mapIndex].Cast<ContentCatalogData.CompactLocation>();
            var mapInfo = new MapInfo(map);
            if (Directory.Exists($"./UserData/ExportedMaps/{mapInfo.ScenePrimaryKey}/")) return;

            Directory.CreateDirectory($"./UserData/ExportedMaps/{mapInfo.ScenePrimaryKey}/");
            File.WriteAllText($"./UserData/ExportedMaps/{mapInfo.ScenePrimaryKey}/manifest.json", Newtonsoft.Json.JsonConvert.SerializeObject(mapInfo, Newtonsoft.Json.Formatting.Indented));
            Directory.CreateDirectory($"./UserData/ExportedMaps/{mapInfo.ScenePrimaryKey}/Dependencies");
            for (int depIndex = 0; depIndex < mapInfo.Dependencies.Length; depIndex++)
            {
                string depPath = map.Dependencies[depIndex].InternalId;
                File.Copy(depPath, $"./UserData/ExportedMaps/{mapInfo.ScenePrimaryKey}/Dependencies/{mapInfo.Dependencies[depIndex].InternalId}");
            }
        }
        this.Unregister("Finished Exporting!");
    }
}

public struct AssetBundleRequestOptions
{

    public string BundleName;
    public long BundleSize;
    public string AssetLoadMode;


    public AssetBundleRequestOptions(UnityEngine.ResourceManagement.ResourceProviders.AssetBundleRequestOptions req)
    {
        BundleName = req.BundleName;
        BundleSize = req.BundleSize;

        AssetLoadMode = req.AssetLoadMode.ToString();
    }
}

public struct SimpleDependency
{

    public string InternalId;
    public string PrimaryKey;
    public string ProviderId;

    public AssetBundleRequestOptions Data;


    public SimpleDependency(UnityEngine.AddressableAssets.ResourceLocators.ContentCatalogData.CompactLocation bundle)
    {
        if (bundle.HasDependencies)
        {
            MelonLogger.BigError("generating simple dependency bundle has deps", bundle.PrimaryKey);
        }

        InternalId = bundle.InternalId.Split('\\').Last();
        PrimaryKey = bundle.PrimaryKey;
        ProviderId = bundle.ProviderId;

        Data = new(bundle.Data.Cast<UnityEngine.ResourceManagement.ResourceProviders.AssetBundleRequestOptions>());

    }
}

public sealed class MapInfo
{
    public string SceneInternalKey;
    public string ScenePrimaryKey;
    public SimpleDependency[] Dependencies;

    public MapInfo(ContentCatalogData.CompactLocation map)
    {
        SceneInternalKey = map.InternalId;
        ScenePrimaryKey = map.PrimaryKey;
        List<SimpleDependency> deps = new();
        foreach (var dep in map.Dependencies.Cast<Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<IResourceLocation>>())
            deps.Add(new SimpleDependency(dep.Cast<ContentCatalogData.CompactLocation>()));
        Dependencies = deps.ToArray();
    }
}
