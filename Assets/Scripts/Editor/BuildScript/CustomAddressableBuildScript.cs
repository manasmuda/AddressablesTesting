using System;

using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public class CustomAddressableBuildScript
{
    private const string build_script = "Assets/AddressableAssetsData/DataBuilders/BuildScriptPackedMode.asset";

    static bool BuildAddressableContent()
    {
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        bool success = string.IsNullOrEmpty(result.Error);

        if(!success)
        {
            Debug.LogError("Addressables build error encountered: " + result.Error);
        }
        return success;
    }
    
    public static bool BuildAddressables(AddressableAssetSettings settings) {

        IDataBuilder builderScript = AssetDatabase.LoadAssetAtPath<ScriptableObject>(build_script) as IDataBuilder;

        if (builderScript == null)
        {
            Debug.LogError(build_script + " couldn't be found or isn't a build script.");
            return false;
        }
        
        int index = settings.DataBuilders.IndexOf((ScriptableObject) builderScript);

        if (index > 0)
            settings.ActivePlayerDataBuilderIndex = index;
        else
            Debug.LogError($"{builderScript} must be added to the " + $"DataBuilders list before it can be made " + $"active. Using last run builder instead.");
       

        return BuildAddressableContent();
    }

    public static bool UpdateAddressablesBuild(AddressableAssetSettings settings) {
        var path = ContentUpdateScript.GetContentStateDataPath(true);
        if (!string.IsNullOrEmpty(path)) {
            var result = ContentUpdateScript.BuildContentUpdate(settings, path);
            if (!string.IsNullOrEmpty(result.Error)) {
                Debug.LogError(result.Error);
                return false;
            }
            return true;
        }

        return false;
    }
}