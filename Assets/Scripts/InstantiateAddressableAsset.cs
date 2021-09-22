using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class InstantiateAddressableAsset : MonoBehaviour {
    public Logger logger;
    public AssetReference reference;

    public void OnEnable() {

    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.Escape)) {
            InstantiateThis();
        }
    }

    private void InstantiateThis() {
        Addressables.InstantiateAsync(reference, transform.parent).Completed += (handle) => {
            if(handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Failed) {
                logger.Log("Instantiate Failed");
            }
            if(!handle.IsValid()) {
                logger.Log("Instantiate invalid");
            } else {
                logger.Log("Instantiate complete");
            }
            logger.Log("Instantiate process complete");
        };
    }
}
