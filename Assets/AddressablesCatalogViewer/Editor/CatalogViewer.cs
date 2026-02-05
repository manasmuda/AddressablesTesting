using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace AddressablesCatalogViewer.Editor
{
    /// <summary>
    /// Unity Editor window to visualize Addressables catalog using Unity's own deserialization.
    /// This is the CORRECT way to decode catalog files - using Unity's ContentCatalogData.CreateLocator().
    /// 
    /// The catalog binary format is complex and has multiple versions (Json v1/v2/v3, Binv1, Binv2).
    /// Rather than manually parsing, we use Unity's built-in deserializer.
    /// 
    /// For external tools (outside Unity), see:
    /// - https://github.com/nesrak1/AddressablesTools (C#)
    /// - https://github.com/anosu/AddressablesToolsPy (Python)
    /// </summary>
    public class CatalogViewer : EditorWindow
    {
        private string _catalogPath = "";
        private Vector2 _scrollPosition;
        private IResourceLocator _locator;
        private ContentCatalogData _catalogData;
        
        private List<LocationInfo> _allLocations = new List<LocationInfo>();
        private Dictionary<string, List<LocationInfo>> _bundleToAssets = new Dictionary<string, List<LocationInfo>>();
        private List<string> _allBundles = new List<string>();
        
        private bool _showBundles = true;
        private bool _showAssets = true;
        private bool _showDependencies = true;
        
        private string _searchFilter = "";
        private int _selectedBundleIndex = -1;
        private LocationInfo _selectedLocation;

        private class LocationInfo
        {
            public string PrimaryKey;
            public string InternalId;
            public string ProviderId;
            public Type ResourceType;
            public List<string> DependencyKeys = new List<string>();
            public IResourceLocation Location;
            
            public bool IsBundle => ProviderId?.Contains("AssetBundleProvider") == true;
            public bool IsBundledAsset => ProviderId?.Contains("BundledAssetProvider") == true;
            public bool IsRemote => InternalId?.StartsWith("http") == true;
            
            public string ProviderShortName => ProviderId?.Split('.').LastOrDefault() ?? "Unknown";
        }

        [MenuItem("Window/Addressables/Catalog Viewer (Correct)")]
        public static void ShowWindow()
        {
            var window = GetWindow<CatalogViewer>("Catalog Viewer");
            window.minSize = new Vector2(900, 600);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            DrawHeader();
            
            if (_locator == null)
            {
                EditorGUILayout.HelpBox(
                    "Load a catalog.json file to view its contents.\n\n" +
                    "This tool uses Unity's ContentCatalogData.CreateLocator() to properly deserialize the catalog, " +
                    "which handles all the binary format complexity correctly.\n\n" +
                    "For external tools (outside Unity), see:\n" +
                    "• https://github.com/nesrak1/AddressablesTools (C#)\n" +
                    "• https://github.com/anosu/AddressablesToolsPy (Python)", 
                    MessageType.Info);
                return;
            }

            DrawSearchAndFilters();
            
            EditorGUILayout.Space(5);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            if (_showBundles)
            {
                DrawBundles();
            }
            
            if (_showAssets)
            {
                DrawAssets();
            }
            
            if (_showDependencies && _selectedLocation != null)
            {
                DrawDependencyTree();
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Catalog JSON Path:", GUILayout.Width(120));
            _catalogPath = EditorGUILayout.TextField(_catalogPath);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFilePanel("Select Catalog JSON", "", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    _catalogPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Catalog", GUILayout.Height(30)))
            {
                LoadCatalog();
            }
            if (GUILayout.Button("Load Current Project Catalog", GUILayout.Height(30)))
            {
                LoadCurrentProjectCatalog();
            }
            if (GUILayout.Button("Export Report", GUILayout.Height(30)))
            {
                ExportReport();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSearchAndFilters()
        {
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            _searchFilter = EditorGUILayout.TextField(_searchFilter);
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                _searchFilter = "";
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _showBundles = GUILayout.Toggle(_showBundles, "Bundles", "Button");
            _showAssets = GUILayout.Toggle(_showAssets, "Assets", "Button");
            _showDependencies = GUILayout.Toggle(_showDependencies, "Dependency Tree", "Button");
            EditorGUILayout.EndHorizontal();
            
            // Stats
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Total Locations: {_allLocations.Count}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Bundles: {_allBundles.Count}", EditorStyles.miniLabel);
            int assetCount = _allLocations.Count(l => l.IsBundledAsset);
            EditorGUILayout.LabelField($"Bundled Assets: {assetCount}", EditorStyles.miniLabel);
            int remoteCount = _allLocations.Count(l => l.IsRemote);
            EditorGUILayout.LabelField($"Remote: {remoteCount}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void LoadCatalog()
        {
            if (string.IsNullOrEmpty(_catalogPath) || !File.Exists(_catalogPath))
            {
                EditorUtility.DisplayDialog("Error", "Please select a valid catalog.json file.", "OK");
                return;
            }

            try
            {
                string json = File.ReadAllText(_catalogPath);
                _catalogData = JsonUtility.FromJson<ContentCatalogData>(json);
                
                // Use Unity's own CreateLocator to properly deserialize
                _locator = _catalogData.CreateLocator();
                
                ProcessLocator();
                
                Debug.Log($"<color=green>Catalog loaded successfully!</color>\n" +
                         $"Locator ID: {_locator.LocatorId}\n" +
                         $"Total Keys: {_locator.Keys.Count()}\n" +
                         $"Bundles: {_allBundles.Count}\n" +
                         $"Locations: {_allLocations.Count}");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to load catalog: {e.Message}", "OK");
                Debug.LogException(e);
            }
        }

        private void LoadCurrentProjectCatalog()
        {
            try
            {
                // Try to get the current project's catalog from Addressables
                var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
                if (settings == null)
                {
                    EditorUtility.DisplayDialog("Error", "No Addressables settings found in this project.", "OK");
                    return;
                }

                // Look for catalog in the build path
                string buildPath = settings.RemoteCatalogBuildPath.GetValue(settings);
                if (string.IsNullOrEmpty(buildPath))
                {
                    buildPath = "Library/com.unity.addressables/aa/";
                }

                string[] catalogFiles = Directory.GetFiles(buildPath, "catalog*.json", SearchOption.AllDirectories);
                if (catalogFiles.Length == 0)
                {
                    // Try StreamingAssets
                    string streamingPath = Path.Combine(Application.streamingAssetsPath, "aa");
                    if (Directory.Exists(streamingPath))
                    {
                        catalogFiles = Directory.GetFiles(streamingPath, "catalog*.json", SearchOption.AllDirectories);
                    }
                }

                if (catalogFiles.Length == 0)
                {
                    EditorUtility.DisplayDialog("Info", 
                        "No built catalog found. Please build Addressables first.\n\n" +
                        "Go to: Window > Asset Management > Addressables > Groups\n" +
                        "Then: Build > New Build > Default Build Script", "OK");
                    return;
                }

                _catalogPath = catalogFiles[0];
                LoadCatalog();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to load project catalog: {e.Message}", "OK");
                Debug.LogException(e);
            }
        }

        private void ProcessLocator()
        {
            _allLocations.Clear();
            _bundleToAssets.Clear();
            _allBundles.Clear();

            foreach (var key in _locator.Keys)
            {
                if (_locator.Locate(key, typeof(object), out var locations))
                {
                    foreach (var loc in locations)
                    {
                        // Avoid duplicates
                        if (_allLocations.Any(l => l.InternalId == loc.InternalId && l.PrimaryKey == key?.ToString()))
                            continue;

                        var info = new LocationInfo
                        {
                            PrimaryKey = key?.ToString() ?? "",
                            InternalId = loc.InternalId,
                            ProviderId = loc.ProviderId,
                            ResourceType = loc.ResourceType,
                            Location = loc
                        };

                        // Get dependency keys
                        if (loc.Dependencies != null)
                        {
                            foreach (var dep in loc.Dependencies)
                            {
                                info.DependencyKeys.Add(dep.PrimaryKey?.ToString() ?? dep.InternalId);
                            }
                        }

                        _allLocations.Add(info);

                        // Track bundles
                        if (info.IsBundle)
                        {
                            if (!_allBundles.Contains(info.InternalId))
                            {
                                _allBundles.Add(info.InternalId);
                            }
                        }

                        // Map assets to their bundles
                        if (info.IsBundledAsset && info.DependencyKeys.Count > 0)
                        {
                            string bundleKey = info.DependencyKeys[0];
                            if (!_bundleToAssets.ContainsKey(bundleKey))
                                _bundleToAssets[bundleKey] = new List<LocationInfo>();
                            _bundleToAssets[bundleKey].Add(info);
                        }
                    }
                }
            }
        }

        private void DrawBundles()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Asset Bundles ({_allBundles.Count})", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox(
                "AssetBundles are the container files. Each bundle can contain multiple assets.\n" +
                "• GREEN = Local bundle (in StreamingAssets)\n" +
                "• ORANGE = Remote bundle (downloaded from URL)", 
                MessageType.Info);

            int localCount = 0, remoteCount = 0;
            
            foreach (var bundleId in _allBundles)
            {
                if (!string.IsNullOrEmpty(_searchFilter) && 
                    !bundleId.ToLower().Contains(_searchFilter.ToLower()))
                    continue;

                bool isRemote = bundleId.StartsWith("http");
                if (isRemote) remoteCount++; else localCount++;

                Color originalBg = GUI.backgroundColor;
                GUI.backgroundColor = isRemote ? new Color(1f, 0.8f, 0.6f) : new Color(0.6f, 1f, 0.6f);

                EditorGUILayout.BeginHorizontal("box");
                
                string label = isRemote ? "[REMOTE]" : "[LOCAL]";
                EditorGUILayout.LabelField(label, GUILayout.Width(70));
                
                // Show bundle name (shortened)
                string displayName = bundleId;
                if (displayName.Length > 80)
                {
                    displayName = "..." + displayName.Substring(displayName.Length - 77);
                }
                EditorGUILayout.SelectableLabel(displayName, GUILayout.Height(18));
                
                // Asset count in this bundle
                int assetCount = 0;
                foreach (var kvp in _bundleToAssets)
                {
                    if (kvp.Key.Contains(bundleId) || bundleId.Contains(kvp.Key))
                    {
                        assetCount = kvp.Value.Count;
                        break;
                    }
                }
                EditorGUILayout.LabelField($"({assetCount} assets)", GUILayout.Width(80));
                
                EditorGUILayout.EndHorizontal();
                GUI.backgroundColor = originalBg;
            }
            
            EditorGUILayout.LabelField($"Summary: {localCount} local, {remoteCount} remote", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawAssets()
        {
            EditorGUILayout.BeginVertical("box");
            
            int bundledAssetCount = _allLocations.Count(l => l.IsBundledAsset);
            EditorGUILayout.LabelField($"Bundled Assets ({bundledAssetCount})", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox(
                "These are assets loaded FROM bundles using BundledAssetProvider.\n" +
                "Click on an asset to see its dependencies in the Dependency Tree section.", 
                MessageType.Info);

            int displayed = 0;
            foreach (var loc in _allLocations.Where(l => l.IsBundledAsset))
            {
                if (!string.IsNullOrEmpty(_searchFilter))
                {
                    if (!loc.InternalId.ToLower().Contains(_searchFilter.ToLower()) &&
                        !loc.PrimaryKey.ToLower().Contains(_searchFilter.ToLower()))
                        continue;
                }

                if (displayed >= 100)
                {
                    EditorGUILayout.LabelField($"... and {bundledAssetCount - 100} more (use search to filter)");
                    break;
                }

                bool isSelected = _selectedLocation == loc;
                Color originalBg = GUI.backgroundColor;
                if (isSelected)
                    GUI.backgroundColor = Color.cyan;

                EditorGUILayout.BeginHorizontal("box");
                
                // Type indicator
                string typeName = loc.ResourceType?.Name ?? "?";
                EditorGUILayout.LabelField($"[{typeName}]", GUILayout.Width(100));
                
                // Primary key (address)
                if (GUILayout.Button(loc.PrimaryKey, EditorStyles.label))
                {
                    _selectedLocation = loc;
                }
                
                // Dependency count
                EditorGUILayout.LabelField($"({loc.DependencyKeys.Count} deps)", GUILayout.Width(70));
                
                EditorGUILayout.EndHorizontal();
                GUI.backgroundColor = originalBg;
                
                displayed++;
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawDependencyTree()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Dependency Tree", EditorStyles.boldLabel);

            if (_selectedLocation == null)
            {
                EditorGUILayout.HelpBox("Select an asset above to view its dependency tree.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // Show selected asset info
            EditorGUILayout.BeginVertical("box");
            GUI.color = Color.cyan;
            EditorGUILayout.LabelField("Selected Asset:", EditorStyles.boldLabel);
            GUI.color = Color.white;
            
            EditorGUILayout.LabelField($"Address: {_selectedLocation.PrimaryKey}");
            EditorGUILayout.LabelField($"InternalId: {_selectedLocation.InternalId}");
            EditorGUILayout.LabelField($"Provider: {_selectedLocation.ProviderShortName}");
            EditorGUILayout.LabelField($"Type: {_selectedLocation.ResourceType?.FullName ?? "Unknown"}");
            EditorGUILayout.EndVertical();

            // Show dependencies
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Dependencies ({_selectedLocation.DependencyKeys.Count}):", EditorStyles.boldLabel);
            
            if (_selectedLocation.Location?.Dependencies != null)
            {
                DrawDependenciesRecursive(_selectedLocation.Location.Dependencies, 0);
            }
            else if (_selectedLocation.DependencyKeys.Count > 0)
            {
                foreach (var depKey in _selectedLocation.DependencyKeys)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField($"→ {depKey}");
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                EditorGUILayout.LabelField("No dependencies", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDependenciesRecursive(IList<IResourceLocation> dependencies, int depth)
        {
            if (dependencies == null || depth > 5) return;

            foreach (var dep in dependencies)
            {
                EditorGUI.indentLevel = depth + 1;
                
                string providerShort = dep.ProviderId?.Split('.').LastOrDefault() ?? "?";
                bool isBundle = dep.ProviderId?.Contains("AssetBundleProvider") == true;
                
                Color original = GUI.color;
                if (isBundle)
                {
                    GUI.color = dep.InternalId.StartsWith("http") ? 
                        new Color(1f, 0.7f, 0.5f) : new Color(0.5f, 1f, 0.5f);
                }
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"→ [{providerShort}]", GUILayout.Width(150));
                
                string displayId = dep.InternalId;
                if (displayId.Length > 60)
                    displayId = "..." + displayId.Substring(displayId.Length - 57);
                EditorGUILayout.LabelField(displayId);
                EditorGUILayout.EndHorizontal();
                
                GUI.color = original;

                // Recurse into sub-dependencies
                if (dep.Dependencies != null && dep.Dependencies.Count > 0)
                {
                    DrawDependenciesRecursive(dep.Dependencies, depth + 1);
                }
            }
            
            EditorGUI.indentLevel = 0;
        }

        private void ExportReport()
        {
            if (_locator == null)
            {
                EditorUtility.DisplayDialog("Error", "Please load a catalog first.", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel("Export Catalog Report", "", "catalog_report", "txt");
            if (string.IsNullOrEmpty(path)) return;

            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
                writer.WriteLine("ADDRESSABLES CATALOG REPORT");
                writer.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
                writer.WriteLine($"Generated: {DateTime.Now}");
                writer.WriteLine($"Locator ID: {_locator.LocatorId}");
                writer.WriteLine($"Total Locations: {_allLocations.Count}");
                writer.WriteLine($"Total Bundles: {_allBundles.Count}");
                writer.WriteLine();

                // Bundles section
                writer.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
                writer.WriteLine("ASSET BUNDLES");
                writer.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
                
                foreach (var bundle in _allBundles)
                {
                    string location = bundle.StartsWith("http") ? "[REMOTE]" : "[LOCAL]";
                    writer.WriteLine($"{location} {bundle}");
                }
                writer.WriteLine();

                // Assets section
                writer.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
                writer.WriteLine("ASSETS WITH DEPENDENCIES");
                writer.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
                
                foreach (var loc in _allLocations.Where(l => l.IsBundledAsset))
                {
                    writer.WriteLine($"\nAsset: {loc.PrimaryKey}");
                    writer.WriteLine($"  InternalId: {loc.InternalId}");
                    writer.WriteLine($"  Provider: {loc.ProviderShortName}");
                    writer.WriteLine($"  Type: {loc.ResourceType?.Name ?? "Unknown"}");
                    
                    if (loc.DependencyKeys.Count > 0)
                    {
                        writer.WriteLine($"  Dependencies:");
                        foreach (var dep in loc.DependencyKeys)
                        {
                            writer.WriteLine($"    → {dep}");
                        }
                    }
                }

                // Dependency summary
                writer.WriteLine();
                writer.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
                writer.WriteLine("BUNDLE → ASSETS MAPPING");
                writer.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
                
                foreach (var kvp in _bundleToAssets.OrderByDescending(k => k.Value.Count))
                {
                    writer.WriteLine($"\nBundle: {kvp.Key}");
                    writer.WriteLine($"  Contains {kvp.Value.Count} assets:");
                    foreach (var asset in kvp.Value.Take(20))
                    {
                        writer.WriteLine($"    - {asset.PrimaryKey}");
                    }
                    if (kvp.Value.Count > 20)
                    {
                        writer.WriteLine($"    ... and {kvp.Value.Count - 20} more");
                    }
                }
            }

            EditorUtility.DisplayDialog("Success", $"Report exported to:\n{path}", "OK");
            EditorUtility.RevealInFinder(path);
        }
    }
}
