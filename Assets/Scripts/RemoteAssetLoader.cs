using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class RemoteAssetLoader : MonoBehaviour {
    public Logger logger;
    public Button downlaodBtn, clearLog, checkForUpdate;
    public Image loader;

    private void Start() {
        downlaodBtn.onClick.AddListener(delegate () {
            StartCoroutine(StartDownload("Remote"));
        });
        clearLog.onClick.AddListener(delegate () {
            ClearLog();
        });
        checkForUpdate.onClick.AddListener(delegate () {
            CheckForUpdate();
        });
    }

    private void OnEnable() {
        loader.fillAmount = 0;
        ClearLog();
    }

    private void CheckForUpdate() {
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

    private IEnumerator StartDownload(string key) {
        //Log("Clearing Cache");
        //Addressables.ClearDependencyCacheAsync(key);
        loader.fillAmount = 0;
        Log("Fetching download size");

        AsyncOperationHandle<long> downloadSize = Addressables.GetDownloadSizeAsync(key);
        yield return downloadSize;

        Log("Download size : " + downloadSize.Result);
        Log("Starting download..");
        if(downloadSize.Result > 0) {
            AsyncOperationHandle handle = Addressables.DownloadDependenciesAsync(key);
            while(!handle.IsDone) {
                var status = handle.GetDownloadStatus();
                loader.fillAmount = status.Percent;
                yield return null;
            }
            Log("Download complete");
        }
    }

    private void Log(string text) {
        logger.Log(text);
    }
    private void ClearLog() {
        logger.ClearLog();
    }
}
