using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "AddressableDownloadCatalog", menuName = "AddressableDownloadCatalog", order = 1)]
public class AddressablesDownloadCatalogSO : ScriptableObject {
    public List<AssetDownloadTriggerData> downloadTriggerData;

    public List<string> GetDownloadKeysForMRPLevel(long mrp_level) {
        List<string> download_keys = new List<string>();
        foreach (AssetDownloadTriggerData data in downloadTriggerData) {
            if (data.mrp_level <= mrp_level) {
                download_keys.Add(data.download_key);
            }
        }
        return download_keys;
    }

    

    
}

[System.Serializable]
public class AssetDownloadTriggerData : TriggerData {
    public long mrp_level;
}

[System.Serializable]
public class TriggerData {
    public string download_key;

}