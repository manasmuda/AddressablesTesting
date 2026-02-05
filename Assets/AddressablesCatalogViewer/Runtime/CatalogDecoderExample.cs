using UnityEngine;
using System.IO;
using AddressablesCatalogViewer;

/// <summary>
/// Example script demonstrating how to use the CatalogDecoder
/// to inspect Addressables catalog structure and dependencies.
/// </summary>
public class CatalogDecoderExample : MonoBehaviour
{
    [Header("Catalog Settings")]
    [Tooltip("Path to catalog.json file (relative to StreamingAssets or absolute)")]
    public string catalogPath = "aa/Android/catalog.json";
    
    [Header("Debug Options")]
    public bool printSummaryOnStart = true;
    public bool printAllEntries = false;
    public bool printDependencyTree = true;

    private CatalogDecoder.DecodedCatalog _decodedCatalog;

    private void Start()
    {
        LoadAndDecodeCatalog();
        
        if (_decodedCatalog != null)
        {
            if (printSummaryOnStart)
                PrintSummary();
            
            if (printAllEntries)
                PrintAllEntries();
            
            if (printDependencyTree)
                PrintDependencyTree();
        }
    }

    private void LoadAndDecodeCatalog()
    {
        // Try loading from StreamingAssets first
        string fullPath = Path.Combine(Application.streamingAssetsPath, catalogPath);
        
        if (!File.Exists(fullPath))
        {
            // Try as absolute path
            fullPath = catalogPath;
        }

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"Catalog not found at: {fullPath}");
            return;
        }

        try
        {
            string catalogJson = File.ReadAllText(fullPath);
            _decodedCatalog = CatalogDecoder.DecodeCatalogJson(catalogJson);
            Debug.Log($"<color=green>Catalog decoded successfully!</color> {_decodedCatalog.Entries.Count} entries found.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to decode catalog: {e.Message}");
        }
    }

    private void PrintSummary()
    {
        CatalogDecoder.PrintCatalogSummary(_decodedCatalog);
    }

    private void PrintAllEntries()
    {
        Debug.Log("═══════════════════════════════════════════════════════════════");
        Debug.Log("ALL CATALOG ENTRIES");
        Debug.Log("═══════════════════════════════════════════════════════════════");

        foreach (var entry in _decodedCatalog.Entries)
        {
            string entryType = entry.IsBundle ? "[BUNDLE]" : 
                              entry.IsBundledAsset ? "[ASSET]" : 
                              entry.IsResource ? "[RESOURCE]" : "[OTHER]";
            
            string location = entry.IsRemote ? "(Remote)" : "(Local)";
            
            Debug.Log($"{entryType} {location} Entry[{entry.Index}]:\n" +
                     $"  InternalId: {entry.InternalId}\n" +
                     $"  Provider: {entry.ProviderId?.Split('.')[^1]}\n" +
                     $"  DepKeyIdx: {entry.DependencyKeyIndex}\n" +
                     $"  Type: {entry.ResourceTypeName ?? "N/A"}");
        }
    }

    private void PrintDependencyTree()
    {
        Debug.Log("═══════════════════════════════════════════════════════════════");
        Debug.Log("DEPENDENCY TREE");
        Debug.Log("═══════════════════════════════════════════════════════════════");

        // Print bundles first
        Debug.Log("\n<color=cyan>BUNDLES (these are loaded as dependencies):</color>");
        var bundles = _decodedCatalog.GetBundleEntries();
        foreach (var bundle in bundles)
        {
            string location = bundle.IsRemote ? "<color=orange>[REMOTE]</color>" : "<color=green>[LOCAL]</color>";
            Debug.Log($"  {location} {bundle.InternalId}");
        }

        // Print dependency groups
        Debug.Log("\n<color=yellow>DEPENDENCY GROUPS:</color>");
        Debug.Log("(Assets are grouped by which bundles they depend on)\n");

        foreach (var kvp in _decodedCatalog.DependencyGroups)
        {
            Debug.Log($"<color=cyan>━━━ Dependency Group: DepKeyIdx = {kvp.Key} ━━━</color>");
            Debug.Log($"    Contains {kvp.Value.Count} entries:");
            
            int shown = 0;
            foreach (var entry in kvp.Value)
            {
                if (shown >= 5 && kvp.Value.Count > 5)
                {
                    Debug.Log($"    ... and {kvp.Value.Count - 5} more entries");
                    break;
                }
                
                string providerShort = entry.ProviderId?.Split('.')[^1] ?? "Unknown";
                Debug.Log($"    → [{entry.Index}] {entry.InternalId} ({providerShort})");
                shown++;
            }
            Debug.Log("");
        }
    }

    /// <summary>
    /// Example: Find what bundles are needed to load a specific asset.
    /// </summary>
    public void FindDependenciesForAsset(string assetPath)
    {
        if (_decodedCatalog == null)
        {
            Debug.LogError("Catalog not loaded!");
            return;
        }

        var matches = _decodedCatalog.FindByInternalId(assetPath);
        
        if (matches.Count == 0)
        {
            Debug.LogWarning($"No entries found matching: {assetPath}");
            return;
        }

        Debug.Log($"<color=green>Found {matches.Count} matching entries for: {assetPath}</color>");
        
        foreach (var entry in matches)
        {
            Debug.Log($"\nEntry[{entry.Index}]: {entry.InternalId}");
            Debug.Log($"  Provider: {entry.ProviderId?.Split('.')[^1]}");
            Debug.Log($"  DepKeyIdx: {entry.DependencyKeyIndex}");
            
            if (entry.HasDependencies)
            {
                Debug.Log($"  <color=yellow>This asset requires bundles from dependency group {entry.DependencyKeyIndex}:</color>");
                
                // Find bundle entries that might be the dependency
                var bundles = _decodedCatalog.GetBundleEntries();
                foreach (var bundle in bundles)
                {
                    Debug.Log($"    → Potential bundle: {bundle.InternalId}");
                }
            }
            else
            {
                Debug.Log($"  <color=green>This entry has no dependencies.</color>");
            }
        }
    }

    /// <summary>
    /// Example: List all remote bundles that need to be downloaded.
    /// </summary>
    public void ListRemoteBundles()
    {
        if (_decodedCatalog == null)
        {
            Debug.LogError("Catalog not loaded!");
            return;
        }

        Debug.Log("<color=cyan>═══ REMOTE BUNDLES ═══</color>");
        
        long totalSize = 0;
        int remoteCount = 0;
        
        foreach (var bundle in _decodedCatalog.GetBundleEntries())
        {
            if (bundle.IsRemote)
            {
                remoteCount++;
                Debug.Log($"  {bundle.InternalId}");
                
                // Try to find matching bundle options for size info
                foreach (var options in _decodedCatalog.BundleOptions)
                {
                    if (bundle.InternalId.Contains(options.Hash))
                    {
                        Debug.Log($"    Size: {options.BundleSize:N0} bytes, CRC: {options.Crc}");
                        totalSize += options.BundleSize;
                        break;
                    }
                }
            }
        }
        
        Debug.Log($"\n<color=yellow>Total: {remoteCount} remote bundles, {totalSize:N0} bytes ({totalSize / 1024.0 / 1024.0:F2} MB)</color>");
    }
}
