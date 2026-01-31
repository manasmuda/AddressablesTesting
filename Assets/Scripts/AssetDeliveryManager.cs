using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using Zenject;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;

public class AssetDeliveryManager : IInitializable {

    private AsyncOperationHandle<long> download_size_handle;
    private AsyncOperationHandle download_handle;

    private long current_download_size = 0;
    private int retries = 0;
    private const int MAX_RETRIES = 5;

    private const string download_key = "TestLabel";

    public void Initialize() {
        UpdateCatalogAndDoDownload();
    }

    private async void UpdateCatalogAndDoDownload() {
        AsyncOperationHandle<IResourceLocator> catalog_load_handle =  Addressables.LoadContentCatalogAsync("https://s3.ap-south-1.amazonaws.com/superstars.assetbundles.testbuild/SP_Test/Android/catalog_master.json","_SP");
        await catalog_load_handle.Task;
        //Debug.LogError(catalog_load_handle.Status);
        //Debug.LogError(catalog_load_handle.Result);
        //catalog_load_handle.Result.Keys.ToList().ForEach(x => Debug.LogError(x));
        FetchSizeAndStartDownload();
    }

    private void FetchSizeAndStartDownload() {
        FetchDownloadSize();
        CheckAndStartDownload();
    }

    private async void FetchDownloadSize() {
        download_size_handle = Addressables.GetDownloadSizeAsync(download_key);
        await download_size_handle.Task;
        Debug.Log("Download Size:"+download_size_handle.Result);
    }

    /// <summary>
    /// Returns download size in bytes
    /// </summary>
    /// <returns></returns>
    public async Task<long> GetDowloadSize() {
        if(download_size_handle.IsValid()) {
            await download_size_handle.Task;
            return download_size_handle.Result;
        }
        return 0;
    }

    public async Task<bool> IsDownloadAvailable() {
        long download_size = await GetDowloadSize();
        if(download_size > 0) {
            return true;
        } else {
            return false;
        }
    }

    /// <summary>
    /// Starts Download Addressable dependencies
    /// </summary>
    public async void CheckAndStartDownload() {
        current_download_size = await GetDowloadSize();
        foreach(UnityEngine.AddressableAssets.ResourceLocators.IResourceLocator l in Addressables.ResourceLocators) {
            Debug.LogError(l);
            IList<IResourceLocation> locs;
            Debug.LogError(l.Locate(download_key, null, out locs));
        }
        if(current_download_size > 0) {
            download_handle = Addressables.DownloadDependenciesAsync(download_key); // Removed auto release handler look into it later
            download_handle.Completed += OnDownloadComplete;
        }
    }

    private void OnDownloadComplete(AsyncOperationHandle op) {
        Debug.Log("Download Completed");
        if(download_handle.Status == AsyncOperationStatus.Succeeded) {
            ResetDownloadSize();
            Debug.Log("Download Success");
        } else {
            RestartDownload();
            Debug.Log("Download Failed");
        }
        foreach(UnityEngine.AddressableAssets.ResourceLocators.IResourceLocator l in Addressables.ResourceLocators) {
            Debug.LogError(l);
            IList<IResourceLocation> locs;
            Debug.LogError(l.Locate(download_key, null, out locs));
        }
    }

    public bool IsDownloading() {
        return download_handle.IsValid() && !download_handle.IsDone;
    }

    public float GetProgress() {
        if(download_handle.IsValid()) {
            return download_handle.GetDownloadStatus().Percent;
        } else {
            return 0;
        }
    }

    public IEnumerator WaitForDownload() {
        if(download_handle.IsValid()) {
            yield return download_handle;
            yield return new WaitForSeconds(0.05f);
        } else {
            yield return null;
        }
    }

    public async Task<bool> WaitForDownloadTask() {
        if(download_handle.IsValid()) {
            await download_handle.Task;
            if(download_handle.Status == AsyncOperationStatus.Succeeded) {
                return true;
            } else {
                return false;
            }
        } else {
            return false;
        }
    }

    public void SetDownloadCompleteListener(Action onComplete) {
        if(download_handle.IsValid()) {
            download_handle.Completed += (op) => {
                onComplete?.Invoke();
            };
        } else {
            onComplete?.Invoke();
        }
    }

    public long GetDownloadCompletedSize() {
        if(current_download_size > 0) {
            return  (long)(current_download_size * (GetProgress()));
        }
        return 0;
    }

    public long GetTotalDownloadSize() {
        return current_download_size;
    }

    private void ResetDownloadSize() {
        current_download_size = 0;
        FetchDownloadSize();
    }

    private void RestartDownload() {
        FetchDownloadSize();
        if(retries < MAX_RETRIES) {
            CheckAndStartDownload();
        }
    }

}
