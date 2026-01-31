using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Zenject;
using Object = UnityEngine.Object;

public static class AddressableAssetManager
{
    public static UniTaskCompletionSource LoadCatalogTaskSource;

    public static async UniTask<bool> IsDownloaded(AssetReference aRef)
    {
        return (await GetDownloadSize(aRef)) == 0;
    }

    public static async UniTask<long> GetDownloadSize(AssetReference aRef)
    {
        return await Addressables.GetDownloadSizeAsync(aRef);
    }

    public static bool IsLoaded(AssetReference aRef)
    {
        return aRef.IsValid() && aRef.Asset != null;
    }

    public static bool IsLoading(AssetReference aRef)
    {
        return aRef.IsValid() && aRef.OperationHandle.Status == AsyncOperationStatus.None && aRef.Asset == null;
    }

    public static bool IsLoaded(AssetAddressReference aRef)
    {
        return aRef.IsValid() && aRef.Asset != null;
    }

    public static bool IsLoading(AssetAddressReference aRef)
    {
        return aRef.IsValid() && aRef.OperationHandle.Status == AsyncOperationStatus.None && aRef.Asset == null;
    }


    public static bool TryGetOrLoadObjectAsync<TObjectType>(AssetReference aRef,
        out AsyncOperationHandle<TObjectType> handle) where TObjectType : Object
    {
        if (aRef == null || !aRef.RuntimeKeyIsValid())
        {
            handle = Addressables.ResourceManager.CreateCompletedOperation(default(TObjectType), "Invalid Runtime key");
            return false;
        }

        if (IsLoaded(aRef))
        {
            try
            {
                handle = aRef.OperationHandle.Convert<TObjectType>();
                Debug.Log("Asset Already available");
            }
            catch
            {
                handle = Addressables.ResourceManager.CreateCompletedOperation(default(TObjectType),
                    "Loaded Asset Type and Requested asset type do not match");
            }

            return false;
        }


        if (IsLoading(aRef))
        {
            try
            {
                handle = aRef.OperationHandle.Convert<TObjectType>();
                Debug.Log("Asset is loading");
            }
            catch
            {
                handle = handle = Addressables.ResourceManager.CreateCompletedOperation(default(TObjectType),
                    "Loaded Asset Type and Requested asset type do not match");
            }

            return false;
        }


        handle = aRef.LoadAssetAsync<TObjectType>();

        return true;
    }

    public static void TryGetOrLoadObjectAsync<TObjectType>(AssetReference aRef, System.Action<TObjectType> action)
        where TObjectType : UnityEngine.Object
    {
        TObjectType asset = default(TObjectType);
        if (aRef == null || !aRef.RuntimeKeyIsValid())
        {
            Debug.LogError("Invalid run time key");
            return;
        }

        if (IsLoaded(aRef))
        {
            try
            {
                asset = (TObjectType)aRef.Asset;
            }
            catch
            {
                Debug.LogError("Loaded Asset Type and Requested asset type do not match");
            }
        }
        else if (IsLoading(aRef))
        {
            try
            {
                aRef.OperationHandle.Convert<TObjectType>().Completed += op =>
                {
                    if (op.Result != null)
                    {
                        action.Invoke(op.Result);
                    }
                };
            }
            catch
            {
                Debug.LogError("Loaded Asset Type and Requested asset type do not match");
            }
        }
        else
        {
            AsyncOperationHandle<TObjectType> handle = aRef.LoadAssetAsync<TObjectType>();

            handle.Completed += op2 =>
            {
                if (op2.Result != null)
                {
                    action.Invoke(op2.Result);
                }
            };
        }
    }

    public static async UniTask<TObjectType> TryGetOrLoadObjectAsync<TObjectType>(AssetReference aRef)
        where TObjectType : UnityEngine.Object
    {
        TObjectType asset = default(TObjectType);
        if (aRef == null || !aRef.RuntimeKeyIsValid())
        {
            Debug.LogError("Invalid run time key");
            return asset;
        }

        if (IsLoaded(aRef))
        {
            try
            {
                asset = (TObjectType)aRef.Asset;
            }
            catch
            {
                Debug.LogError("Loaded Asset Type and Requested asset type do not match");
            }
        }
        else if (IsLoading(aRef))
        {
            try
            {
                await aRef.OperationHandle.Convert<TObjectType>().Task;
                if (aRef.Asset != null)
                {
                    asset = (TObjectType)aRef.Asset;
                }
            }
            catch
            {
                Debug.LogError("Loaded Asset Type and Requested asset type do not match");
            }
        }
        else
        {
            AsyncOperationHandle<TObjectType> handle = aRef.LoadAssetAsync<TObjectType>();
            try
            {
                await handle;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            if (aRef.Asset != null)
            {
                asset = (TObjectType)aRef.Asset;
            }
            else
            {
                Debug.LogError("Failed Loading " + aRef);
            }
        }

        return asset;
    }

    public static async UniTask<TObjectType> TryGetOrLoadObjectAsync<TObjectType>(
        this AssetAddressReferenceT<TObjectType> aRef) where TObjectType : Object
    {
        TObjectType asset = default(TObjectType);
        if (aRef == null || !aRef.RuntimeKeyIsValid())
        {
            Debug.LogError("Invalid run time key");
            return asset;
        }

        if (aRef.IsLoaded())
        {
            try
            {
                asset = (TObjectType)aRef.Asset;
            }
            catch
            {
                Debug.LogError("Loaded Asset Type and Requested asset type do not match");
            }
        }
        else if (aRef.IsLoading())
        {
            try
            {
                await aRef.OperationHandle.Task;
                if (aRef.Asset != null)
                {
                    asset = (TObjectType)aRef.Asset;
                }
            }
            catch
            {
                Debug.LogError("Loaded Asset Type and Requested asset type do not match");
            }
        }
        else
        {
            try
            {
                AsyncOperationHandle<TObjectType> handle = aRef.LoadAssetAsync();
                await handle;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (aRef.Asset != null)
            {
                asset = (TObjectType)aRef.Asset;
            }
            else
            {
                Debug.LogError("Failed Loading " + aRef);
            }
        }

        return asset;
    }


    public static async UniTask<List<TObjectType>> TryGetOrLoadObjectsAsync<TObjectType>(
        List<AssetReferenceT<TObjectType>> aRef) where TObjectType : UnityEngine.Object
    {
        List<TObjectType> objectsList = new List<TObjectType>();
        for (int i = 0; i < aRef.Count; i++)
        {
            TObjectType loaded_object = await TryGetOrLoadObjectAsync<TObjectType>(aRef[i]);
            objectsList.Add(loaded_object);
        }

        return objectsList;
    }

    public static async UniTask<TObjectType> TryGetOrLoadObjectAsync<TObjectType>(AssetAddressReference aRef)
        where TObjectType : UnityEngine.Object
    {
        TObjectType asset = default(TObjectType);
        if (aRef == null || !aRef.RuntimeKeyIsValid())
        {
            Debug.LogError("Invalid run time key");
            return asset;
        }

        if (IsLoaded(aRef))
        {
            try
            {
                asset = (TObjectType)aRef.Asset;
            }
            catch
            {
                Debug.LogError("Loaded Asset Type and Requested asset type do not match");
            }
        }
        else if (IsLoading(aRef))
        {
            try
            {
                await aRef.OperationHandle.Convert<TObjectType>().Task;
                if (aRef.Asset != null)
                {
                    asset = (TObjectType)aRef.Asset;
                }
            }
            catch
            {
                Debug.LogError("Loaded Asset Type and Requested asset type do not match");
            }
        }
        else
        {
            AsyncOperationHandle<TObjectType> handle = aRef.LoadAssetAsync<TObjectType>();
            //aRef.ShowResourceLocations();
            await handle.Task;
            if (aRef.Asset != null)
            {
                asset = (TObjectType)aRef.Asset;
            }
            else
            {
                Debug.LogError("Failed Loading " + aRef);
            }
        }

        return asset;
    }

    public static async UniTask<TObjectType> TryGetOrLoadObjectAsync<TObjectType>(AssetAddressReference aRef,
        IResourceLocation resourceLocation) where TObjectType : UnityEngine.Object
    {
        TObjectType asset = default(TObjectType);
        if (aRef == null || !aRef.RuntimeKeyIsValid())
        {
            Debug.LogError("Invalid run time key");
            return asset;
        }

        if (IsLoaded(aRef))
        {
            try
            {
                asset = (TObjectType)aRef.Asset;
            }
            catch
            {
                Debug.LogError("Loaded Asset Type and Requested asset type do not match");
            }
        }
        else if (IsLoading(aRef))
        {
            try
            {
                await aRef.OperationHandle.Convert<TObjectType>().Task;
                if (aRef.Asset != null)
                {
                    asset = (TObjectType)aRef.Asset;
                }
            }
            catch
            {
                Debug.LogError("Loaded Asset Type and Requested asset type do not match");
            }
        }
        else
        {
            AsyncOperationHandle<TObjectType> handle = aRef.LoadAssetAsync<TObjectType>(resourceLocation);
            //aRef.ShowResourceLocations();
            await handle.Task;
            if (aRef.Asset != null)
            {
                asset = (TObjectType)aRef.Asset;
            }
            else
            {
                Debug.LogError("Failed Loading " + aRef);
            }
        }

        return asset;
    }

    public static async void TryLoadAndInstatiate(AssetReferenceGameObject assetReference, Transform parent,
        System.Action<GameObject> callBack = null)
    {
        GameObject loaded_prefab = await TryGetOrLoadObjectAsync<GameObject>(assetReference);
        if (loaded_prefab != null)
        {
            GameObject instantiated_object = GameObject.Instantiate(loaded_prefab, parent);
            callBack?.Invoke(instantiated_object);
            AddNotifyOnDestroy(assetReference, instantiated_object);
        }
    }

    public static async UniTask<GameObject> TryLoadAndInstantiateGameObject(AssetReferenceGameObject assetReference,
        Transform parent)
    {
        GameObject loaded_prefab = await TryGetOrLoadObjectAsync<GameObject>(assetReference);
        if (loaded_prefab != null)
        {
            GameObject instantiated_object = GameObject.Instantiate(loaded_prefab, parent);
            AddNotifyOnDestroy(assetReference, instantiated_object);
            return instantiated_object;
        }

        return null;
    }

    public static async UniTask<GameObject> TryLoadAndInstantiateGameObject(AssetReferenceGameObject assetReference)
    {
        GameObject loaded_prefab = await TryGetOrLoadObjectAsync<GameObject>(assetReference);
        if (loaded_prefab != null)
        {
            GameObject instantiated_object = GameObject.Instantiate(loaded_prefab);
            AddNotifyOnDestroy(assetReference, instantiated_object);
            return instantiated_object;
        }

        return null;
    }

    public static async UniTask<GameObject> TryLoadAndInstantiateGameObject(AssetReferenceGameObject assetReference,
        Transform parent, DiContainer container)
    {
        GameObject loaded_prefab = await TryGetOrLoadObjectAsync<GameObject>(assetReference);
        if (loaded_prefab != null)
        {
            GameObject instantiated_object = container.InstantiatePrefab(loaded_prefab, parent);
            AddNotifyOnDestroy(assetReference, instantiated_object);
            return instantiated_object;
        }

        return null;
    }

    public static async UniTask<T> TryLoadAndInstantiateGameObject<T>(GameObjectComponentReference<T> assetReference,
        Transform parent, DiContainer container) where T : Component
    {
        GameObject loadedPrefab =
            await TryLoadAndInstantiateGameObject((AssetReferenceGameObject)assetReference, parent, container);
        if (loadedPrefab != null)
        {
            T component = loadedPrefab.GetComponent<T>();
            assetReference.SetComponent(component);
            return component;
        }

        return default;
    }

    public static async UniTask<GameObject> TryLoadAndInstantiateGameObject(AssetReferenceGameObject assetReference,
        DiContainer container)
    {
        GameObject loaded_prefab = await TryGetOrLoadObjectAsync<GameObject>(assetReference);
        if (loaded_prefab != null)
        {
            GameObject instantiated_object = container.InstantiatePrefab(loaded_prefab);
            AddNotifyOnDestroy(assetReference, instantiated_object);
            return instantiated_object;
        }

        return null;
    }


    public static async UniTask<TObjectType> LoadLatestObjectAsync<TObjectType>(AssetAddressReference aRef)
        where TObjectType : UnityEngine.Object
    {
        TObjectType asset = default(TObjectType);
        if (aRef == null || !aRef.RuntimeKeyIsValid())
        {
            Debug.LogError("Invalid run time key");
            return asset;
        }

        IResourceLocation upgradedResourceLocation = await GetUpgradedLocation(aRef);
        if (upgradedResourceLocation != null && !string.IsNullOrEmpty(upgradedResourceLocation.PrimaryKey))
        {
            Debug.Log("Load Upgraded Asset");
            return await TryGetOrLoadObjectAsync<TObjectType>(aRef, upgradedResourceLocation);
        }
        else
        {
            Debug.Log("Load Local Catalog Asset");
            return await TryGetOrLoadObjectAsync<TObjectType>(aRef);
        }
    }

    private static async UniTask<IResourceLocation> GetUpgradedLocation(AssetAddressReference aRef)
    {
        IResourceLocation upgradedResourceLocation = default(IResourceLocation);
        AsyncOperationHandle<IList<IResourceLocation>> resourceLocationHandler = aRef.GetUpgradedResourceLocations();
        if (resourceLocationHandler.IsValid())
        {
            IList<IResourceLocation> resourceLocations = await resourceLocationHandler.Task;
            if (resourceLocations != null && resourceLocations.Count > 0)
            {
                upgradedResourceLocation = resourceLocations[0];
            }
        }

        return upgradedResourceLocation;
    }

    private static void AddNotifyOnDestroy(AssetReference assetReference, GameObject gameObject)
    {
        NotifyOnDestroy notifyOnDestroy = gameObject.GetComponent<NotifyOnDestroy>();
        if (notifyOnDestroy == null)
        {
            notifyOnDestroy = (NotifyOnDestroy)gameObject.AddComponent(typeof(NotifyOnDestroy));
            notifyOnDestroy.Destroyed += Unload;
        }

        notifyOnDestroy.AssetReference = assetReference;
    }

    public static void Unload(this AssetReference aRef, NotifyOnDestroy notifyOnDestroy = null)
    {
        if (aRef != null)
        {
            if (IsLoaded(aRef) || IsLoading(aRef) || aRef.IsValid())
            {
                aRef.ReleaseAsset();
            }
            else
            {
                Debug.LogWarning("Asset is not loaded or loading");
            }
        }
    }

    public static void Unload<TObject>(this AssetAddressReferenceT<TObject> aRef,
        NotifyOnDestroy notifyOnDestroy = null) where TObject : Object
    {
        if (aRef != null)
        {
            if (aRef.IsLoaded() || aRef.IsLoading() || aRef.IsValid())
            {
                aRef.ReleaseAsset();
            }
            else
            {
                Debug.LogWarning("Asset is not loaded or loading");
            }
        }
    }
}
