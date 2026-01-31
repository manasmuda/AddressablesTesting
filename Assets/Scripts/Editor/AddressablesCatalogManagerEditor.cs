using System;
using System.Collections.Generic;
using System.Reflection;
using ModestTree;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public class AddressablesCatalogManagerEditor : EditorWindow
{
    [MenuItem("Hitwicket/Addressables/Open Catalog Manager")]
    public static void ShowWindow() {
        GetWindow(typeof(AddressablesCatalogManagerEditor));
    }

    private List<string> _settingsPathList = new List<string>() {
        "Assets/AddressableAssetsData/AddressableAssetSettings.asset",
        "Assets/AddressableAssetsData/AddressableAssetSettings_WV.asset"
    };

    private List<AddressableAssetSettings> _settingsList;
    private List<string> _settingsNames;
    
    private string _selectedSettings;
    private string _selectedProfileName;

    private void Awake() {
        InitializeManager();
    }

    private void InitializeManager() {
        _settingsList = new List<AddressableAssetSettings>();
        _settingsNames = new List<string>();
        foreach (var path in _settingsPathList) {
            AddressableAssetSettings settings = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(path);
            if (settings != null) {
                _settingsList.Add(settings);
                _settingsNames.Add(settings.name);
            }
        }
        _selectedSettings = DefaultAddressableAssetSettings.name;
        _selectedProfileName = DefaultAddressableAssetSettings.profileSettings.GetProfileName(DefaultAddressableAssetSettings.activeProfileId);
    }

    private AddressableAssetSettings DefaultAddressableAssetSettings => AddressableAssetSettingsDefaultObject.Settings;

    private int DefaultSettingsId => _settingsNames.IndexOf(DefaultAddressableAssetSettings.name);

    private int SelectedSettingsIndex => _settingsNames.IndexOf(_selectedSettings);

    private AddressableAssetSettings SelectedSettings => _settingsList[SelectedSettingsIndex];

    
    private string[] SettingsProfiles => SelectedSettings.profileSettings.GetAllProfileNames().ToArray();
    private string ActiveProfileId => SelectedSettings.activeProfileId;
    private string ActiveProfileName => SelectedSettings.profileSettings.GetProfileName(ActiveProfileId);
    private int ActiveProfileIndex => SettingsProfiles.IndexOf(ActiveProfileName);
    private int SelectedProfileIndex => SettingsProfiles.IndexOf(_selectedProfileName);
    private string SelectedProfileId => SelectedSettings.profileSettings.GetProfileId(_selectedProfileName);
    
    
    private void SetSelectedSettings(int index) {
        int prevIndex = SelectedSettingsIndex;
        _selectedSettings = _settingsNames[index];
        if (prevIndex != SelectedSettingsIndex) {
            UpdateSelectedProfileBySettings();
        }
    }

    private void SetSelectedProfile(int index) {
        _selectedProfileName = SettingsProfiles[index];
    }

    private void UpdateSelectedProfileBySettings() {
        _selectedProfileName = SelectedSettings.profileSettings.GetProfileName(SelectedSettings.activeProfileId);
    }
    
    private void OnGUI() {
        EditorGUILayout.LabelField("Default Addressable Settings: "+ DefaultAddressableAssetSettings.name, EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Settings Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space();
       
        int selectedSettings = EditorGUILayout.Popup("Available Settings", SelectedSettingsIndex, _settingsNames.ToArray());
        SetSelectedSettings(selectedSettings);
        
        int selectedProfile = EditorGUILayout.Popup("Profiles", SelectedProfileIndex, SettingsProfiles);
        SetSelectedProfile(selectedProfile);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Default")) {
            SetSelectedAsDefault();
        }

        if (GUILayout.Button("Build Addressables")) {
            FixAllAddressableSettings();
            BuildSelectedSettings();
        }

        if (GUILayout.Button("Update")) {
            FixAllAddressableSettings();
            UpdateSelectedSettings();
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Fix All Settings")) {
            FixAllAddressableSettings();
        }
        
        EditorGUILayout.Space();

        if (GUILayout.Button("Open Default Settings")) {
            SetSelectedAsDefault();
        }

        RepaintAddressablesWindow();
    }

    private void SetSelectedAsDefault() {
        string actualProfileId = SelectedSettings.activeProfileId;
        SelectedSettings.activeProfileId = SelectedProfileId;
        int actualDefaultId = DefaultSettingsId;
        AddressableAssetSettingsDefaultObject.Settings = SelectedSettings;
        AssetDatabase.Refresh();
        if (actualDefaultId != DefaultSettingsId || actualProfileId!=SelectedProfileId) {
            UpdateAddressabledWindow();
        }
    }

    private void BuildSelectedSettings() {
        
        var actualProfileId = SelectedSettings.activeProfileId;
        SelectedSettings.activeProfileId = SelectedProfileId;
        
        var actualDefaultSettings = DefaultAddressableAssetSettings;
        AddressableAssetSettingsDefaultObject.Settings = SelectedSettings;
        
        AssetDatabase.Refresh();

        bool sucess = CustomAddressableBuildScript.BuildAddressables(SelectedSettings);
        Debug.Log("Build Completed:" + sucess);
        
        AddressableAssetSettingsDefaultObject.Settings = actualDefaultSettings;
        SelectedSettings.activeProfileId = actualProfileId;
        
        AssetDatabase.Refresh();
        
        Repaint();
    }
    
    private void UpdateSelectedSettings() {
        
        var actualProfileId = SelectedSettings.activeProfileId;
        SelectedSettings.activeProfileId = SelectedProfileId;
        
        var actualDefaultSettings = DefaultAddressableAssetSettings;
        AddressableAssetSettingsDefaultObject.Settings = SelectedSettings;

        AssetDatabase.Refresh();

        bool sucess = CustomAddressableBuildScript.UpdateAddressablesBuild(SelectedSettings);
        Debug.Log("Update Completed:" + sucess);
        
        AddressableAssetSettingsDefaultObject.Settings = actualDefaultSettings;
        SelectedSettings.activeProfileId = actualProfileId;
        
        AssetDatabase.Refresh();
        Repaint();
    }

    private void RepaintAddressablesWindow() {
        Assembly assembly = typeof(UnityEditor.AddressableAssets.GUI.AnalyzeWindow).Assembly;
        //Debug.LogError(assembly.GetName());
        Type type = assembly.GetType("UnityEditor.AddressableAssets.GUI.AddressableAssetsWindow");
        //Debug.LogError(type);
        UnityEngine.Object[] objectsOfTypeAll = Resources.FindObjectsOfTypeAll(type);
        if (objectsOfTypeAll != null && objectsOfTypeAll.Length != 0) {
            GetWindow(type).Repaint();
        } else {
            Debug.Log("Not open");
        }
    }

    private void UpdateAddressabledWindow() {
        Assembly assembly = typeof(UnityEditor.AddressableAssets.GUI.AnalyzeWindow).Assembly;
        //Debug.LogError(assembly.GetName());
        Type type = assembly.GetType("UnityEditor.AddressableAssets.GUI.AddressableAssetsWindow");
        //Debug.LogError(type);
        UnityEngine.Object[] objectsOfTypeAll = Resources.FindObjectsOfTypeAll(type);
        if (objectsOfTypeAll != null && objectsOfTypeAll.Length != 0) {
            GetWindow(type).Close();
            GetWindow(type).Show();
        } else {
            GetWindow(type).Show();
            Debug.Log("Not open");
        }
    }

    private void FixAllAddressableSettings() {
        foreach (var settings in _settingsList) {
            if (FixAddressableSettings(settings)) {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssetIfDirty(settings);
                Debug.Log(settings.name+" is updated");
            }
        }
    }
    
    private static bool FixAddressableSettings(AddressableAssetSettings settings) {
        List<AddressableAssetGroup> groupsToRemove = new List<AddressableAssetGroup>();
        foreach (var group in settings.groups) {
            if (group.Settings.name != settings.name) {
                Debug.LogError(group.name + " is not part of "+settings.name);
                groupsToRemove.Add(group);
            }
        }

        foreach (var group in groupsToRemove) {
            settings.groups.Remove(group);
        }

        return groupsToRemove.Count > 0;
    }


}
