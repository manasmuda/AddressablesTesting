using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public class AddressableAssetManager
{
    public static bool IsLoaded(AssetReference aRef) {
        return aRef.IsValid() && aRef.Asset != null;
    }

    public static bool IsLoading(AssetReference aRef) {
        return aRef.IsValid() && aRef.OperationHandle.Status == AsyncOperationStatus.None && aRef.Asset == null;
    }

    public static bool TryGetOrLoadObjectAsync<TObjectType>(AssetReference aRef, out AsyncOperationHandle<TObjectType> handle) where TObjectType : Object {
        if(aRef == null || !aRef.RuntimeKeyIsValid()) {
            handle = Addressables.ResourceManager.CreateCompletedOperation(default(TObjectType), "Invalid Runtime key");
            return false;
        }
        if(IsLoaded(aRef)) {
            try {
                handle = aRef.OperationHandle.Convert<TObjectType>();
                Debug.Log("Asset Already available");
            } catch {
                handle = Addressables.ResourceManager.CreateCompletedOperation(default(TObjectType), "Loaded Asset Type and Requested asset type do not match");
            }

            return false;
        }


        if(IsLoading(aRef)) {
            try {
                handle = aRef.OperationHandle.Convert<TObjectType>();
                Debug.Log("Asset is loading");
            } catch {
                handle = handle = Addressables.ResourceManager.CreateCompletedOperation(default(TObjectType), "Loaded Asset Type and Requested asset type do not match");
            }
            return false;
        }


        handle = aRef.LoadAssetAsync<TObjectType>();

        return true;
    }

    public static void TryGetOrLoadObjectAsync<TObjectType>(AssetReference aRef, System.Action<TObjectType> action) where TObjectType : UnityEngine.Object {
        TObjectType asset = default(TObjectType);
        if(aRef == null || !aRef.RuntimeKeyIsValid()) {
            Debug.LogError("Invalid run time key");
            return;
        }
        if(IsLoaded(aRef)) {
            try {
                asset = (TObjectType)aRef.Asset;
            } catch {
                Debug.LogError("Loaded Asset Type and Requested asset type do not match");
            }
        } else if(IsLoading(aRef)) {
            try {
                aRef.OperationHandle.Convert<TObjectType>().Completed += op => {
                    if(op.Result != null) {
                        action.Invoke(op.Result);
                    }
                };
            } catch {
                Debug.LogError("Loaded Asset Type and Requested asset type do not match");
            }
        } else {
            AsyncOperationHandle<TObjectType> handle = aRef.LoadAssetAsync<TObjectType>();

            handle.Completed += op2 => {
                if(op2.Result != null) {
                    action.Invoke(op2.Result);
                }
            };
        }
    }

    public static async Task<TObjectType> TryGetOrLoadObjectAsync<TObjectType>(AssetReference aRef) where TObjectType : UnityEngine.Object {
        TObjectType asset = default(TObjectType);
        if(aRef == null || !aRef.RuntimeKeyIsValid()) {
            Debug.LogError("Invalid run time key");
            return asset;
        }
        if(IsLoaded(aRef)) {
            try {
                asset = (TObjectType)aRef.Asset;
            } catch {
                Debug.LogError("Loaded Asset Type and Requested asset type do not match");
            }
        } else if(IsLoading(aRef)) {
            try {
                await aRef.OperationHandle.Convert<TObjectType>().Task;
                if(aRef.Asset != null) {
                    asset = (TObjectType)aRef.Asset;
                }
            } catch {
                Debug.LogError("Loaded Asset Type and Requested asset type do not match");
            }
        } else {
            AsyncOperationHandle<TObjectType> handle = aRef.LoadAssetAsync<TObjectType>();
            await handle.Task;
            if(aRef.Asset != null) {
                asset = (TObjectType)aRef.Asset;
            } else {
                Debug.LogError("Failed Loading " + aRef);
            }
        }
        return asset;
    }

    public static async void TryLoadAndInstatiate(AssetReferenceGameObject assetReference, Transform parent, string name) {
        GameObject loaded_prefab = await TryGetOrLoadObjectAsync<GameObject>(assetReference);
        GameObject created_object = GameObject.Instantiate(loaded_prefab, parent);
        created_object.name = name;
    }

    public static void Unload(AssetReference aRef) {
        if(IsLoaded(aRef) || IsLoading(aRef)) {
            aRef.ReleaseAsset();
        } else {
            Debug.LogError("Asset is not loaded or loading");
        }
    }
}
