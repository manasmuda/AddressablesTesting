using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine.U2D;

public class AddressableGroupDependenciesViewer : EditorWindow {
    public List<AddressableAssetGroup> properties;
    public List<string> all_asset_bundles;

    List<AssetBundleDependency> dependencies = new List<AssetBundleDependency>();

    List<SpriteAtlas> all_sprite_atlas = new List<SpriteAtlas>();

    // Dependency Group Type
    int selected_dependency_group_type = 0;
    string[] dependency_group_types = new string[3] { "Group By Bundles", "Group By Addressable Groups", "No Grouping" };

    //UI Related properties
    Vector2 dependencies_scroll_position = Vector2.zero;
    Vector2 asset_bundles_scroll_position = Vector2.zero;
    bool show_all_asset_bundles = false;
    bool check_sprite_atlas_dependencies = true;
    bool show_groups = false;
    bool check_bundle_loop = false;

    //Custon Styles;
    private GUIStyle red_color_text;
    private GUIStyle green_color_text;

    [MenuItem("Window/Custom Tools/Addressable Dependencies/AddressableGroupDependenciesViewer")]
    public static void ShowWindow() {
        //Show existing window instance. If one doesn't exist, make one.
        EditorWindow.GetWindow(typeof(AddressableGroupDependenciesViewer));
    }

    private void Awake() {
        red_color_text = new GUIStyle(EditorStyles.label);
        red_color_text.normal.textColor = Color.red;

        green_color_text = new GUIStyle(EditorStyles.label);
        green_color_text.normal.textColor = Color.green;

        all_asset_bundles = GetAllAssetBundles();

        UpdateAllSpriteAtlases();

        InitializePanels();
    }

    private void InitializePanels() {
        properties = new List<AddressableAssetGroup>();
        properties.Add(CreateInstance<AddressableAssetGroup>());
        dependencies = new List<AssetBundleDependency>();
    }

    void OnGUI() {

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Fetch Dependent Asset Bundles of ", EditorStyles.label);
        if(GUILayout.Button("+", GUILayout.MaxWidth(25f))) {
            AddNewProperty();
        }
        EditorGUILayout.EndHorizontal();

        for(int i = 0; i < properties.Count; i++) {
            AddressableAssetGroup property = properties[i];
            EditorGUILayout.BeginHorizontal();

            properties[i] = (AddressableAssetGroup) EditorGUILayout.ObjectField(property, typeof(AddressableAssetGroup), true);
            if(i > 0) {
                if(GUILayout.Button("-", GUILayout.MaxWidth(25f))) {
                    RemoveProperty(property);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        check_sprite_atlas_dependencies = EditorGUILayout.ToggleLeft("Check Sprite Atlas Dependencies", check_sprite_atlas_dependencies);
        check_bundle_loop = EditorGUILayout.ToggleLeft("Check Bundle Dependencies", check_bundle_loop);

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        if(GUILayout.Button("Fetch", GUILayout.Height(30f))) {
            dependencies_scroll_position = Vector2.zero;
            UpdateAllDependencies();
        }
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button("Reset")) {
            InitializePanels();
        }
        if(GUILayout.Button("List All Bundles")) {
            asset_bundles_scroll_position = Vector2.zero;
            UpdateAllAssetBundles();
        }
        EditorGUILayout.EndHorizontal();
        if(dependencies.Count > 0) {
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dependent Asset Bundles (" + dependencies.GroupBy(x => x.bundle_name).Select(g => g.Key).Count() + ")", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            show_groups = EditorGUILayout.ToggleLeft("Show Groups", show_groups);
            EditorGUILayout.Space();
            if(show_groups) {
                selected_dependency_group_type = GUILayout.SelectionGrid(selected_dependency_group_type, dependency_group_types, 3, EditorStyles.radioButton);
                EditorGUILayout.Space();
                dependencies_scroll_position = EditorGUILayout.BeginScrollView(dependencies_scroll_position, GUILayout.MaxHeight(200), GUILayout.MinHeight(0));
                float width = EditorGUIUtility.currentViewWidth;
                if(selected_dependency_group_type == 0) {
                    dependencies.GroupBy(x => x.bundle_name, (key, g) => new { bundle_name = key, asset_references = g.Select(s => s.group_name).ToList() })
                        .ToList()
                        .ForEach((bundle_group) => {
                            EditorGUILayout.BeginHorizontal("box");
                            EditorGUILayout.LabelField(bundle_group.bundle_name, EditorStyles.wordWrappedLabel, GUILayout.Width(width / 2));
                            EditorGUILayout.LabelField(string.Join(",\n", bundle_group.asset_references), EditorStyles.wordWrappedLabel, GUILayout.Width(width / 2));
                            EditorGUILayout.EndHorizontal();
                        });
                } else if(selected_dependency_group_type == 1) {
                    dependencies.GroupBy(x => x.group_name, (key, g) => new { bundle_names = g.Select(s => s.bundle_name).ToList(), asset_reference = key })
                        .ToList()
                        .ForEach((asset_reference_group) => {
                            EditorGUILayout.BeginHorizontal("box");
                            EditorGUILayout.LabelField(asset_reference_group.asset_reference, EditorStyles.wordWrappedLabel, GUILayout.Width(width / 2));
                            EditorGUILayout.LabelField(string.Join(",\n", asset_reference_group.bundle_names), EditorStyles.wordWrappedLabel, GUILayout.Width(width / 2));
                            EditorGUILayout.EndHorizontal();
                        });
                } else {
                    for(int i = 0; i < dependencies.Count; i++) {
                        EditorGUILayout.BeginHorizontal("box");
                        EditorGUILayout.LabelField(dependencies[i].bundle_name, EditorStyles.wordWrappedLabel, GUILayout.Width(width / 2));
                        EditorGUILayout.LabelField(dependencies[i].group_name, EditorStyles.wordWrappedLabel, GUILayout.Width(width / 2));
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndScrollView();
            } else {
                dependencies_scroll_position = EditorGUILayout.BeginScrollView(dependencies_scroll_position, GUILayout.MaxHeight(200), GUILayout.MinHeight(0));
                dependencies.Select(x => x.bundle_name).Distinct<string>().ToList().ForEach(bundle => {
                    EditorGUILayout.BeginHorizontal("box");
                    EditorGUILayout.LabelField(bundle);
                    EditorGUILayout.EndHorizontal();
                });
                dependencies_scroll_position = EditorGUILayout.BeginScrollView(dependencies_scroll_position, GUILayout.MaxHeight(200), GUILayout.MinHeight(0));
            }
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }
        EditorGUILayout.Space();
        show_all_asset_bundles = EditorGUILayout.BeginToggleGroup("Show All Asset Bundles", show_all_asset_bundles);
        if(show_all_asset_bundles) {
            if(all_asset_bundles.Count > 0) {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Total Asset Bundles (" + all_asset_bundles.Count + ")", EditorStyles.boldLabel);
                EditorGUILayout.Space();
                asset_bundles_scroll_position = EditorGUILayout.BeginScrollView(asset_bundles_scroll_position, GUILayout.MaxHeight(200), GUILayout.MinHeight(0));
                for(int i = 0; i < all_asset_bundles.Count; i++) {
                    EditorGUILayout.BeginHorizontal();
                    if(dependencies.Select(g => g.bundle_name).Distinct().ToList().Contains(all_asset_bundles[i])) {
                        EditorGUILayout.LabelField(all_asset_bundles[i], green_color_text);
                    } else {
                        EditorGUILayout.LabelField(all_asset_bundles[i], red_color_text);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
        }
        EditorGUILayout.EndToggleGroup();
    }

    private void AddNewProperty() {
        properties.Add(CreateInstance<AddressableAssetGroup>());
        Repaint();
    }

    private void RemoveProperty(AddressableAssetGroup property) {
        properties.Remove(property);
        Repaint();
    }

    #region Getting Asset Bundle Dependencies of Asset Reference

    private void UpdateAllDependencies() {
        dependencies = new List<AssetBundleDependency>();
        List<string> groups = new List<string>();
        for(int i = 0; i < properties.Count; i++) {
            groups.Add(properties[i].Name);
        }
        int loop_count = 0;
        while(groups.Count != 0) {
            groups = UpdateDependenciesForGroups(groups);
            loop_count++;
            if(!check_bundle_loop || loop_count > 100) {
                Debug.LogError("Dependecies loop crossed 100");
                break;
            }
        }
        dependencies = dependencies.Distinct().ToList();
        Repaint();
    }

  
    private List<string> UpdateDependenciesForGroups(List<string> groups) {
        // If this is a label, we want to go and grab all the assets with that label, either way we want to start going through all the entries
        AddressableAssetSettings aaSettings = AddressableAssetSettingsDefaultObject.Settings;
        //Debug.Log("<color=yellow>Total Groups:"+aaSettings.groups.Count+"</color>");
        List<string> combined_dependecies = new List<string>();
        var assetsToCheckList = new List<string>();
        foreach(AddressableAssetGroup addressableGroup in aaSettings.groups) {
            if(groups.Contains(addressableGroup.Name)) {
                
                // Debug.Log("<color=green>Group Name:"+addressableGroup.Name+" contains "+addressableGroup.entries.Count+" addressable assets</color>");
                foreach(AddressableAssetEntry groupEntry in addressableGroup.entries) {
                    //Debug.Log("Group Entry Name:"+groupEntry.address);
                    // We check to see if the key is a label for any assets or if the key is an address of anything
                    // If it is, we add it to our assets to check list so we can use those to build our dependencies


                    if(!assetsToCheckList.Contains(groupEntry.AssetPath))
                        assetsToCheckList.Add(groupEntry.AssetPath);
                }

                var dependenciesList = new List<string>();
                foreach(string assetPath in assetsToCheckList) {
                    List<string> childDependencies = GetAddressableDependenciesByPath(assetPath);
                    dependenciesList = dependenciesList.Union(childDependencies).ToList();
                }
                combined_dependecies.AddRange(dependenciesList);
                dependenciesList.ForEach(x => dependencies.Add(new AssetBundleDependency() { bundle_name = x, group_name = addressableGroup.Name }));
            }
            // Debug.LogError("Assets to Check:" + assetsToCheckList.Count);
            //Debug.LogError("Partial Dependcies:" + dependenciesList.Count);

        }
        return combined_dependecies.Distinct().ToList();
    }

    private List<string> GetAddressableDependenciesByPath(string filePath) {
        AddressableAssetSettings aaSettings = AddressableAssetSettingsDefaultObject.Settings;
        string[] deps = AssetDatabase.GetDependencies(filePath, true);
        var dependenciesList = new List<string>();

        foreach(string dep in deps) {
            //Debug.LogError(dep);
            if(check_sprite_atlas_dependencies) {
                List<string> sprite_atlas_bundles = GetSpriteAtlasDepencyBundles(dep);
                foreach(string atlas_bunle in sprite_atlas_bundles) {
                    if(!dependenciesList.Contains(atlas_bunle))
                        dependenciesList.Add(atlas_bunle);
                }
            }
            string guid = AssetDatabase.AssetPathToGUID(dep);
            AddressableAssetEntry entry = aaSettings.FindAssetEntry(guid);
            if(entry == null)
                continue;

            string bundleFileName = CheckAndGetBundleName(entry);
            if(!dependenciesList.Contains(bundleFileName))
                dependenciesList.Add(bundleFileName);
        }

        return dependenciesList;
    }

    private List<string> GetSpriteAtlasDepencyBundles(string asset_path) {
        List<string> sprite_atlas_bundles = new List<string>();
        if(CheckAndGetSprite(asset_path, out Sprite sprite)) {
            //Debug.LogError(asset_path);
            List<SpriteAtlas> spriteAtlases = GetDependentSpriteAtlas(sprite);
            foreach(SpriteAtlas atlas in spriteAtlases) {
                //Debug.LogError(atlas);
                string atlas_guid = GetGUIDOfObject(atlas);
                if(!string.IsNullOrEmpty(atlas_guid)) {
                    AddressableAssetSettings aaSettings = AddressableAssetSettingsDefaultObject.Settings;
                    AddressableAssetEntry entry = aaSettings.FindAssetEntry(atlas_guid);
                    if(entry == null)
                        continue;
                    string bundleFileName = CheckAndGetBundleName(entry);
                    if(!sprite_atlas_bundles.Contains(bundleFileName))
                        sprite_atlas_bundles.Add(bundleFileName);
                }
            }
        }
        return sprite_atlas_bundles;
    }

    private static bool CheckAndGetSprite(string assetPath, out Sprite sprite) {
        sprite = null;
        if(assetPath != null && assetPath.Contains(".png")) {
            try {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if(sprite != null) {
                    return true;
                }
            } catch(System.Exception e) {
                Debug.LogError(e);
            }
        }
        return false;
    }

    private List<SpriteAtlas> GetDependentSpriteAtlas(Sprite sprite) {
        List<SpriteAtlas> dependent_sprite_atlas = new List<SpriteAtlas>();
        for(int i = 0; i < all_sprite_atlas.Count; i++) {
            if(all_sprite_atlas[i].CanBindTo(sprite)) {
                dependent_sprite_atlas.Add(all_sprite_atlas[i]);
            }
        }

        return dependent_sprite_atlas;
    }

    private static string GetGUIDOfObject(Object obj) {
        string asset_path = AssetDatabase.GetAssetPath(obj);
        return AssetDatabase.AssetPathToGUID(asset_path);
    }

    private static string CheckAndGetBundleName(AddressableAssetEntry assetEntry) {
        if(assetEntry != null) {
            BundledAssetGroupSchema group_bundle_schema = assetEntry.parentGroup.GetSchema<BundledAssetGroupSchema>();
            if(group_bundle_schema != null) {
                if(group_bundle_schema.BundleMode == BundledAssetGroupSchema.BundlePackingMode.PackTogether) {
                    return GetBundleNameTypeBundledTogether(assetEntry.parentGroup);
                } else if(group_bundle_schema.BundleMode == BundledAssetGroupSchema.BundlePackingMode.PackSeparately) {
                    return GetBundleNameTypeBundledSeperately(assetEntry);
                } else if(group_bundle_schema.BundleMode == BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel) {
                    return GetBundleNameTypeBundledTogether(assetEntry.parentGroup); //TODO: Change it later, not used now
                }

            }
        }
        return "";
    }

    private static string GetBundleNameTypeBundledTogether(AddressableAssetGroup assetGroup) {
        return assetGroup.Name;// +"_bundle";
    }

    private static string GetBundleNameTypeBundledSeperately(AddressableAssetEntry assetEntry) {
        return assetEntry.parentGroup.Name + "_" + assetEntry.address;// +"_bundle";
    }

    private int GetTotalAssetBundlesCount() {
        return all_asset_bundles.Count;
    }

    private void UpdateAllAssetBundles() {
        all_asset_bundles = GetAllAssetBundles();
        Repaint();
    }

    private List<string> GetAllAssetBundles() {

        List<string> asset_bundles = new List<string>();
        AddressableAssetSettings aaSettings = AddressableAssetSettingsDefaultObject.Settings;

        foreach(AddressableAssetGroup addressableGroup in aaSettings.groups) {
            if(addressableGroup.entries.Count > 0) {
                BundledAssetGroupSchema group_bundle_schema = addressableGroup.GetSchema<BundledAssetGroupSchema>();
                if(group_bundle_schema != null) {
                    if(group_bundle_schema.BundleMode == BundledAssetGroupSchema.BundlePackingMode.PackTogether) {
                        asset_bundles.Add(GetBundleNameTypeBundledTogether(addressableGroup));
                    } else if(group_bundle_schema.BundleMode == BundledAssetGroupSchema.BundlePackingMode.PackSeparately) {
                        foreach(AddressableAssetEntry groupEntry in addressableGroup.entries) {
                            asset_bundles.Add(GetBundleNameTypeBundledSeperately(groupEntry));
                        }
                    }
                }
            }
        }
        asset_bundles = asset_bundles.Distinct().ToList();
        return asset_bundles;
    }

    private void UpdateAllSpriteAtlases() {
        var atlasesGUID = AssetDatabase.FindAssets("t:spriteatlas");
        all_sprite_atlas = new List<SpriteAtlas>();
        foreach(var atlasGUID in atlasesGUID) {
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(atlasGUID));
            all_sprite_atlas.Add(atlas);
        }
    }

    #endregion

    #region Serialized Property to System.Object

    public static object GetTargetObjectOfProperty(SerializedProperty prop) {
        var path = prop.propertyPath.Replace(".Array.data[", "[");
        object obj = prop.serializedObject.targetObject;
        var elements = path.Split('.');
        foreach(var element in elements) {
            if(element.Contains("[")) {
                var elementName = element.Substring(0, element.IndexOf("[", System.StringComparison.Ordinal));
                var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[", System.StringComparison.Ordinal)).Replace("[", "").Replace("]", ""));
                obj = GetValue_Imp(obj, elementName, index);
            } else {
                obj = GetValue_Imp(obj, element);
            }
        }
        return obj;
    }

    /// <summary>
    /// Gets the object that the property is a member of
    /// </summary>
    /// <param name="prop"></param>
    /// <returns></returns>
    public static object GetTargetObjectWithProperty(SerializedProperty prop) {
        if(prop == null) return null;

        var path = prop.propertyPath.Replace(".Array.data[", "[");
        object obj = prop.serializedObject.targetObject;
        var elements = path.Split('.');
        foreach(var element in elements.Take(elements.Length - 1)) {
            if(element.Contains("[")) {
                var elementName = element.Substring(0, element.IndexOf("[", System.StringComparison.Ordinal));
                var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[", System.StringComparison.Ordinal)).Replace("[", "").Replace("]", ""));
                obj = GetValue_Imp(obj, elementName, index);
            } else {
                obj = GetValue_Imp(obj, element);
            }
        }
        return obj;
    }

    private static object GetValue_Imp(object source, string name) {
        if(source == null)
            return null;
        var type = source.GetType();

        while(type != null) {
            var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if(f != null)
                return f.GetValue(source);

            var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if(p != null)
                return p.GetValue(source, null);

            type = type.BaseType;
        }
        return null;
    }

    private static object GetValue_Imp(object source, string name, int index) {
        if(!(GetValue_Imp(source, name) is System.Collections.IEnumerable enumerable)) return null;
        var enm = enumerable.GetEnumerator();
        //while (index-- >= 0)
        //    enm.MoveNext();
        //return enm.Current;

        for(int i = 0; i <= index; i++) {
            if(!enm.MoveNext()) return null;
        }
        return enm.Current;
    }

    #endregion

    public class AssetBundleDependency {
        public string bundle_name;
        public string group_name;
    }
}