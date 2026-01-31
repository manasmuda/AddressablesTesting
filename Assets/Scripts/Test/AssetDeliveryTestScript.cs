using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;


public class AssetDeliveryTestScript : MonoBehaviour {

    public Button download_everything_button, check_references_button, check_updates_button, clear_cache_button, clear_logs_button, update_catalogs_button;
    public Text logs_text;
    public List<AssetReference> references;
    public AssetReference ripple;

    public const int BUNDLE_VERSION = 11;

    // Start is called before the first frame update
    void Start() {
        download_everything_button.onClick.AddListener(delegate () {
            StartCoroutine(StartDownload("Remote"));
        });
        clear_logs_button.onClick.AddListener(delegate () {
            ClearLog();
        });
        check_updates_button.onClick.AddListener(delegate () {
            CheckForUpdate();
        });
        clear_cache_button.onClick.AddListener(delegate () {
            ClearCache("Remote");
        });
        check_references_button.onClick.AddListener(delegate () {
            StartCoroutine(CheckReferences());
        });
        update_catalogs_button.onClick.AddListener(delegate () {
            StartCoroutine(UpdateCatalog());
        });
       // Ripple();
    }

    private async void Ripple() {
        await ripple.LoadAssetAsync<GameObject>().Task;
        await ripple.InstantiateAsync(transform).Task;
    }

    private void CheckForUpdate() {
        ClearLog();
        AsyncOperationHandle<List<string>> operation = Addressables.CheckForCatalogUpdates();
        Log("Checking for updates..");
        operation.Completed += (handle) => {
            if(handle.Result != null && handle.Result.Count > 0) {
                foreach(string catelog in handle.Result) {
                    Log(catelog);
                }
            }
            Log("Update check done");
        };
    }

    private IEnumerator UpdateCatalog() {
        ClearLog();
        Log("Updating Catalog");
        AsyncOperationHandle<List<IResourceLocator>> updateHandle = Addressables.UpdateCatalogs();
        yield return updateHandle;
        Log("Done");
        if(updateHandle.Result != null && updateHandle.Result.Count > 0) {
            for(int i = 0; i < updateHandle.Result.Count; i++) {
                Log(updateHandle.Result[i].ToString());
            }
        } else {
            Log("No Updates");
        }
    }

    private IEnumerator CheckReferences() {
        ClearLog();
        for(int i = 0; i < references.Count; i++) {
            AssetReference reference = references[i];
            if(reference.Asset != null) {
                Log(reference.Asset.name + " is loaded");
                reference.InstantiateAsync(transform).Completed += (op) => {
                    Log(reference.Asset.name + " is instantiated");
                };
            } else if(reference.OperationHandle.IsValid() && reference.OperationHandle.IsDone) {
                Log(reference.ToString() + " is already loaded but not found");
            } else {
                float init_time = Time.realtimeSinceStartup;
                AsyncOperationHandle<GameObject> loader = reference.LoadAssetAsync<GameObject>();
                yield return loader;
                float time_taken = (Time.realtimeSinceStartup - init_time);
                if(loader.IsDone) {
                    if(reference.Asset != null) {
                        Log(reference.Asset.name + " " + loader.Status.ToString() + " " + time_taken.ToString());
                    } else {
                        Log(reference.ToString() + " " + loader.Status.ToString() + " " + time_taken.ToString());
                    }
                } else {
                    Log(reference.ToString() + " Failed");
                }
            }
        }
    }

    private IEnumerator StartDownload(string key) {

        ClearLog();
        Log("Fetching download size");

       

        AsyncOperationHandle<long> downloadSize = Addressables.GetDownloadSizeAsync(key);
        yield return downloadSize;
        Log("Download size : " + downloadSize.Result);
        float time = Time.realtimeSinceStartup;
         if(downloadSize.Result > 0) {
             Log("Starting download..");
             AsyncOperationHandle handle = Addressables.DownloadDependenciesAsync(key);
             while(!handle.IsDone) {
                 var status = handle.GetDownloadStatus();
                 Debug.Log(status.Percent.ToString());
                 yield return null;
             }

             float time_taken = Time.realtimeSinceStartup - time;
             Log("Download complete:" + time_taken);
         }
        

    }

    public void ClearCache(string key) {
        ClearLog();
        if(Caching.ClearCache()) {
            Log("Cache cleared");
        } else {
            Log("Cache is being used");
        }

        Addressables.ClearDependencyCacheAsync(key);
        Log("Addressables Cache cleared");
    }

    private void Log(string text, bool clear_log = false) {
        if(clear_log) {
            ClearLog();
        }
        logs_text.text += text + "\n";
    }
    private void ClearLog() {
        logs_text.text = "";
    }

    
}
